using DesktopRFID.Data.Dto;
using DesktopRFID.Data.Helpers;
using DesktopRFID.Data.Interfaces;

namespace DesktopRFID.Data.Services
{
    public sealed class RfidAssignmentService
    {
        private const string CLEARED_PLATE = "0000000";
        private const string CLEARED_INFILE = "000000";
        private readonly IRfidReader _reader;
        private readonly RfidService _rfid;
        private readonly IMobileApiService _api;
        private readonly int _comPort;
        private readonly IFileLogger _logger;
        private FilePerGarageByPlateResponse? _lastFileInfo;
        public RfidAssignmentService(IFileLogger fileLogger, IRfidReader reader, RfidService rfid, IMobileApiService api, int comPort)
        {
            _reader = reader;
            _rfid = rfid;
            _api = api;
            _comPort = comPort;
            _logger = fileLogger;
        }
        public async Task ConnectAsync()
        {
            await _reader.ConnectSerialAsync(_comPort, baudIndex: 5, comAddr: 0x00);
            await SafeTry(async () => await _reader.SetTxPowerDbmAsync(15));
            await SafeTry(async () => await _reader.SetScanTimeAsync(0x04));
        }
        public bool IsConnected => _reader.IsConnected;
        public async Task<(string epcHex, string? tidHex, string? plate, string? inFileId)> ScanOnceAsync(int timeoutMs = 5000)
        {
            var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            _logger.Info("Tag Tarama Başladı. ScanOnceAsync");
            while (DateTime.UtcNow < deadline)
            {
                var tags = await _rfid.ScanAsync();
                if (tags.Count > 0)
                {
                    var t = tags[0];

                    string? tid = null;
                    if (_reader is ITidCapableReader tidReader)
                        tid = await tidReader.ReadTidHexAsync(t.EPCHex);

                    string? plate = null, inFile = null;
                    if (EpcCodec.TryParseEpcFFHex(t.EPCHex, out var p, out var f))
                    {
                        plate = p;
                        inFile = string.IsNullOrWhiteSpace(f) ? null : f;
                    }

                    return (t.EPCHex, tid, plate, inFile);
                }
                await Task.Delay(150);
            }
            _logger.Info("Tag Tarama Bitti Etiket Bulunamadı. ScanOnceAsync");
            throw new TimeoutException("Etiket bulunamadı.");
        }
        public async Task<bool> VerifyPresenceAsync(string epcHex, string? tidHex)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(tidHex) && _reader is ITidCapableReader tidReader)
                {
                    var tid = await tidReader.ReadTidHexAsync(epcHex);
                    if (!string.IsNullOrWhiteSpace(tid) &&
                        tid.Equals(tidHex, StringComparison.OrdinalIgnoreCase))
                        return true;
                }

                var deadline = DateTime.UtcNow.AddSeconds(2);
                while (DateTime.UtcNow < deadline)
                {
                    var tags = await _rfid.ScanAsync();
                    if (tags.Any(t => string.Equals(t.EPCHex, epcHex, StringComparison.OrdinalIgnoreCase)))
                        return true;

                    await Task.Delay(120);
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, " VerifyPresenceAsync");
                throw;
            }
        }
        public async Task<FilePerGarageByPlateResponse?> FetchPlateAsync(string plate)
        {
            var res = await _api.GetFilesPerGarageByPlateAsync(plate);
            _lastFileInfo = res;
            return res;
        }
        public async Task<bool> AssignAsync(string epcHex, string tidHex, string normalizedPlate, string inFileId, string noteSuffix = "Plaka Ataması Yapıldı.")
        {
            if (EpcCodec.TryParseEpcFFHex(epcHex, out var existingPlate, out var existingInFile))
            {
                if (!IsClearedContent(existingPlate, existingInFile))
                    throw new InvalidOperationException(
                        $"Tag üzerinde veri var (Plaka: {existingPlate ?? "-"}, InFileId: {existingInFile ?? "-"}) — önce 'Tag Kaldırma' yapınız.");
            }

            if (!string.IsNullOrWhiteSpace(_lastFileInfo?.StTagNumber))
                throw new InvalidOperationException(
                    $"Bu plakaya zaten RFID atanmış görünüyor (TagNumber: {_lastFileInfo.StTagNumber}). Yeni atama yapılmaz.");

            if (!await VerifyPresenceAsync(epcHex, tidHex))
                throw new InvalidOperationException("Etikete erişilemiyor.");

            var okApi = await _api.AssignTagToFileAsync(int.Parse(inFileId), tidHex, $"{normalizedPlate} {noteSuffix}");
            if (!okApi) return false;

            var tag = new TagRecord { EPCHex = epcHex, EpcByteLen = epcHex.Length / 2 };
            return await _rfid.ProgramAsync(tag, normalizedPlate, inFileId);
        }
        public async Task<(bool ok, string? message)> RemoveTagAsync(string epcHex, string? tidHex)
        {
            try
            {
                string? plate = null, inFile = null;
                if (EpcCodec.TryParseEpcFFHex(epcHex, out var p, out var f))
                {
                    plate = p;
                    inFile = string.IsNullOrWhiteSpace(f) ? null : f;
                    if (IsClearedContent(plate, inFile))
                        return (false, "Tag boş olduğundan kaldırma işlemi yapılamaz.");
                }

                if (!await VerifyPresenceAsync(epcHex, tidHex))
                {
                    _logger.Error("Etikete erişilemiyor. VerifyPresenceAsync");
                    return (false, "Etikete erişilemiyor.");
                }

                var (apiOk, apiMsg) = await _api.DeliverByTagIdAsync(tidHex ?? "");


                if (!apiOk) return (false, apiMsg ?? "API başarısız.");
                var tag = new TagRecord { EPCHex = epcHex, EpcByteLen = epcHex.Length / 2 };
                var cleared = await ClearTagAsync(tag);
                if (!cleared)
                    return (false, "API başarılı fakat tag içeriği temizlenemedi.");

                if (_lastFileInfo != null) _lastFileInfo.StTagNumber = null;
                return (true, apiMsg ?? "Tag kaldırma işlemi tamamlandı.");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, " RemoveTagAsync");
                throw;
            }

        }
        private async Task<bool> ClearTagAsync(TagRecord tag)
        {
            _logger.Info($"Etiket Kaldırma Başlıyo , EPC = {tag.EPCHex} ClearTagAsync");
            var res = await _rfid.ProgramAsync(tag, CLEARED_PLATE, CLEARED_INFILE);
            _logger.Info($"Etiket Kaldırma Bitti , EPC = {tag.EPCHex} ClearTagAsync");
            return res;
        }
        public static string NormalizePlate(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
            return new string(raw.ToUpperInvariant()
                .Where(c => (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9'))
                .ToArray());
        }
        private static bool IsClearedContent(string? plate, string? inFile)
        {
            bool plateCleared = string.IsNullOrWhiteSpace(plate) || plate == CLEARED_PLATE;
            bool inFileCleared =
                string.IsNullOrWhiteSpace(inFile) ||
                inFile == CLEARED_INFILE;
            return plateCleared && inFileCleared;
        }
        private static async Task SafeTry(Func<Task> action)
        {
            try { await action(); } catch { }
        }
    }
}