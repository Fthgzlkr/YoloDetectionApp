namespace YoloDetectionApp.Models
{
    public class ClassSummaryItem
    {
        public string ClassName { get; set; } = string.Empty;
        public int Count { get; set; }
        public string ColorHex { get; set; } = string.Empty;
    }

    public class UploadResultViewModel
    {
        public List<string> OriginalImages { get; set; } = new();
        public List<string> ProcessedImages { get; set; } = new();

        // Key = resim yolu, Value = class listesi
        public Dictionary<string, List<ClassSummaryItem>> ClassSummaries { get; set; } = new();
    }
}
