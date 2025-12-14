namespace DesktopRFID.Data.Interfaces;

public interface IRfidReader : IDisposable
{
    bool IsConnected { get; }
    Task ConnectSerialAsync(int comPort, byte baudIndex = 5, byte comAddr = 0x00);
    Task DisconnectAsync();
    Task<IReadOnlyList<TagRecord>> InventoryOnceAsync();
    Task<byte[]?> ReadTidShortAsync(string epcHex);
    Task<bool> WriteEpcAsync(string currentEpcHex, byte[] newEpcPayload, byte[] accessPwd4B /* 4 byte */);
    Task SetTxPowerDbmAsync(byte dbm);
    Task<byte> GetTxPowerDbmAsync();
    Task SetScanTimeAsync(byte scanTimeHex);
}