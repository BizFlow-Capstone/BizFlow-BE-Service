using BizFlow.Application.DTOs.Admin;

namespace BizFlow.Application.Interfaces.Services;

public interface IAdminAccountingService
{
    Task<AdminAccountingOverviewDto> GetOverviewAsync();
    Task<AdminTemplateVersionDetailDto> GetTemplateVersionDetailAsync(int templateVersionId);
    Task<List<AdminTemplateFormulaDto>> GetTemplateFormulasAsync(int templateVersionId);

    Task<AdminTemplateDto> CreateTemplateAsync(CreateTemplateRequest request, Guid actorUserId);
    Task<AdminTemplateVersionDto> CreateTemplateVersionAsync(int templateId, CreateTemplateVersionRequest request, Guid actorUserId);
    Task<AdminTemplateVersionDto> CloneTemplateVersionAsync(int templateVersionId, Guid actorUserId);
    Task<AdminTemplateVersionDto> UpdateTemplateVersionAsync(int templateVersionId, UpdateTemplateVersionRequest request);
    Task<AdminTemplateVersionDto> ActivateTemplateVersionAsync(int templateVersionId);
    Task<AdminTemplateVersionDto> DeactivateTemplateVersionAsync(int templateVersionId);
    Task DeleteTemplateVersionDraftAsync(int templateVersionId);

    Task<AdminTemplateFieldMappingDto> UpdateTemplateFieldMappingForTestingAsync(int mappingId, UpdateTemplateFieldMappingRequest request);

    Task<AdminFormulaDto> GetFormulaDetailAsync(long formulaId);
    Task<AdminFormulaDto> CreateFormulaAsync(CreateFormulaRequest request, Guid actorUserId);
    Task<AdminFormulaDto> UpdateFormulaForTestingAsync(long formulaId, UpdateFormulaForTestingRequest request);
    Task<AdminFormulaDto> CloneFormulaAsync(long formulaId, CloneFormulaRequest request);
    Task<bool> DeleteFormulaAsync(long formulaId);

    Task<AdminTaxRulesetDto> ActivateTaxRulesetAsync(int rulesetId);
    Task<AdminTaxRulesetDto> DeactivateTaxRulesetAsync(int rulesetId);

    Task<AdminPreviewResponse> PreviewAsync(AdminPreviewRequest request);
    Task<GenerateConsultantSampleDataResponse> GenerateConsultantSampleDataAsync(GenerateConsultantSampleDataRequest request, Guid actorUserId);

    // ── Compare ──
    Task<AdminCompareResponse> CompareAsync(AdminCompareRequest request);

    // ── Trace ──
    Task<AdminTraceResponse> TraceFormulaAsync(AdminTraceRequest request);

    // ── Reference ──
    AdminReferenceDto GetReference();

    // ── Node Schema ──
    List<FormulaNodeSchemaDto> GetFormulaNodeSchemas();

    // ── MappableEntities CRUD ──
    Task<List<AdminMappableEntityDto>> GetMappableEntitiesAsync(bool? active);
    Task<AdminMappableEntityDetailDto> GetMappableEntityDetailAsync(int entityId);
    Task<AdminMappableEntityDto> CreateMappableEntityAsync(CreateMappableEntityRequest request, Guid actorUserId);
    Task<AdminMappableEntityDto> UpdateMappableEntityAsync(int entityId, UpdateMappableEntityRequest request);
    Task DeleteMappableEntityAsync(int entityId);

    // ── MappableFields CRUD ──
    Task<AdminMappableFieldDto> CreateMappableFieldAsync(int entityId, CreateMappableFieldRequest request);
    Task<AdminMappableFieldDto> UpdateMappableFieldAsync(int fieldId, UpdateMappableFieldRequest request);

    // ── RowDefinitions CRUD ──
    Task<List<AdminRowDefinitionDto>> GetRowDefinitionsAsync(int templateVersionId);
    Task<AdminRowDefinitionDto> CreateRowDefinitionAsync(int templateVersionId, CreateRowDefinitionRequest request);
    Task<AdminRowDefinitionDto> UpdateRowDefinitionAsync(int rowDefId, UpdateRowDefinitionRequest request);
    Task DeleteRowDefinitionAsync(int rowDefId);

    // ── FieldMappings create/delete ──
    Task<AdminTemplateFieldMappingDto> CreateFieldMappingAsync(int templateVersionId, CreateFieldMappingRequest request);
    Task DeleteFieldMappingAsync(int mappingId);

    // ── Full structure ──
    Task<AdminFullStructureDto> GetFullStructureAsync(int templateVersionId);

    // ── BusinessTypes + IndustryTaxRates Admin ──
    Task<List<AdminBusinessTypesWithRatesDto>> GetBusinessTypesWithRatesAsync(int rulesetId);
    Task<AdminBusinessTypeDetailDto> CreateBusinessTypeAsync(CreateBusinessTypeRequest request, Guid actorUserId);
    Task<AdminBusinessTypeDetailDto> UpdateBusinessTypeAsync(Guid businessTypeId, UpdateBusinessTypeRequest request, Guid actorUserId);
    Task<bool> DeleteBusinessTypeAsync(Guid businessTypeId, Guid actorUserId);
    Task<List<AdminIndustryTaxRateDto>> UpsertIndustryTaxRatesAsync(int rulesetId, Guid businessTypeId, UpsertIndustryTaxRatesRequest request);

    // ── TaxRuleset CRUD ──
    Task<AdminTaxRulesetDto> CreateTaxRulesetAsync(CreateTaxRulesetRequest request, Guid actorUserId);
    Task<AdminTaxRulesetDto> UpdateTaxRulesetAsync(int rulesetId, UpdateTaxRulesetRequest request);
}
