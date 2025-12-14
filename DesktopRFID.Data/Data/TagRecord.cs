namespace DesktopRFID.Data;

public sealed class TagRecord
{
    public int EpcByteLen { get; init; }
    public string EPCHex { get; init; } = "";
    public string EPCAscii { get; init; } = "";
}