using DesktopRFID.Data.Interfaces;
using System.Text;

namespace DesktopRFID.Infrastructure.Adapters.Argox
{
    public sealed class WinpplaPrinter : IWinpplaPrinter, IDisposable
    {
        private bool _opened;
        private readonly int _usbIndex;
        private readonly int _darkness;
        private readonly int _labelLenTenthMm;
        private readonly int _gapTenthMm;
        private readonly object _sync = new();
        public WinpplaPrinter(int usbIndex = 1, int darkness = 8, int labelLenTenthMm = 300, int gapTenthMm = 20)
        {
            _usbIndex = usbIndex;
            _darkness = darkness;
            _labelLenTenthMm = labelLenTenthMm;
            _gapTenthMm = gapTenthMm;
        }
        public void Print(string text)
        {
            lock (_sync)
            {
                if (text.Length == 9)
                {
                    PrintTextLabel(text, xMm: 160, yMm: 110, pointSize: 115, fontName: "Ebrima", boldWeight: 700, copies: 1, amount: 1);
                }
                else
                {
                    PrintTextLabel(text, xMm: 115, yMm: 110, pointSize: 115, fontName: "Ebrima", boldWeight: 700, copies: 1, amount: 1);
                }
            }
        }
        private static string GetUsbDevicePath(int usbIndex)
        {
            int nlen = WinpplaNative.A_GetUSBBufferLen() + 1;
            if (nlen <= 1) throw new InvalidOperationException("USB Argox yazıcı bulunamadı.");

            var list = new byte[nlen];
            WinpplaNative.A_EnumUSB(list);

            int nameLen = 256, pathLen = 256;
            var devName = new byte[nameLen];
            var devPath = new byte[pathLen];

            int rc = WinpplaNative.A_GetUSBDeviceInfo(usbIndex, devName, out nameLen, devPath, out pathLen);
            if (rc != 0 || pathLen <= 0)
                throw new InvalidOperationException($"USB cihaz bilgisi alınamadı (index={usbIndex}, rc={rc}).");

            return Encoding.ASCII.GetString(devPath, 0, pathLen);
        }
        private void Open()
        {
            if (_opened) return;
            string devicePath = GetUsbDevicePath(_usbIndex);
            int rc = WinpplaNative.A_CreatePrn(12, devicePath); // 12=USB path
            if (rc != 0) throw new InvalidOperationException($"Yazıcıya bağlanılamadı. Hata kodu: {rc}");
            _opened = true;
        }
        private void Close()
        {
            if (_opened)
            {
                WinpplaNative.A_ClosePrn();
                _opened = false;
            }
        }
        public void PrintTextLabel(string text, int xMm, int yMm, int pointSize, string fontName, int boldWeight, int copies, int amount)
        {
            Open();
            try
            {
                WinpplaNative.A_Set_ErrorDlg(1);
                WinpplaNative.A_Set_Unit('m');
                WinpplaNative.A_Set_Syssetting(1, 0, 0, 0, 0);
                WinpplaNative.A_Set_Darkness(_darkness);
                WinpplaNative.A_Set_LabelForSmartPrint(_labelLenTenthMm, _gapTenthMm);
                WinpplaNative.A_Clear_Memory();

                int rc = WinpplaNative.A_Prn_Text_TrueType(
                    x: xMm, y: yMm, FSize: pointSize, FType: fontName,
                    Fspin: 1, FWeight: boldWeight, FItalic: 0, FUnline: 0, FStrikeOut: 0,
                    id_name: "TT1", data: text, mem_mode: 1);

                if (rc != 0) throw new InvalidOperationException($"A_Prn_Text_TrueType hata kodu: {rc}");

                rc = WinpplaNative.A_Print_Out(1, 1, copies, amount);
                if (rc != 0) throw new InvalidOperationException($"A_Print_Out hata kodu: {rc}");
            }
            finally { Close(); }
        }
        public void Dispose() => Close();
    }
}