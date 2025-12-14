namespace DesktopRFID.Forms
{
    public partial class ImagePreviewForm : Form
    {
        private Image? _ownedCopy;

        public ImagePreviewForm(Image image)
        {
            InitializeComponent();

            _ownedCopy = (Image)image.Clone();
            pic.Image = (Image)_ownedCopy.Clone();

            KeyPreview = true;
            KeyDown += (_, e) => { if (e.KeyCode == Keys.Escape) Close(); };

            pic.DoubleClick += (_, __) => Close();
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _ownedCopy?.Dispose();
                _ownedCopy = null;
                components?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}