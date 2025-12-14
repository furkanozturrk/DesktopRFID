using System.Text;

namespace DesktopRFID.Data.Helpers;

public static class EpcCodec
{
    public const byte SeparatorFF = 0xFF;
    public const byte TailPadF0 = 0xF0;
    public static bool TryBuildPlateRaw_NoPad(string s, out byte[]? plateRaw)
    {
        plateRaw = null;
        if (string.IsNullOrWhiteSpace(s)) return false;
        s = s.Trim();
        if (s.Length is not (7 or 8)) return false;
        if (!IsPrintableAscii(s)) return false;
        plateRaw = Encoding.ASCII.GetBytes(s);
        return true;
    }
    public static bool TryBuildBcdFlexible(string digits, out byte[]? bcd)
    {
        bcd = null;
        if (string.IsNullOrWhiteSpace(digits)) return false;
        foreach (var ch in digits) if (ch < '0' || ch > '9') return false;

        int n = digits.Length;
        int bytes = (n + 1) / 2;
        var outb = new byte[bytes];
        int di = 0;
        for (int i = 0; i < bytes; i++)
        {
            int hi = digits[di++] - '0';
            int lo = (di < n) ? (digits[di++] - '0') : 0x0F;
            outb[i] = (byte)((hi << 4) | (lo & 0x0F));
        }
        bcd = outb;
        return true;
    }
    public static byte[] BuildCompositeEpcFF_WithTail(byte[] plateRaw, byte[] bcdId)
    {
        int total = plateRaw.Length + 1 + bcdId.Length;
        bool needTail = (total % 2) == 1;
        var buf = new byte[total + (needTail ? 1 : 0)];
        int p = 0;
        Buffer.BlockCopy(plateRaw, 0, buf, p, plateRaw.Length); p += plateRaw.Length;
        buf[p++] = SeparatorFF;
        Buffer.BlockCopy(bcdId, 0, buf, p, bcdId.Length); p += bcdId.Length;
        if (needTail) buf[p++] = TailPadF0;
        return buf;
    }
    public static bool TryParseEpcFFHex(string epcHex, out string plateAscii, out string idDigits)
    {
        plateAscii = ""; idDigits = "";
        var epc = HexToBytes(epcHex);
        if (epc.Length < 2) return false;

        int ff = Array.IndexOf(epc, SeparatorFF);
        if (ff <= 0 || ff > 8) return false;

        var pRaw = new byte[ff];
        Buffer.BlockCopy(epc, 0, pRaw, 0, ff);
        plateAscii = BytesToAsciiUntilNul(pRaw);

        int start = ff + 1;
        int len = epc.Length - start;
        if (len > 0 && epc[^1] == TailPadF0) len -= 1;
        if (len <= 0) { idDigits = ""; return true; }

        var bcd = new byte[len];
        Buffer.BlockCopy(epc, start, bcd, 0, len);
        idDigits = BcdToDigitsAuto(bcd);
        return true;
    }
    public static string BytesToAsciiUntilNul(byte[] data)
    {
        int end = Array.IndexOf(data, (byte)0x00);
        if (end < 0) end = data.Length;
        if (end == 0) return string.Empty;
        for (int i = 0; i < end; i++)
            if (data[i] < 0x20 || data[i] > 0x7E) return string.Empty;
        return Encoding.ASCII.GetString(data, 0, end);
    }
    public static string BcdToDigitsAuto(byte[] bcd)
    {
        if (bcd.Length == 0) return "";
        int digits = bcd.Length * 2;
        if ((bcd[^1] & 0x0F) == 0x0F) digits -= 1;
        var sb = new StringBuilder(digits);
        int produced = 0;
        for (int i = 0; i < bcd.Length && produced < digits; i++)
        {
            int hi = (bcd[i] >> 4) & 0x0F;
            int lo = bcd[i] & 0x0F;
            if (produced++ < digits) sb.Append((char)('0' + hi));
            if (produced++ <= digits) sb.Append((char)('0' + lo));
        }
        return sb.ToString();
    }
    public static string ToHex(byte[] data) => BitConverter.ToString(data).Replace("-", "");
    public static byte[] HexToBytes(string hex)
    {
        string s = new string(hex.Where(IsHex).ToArray());
        if ((s.Length % 2) != 0) s = "0" + s;
        var buf = new byte[s.Length / 2];
        for (int i = 0; i < buf.Length; i++)
            buf[i] = Convert.ToByte(s.Substring(i * 2, 2), 16);
        return buf;
    }
    private static bool IsHex(char c) => (c >= '0' && c <= '9') || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f');
    private static bool IsPrintableAscii(string s) => s.All(ch => ch >= 0x20 && ch <= 0x7E);
}