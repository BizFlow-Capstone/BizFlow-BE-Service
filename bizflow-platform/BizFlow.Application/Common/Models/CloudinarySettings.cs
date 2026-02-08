namespace BizFlow.Application.Common.Models
{
    public class CloudinarySettings
    {
        public const string SectionName = "CloudinarySettings";
        public string CloudName { get; set;}
        public string ApiKey { get; set; }
        public string ApiSecret { get; set; }
        public Dictionary<string, string> UploadPresets { get; set; } = new();
    }
}
