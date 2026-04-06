namespace BizFlow.Application.Common.Models;

/// <summary>Public URLs for transactional emails (e.g. consultant welcome CTA).</summary>
public class AppPublicUrlsOptions
{
    public const string SectionName = "AppPublicUrls";

    /// <summary>Web app login page URL (e.g. https://app.bizflow.com/login).</summary>
    public string LoginUrl { get; set; } = string.Empty;
}
