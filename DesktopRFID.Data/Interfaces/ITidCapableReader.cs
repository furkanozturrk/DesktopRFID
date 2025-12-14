namespace DesktopRFID.Data.Interfaces
{
    public interface ITidCapableReader
    {
        Task<string?> ReadTidHexAsync(string epcHex);
    }
}
