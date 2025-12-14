namespace DesktopRFID.Data.Dto
{
    public sealed class FilePerGarageByPlateResponse
    {
        public bool? Status { get; set; }
        public string? Message { get; set; }

        public string? StNumberplate { get; set; }
        public string? StVehicleMarkName { get; set; }
        public string? StVehicleModelName { get; set; }
        public int? InFileId { get; set; }
        public string? StChasisNo { get; set; }
        public string? StVehicleColor { get; set; }
        public string? StTagNumber { get; set; }
        public string? StSiteNo { get; set; }
        public string? PictureThumbnailUrl { get; set; }
        public string? PictureUrl { get; set; }
    }
}