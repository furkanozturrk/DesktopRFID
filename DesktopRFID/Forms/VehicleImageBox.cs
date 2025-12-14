namespace DesktopRFID.Forms
{
    public partial class VehicleImageBox : UserControl
    {
        private Image? _lastImage;

        public bool EnableClickPreview { get; set; } = true;
        public Image? CurrentImage => _lastImage;

        public VehicleImageBox()
        {
            InitializeComponent();

            picMain.SizeMode = PictureBoxSizeMode.Zoom;
            picMain.BackColor = Color.Gainsboro;
            picMain.BorderStyle = BorderStyle.FixedSingle;
            picMain.Cursor = Cursors.Hand;

            picMain.DoubleClick += (_, __) =>
            {
                if (!EnableClickPreview || _lastImage == null) return;
                using (var f = new ImagePreviewForm(_lastImage)) f.ShowDialog(this);
            };

            ShowPlaceholder();
        }

        public async Task SetImageUrls(string? thumbnailUrl, string? fullUrl)
        {
            if (await TryLoadAsync(thumbnailUrl)) return;
            if (await TryLoadAsync(fullUrl)) return;
            ShowPlaceholder();
        }

        public void ShowPreview(IWin32Window? owner = null)
        {
            if (_lastImage == null) return;
            using var f = new ImagePreviewForm(_lastImage);
            if (owner != null) f.ShowDialog(owner);
            else f.ShowDialog();
        }

        private async Task<bool> TryLoadAsync(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;
            try
            {
                using var http = new HttpClient();
                using var resp = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                if (!resp.IsSuccessStatusCode) return false;

                await using var stream = await resp.Content.ReadAsStreamAsync();
                using var img = Image.FromStream(stream);
                _lastImage = (Image)img.Clone();
                picMain.Image = (Image)_lastImage.Clone();
                return true;
            }
            catch { return false; }
        }

        private void ShowPlaceholder()
        {
            var bmp = new Bitmap(800, 500);
            using var g = Graphics.FromImage(bmp);
            g.Clear(Color.Gainsboro);
            using var pen = new Pen(Color.Silver, 2);
            g.DrawRectangle(pen, 1, 1, bmp.Width - 2, bmp.Height - 2);
            g.DrawLine(pen, 0, 0, bmp.Width, bmp.Height);
            g.DrawLine(pen, bmp.Width, 0, 0, bmp.Height);
            _lastImage = bmp;
            picMain.Image = (Image)bmp.Clone();
        }
    }
}
