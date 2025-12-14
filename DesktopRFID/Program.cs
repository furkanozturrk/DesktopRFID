using DesktopRFID.Data.Interfaces;
using DesktopRFID.Forms;
using DesktopRFID.Infrastructure.Logging;
using System.Runtime.InteropServices;

namespace DesktopRFID
{
    internal static class Program
    {
        private const string MutexName = @"Global\DesktopRFID-{7A7C34F8-3E50-4E42-8F7C-0E2F2C2BCE6B}";
        private static Mutex? _singleInstanceMutex;
        private static readonly IFileLogger Log = FileLogger.Default;
        private static Icon? s_appIcon;
        private static Icon? s_smallIcon;
        private static Icon? s_bigIcon;
        private const int WM_SETICON = 0x80;
        private const int ICON_SMALL = 0;
        private const int ICON_BIG = 1;
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        private static Icon? LoadAppIcon()
        {
            try
            {
                var icoPath = Path.Combine(AppContext.BaseDirectory, "favicon.ico");
                if (File.Exists(icoPath))
                    return new Icon(icoPath);
            }
            catch { }
            return null;
        }
        private static void EnsureIconForAllOpenForms()
        {
            if (s_appIcon is null) return;

            foreach (Form f in Application.OpenForms)
            {
                if (!ReferenceEquals(f.Icon, s_appIcon))
                    f.Icon = s_appIcon;

                try
                {
                    if (s_smallIcon != null)
                        SendMessage(f.Handle, WM_SETICON, (IntPtr)ICON_SMALL, s_smallIcon.Handle);
                    if (s_bigIcon != null)
                        SendMessage(f.Handle, WM_SETICON, (IntPtr)ICON_BIG, s_bigIcon.Handle);
                }
                catch { }
            }
        }

        [STAThread]
        static void Main()
        {
            bool createdNew = false;

            try
            {
                _singleInstanceMutex = new Mutex(initiallyOwned: true, name: MutexName, createdNew: out createdNew);
                if (!createdNew)
                {
                    MessageBox.Show("Uygulama zaten çalýþýyor.", "DesktopRFID",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

                Application.ThreadException += (s, e) =>
                {
                    Log.Error(e.Exception, "UI thread hatasý");
                    MessageBox.Show("Beklenmeyen bir hata oluþtu. Loglara bakýnýz.", "DesktopRFID",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                };

                AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                {
                    var ex = e.ExceptionObject as Exception ?? new Exception("Bilinmeyen hata");
                    Log.Error(ex, "UnhandledException (AppDomain)");
                };

                TaskScheduler.UnobservedTaskException += (s, e) =>
                {
                    Log.Error(e.Exception, "UnobservedTaskException (Task)");
                    e.SetObserved();
                };

                Application.ApplicationExit += (_, __) =>
                {
                    Log.Info("Uygulama kapanýyor");
                    Log.FlushAndStop();

                    s_smallIcon?.Dispose();
                    s_bigIcon?.Dispose();
                    s_appIcon?.Dispose();
                };

                ApplicationConfiguration.Initialize();

                s_appIcon = LoadAppIcon();
                if (s_appIcon != null)
                {
                    s_smallIcon = new Icon(s_appIcon, new Size(16, 16));
                    s_bigIcon = new Icon(s_appIcon, new Size(256, 256));
                }

                Application.Idle += (_, __) => { EnsureIconForAllOpenForms(); };

                try
                {
                    var login = new LoginForm();
                    if (s_appIcon != null)
                        login.Icon = s_appIcon;

                    Application.Run(login);
                }
                catch (Exception runEx)
                {
                    Log.Error(runEx, "Application.Run sýrasýnda beklenmeyen hata");
                }
            }
            finally
            {
                if (createdNew && _singleInstanceMutex is not null)
                {
                    _singleInstanceMutex.ReleaseMutex();
                    _singleInstanceMutex.Dispose();
                }
            }
        }
    }
}