using DesktopRFID.Data;
using DesktopRFID.Data.Helpers;
using DesktopRFID.Data.Interfaces;
using DesktopRFID.Infrastructure.Logging;
using System.Text;

namespace DesktopRFID.Infrastructure.Adapters.RFID
{
    public sealed class RwDevReader : IRfidReader, IDisposable, ITidCapableReader
    {
        private bool _connected;
        private byte _comAddr;
        private int _portHandle;
        public bool IsConnected => _connected;

        private readonly IFileLogger _logger;
        public RwDevReader(IFileLogger logger) => _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        public RwDevReader() : this(FileLogger.Default) { }
        public async Task ConnectSerialAsync(int comPort, byte baudIndex = 5, byte comAddr = 0x00)
        {
            try
            {
                _logger.Info($"[ConnectSerialAsync] Açılıyor: COM{comPort}, baudIdx={baudIndex}, addr=0x{comAddr:X2}");
                var started = DateTime.UtcNow;

                await Task.Run(() =>
                {
                    _comAddr = comAddr;
                    int rc = RWDev.OpenComPort(comPort, ref _comAddr, baudIndex, ref _portHandle);
                    if (rc != 0) throw new InvalidOperationException($"RFID Cihazına bağlanamadı.(OpenComPort rc={rc})");

                    RWDev.SetAntennaMultiplexing(ref _comAddr, 0x00, _portHandle);
                    RWDev.SetInventoryScanTime(ref _comAddr, 0x0A, _portHandle);
                    _connected = true;
                });

                _logger.Info($"[ConnectSerialAsync] Bağlandı: handle={_portHandle}, süre={(DateTime.UtcNow - started).TotalMilliseconds:F0} ms");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "[ConnectSerialAsync] Hata");
                throw;
            }
        }
        public Task DisconnectAsync()
        {
            return Task.Run(() =>
            {
                try
                {
                    _logger.Info("[DisconnectAsync] Kapanıyor...");
                    RWDev.CloseComPort();
                    _connected = false;
                    _logger.Info("[DisconnectAsync] Kapandı");
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "[DisconnectAsync] Hata");
                    throw;
                }
            });
        }
        public Task<IReadOnlyList<TagRecord>> InventoryOnceAsync()
        {
            return Task.Run<IReadOnlyList<TagRecord>>(() =>
            {
                try
                {
                    if (!IsConnected) throw new InvalidOperationException("Reader bağlı değil.");
                    var t0 = DateTime.UtcNow;

                    var list = TryInventory() ?? TryFastInventory() ?? new List<TagRecord>();
                    var uniq = new Dictionary<string, TagRecord>(StringComparer.OrdinalIgnoreCase);
                    foreach (var t in list)
                        if (!uniq.ContainsKey(t.EPCHex)) uniq[t.EPCHex] = t;

                    var result = uniq.Values.OrderBy(x => x.EPCHex, StringComparer.OrdinalIgnoreCase).ToList();
                    _logger.Info($"[InventoryOnceAsync] Tag sayısı: raw={list.Count}, uniq={result.Count}, süre={(DateTime.UtcNow - t0).TotalMilliseconds:F0} ms");
                    return result;
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "[InventoryOnceAsync] Hata");
                    throw;
                }
            });
        }
        public Task<byte[]?> ReadTidShortAsync(string epcHex)
        {
            return Task.Run(() =>
            {
                try
                {
                    if (!IsConnected) throw new InvalidOperationException("Reader bağlı değil.");
                    _logger.Info($"[ReadTidShortAsync] TID okunuyor, EPC={epcHex}");
                    var epc = EpcCodec.HexToBytes(epcHex);
                    byte eNum = (byte)(epc.Length / 2);
                    byte mem = 0x02;
                    byte wordPtr = 0x00;
                    byte num = 0x04;
                    var outBytes = new byte[num * 2];
                    byte[] pwd = new byte[] { 0, 0, 0, 0 };
                    byte maskMem = 0x00;
                    byte[] maskAdr = new byte[2];
                    byte maskLen = 0x00;
                    byte[] maskData = new byte[256];
                    int err = 0;

                    int rc = RWDev.ReadData_G2(ref _comAddr, epc, eNum, mem, wordPtr, num,
                                               pwd, maskMem, maskAdr, maskLen, maskData,
                                               outBytes, ref err, _portHandle);
                    if (rc == 0 || rc == 1)
                    {
                        _logger.Info("[ReadTidShortAsync] Başarılı");
                        return outBytes;
                    }
                    _logger.Warn($"[ReadTidShortAsync] Başarısız rc={rc}, err={err}");
                    return null;
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "[ReadTidShortAsync] Hata");
                    throw;
                }
            });
        }
        public async Task<string?> ReadTidHexAsync(string epcHex)
        {
            try
            {
                var b = await ReadTidShortAsync(epcHex);
                var hex = b == null ? null : BitConverter.ToString(b).Replace("-", "");
                _logger.Info($"Tag Tarama Bitti . [ReadTidHexAsync] EPC={epcHex}, TID={hex ?? "null"}");
                return hex;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "[ReadTidHexAsync] Hata");
                throw;
            }
        }
        public Task<bool> WriteEpcAsync(string currentEpcHex, byte[] newEpcPayload, byte[] accessPwd4B)
        {
            return Task.Run(() =>
            {
                try
                {
                    if (!IsConnected) throw new InvalidOperationException("Reader bağlı değil.");
                    _logger.Info($"[WriteEpcAsync] Yazılıyor: currEPC={currentEpcHex}, newWords={newEpcPayload.Length / 2}");

                    {
                        byte eNum = (byte)(newEpcPayload.Length / 2);
                        int err = 0;
                        int rc = RWDev.WriteEPC_G2(ref _comAddr, accessPwd4B, newEpcPayload, eNum, ref err, _portHandle);
                        _logger.Info($"[WriteEpcAsync] WriteEPC_G2 rc={rc}, err={err}");
                        if (rc == 0 || rc == 1) return true;
                    }

                    {
                        byte[] sel = EpcCodec.HexToBytes(currentEpcHex);
                        byte eNum = (byte)(sel.Length / 2);
                        byte mem = 0x01;
                        byte wNum = (byte)(newEpcPayload.Length / 2);
                        byte[] wordPtrArr = new byte[2] { 0x00, 0x02 };
                        byte maskMem = 0x00; byte[] maskAdr = new byte[2]; byte maskLen = 0x00; byte[] maskData = new byte[256];
                        int error = 0;

                        int rc = RWDev.ExtWriteData_G2(ref _comAddr, sel, wNum, eNum, mem, wordPtrArr,
                                                       newEpcPayload, accessPwd4B,
                                                       maskMem, maskAdr, maskLen, maskData, ref error, _portHandle);
                        _logger.Info($"[WriteEpcAsync] ExtWriteData_G2 rc={rc}, err={error}");
                        if (rc == 0 || rc == 1) return true;
                    }

                    {
                        byte[] sel = EpcCodec.HexToBytes(currentEpcHex);
                        byte eNum = (byte)(sel.Length / 2);
                        byte mem = 0x01;
                        byte wNum = (byte)(newEpcPayload.Length / 2);
                        byte wordPtr = 0x02;
                        byte maskMem = 0x00; byte[] maskAdr = new byte[2]; byte maskLen = 0x00; byte[] maskData = new byte[256];
                        int error = 0;

                        int rc = RWDev.BlockWrite_G2(ref _comAddr, sel, wNum, eNum, mem, wordPtr,
                                                     newEpcPayload, accessPwd4B,
                                                     maskMem, maskAdr, maskLen, maskData, ref error, _portHandle);
                        _logger.Info($"[WriteEpcAsync] BlockWrite_G2 rc={rc}, err={error}");
                        if (rc == 0 || rc == 1) return true;
                    }

                    _logger.Warn("[WriteEpcAsync] Tüm yazma denemeleri başarısız");
                    return false;
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "[WriteEpcAsync] Hata");
                    throw;
                }
            });
        }
        public async Task SetTxPowerDbmAsync(byte dbm)
        {
            if (!IsConnected) throw new InvalidOperationException("Reader bağlı değil.");
            if (dbm < 0 || dbm > 30) throw new ArgumentOutOfRangeException(nameof(dbm), "0–30 dBm aralığında olmalı.");

            try
            {
                _logger.Info($"[SetTxPowerDbmAsync] Ayarlanıyor: {dbm} dBm");
                await Task.Run(() =>
                {
                    byte addr = _comAddr;
                    int rc = RWDev.SetRfPower(ref addr, dbm, _portHandle);
                    if (rc != 0 && rc != 1)
                        throw new Exception($"SetRfPower başarısız (rc={rc}).");
                    _comAddr = addr;
                });
                _logger.Info("[SetTxPowerDbmAsync] Başarılı");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "[SetTxPowerDbmAsync] Hata");
                throw;
            }
        }
        public async Task<byte> GetTxPowerDbmAsync()
        {
            if (!IsConnected) throw new InvalidOperationException("Reader bağlı değil.");
            try
            {
                _logger.Info("[GetTxPowerDbmAsync] Okunuyor...");
                var power = await Task.Run(() =>
                {
                    byte addr = _comAddr;
                    byte[] ver = new byte[2];
                    byte readerType = 0, trType = 0, dmax = 0, dmin = 0, power = 0,
                         scanTime = 0, ant = 0, beep = 0, outRep = 0, checkAnt = 0;
                    int rc = RWDev.GetReaderInformation(ref addr, ver, ref readerType, ref trType,
                                                        ref dmax, ref dmin, ref power, ref scanTime,
                                                        ref ant, ref beep, ref outRep, ref checkAnt, _portHandle);
                    if (rc != 0 && rc != 1) throw new Exception($"GetReaderInformation rc={rc}");
                    _comAddr = addr;
                    return power;
                });
                _logger.Info($"[GetTxPowerDbmAsync] Güç={power} dBm");
                return power;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "[GetTxPowerDbmAsync] Hata");
                throw;
            }
        }
        public async Task SetScanTimeAsync(byte scanTimeHex)
        {
            if (!IsConnected) throw new InvalidOperationException("Reader bağlı değil.");
            try
            {
                _logger.Info($"[SetScanTimeAsync] Ayarlanıyor: 0x{scanTimeHex:X2}");
                await Task.Run(() =>
                {
                    byte addr = _comAddr;
                    int rc = RWDev.SetInventoryScanTime(ref addr, scanTimeHex, _portHandle);
                    if (rc != 0 && rc != 1)
                        throw new Exception($"SetInventoryScanTime rc={rc}");
                    _comAddr = addr;
                });
                _logger.Info("[SetScanTimeAsync] Başarılı");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "[SetScanTimeAsync] Hata");
                throw;
            }
        }
        private List<TagRecord>? TryInventory()
        {
            try
            {
                var EPCList = new byte[8192];
                var MaskAdr = new byte[2];
                var MaskData = new byte[256];
                byte ant = 0; int totalLen = 0; int cardNum = 0;

                int r = RWDev.Inventory_G2(
                    ref _comAddr,
                    4, 0, 0, MaskAdr, 0, MaskData, 0,
                    0, 0, 0, 0, 0, 0x0A, 1,
                    EPCList, ref ant, ref totalLen, ref cardNum, _portHandle);

                if (r != 1 || totalLen <= 0 || cardNum <= 0)
                {
                    _logger.Warn($"[TryInventory] rc={r}, totalLen={totalLen}, cardNum={cardNum} (başarısız)");
                    return null;
                }

                var parsed = ParseLenPlusEpc(EPCList, totalLen);
                _logger.Info($"[TryInventory] OK: cardNum={cardNum}, parsed={parsed.Count}");
                return parsed;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "[TryInventory] Hata");
                return null;
            }
        }
        private List<TagRecord>? TryFastInventory()
        {
            try
            {
                var EPCList = new byte[8192];
                byte ant = 0; int totalLen = 0; int cardNum = 0;

                int r = RWDev.Fast_EPC_Inventory_G2(
                    ref _comAddr, 4, 0, 0, 0x0A,
                    ref ant, EPCList, ref totalLen, ref cardNum, _portHandle);

                if (r != 1 || totalLen <= 0 || cardNum <= 0)
                {
                    _logger.Warn($"[TryFastInventory] rc={r}, totalLen={totalLen}, cardNum={cardNum} (başarısız)");
                    return null;
                }

                var parsed = ParseLenPlusEpc(EPCList, totalLen);
                _logger.Info($"[TryFastInventory] OK: cardNum={cardNum}, parsed={parsed.Count}");
                return parsed;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "[TryFastInventory] Hata");
                return null;
            }
        }
        private static List<TagRecord> ParseLenPlusEpc(byte[] buf, int totalLen)
        {
            var list = new List<TagRecord>();
            try
            {
                int idx = 0;
                while (idx < totalLen)
                {
                    int len = buf[idx];
                    int next = idx + 1 + len;
                    if (len <= 0 || next > totalLen) break;

                    string epcHex = BitConverter.ToString(buf, idx + 1, len).Replace("-", "");
                    string ascii;
                    if (EpcCodec.TryParseEpcFFHex(epcHex, out var p, out var _)
                        && !string.IsNullOrWhiteSpace(p))
                        ascii = p;
                    else
                        ascii = HexToAsciiSafe(epcHex);

                    list.Add(new TagRecord { EpcByteLen = len, EPCHex = epcHex, EPCAscii = ascii });
                    idx = next;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ParseLenPlusEpc] Hata: {ex}");
            }
            return list;

            static string HexToAsciiSafe(string hex)
            {
                var b = EpcCodec.HexToBytes(hex);
                int nul = Array.IndexOf(b, (byte)0x00);
                if (nul >= 0) b = b.Take(nul).ToArray();
                if (b.Length == 0 || b.Any(x => x < 0x20 || x > 0x7E)) return "";
                return Encoding.ASCII.GetString(b);
            }
        }
        public void Dispose()
        {
            try
            {
                _logger.Info("[Dispose] CloseComPort çağrılıyor");
                RWDev.CloseComPort();
                _connected = false;
                _logger.Info("[Dispose] Tamam");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "[Dispose] Hata");
            }
        }
    }
}
