using System.Text.Json;
using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Application.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace BizFlow.Infrastructure.Services.RuleEngine;

/// <summary>
/// MVP implementation of rule engine suggestion service.
/// Uses template metadata (ApplicableGroups, ApplicableMethods) to validate and suggest.
/// </summary>
public class RuleEngineSuggestionService : IRuleEngineSuggestionService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<RuleEngineSuggestionService> _logger;

    public RuleEngineSuggestionService(IUnitOfWork uow, ILogger<RuleEngineSuggestionService> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task<List<TemplateSuggestion>> SuggestTemplatesAsync(
        int groupNumber, string taxMethod, IEnumerable<Guid> businessTypeIds)
    {
        var templates = await _uow.AccountingTemplates.GetActiveTemplatesAsync();
        var suggestions = new List<TemplateSuggestion>();

        foreach (var t in templates)
        {
            var groups = JsonSerializer.Deserialize<List<int>>(t.ApplicableGroups) ?? new();
            if (!groups.Contains(groupNumber)) continue;

            var methods = t.ApplicableMethods != null
                ? JsonSerializer.Deserialize<List<string>>(t.ApplicableMethods) ?? new()
                : new List<string>();

            if (methods.Count > 0 && !methods.Contains(taxMethod)) continue;

            var isRequired = t.TemplateCode switch
            {
                "S1a" => groupNumber == 1,
                "S2a" => taxMethod == "method_1",
                "S2b" or "S2c" or "S2d" or "S2e" => taxMethod == "method_2",
                _ => false
            };

            var reason = t.TemplateCode switch
            {
                "S1a" => "Sổ chi tiết bán hàng — bắt buộc nhóm 1",
                "S2a" => "Sổ doanh thu theo cách tính thuế phương pháp 1",
                "S2b" => "Sổ doanh thu phương pháp 2",
                "S2c" => "Sổ doanh thu, chi phí — xác định thu nhập",
                "S2d" => "Sổ kho hàng hóa xuất nhập tồn",
                "S2e" => "Sổ theo dõi tiền mặt + ngân hàng",
                _ => "Mẫu sổ kế toán"
            };

            suggestions.Add(new TemplateSuggestion
            {
                TemplateCode = t.TemplateCode,
                TemplateName = t.Name,
                Reason = reason,
                Priority = isRequired ? 1 : 2,
                IsRequired = isRequired
            });
        }

        return suggestions.OrderBy(s => s.Priority).ToList();
    }

    public async Task<List<string>> ValidateTemplateCodesAsync(
        IEnumerable<string> templateCodes, int groupNumber, string taxMethod)
    {
        var invalid = new List<string>();
        var templates = await _uow.AccountingTemplates.GetActiveTemplatesAsync();

        foreach (var code in templateCodes)
        {
            var template = templates.FirstOrDefault(t => t.TemplateCode == code);
            if (template == null) { invalid.Add(code); continue; }

            var groups = JsonSerializer.Deserialize<List<int>>(template.ApplicableGroups) ?? new();
            if (!groups.Contains(groupNumber)) { invalid.Add(code); continue; }

            if (template.ApplicableMethods != null)
            {
                var methods = JsonSerializer.Deserialize<List<string>>(template.ApplicableMethods) ?? new();
                if (methods.Count > 0 && !methods.Contains(taxMethod))
                    invalid.Add(code);
            }
        }

        return invalid;
    }
}
