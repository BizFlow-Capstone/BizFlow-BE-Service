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
