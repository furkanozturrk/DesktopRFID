using System.Drawing;
using System.Windows.Forms;

namespace DesktopRFID.Forms
{
    partial class ImagePreviewForm
    {
        private System.ComponentModel.IContainer components = null;
        private PictureBox pic;

        private void InitializeComponent()
        {
            this.pic = new PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.pic)).BeginInit();
            this.SuspendLayout();
            this.pic.BackColor = Color.Black;
            this.pic.Dock = DockStyle.Fill;
            this.pic.SizeMode = PictureBoxSizeMode.Zoom;
            this.pic.TabStop = false;
            this.AutoScaleMode = AutoScaleMode.Font;
            this.BackColor = Color.Black;
            this.ClientSize = new Size(1200, 800);
            this.Controls.Add(this.pic);
            this.MinimumSize = new Size(800, 600);
            this.Name = "ImagePreviewForm";
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Görsel Önizleme";
            this.WindowState = FormWindowState.Maximized;
            ((System.ComponentModel.ISupportInitialize)(this.pic)).EndInit();
            this.ResumeLayout(false);
        }
    }
}
