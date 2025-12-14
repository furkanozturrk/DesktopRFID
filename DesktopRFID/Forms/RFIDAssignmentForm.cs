using DesktopRFID.Data.Interfaces;
using DesktopRFID.Data.Services;
using DesktopRFID.Infrastructure.Adapters.Argox;
using DesktopRFID.Infrastructure.Adapters.RFID;

namespace DesktopRFID.Forms
{
    public partial class RFIDAssignmentForm : Form
    {
        private readonly RfidAssignmentService _svc;
        private readonly IWinpplaPrinter _printer;
        private string? _currentEpc;
        private string? _currentTid;
        private const int ScanTimeoutSeconds = 5;
        private readonly IFileLogger _logger;
        private sealed class PlateOption { public string Text { get; set; } = ""; public string Value { get; set; } = ""; public override string ToString() => Text; }
        public RFIDAssignmentForm(int comPort = 3, IAuthService? auth = null, string? loggedUserId = null, IFileLogger logger = null)
        {
            InitializeComponent();

            var reader = new RwDevReader();
            var rfid = new RfidService(reader);
            var api = new MobileApiService(fileLogger: logger, auth);
            _svc = new RfidAssignmentService(fileLogger: logger, reader, rfid, api, comPort);

            _printer = new WinpplaPrinter();

            _loggedUserId = loggedUserId;
            WireUi();
            _logger = logger;
        }
        private readonly string? _loggedUserId;
        private void WireUi()
        {
            btnScan.Click += async (_, __) => await ScanAsync();
            btnAssign.Click += async (_, __) => await AssignAsync();
            btnDeliver.Click += async (_, __) => await DeliverAsync();
            btnExit.Click += (_, __) =>
            {
                if (MessageBox.Show("Uygulamadan çıkılsın mı?", "Çıkış",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    Application.Exit();
            };

            cmbPresetPlate.DisplayMember = nameof(PlateOption.Text);
            cmbPresetPlate.ValueMember = nameof(PlateOption.Value);
            cmbPresetPlate.Items.Add(new PlateOption { Text = "Seçiniz…", Value = "" });
            cmbPresetPlate.SelectedIndex = 0;

            btnFetchPlate.Click += async (_, __) => await FetchPlateAsync();

            btnPrintSite.Click += (_, __) => PrintSiteNo();

            this.Load += async (_, __) =>
            {
                UpdateUserLabel();
                await AutoConnectAsync();
            };
        }
        private void PrintSiteNo()
        {
            var site = (txtStSiteNo.Text ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(site) || site == "-")
            {
                MessageBox.Show("Site No boş.Plakayı Getir yapınız.", "Yazdırma", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                _logger.Info($"Yazdırma İşlemi {site} Başladı.");
                _printer.Print(site);
                _logger.Info($"Yazdırma İşlemi {site} Bitti.");
                MessageBox.Show("Yazdırma gönderildi.", "Yazdırma", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Yazdırma hatası PrintSiteNo");
                MessageBox.Show("Yazdırma hatası: " + ex.Message, "Yazdırma", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private async Task AutoConnectAsync()
        {
            SetConnStatus("Bağlanıyor…");
            try
            {
                await _svc.ConnectAsync();
                SetConnStatus("Bağlı (RFID)");
                ResetUiValues();
                ClearApiValues();
            }
            catch (Exception ex)
            {
                SetConnStatus("Bağlı değil");
                MessageBox.Show("Bağlantı hatası: " + ex.Message, "RFID",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void ResetUiValues()
        {
            SetScanStatus(false);
            lblPlateValue.Text = "-";
            lblInFileIdValue.Text = "-";
            lblTagIdValue.Text = "-";
            _currentEpc = _currentTid = null;
        }
        private void ClearApiValues()
        {
            lblNPVal.Text = "-";
            lblMarkVal.Text = "-";
            lblModelVal.Text = "-";
            lblFileIdVal.Text = "-";
            lblChassisVal.Text = "-";
            lblColorVal.Text = "-";
            lblStTagNumber.Text = "-";
            txtStSiteNo.Text = "-";
            _ = imgBox?.SetImageUrls(null, null);
        }
        private async Task ScanAsync()
        {
            if (!_svc.IsConnected)
            {
                MessageBox.Show("Okuyucu bağlı değil.", "RFID",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SetScanStatus(true);
            ResetUiValues();

            try
            {
                var (epc, tid, plate, inFile) = await _svc.ScanOnceAsync(ScanTimeoutSeconds * 1000);
                _currentEpc = epc;
                _currentTid = tid;

                lblTagIdValue.Text = string.IsNullOrWhiteSpace(tid) ? "-" : tid;
                lblPlateValue.Text = string.IsNullOrWhiteSpace(plate) ? "Plaka bulunamadı" : plate;
                lblInFileIdValue.Text = string.IsNullOrWhiteSpace(inFile) ? "-" : inFile;
            }
            catch (TimeoutException)
            {
                MessageBox.Show("Etiket bulunamadı.", "RFID",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Taramada hata: " + ex.Message, "RFID",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { SetScanStatus(false); }
        }
        private async Task AssignAsync()
        {
            if (!_svc.IsConnected) { MessageBox.Show("Okuyucu bağlı değil.", "RFID"); return; }
            if (string.IsNullOrWhiteSpace(_currentEpc) || string.IsNullOrWhiteSpace(_currentTid))
            { MessageBox.Show("Önce bir etiket tarayın.", "RFID"); return; }

            var apiPlateRaw = (lblNPVal.Text ?? "").Trim();
            var apiInFileRaw = (lblFileIdVal.Text ?? "").Trim();
            var plate = RfidAssignmentService.NormalizePlate(apiPlateRaw);

            if (string.IsNullOrWhiteSpace(apiPlateRaw) || apiPlateRaw == "-")
            { MessageBox.Show("API’den plaka alınmadı. Önce 'Plakayı Getir' ile sorgulayın."); return; }
            if (string.IsNullOrWhiteSpace(apiInFileRaw) || apiInFileRaw == "-")
            { MessageBox.Show("API’den InFileId alınmadı. Önce 'Plakayı Getir' ile sorgulayın."); return; }
            if (string.IsNullOrWhiteSpace(_currentTid) || _currentTid == "-")
            { MessageBox.Show("Tag ID okunmadı. Etiketi tekrar tarayınız."); return; }
            if (plate.Length is < 7 or > 8)
            { MessageBox.Show("Plaka 7–8 karakter (A–Z/0–9) olmalı."); return; }
            if (!apiInFileRaw.All(char.IsDigit))
            { MessageBox.Show("InFileId sadece rakam olmalı."); return; }

            try
            {
                SetScanStatus(true);

                var ok = await _svc.AssignAsync(_currentEpc!, _currentTid!, plate, apiInFileRaw);
                MessageBox.Show(ok ? "Atama yapıldı." : "Atama başarısız.", "RFID");

                await ScanAsync();

                if (string.IsNullOrWhiteSpace(txtPlateApi.Text) ||
                    !string.Equals(RfidAssignmentService.NormalizePlate(txtPlateApi.Text), plate, StringComparison.OrdinalIgnoreCase))
                {
                    txtPlateApi.Text = plate;
                }
                await FetchPlateAsync();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Atama hatası AssignAsync");
                MessageBox.Show("Atama hatası: " + ex.Message, "RFID",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetScanStatus(false);
            }
        }
        private async Task DeliverAsync()
        {
            if (!_svc.IsConnected) { MessageBox.Show("Okuyucu bağlı değil.", "RFID"); return; }
            if (string.IsNullOrWhiteSpace(_currentTid))
            { MessageBox.Show("Tag ID okunmadı. Etiketi tarayın."); return; }

            bool uiPlateEmpty = string.IsNullOrWhiteSpace(lblPlateValue.Text) || lblPlateValue.Text == "-" || lblPlateValue.Text.All(c => c == '0');
            bool uiInFileEmpty = string.IsNullOrWhiteSpace(lblInFileIdValue.Text) || lblInFileIdValue.Text == "-" || lblInFileIdValue.Text.Trim('0', '?').Length == 0;
            if (uiPlateEmpty && uiInFileEmpty)
            {
                MessageBox.Show("Tag boş olduğundan kaldırma işlemi yapılamaz.", "Uyarı",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                SetScanStatus(true);

                var (ok, msg) = await _svc.RemoveTagAsync(_currentEpc ?? "", _currentTid);

                if (ok)
                {
                    MessageBox.Show(msg ?? "Tag kaldırma tamamlandı.", "Bilgi",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);

                    await ScanAsync();

                    var plateNorm = RfidAssignmentService.NormalizePlate(lblNPVal.Text ?? "");
                    if (!string.IsNullOrWhiteSpace(plateNorm))
                    {
                        txtPlateApi.Text = plateNorm;
                        await FetchPlateAsync();
                    }
                    else
                    {
                        lblStTagNumber.Text = "-";
                    }
                }
                else
                {
                    MessageBox.Show(msg ?? "Tag kaldırma başarısız.", "Uyarı",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Tag kaldırma isteğinde hata: " + ex.Message, "Hata",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetScanStatus(false);
            }
        }
        private async Task FetchPlateAsync()
        {
            var plate = (txtPlateApi.Text ?? "").Trim();
            if (string.IsNullOrWhiteSpace(plate))
            { MessageBox.Show("Lütfen plaka giriniz."); return; }

            ClearApiValues();

            try
            {
                var model = await _svc.FetchPlateAsync(plate);

                if (model?.Status == false)
                {
                    var msg = string.IsNullOrWhiteSpace(model.Message) ? "Kayıt bulunamadı." : model.Message!;
                    MessageBox.Show(msg, "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                lblNPVal.Text = model?.StNumberplate ?? "-";
                lblMarkVal.Text = model?.StVehicleMarkName ?? "-";
                lblModelVal.Text = model?.StVehicleModelName ?? "-";
                lblFileIdVal.Text = model?.InFileId?.ToString() ?? "-";
                lblChassisVal.Text = model?.StChasisNo ?? "-";
                lblColorVal.Text = model?.StVehicleColor ?? "-";
                lblStTagNumber.Text = model?.StTagNumber ?? "-";
                txtStSiteNo.Text = model?.StSiteNo ?? "-";

                await imgBox.SetImageUrls(model?.PictureThumbnailUrl, model?.PictureUrl);
            }
            catch (Exception ex)
            {
                MessageBox.Show("İstek sırasında hata: " + ex.Message, "API",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void UpdateUserLabel()
        {
            lblUser.Text = $"Kullanıcı: {(_loggedUserId ?? "-")}";
            lblUser.Left = lblConn.Right + 16;
            lblUser.Top = lblConn.Top;
        }
        private void SetConnStatus(string text) { lblConn.Text = text; UpdateUserLabel(); }
        private void SetScanStatus(bool scanning)
        {
            lblScan.Text = scanning ? "Taranıyor…" : "Hazır";
            btnScan.Enabled = !scanning;
            btnAssign.Enabled = !scanning;
            btnDeliver.Enabled = !scanning;
            cmbPresetPlate.Enabled = !scanning;
            if (btnFetchPlate != null) btnFetchPlate.Enabled = !scanning;
        }
    }
}