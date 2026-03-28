using BizFlow.Application.DTOs.Admin;

namespace BizFlow.Application.Interfaces.Services;

public interface IAdminAccountingService
{
    Task<AdminAccountingOverviewDto> GetOverviewAsync();
    Task<AdminTemplateVersionDetailDto> GetTemplateVersionDetailAsync(int templateVersionId);
    Task<List<AdminTemplateFormulaDto>> GetTemplateFormulasAsync(int templateVersionId);

    Task<AdminTemplateVersionDto> CloneTemplateVersionAsync(int templateVersionId, Guid actorUserId);
    Task<AdminTemplateVersionDto> UpdateTemplateVersionAsync(int templateVersionId, UpdateTemplateVersionRequest request);
    Task<AdminTemplateVersionDto> ActivateTemplateVersionAsync(int templateVersionId);
    Task<AdminTemplateVersionDto> DeactivateTemplateVersionAsync(int templateVersionId);
    Task DeleteTemplateVersionDraftAsync(int templateVersionId);

    Task<AdminTemplateFieldMappingDto> UpdateTemplateFieldMappingForTestingAsync(int mappingId, UpdateTemplateFieldMappingRequest request);

    Task<AdminFormulaDto> GetFormulaDetailAsync(long formulaId);
    Task<AdminFormulaDto> UpdateFormulaForTestingAsync(long formulaId, UpdateFormulaForTestingRequest request);
    Task<AdminFormulaDto> CloneFormulaAsync(long formulaId, CloneFormulaRequest request);

    Task<AdminTaxRulesetDto> ActivateTaxRulesetAsync(int rulesetId);
    Task<AdminTaxRulesetDto> DeactivateTaxRulesetAsync(int rulesetId);

    Task<AdminPreviewResponse> PreviewAsync(AdminPreviewRequest request);
}
