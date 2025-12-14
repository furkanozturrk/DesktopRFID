using DesktopRFID.Data.Interfaces;
using DesktopRFID.Data.Services;
using DesktopRFID.Infrastructure.Logging;

namespace DesktopRFID.Forms
{
    public partial class LoginForm : Form
    {
        private readonly IAuthService _auth;
        private readonly ApiClient _api;
        private readonly IFileLogger _logger;
        public LoginForm()
        {
            InitializeComponent();

            _api = new ApiClient(logger: new FileLogger(), "https://test.com.tr");
            var auth = new AuthService(fileLogger: new FileLogger(), api: _api);
            _api.AttachAuthService(auth);
            _auth = auth;

            _logger = new FileLogger();

            txtPass.UseSystemPasswordChar = true;
            this.AcceptButton = btnLogin;

            btnLogin.Click += async (_, __) =>
            {
                await DoLoginAsync();
            };
        }
        private async System.Threading.Tasks.Task DoLoginAsync()
        {
            var clientId = txtUser.Text.Trim();
            var clientSecret = txtPass.Text;

            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
            {
                _logger.Warn("Login iptal: kullanıcı adı veya şifre boş");
                MessageBox.Show("Kullanıcı adı ve şifre zorunludur.", "Uyarı",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnLogin.Enabled = false;
            try
            {
                _logger.Info($"Kimlik doğrulama denemesi: user='{clientId}'");

                var result = await _auth.AuthenticateAsync(clientId, clientSecret);

                if (result.Succeeded)
                {
                    _logger.Info($"Giriş başarılı: user='{clientId}'");

                    Hide();
                    var main = new RFIDAssignmentForm(comPort: 3, auth: _auth, loggedUserId: clientId, logger: _logger);
                    main.FormClosed += (_, __) =>
                    {
                        _logger.Info("RFIDAssignmentForm kapandı; LoginForm da kapanıyor");
                        Close();
                    };
                    _logger.Info("RFIDAssignmentForm açılıyor");
                    main.Show();
                }
                else
                {
                    _logger.Warn($"Giriş başarısız: user='{clientId}', message='{result.Message ?? "null"}'");
                    MessageBox.Show(result.Message ?? "Giriş başarısız.", "Hata",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    txtPass.Clear();
                    txtPass.Focus();
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Beklenmeyen hata (login akışı): user='{clientId}'");
                MessageBox.Show("Beklenmeyen bir hata oluştu: " + ex.Message, "Hata",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnLogin.Enabled = true;
            }
        }
    }
}