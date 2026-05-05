
using BizFlow.Api.Common.Controllers;
using BizFlow.Api.Common.Extensions;
using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Exceptions;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.DTOs.Admin;
using BizFlow.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace BizFlow.Api.Controllers.Accounting;

[Authorize]
[Route("api/admin/accounting")]
public class AdminAccountingController : BaseApiController
{
    private readonly IAdminAccountingService _adminAccountingService;

    public AdminAccountingController(
        IAdminAccountingService adminAccountingService,
        IMessageService messageService,
        ILogger<AdminAccountingController> logger)
        : base(messageService, logger)
    {
        _adminAccountingService = adminAccountingService;
    }

    [HttpGet("overview")]
    [SwaggerOperation(Summary = "Get accounting admin overview")]
    public async Task<IActionResult> GetOverview()
    {
        EnsureAdminOrConsultant();
        var result = await _adminAccountingService.GetOverviewAsync();
        return Ok(result, MessageKeys.DataRetrievedSuccessfully);
    }

    [HttpPost("templates")]
    [SwaggerOperation(Summary = "Create a new accounting template (Admin only)")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateTemplate([FromBody] CreateTemplateRequest request)
    {
        EnsureAdminOnly();
        var result = await _adminAccountingService.CreateTemplateAsync(request, User.GetRequiredUserId());
        return Ok(result, MessageKeys.DataCreatedSuccessfully);
    }

    [HttpGet("template-versions/{templateVersionId:int}")]
    [SwaggerOperation(Summary = "Get template version detail")]
    public async Task<IActionResult> GetTemplateVersionDetail(int templateVersionId)
    {
        EnsureAdminOrConsultant();
        var result = await _adminAccountingService.GetTemplateVersionDetailAsync(templateVersionId);
        return Ok(result, MessageKeys.DataRetrievedSuccessfully);
    }

    [HttpGet("template-versions/{templateVersionId:int}/formulas")]
    [SwaggerOperation(Summary = "Get all formulas used in a template version")]
    public async Task<IActionResult> GetTemplateVersionFormulas(int templateVersionId)
    {
        EnsureAdminOrConsultant();
        var result = await _adminAccountingService.GetTemplateFormulasAsync(templateVersionId);
        return Ok(result, MessageKeys.DataRetrievedSuccessfully);
    }

    [HttpPost("template-versions/{templateVersionId:int}/clone")]
    [SwaggerOperation(Summary = "Clone template version to draft")]
    public async Task<IActionResult> CloneTemplateVersion(int templateVersionId)
    {
        EnsureAdminOrConsultant();
        var result = await _adminAccountingService.CloneTemplateVersionAsync(templateVersionId, User.GetRequiredUserId());
        return Ok(result, MessageKeys.DataCreatedSuccessfully);
    }

    [HttpPost("templates/{templateId:int}/versions")]
    [SwaggerOperation(Summary = "Create blank draft version for an existing template (Admin only)")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateTemplateVersion(int templateId, [FromBody] CreateTemplateVersionRequest request)
    {
        EnsureAdminOnly();
        var result = await _adminAccountingService.CreateTemplateVersionAsync(templateId, request, User.GetRequiredUserId());
        return Ok(result, MessageKeys.DataCreatedSuccessfully);
    }

    [HttpPatch("template-versions/{templateVersionId:int}")]
    [SwaggerOperation(Summary = "Update template version metadata")]
    public async Task<IActionResult> UpdateTemplateVersion(int templateVersionId, [FromBody] UpdateTemplateVersionRequest request)
    {
        EnsureAdminOrConsultant();
        var result = await _adminAccountingService.UpdateTemplateVersionAsync(templateVersionId, request);
        return Ok(result, MessageKeys.DataUpdatedSuccessfully);
    }

    [HttpPost("template-versions/{templateVersionId:int}/activate")]
    [SwaggerOperation(Summary = "Activate template version (Admin only)")]
    public async Task<IActionResult> ActivateTemplateVersion(int templateVersionId)
    {
        EnsureAdminOnly();
        var result = await _adminAccountingService.ActivateTemplateVersionAsync(templateVersionId);
        return Ok(result, MessageKeys.DataUpdatedSuccessfully);
    }

    [HttpPost("template-versions/{templateVersionId:int}/deactivate")]
    [SwaggerOperation(Summary = "Deactivate template version (Admin only)")]
    public async Task<IActionResult> DeactivateTemplateVersion(int templateVersionId)
    {
        EnsureAdminOnly();
        var result = await _adminAccountingService.DeactivateTemplateVersionAsync(templateVersionId);
        return Ok(result, MessageKeys.DataUpdatedSuccessfully);
    }

    [HttpDelete("template-versions/{templateVersionId:int}")]
    [SwaggerOperation(Summary = "Delete draft template version (Admin only)")]
    public async Task<IActionResult> DeleteTemplateVersion(int templateVersionId)
    {
        EnsureAdminOnly();
        await _adminAccountingService.DeleteTemplateVersionDraftAsync(templateVersionId);
        return Ok(MessageKeys.DataDeletedSuccessfully);
    }

    [HttpPatch("field-mappings/{mappingId:int}/testing")]
    [SwaggerOperation(Summary = "Update field mapping for testing")]
    public async Task<IActionResult> UpdateFieldMappingForTesting(int mappingId, [FromBody] UpdateTemplateFieldMappingRequest request)
    {
        EnsureAdminOrConsultant();
        var result = await _adminAccountingService.UpdateTemplateFieldMappingForTestingAsync(mappingId, request);
        return Ok(result, MessageKeys.DataUpdatedSuccessfully);
    }

    [HttpGet("formulas/{formulaId:long}")]
    [SwaggerOperation(Summary = "Get formula detail")]
    public async Task<IActionResult> GetFormulaDetail(long formulaId)
    {
        EnsureAdminOrConsultant();
        var result = await _adminAccountingService.GetFormulaDetailAsync(formulaId);
        return Ok(result, MessageKeys.DataRetrievedSuccessfully);
    }

    [HttpPost("formulas")]
    [SwaggerOperation(Summary = "Create a new formula definition")]
    public async Task<IActionResult> CreateFormula([FromBody] CreateFormulaRequest request)
    {
        EnsureAdminOrConsultant();
        var result = await _adminAccountingService.CreateFormulaAsync(request, User.GetRequiredUserId());
        return Ok(result, MessageKeys.DataCreatedSuccessfully);
    }

    [HttpPatch("formulas/{formulaId:long}/testing")]
    [SwaggerOperation(Summary = "Update formula definition for testing")]
    public async Task<IActionResult> UpdateFormulaForTesting(long formulaId, [FromBody] UpdateFormulaForTestingRequest request)
    {
        EnsureAdminOrConsultant();
        var result = await _adminAccountingService.UpdateFormulaForTestingAsync(formulaId, request);
        return Ok(result, MessageKeys.DataUpdatedSuccessfully);
    }

    [HttpPost("formulas/{formulaId:long}/clone")]
    [SwaggerOperation(Summary = "Clone formula to a new draft formula")]
    public async Task<IActionResult> CloneFormula(long formulaId, [FromBody] CloneFormulaRequest request)
    {
        EnsureAdminOrConsultant();
        var result = await _adminAccountingService.CloneFormulaAsync(formulaId, request);
        return Ok(result, MessageKeys.DataCreatedSuccessfully);
    }

    [HttpPost("rulesets/{rulesetId:int}/activate")]
    [SwaggerOperation(Summary = "Activate tax ruleset (Admin only)")]
    public async Task<IActionResult> ActivateTaxRuleset(int rulesetId)
    {
        EnsureAdminOnly();
        var result = await _adminAccountingService.ActivateTaxRulesetAsync(rulesetId);
        return Ok(result, MessageKeys.DataUpdatedSuccessfully);
    }

    [HttpPost("rulesets/{rulesetId:int}/deactivate")]
    [SwaggerOperation(Summary = "Deactivate tax ruleset (Admin only)")]
    public async Task<IActionResult> DeactivateTaxRuleset(int rulesetId)
    {
        EnsureAdminOnly();
        var result = await _adminAccountingService.DeactivateTaxRulesetAsync(rulesetId);
        return Ok(result, MessageKeys.DataUpdatedSuccessfully);
    }

    [HttpPost("testing/preview")]
    [SwaggerOperation(Summary = "Preview template + formula with selected context")]
    public async Task<IActionResult> Preview([FromBody] AdminPreviewRequest request)
    {
        EnsureAdminOrConsultant();
        var result = await _adminAccountingService.PreviewAsync(request);
        return Ok(result, MessageKeys.DataRetrievedSuccessfully);
    }

    [HttpPost("testing/compare")]
    [SwaggerOperation(Summary = "Compare active vs draft version rendering results")]
    public async Task<IActionResult> Compare([FromBody] AdminCompareRequest request)
    {
        EnsureAdminOrConsultant();
        var result = await _adminAccountingService.CompareAsync(request);
        return Ok(result, MessageKeys.DataRetrievedSuccessfully);
    }

    [HttpPost("testing/trace")]
    [SwaggerOperation(Summary = "Trace formula evaluation step-by-step for debugging")]
    public async Task<IActionResult> TraceFormula([FromBody] AdminTraceRequest request)
    {
        EnsureAdminOrConsultant();
        var result = await _adminAccountingService.TraceFormulaAsync(request);
        return Ok(result, MessageKeys.DataRetrievedSuccessfully);
    }

    // ── Reference ──

    [HttpGet("reference")]
    [SwaggerOperation(Summary = "Get reference data (valid RowTypes, Positions, FieldTypes, etc.)")]
    public IActionResult GetReference()
    {
        EnsureAdminOrConsultant();
        var result = _adminAccountingService.GetReference();
        return Ok(result, MessageKeys.DataRetrievedSuccessfully);
    }

    [HttpGet("reference/formula-node-schemas")]
    [SwaggerOperation(Summary = "Get JSON schema for each formula node type (for guided form builder)")]
    public IActionResult GetFormulaNodeSchemas()
    {
        EnsureAdminOrConsultant();
        var result = _adminAccountingService.GetFormulaNodeSchemas();
        return Ok(result, MessageKeys.DataRetrievedSuccessfully);
    }

    // ── MappableEntities ──

    [HttpGet("mappable-entities")]
    [SwaggerOperation(Summary = "List all mappable entities")]
    public async Task<IActionResult> GetMappableEntities([FromQuery] bool? active)
    {
        EnsureAdminOrConsultant();
        var result = await _adminAccountingService.GetMappableEntitiesAsync(active);
        return Ok(result, MessageKeys.DataRetrievedSuccessfully);
    }

    [HttpGet("mappable-entities/{entityId:int}")]
    [SwaggerOperation(Summary = "Get mappable entity detail with fields")]
    public async Task<IActionResult> GetMappableEntityDetail(int entityId)
    {
        EnsureAdminOrConsultant();
        var result = await _adminAccountingService.GetMappableEntityDetailAsync(entityId);
        return Ok(result, MessageKeys.DataRetrievedSuccessfully);
    }

    [HttpPost("mappable-entities")]
    [SwaggerOperation(Summary = "Create mappable entity (Admin only)")]
    public async Task<IActionResult> CreateMappableEntity([FromBody] CreateMappableEntityRequest request)
    {
        EnsureAdminOnly();
        var result = await _adminAccountingService.CreateMappableEntityAsync(request, User.GetRequiredUserId());
        return Ok(result, MessageKeys.DataCreatedSuccessfully);
    }

    [HttpPatch("mappable-entities/{entityId:int}")]
    [SwaggerOperation(Summary = "Update mappable entity (Admin only)")]
    public async Task<IActionResult> UpdateMappableEntity(int entityId, [FromBody] UpdateMappableEntityRequest request)
    {
        EnsureAdminOnly();
        var result = await _adminAccountingService.UpdateMappableEntityAsync(entityId, request);
        return Ok(result, MessageKeys.DataUpdatedSuccessfully);
    }

    [HttpDelete("mappable-entities/{entityId:int}")]
    [SwaggerOperation(Summary = "Delete inactive mappable entity (Admin only)")]
    public async Task<IActionResult> DeleteMappableEntity(int entityId)
    {
        EnsureAdminOnly();
        await _adminAccountingService.DeleteMappableEntityAsync(entityId);
        return Ok(MessageKeys.DataDeletedSuccessfully);
    }

    // ── MappableFields ──

    [HttpPost("mappable-entities/{entityId:int}/fields")]
    [SwaggerOperation(Summary = "Create mappable field (Admin only)")]
    public async Task<IActionResult> CreateMappableField(int entityId, [FromBody] CreateMappableFieldRequest request)
    {
        EnsureAdminOnly();
        var result = await _adminAccountingService.CreateMappableFieldAsync(entityId, request);
        return Ok(result, MessageKeys.DataCreatedSuccessfully);
    }

    [HttpPatch("mappable-fields/{fieldId:int}")]
    [SwaggerOperation(Summary = "Update mappable field (Admin only)")]
    public async Task<IActionResult> UpdateMappableField(int fieldId, [FromBody] UpdateMappableFieldRequest request)
    {
        EnsureAdminOnly();
        var result = await _adminAccountingService.UpdateMappableFieldAsync(fieldId, request);
        return Ok(result, MessageKeys.DataUpdatedSuccessfully);
    }

    // ── RowDefinitions ──

    [HttpGet("template-versions/{templateVersionId:int}/row-definitions")]
    [SwaggerOperation(Summary = "List row definitions for a template version")]
    public async Task<IActionResult> GetRowDefinitions(int templateVersionId)
    {
        EnsureAdminOrConsultant();
        var result = await _adminAccountingService.GetRowDefinitionsAsync(templateVersionId);
        return Ok(result, MessageKeys.DataRetrievedSuccessfully);
    }

    [HttpPost("template-versions/{templateVersionId:int}/row-definitions")]
    [SwaggerOperation(Summary = "Create row definition")]
    public async Task<IActionResult> CreateRowDefinition(int templateVersionId, [FromBody] CreateRowDefinitionRequest request)
    {
        EnsureAdminOrConsultant();
        var result = await _adminAccountingService.CreateRowDefinitionAsync(templateVersionId, request);
        return Ok(result, MessageKeys.DataCreatedSuccessfully);
    }

    [HttpPatch("row-definitions/{rowDefId:int}")]
    [SwaggerOperation(Summary = "Update row definition")]
    public async Task<IActionResult> UpdateRowDefinition(int rowDefId, [FromBody] UpdateRowDefinitionRequest request)
    {
        EnsureAdminOrConsultant();
        var result = await _adminAccountingService.UpdateRowDefinitionAsync(rowDefId, request);
        return Ok(result, MessageKeys.DataUpdatedSuccessfully);
    }

    [HttpDelete("row-definitions/{rowDefId:int}")]
    [SwaggerOperation(Summary = "Delete row definition (Admin only)")]
    public async Task<IActionResult> DeleteRowDefinition(int rowDefId)
    {
        EnsureAdminOnly();
        await _adminAccountingService.DeleteRowDefinitionAsync(rowDefId);
        return Ok(MessageKeys.DataDeletedSuccessfully);
    }

    // ── FieldMappings create/delete ──

    [HttpPost("template-versions/{templateVersionId:int}/field-mappings")]
    [SwaggerOperation(Summary = "Create field mapping")]
    public async Task<IActionResult> CreateFieldMapping(int templateVersionId, [FromBody] CreateFieldMappingRequest request)
    {
        EnsureAdminOrConsultant();
        var result = await _adminAccountingService.CreateFieldMappingAsync(templateVersionId, request);
        return Ok(result, MessageKeys.DataCreatedSuccessfully);
    }

    [HttpDelete("field-mappings/{mappingId:int}")]
    [SwaggerOperation(Summary = "Delete field mapping (Admin only)")]
    public async Task<IActionResult> DeleteFieldMapping(int mappingId)
    {
        EnsureAdminOnly();
        await _adminAccountingService.DeleteFieldMappingAsync(mappingId);
        return Ok(MessageKeys.DataDeletedSuccessfully);
    }

    // ── Full structure ──

    [HttpGet("template-versions/{templateVersionId:int}/full-structure")]
    [SwaggerOperation(Summary = "Get full template structure (columns + rows + render preview)")]
    public async Task<IActionResult> GetFullStructure(int templateVersionId)
    {
        EnsureAdminOrConsultant();
        var result = await _adminAccountingService.GetFullStructureAsync(templateVersionId);
        return Ok(result, MessageKeys.DataRetrievedSuccessfully);
    }

    // ── BusinessTypes + IndustryTaxRates Admin ──

    [HttpGet("business-types")]
    [SwaggerOperation(Summary = "List all business types with their tax rates for a given ruleset")]
    public async Task<IActionResult> GetBusinessTypesWithRates([FromQuery] int rulesetId)
    {
        EnsureAdminOrConsultant();
        var result = await _adminAccountingService.GetBusinessTypesWithRatesAsync(rulesetId);
        return Ok(result, MessageKeys.DataRetrievedSuccessfully);
    }

    [HttpPost("business-types")]
    [SwaggerOperation(Summary = "Create a new business type (Admin only)")]
    public async Task<IActionResult> CreateBusinessType([FromBody] CreateBusinessTypeRequest request)
    {
        EnsureAdminOnly();
        var result = await _adminAccountingService.CreateBusinessTypeAsync(request, User.GetRequiredUserId());
        return Ok(result, MessageKeys.DataCreatedSuccessfully);
    }

    [HttpPatch("business-types/{businessTypeId:guid}")]
    [SwaggerOperation(Summary = "Update business type metadata (name, description, status)")]
    public async Task<IActionResult> UpdateBusinessType(Guid businessTypeId, [FromBody] UpdateBusinessTypeRequest request)
    {
        EnsureAdminOnly();
        var result = await _adminAccountingService.UpdateBusinessTypeAsync(businessTypeId, request, User.GetRequiredUserId());
        return Ok(result, MessageKeys.DataUpdatedSuccessfully);
    }

    [HttpDelete("business-types/{businessTypeId:guid}")]
    [SwaggerOperation(
        Summary = "Delete business type (Admin only)",
        Description = "Try hard-delete first; if constrained by DB references, fallback to inactive.")]
    public async Task<IActionResult> DeleteBusinessType(Guid businessTypeId)
    {
        EnsureAdminOnly();
        var isHardDeleted = await _adminAccountingService.DeleteBusinessTypeAsync(businessTypeId, User.GetRequiredUserId());
        return isHardDeleted
            ? Ok(MessageKeys.DataDeletedSuccessfully)
            : Ok(MessageKeys.DataUpdatedSuccessfully);
    }

    [HttpPut("rulesets/{rulesetId:int}/business-types/{businessTypeId:guid}/tax-rates")]
    [SwaggerOperation(Summary = "Full replace of IndustryTaxRates for a business type within a ruleset")]
    public async Task<IActionResult> UpsertIndustryTaxRates(int rulesetId, Guid businessTypeId, [FromBody] UpsertIndustryTaxRatesRequest request)
    {
        EnsureAdminOnly();
        var result = await _adminAccountingService.UpsertIndustryTaxRatesAsync(rulesetId, businessTypeId, request);
        return Ok(result, MessageKeys.DataUpdatedSuccessfully);
    }

    // ── TaxRuleset CRUD ──

    [HttpPost("rulesets")]
    [SwaggerOperation(Summary = "Create a new tax ruleset (Admin only)")]
    public async Task<IActionResult> CreateTaxRuleset([FromBody] CreateTaxRulesetRequest request)
    {
        EnsureAdminOnly();
        var result = await _adminAccountingService.CreateTaxRulesetAsync(request, User.GetRequiredUserId());
        return Ok(result, MessageKeys.DataCreatedSuccessfully);
    }

    [HttpPatch("rulesets/{rulesetId:int}")]
    [SwaggerOperation(Summary = "Update tax ruleset metadata (Admin only)")]
    public async Task<IActionResult> UpdateTaxRuleset(int rulesetId, [FromBody] UpdateTaxRulesetRequest request)
    {
        EnsureAdminOnly();
        var result = await _adminAccountingService.UpdateTaxRulesetAsync(rulesetId, request);
        return Ok(result, MessageKeys.DataUpdatedSuccessfully);
    }

    private void EnsureAdminOnly()
    {
        if (!User.HasRole("admin"))
            throw new ForbiddenException(MessageKeys.Forbidden);
    }

    private void EnsureAdminOrConsultant()
    {
        if (!User.HasRole("admin") && !User.HasRole("consultant"))
            throw new ForbiddenException(MessageKeys.Forbidden);
    }
}
