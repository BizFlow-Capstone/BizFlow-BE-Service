using BizFlow.Domain.Entities;

namespace BizFlow.Application.Interfaces.Services;

/// <summary>
/// Extension point for future Rule Engine suggestion API.
/// Currently provides template validation. 
/// Phase D+: Will suggest optimal templateCodes based on business profile.
/// </summary>
public interface IRuleEngineSuggestionService
{
    /// <summary>
    /// Suggest applicable template codes based on business profile.
    /// Returns a ranked list of suggested templates with reason.
    /// </summary>
    Task<List<TemplateSuggestion>> SuggestTemplatesAsync(
        int groupNumber,
        string taxMethod,
        IEnumerable<Guid> businessTypeIds);

    /// <summary>
    /// Validate that the given template codes are applicable for the group + method combo.
    /// Returns list of invalid codes. Empty = all valid.
    /// </summary>
    Task<List<string>> ValidateTemplateCodesAsync(
        IEnumerable<string> templateCodes,
        int groupNumber,
        string taxMethod);
}

public class TemplateSuggestion
{
    public string TemplateCode { get; set; } = null!;
    public string TemplateName { get; set; } = null!;
    public string Reason { get; set; } = null!;
    public int Priority { get; set; }
    public bool IsRequired { get; set; }
}
