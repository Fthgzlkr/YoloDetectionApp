namespace YoloDetectionApp.Models
{
    public class UploadResultViewModel
    {
        public List<string> OriginalImages { get; set; } = new();
        public List<string> ProcessedImages { get; set; } = new();
    }
}
