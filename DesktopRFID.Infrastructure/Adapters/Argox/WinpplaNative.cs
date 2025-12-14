using System.Runtime.InteropServices;
public static class WinpplaNative
{
    private const string Dll = "Winppla.dll";
    [DllImport(Dll, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
    internal static extern int A_CreatePrn(int selection, string filename);
    [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
    internal static extern void A_ClosePrn();
    [DllImport(Dll)] internal static extern int A_GetUSBBufferLen();
    [DllImport(Dll)] internal static extern int A_EnumUSB(byte[] buf);

    [DllImport(Dll)]
    internal static extern int A_GetUSBDeviceInfo(
        int nPort, byte[] pDeviceName, out int pDeviceNameLen,
        byte[] pDevicePath, out int pDevicePathLen);
    [DllImport(Dll)] internal static extern void A_Clear_Memory();
    [DllImport(Dll)] internal static extern int A_Set_ErrorDlg(int nShow);
    [DllImport(Dll)] internal static extern int A_Set_Unit(char unit);
    [DllImport(Dll)] internal static extern int A_Set_Syssetting(int transfer, int cut_peel, int length, int zero, int pause);
    [DllImport(Dll)] internal static extern int A_Set_Darkness(int heat);
    [DllImport(Dll)] internal static extern int A_Set_LabelForSmartPrint(int lablength, int gaplength);
    [DllImport(Dll)]
    internal static extern int A_Prn_Text_TrueType(
        int x, int y, int FSize, string FType, int Fspin, int FWeight,
        int FItalic, int FUnline, int FStrikeOut, string id_name,
        string data, int mem_mode);
    [DllImport(Dll)]
    internal static extern int A_Print_Out(int width, int height, int copies, int amount);
}