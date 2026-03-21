namespace BizFlow.Application.Common.Models
{
    public class CloudinarySettings
    {
        public const string SectionName = "CloudinarySettings";
        public string CloudName { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string ApiSecret { get; set; } = string.Empty;
        public Dictionary<string, string> UploadPresets { get; set; } = new();
        public int MaxRetries { get; set; }
    }
}
