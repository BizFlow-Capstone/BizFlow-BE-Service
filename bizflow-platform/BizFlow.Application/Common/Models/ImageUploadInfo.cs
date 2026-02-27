namespace BizFlow.Application.Common.Models
{
    /// <summary>
    /// Result of a successful image upload
    /// </summary>
    public record ImageUploadInfo(string Url, string PublicId);
}
