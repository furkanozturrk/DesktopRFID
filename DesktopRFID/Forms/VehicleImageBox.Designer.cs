using System.Drawing;
using System.Windows.Forms;

namespace DesktopRFID.Forms
{
    partial class VehicleImageBox
    {
        private System.ComponentModel.IContainer components = null;
        private GroupBox grp;
        private PictureBox picMain;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();

            grp = new GroupBox();
            picMain = new PictureBox();

            SuspendLayout();

            grp.Text = "Görsel";
            grp.Font = new Font("Segoe UI", 11F, FontStyle.Regular, GraphicsUnit.Point);
            grp.Dock = DockStyle.Fill;
            grp.Padding = new Padding(12);

            picMain.Dock = DockStyle.Fill;

            grp.Controls.Add(picMain);
            Controls.Add(grp);

            Name = "VehicleImageBox";
            Size = new Size(950, 420);  
            ResumeLayout(false);
        }
    }
}
