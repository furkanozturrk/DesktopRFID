using System.Drawing;
using System.Windows.Forms;

namespace DesktopRFID.Forms
{
    partial class RFIDAssignmentForm
    {
        private System.ComponentModel.IContainer components = null;

        private Label lblConn;
        private Label lblUser;
        private Label lblScan;
        private Button btnScan;
        private Button btnAssign;
        private Button btnDeliver;
        private Button btnExit;

        private GroupBox grpApi;
        private TextBox txtPlateApi;
        private Button btnFetchPlate;
        private Label lblNPVal, lblMarkVal, lblModelVal, lblFileIdVal, lblChassisVal, lblColorVal, lblStTagNumber;
        private TextBox txtStSiteNo;
        private Button btnPrintSite; 

        private GroupBox grpTag;
        private Label lblPlateTitle, lblPlateValue;
        private Label lblInFileIdTitle, lblInFileIdValue;
        private Label lblTagIdTitle, lblTagIdValue;

        private Label lblPresetPlateTitle;
        private ComboBox cmbPresetPlate;

        private VehicleImageBox imgBox;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            this.SuspendLayout();

            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(1500, 1000);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "RFID - Ana Ekran";
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = true;
            this.SizeGripStyle = SizeGripStyle.Hide;

            var baseFont = new Font("Segoe UI", 11F, FontStyle.Regular, GraphicsUnit.Point);

            lblConn = new Label { AutoSize = true, Location = new Point(28, 22), Font = baseFont, Text = "Bağlı değil" };
            lblUser = new Label { AutoSize = true, Location = new Point(220, 22), Font = baseFont, Text = "Kullanıcı: -" };
            lblScan = new Label { AutoSize = true, Location = new Point(1220, 22), Font = baseFont, Text = "Hazır", Anchor = AnchorStyles.Top | AnchorStyles.Right };

            btnScan = new Button
            {
                Text = "Tara",
                Location = new Point(1360, 16),
                Size = new Size(120, 38),
                Font = baseFont,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            btnAssign = new Button
            {
                Text = "Tag Atama",
                Location = new Point(1320, 60),
                Size = new Size(160, 38),
                Font = baseFont,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            btnDeliver = new Button
            {
                Text = "Tag Kaldırma",
                Location = new Point(1320, 104),
                Size = new Size(160, 38),
                Font = baseFont,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            btnExit = new Button
            {
                Text = "Çıkış",
                Size = new Size(200, 56),
                Location = new Point(1280, 920),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                Font = baseFont
            };

            grpApi = new GroupBox
            {
                Text = "Araç Bilgisi",
                Font = baseFont,
                Location = new Point(28, 86),
                Size = new Size(950, 460),
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };

            var lblPlateApi = new Label { Left = 24, Top = 40, Width = 130, Text = "Plaka : " };
            txtPlateApi = new TextBox { Left = 160, Top = 36, Width = 460, PlaceholderText = "Plaka Giriniz." };
            btnFetchPlate = new Button { Left = 630, Top = 34, Width = 220, Height = 32, Text = "Plakayı Getir" };

            int ay = 90; int axL = 24; int axV = 160; int rowH = 34; int wV = 740;
            void Row(string text, out Label val, int top)
            {
                grpApi.Controls.Add(new Label { Left = axL, Top = top, Width = 130, Text = text });
                val = new Label { Left = axV, Top = top, Width = wV, Text = "-" };
                grpApi.Controls.Add(val);
            }

            Row("Plaka:", out lblNPVal, ay); ay += rowH;
            Row("Marka:", out lblMarkVal, ay); ay += rowH;
            Row("Model:", out lblModelVal, ay); ay += rowH;
            Row("inFileId:", out lblFileIdVal, ay); ay += rowH;
            Row("Şasi No:", out lblChassisVal, ay); ay += rowH;
            Row("TagNumber:", out lblStTagNumber, ay); ay += rowH;

            grpApi.Controls.Add(new Label { Left = axL, Top = ay, Width = 130, Text = "Site No:" });

            int siteWidth = wV / 2; 
            txtStSiteNo = new TextBox
            {
                Left = axV,
                Top = ay - 4,
                Width = siteWidth,                  
                ReadOnly = true,
                TabStop = false,
                BorderStyle = BorderStyle.FixedSingle
            };
            grpApi.Controls.Add(txtStSiteNo);

            btnPrintSite = new Button
            {
                Left = axV + siteWidth + 12,        
                Top = ay - 6,
                Width = 120,
                Height = 32,
                Text = "Yazdır",
                Font = baseFont
            };
            grpApi.Controls.Add(btnPrintSite);

            ay += rowH;

            Row("Renk:", out lblColorVal, ay);

            grpApi.Controls.AddRange(new Control[] { lblPlateApi, txtPlateApi, btnFetchPlate });

            grpTag = new GroupBox
            {
                Text = "RFID (Tag)",
                Font = baseFont,
                Location = new Point(1000, 86),
                Size = new Size(480, 230),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            lblPlateTitle = new Label { AutoSize = true, Location = new Point(24, 44), Font = baseFont, Text = "Plaka" };
            lblPlateValue = new Label { AutoSize = false, Location = new Point(140, 40), Size = new Size(310, 30), BorderStyle = BorderStyle.FixedSingle, TextAlign = ContentAlignment.MiddleLeft, Font = baseFont, Text = "-" };
            lblInFileIdTitle = new Label { AutoSize = true, Location = new Point(24, 90), Font = baseFont, Text = "InFileId" };
            lblInFileIdValue = new Label { AutoSize = false, Location = new Point(140, 86), Size = new Size(310, 30), BorderStyle = BorderStyle.FixedSingle, TextAlign = ContentAlignment.MiddleLeft, Font = baseFont, Text = "-" };
            lblTagIdTitle = new Label { AutoSize = true, Location = new Point(24, 136), Font = baseFont, Text = "TagId" };
            lblTagIdValue = new Label { AutoSize = false, Location = new Point(140, 132), Size = new Size(310, 30), BorderStyle = BorderStyle.FixedSingle, TextAlign = ContentAlignment.MiddleLeft, Font = baseFont, Text = "-" };

            grpTag.Controls.AddRange(new Control[] {
                lblPlateTitle, lblPlateValue, lblInFileIdTitle, lblInFileIdValue, lblTagIdTitle, lblTagIdValue
            });

            var gap = 16;
            grpTag.Location = new Point(grpTag.Location.X, btnDeliver.Bottom + gap);

            imgBox = new VehicleImageBox
            {
                Location = new Point(28, 560),
                Size = new Size(950, 420),
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };

            lblPresetPlateTitle = new Label { Visible = false };
            cmbPresetPlate = new ComboBox { Visible = false, Enabled = false };

            this.Controls.Add(lblConn);
            this.Controls.Add(lblUser);
            this.Controls.Add(lblScan);
            this.Controls.Add(btnScan);
            this.Controls.Add(btnAssign);
            this.Controls.Add(btnDeliver);
            this.Controls.Add(btnExit);
            this.Controls.Add(grpApi);
            this.Controls.Add(grpTag);
            this.Controls.Add(imgBox);

            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
