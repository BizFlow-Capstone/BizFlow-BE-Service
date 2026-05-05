using BizFlow.Api.Common.Controllers;
using BizFlow.Api.Common.Extensions;
using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.GeneralLedger;
using BizFlow.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Annotations;

namespace BizFlow.Api.Controllers.GeneralLedger
{
    [Route("api/my-business/accounting")]
    [Authorize]
    public class GeneralLedgerController : PaginatedApiController
    {
        private readonly IGeneralLedgerService _generalLedgerService;

        public GeneralLedgerController(
            IGeneralLedgerService generalLedgerService,
            IMessageService messageService,
            IOptions<PaginationSettings> paginationSettings,
            ILogger<GeneralLedgerController> logger)
            : base(messageService, logger, paginationSettings)
        {
            _generalLedgerService = generalLedgerService;
        }

        [HttpGet("gl-entries")]
        [SwaggerOperation(
            Summary = "List general ledger entries",
            Description = "Returns paginated GL entries for a business location. " +
                          "Use viewMode=audit to view immutable timeline (includes reversal rows). " +
                          "Use viewMode=effective to view effective entries as-of ToDate: excludes reversal rows " +
                          "and excludes originals only when their reversal entry date is <= ToDate. " +
                          "GL list is period-independent (no AccountingPeriod filtering is applied here).")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetEntries([FromQuery] GeneralLedgerQueryParams query)
        {
            ApplyPaginationDefaults(query);

            var userId = GetCurrentUserId();
            var result = await _generalLedgerService.ListAsync(userId, query);
            return OkPaginated(result, MessageKeys.DataRetrievedSuccessfully);
        }

        [HttpGet("gl-totals")]
        [SwaggerOperation(
            Summary = "Tổng doanh thu và tổng chi phí (sổ cái)",
            Description = "Cộng dồn chỉ từ general_ledger_entry: doanh thu theo reference_type=revenue (net Nợ−Có), " +
                          "chi phí theo reference_type=cost (net Có−Nợ). Không đọc bảng revenues/costs. " +
                          "Cùng tham số lọc như gl-entries (viewMode, khoảng ngày, transactionTypes, …).")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetTotals([FromQuery] GeneralLedgerQueryParams query)
        {
            var userId = GetCurrentUserId();
            var result = await _generalLedgerService.GetTotalsAsync(userId, query);
            return Ok(result, MessageKeys.DataRetrievedSuccessfully);
        }

        private Guid GetCurrentUserId() => User.GetRequiredUserId();
    }
}
