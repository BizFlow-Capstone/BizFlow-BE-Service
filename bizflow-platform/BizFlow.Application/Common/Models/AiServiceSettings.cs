namespace BizFlow.Application.Common.Models
{
    public class AiServiceSettings
    {
        public const string SectionName = "AiService";

        public string BaseUrl { get; set; } = "http://localhost:8000";
        public string InternalSecret { get; set; } = string.Empty;
        public int TimeoutSeconds { get; set; } = 60;
    }
}
