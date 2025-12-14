using System.Drawing;
using System.Windows.Forms;

namespace DesktopRFID.Forms
{
    partial class LoginForm
    {
        private System.ComponentModel.IContainer components = null;

        private Label lblUser;
        private Label lblPass;
        private TextBox txtUser;
        private TextBox txtPass;
        private Button btnLogin;
        private Button btnExit;
        private CheckBox chkShow;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();

            lblUser = new Label { AutoSize = true, Text = "Kullanıcı Ad", Location = new Point(20, 30) };
            lblPass = new Label { AutoSize = true, Text = "Kullanıcı Şifre", Location = new Point(20, 70) };

            txtUser = new TextBox { Name = "txtUser", Location = new Point(120, 26), Width = 220, TabIndex = 0 };
            txtPass = new TextBox { Name = "txtPass", Location = new Point(120, 66), Width = 220, TabIndex = 1, UseSystemPasswordChar = true };

            chkShow = new CheckBox { AutoSize = true, Text = "Göster", Location = new Point(120, 96) };
            chkShow.CheckedChanged += (_, __) => txtPass.UseSystemPasswordChar = !chkShow.Checked;

            btnLogin = new Button { Name = "btnLogin", Text = "Giriş", Location = new Point(120, 130), Size = new Size(100, 32), TabIndex = 2 };
            btnExit = new Button { Name = "btnExit", Text = "İptal", Location = new Point(240, 130), Size = new Size(100, 32), TabIndex = 3 };
            btnExit.Click += (_, __) => this.Close();

            this.SuspendLayout();
            this.Text = "RFID - Giriş";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.ClientSize = new Size(380, 190);

            this.AcceptButton = btnLogin;
            this.CancelButton = btnExit;

            this.Controls.Add(lblUser);
            this.Controls.Add(lblPass);
            this.Controls.Add(txtUser);
            this.Controls.Add(txtPass);
            this.Controls.Add(chkShow);
            this.Controls.Add(btnLogin);
            this.Controls.Add(btnExit);

            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}