using BizFlow.Api.Common.Controllers;
using BizFlow.Api.Common.Extensions;
using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.DTOs.Ai;
using BizFlow.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace BizFlow.Api.Controllers.Ai
{
    [Route("api/my-business/ai")]
    [Authorize]
    public class AiController : BaseApiController
    {
        private readonly IAiServiceClient _aiServiceClient;
        private readonly IBusinessLocationService _locationService;
        private readonly IAiDashboardService _aiDashboardService;

        public AiController(
            IAiServiceClient aiServiceClient,
            IBusinessLocationService locationService,
            IAiDashboardService aiDashboardService,
            IMessageService messageService,
            ILogger<AiController> logger)
            : base(messageService, logger)
        {
            _aiServiceClient = aiServiceClient;
            _locationService = locationService;
            _aiDashboardService = aiDashboardService;
        }

        [HttpPost("draft-order")]
        [SwaggerOperation(
            Summary = "Tạo draft order từ giọng nói",
            Description = "Upload audio file → STT → product matching → trả về draft order để user review. Xử lý ~5-8 giây.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> ParseDraftOrder(IFormFile audio, [FromForm] int locationId)
        {
            if (audio == null || audio.Length == 0)
                return BadRequest(MessageKeys.ValidationError);

            var userId = GetCurrentUserId();
            await _locationService.ValidateLocationAccessAsync(userId, locationId);

            using var stream = audio.OpenReadStream();
            var result = await _aiServiceClient.ParseDraftOrderAsync(
                stream, audio.ContentType ?? "audio/webm", locationId);

            return Ok(result, MessageKeys.DataRetrievedSuccessfully);
        }

        [HttpPost("draft-revenue")]
        [SwaggerOperation(
            Summary = "Tạo draft doanh thu từ giọng nói",
            Description = "Upload audio file → STT → trả về draft doanh thu (số tiền, mô tả, ngày, kênh thanh toán) để user review. Ví dụ: 'Hôm nay bán áo thun 3 triệu rưỡi tiền mặt'. Xử lý ~3-6 giây.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> ParseDraftRevenue(IFormFile audio, [FromForm] int locationId)
        {
            if (audio == null || audio.Length == 0)
                return BadRequest(MessageKeys.ValidationError);

            var userId = GetCurrentUserId();
            await _locationService.ValidateLocationAccessAsync(userId, locationId);

            using var stream = audio.OpenReadStream();
            var result = await _aiServiceClient.ParseDraftRevenueAsync(
                stream, audio.ContentType ?? "audio/webm", locationId);

            return Ok(result, MessageKeys.DataRetrievedSuccessfully);
        }

        [HttpPost("draft-cost")]
        [SwaggerOperation(
            Summary = "Tạo draft chi phí từ giọng nói",
            Description = "Upload audio file → STT → trả về draft chi phí (số tiền, mô tả, loại chi phí, ngày, phương thức thanh toán) để user review. Ví dụ: 'Chi tiền điện tháng này 1 triệu 2'. Xử lý ~3-6 giây.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> ParseDraftCost(IFormFile audio, [FromForm] int locationId)
        {
            if (audio == null || audio.Length == 0)
                return BadRequest(MessageKeys.ValidationError);

            var userId = GetCurrentUserId();
            await _locationService.ValidateLocationAccessAsync(userId, locationId);

            using var stream = audio.OpenReadStream();
            var result = await _aiServiceClient.ParseDraftCostAsync(
                stream, audio.ContentType ?? "audio/webm", locationId);

            return Ok(result, MessageKeys.DataRetrievedSuccessfully);
        }

        [HttpPost("ocr/purchase-invoice")]
        [SwaggerOperation(
            Summary = "OCR hóa đơn nhập hàng",
            Description = "Upload ảnh hóa đơn nhập hàng (hóa đơn đỏ từ NCC hoặc phiếu mua hàng 01/TNDN) → GPT-4o Vision trích xuất tên NCC, ngày, SP/SL/đơn giá/tổng. Kết quả draft, không tự lưu.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> OcrPurchaseInvoice(IFormFile image, [FromForm] int locationId)
        {
            if (image == null || image.Length == 0)
                return BadRequest(MessageKeys.ValidationError);

            var userId = GetCurrentUserId();
            await _locationService.ValidateLocationAccessAsync(userId, locationId);

            using var stream = image.OpenReadStream();
            var result = await _aiServiceClient.OcrPurchaseInvoiceAsync(
                stream, image.ContentType ?? "image/jpeg", locationId);

            return Ok(result, MessageKeys.DataRetrievedSuccessfully);
        }

        [HttpPost("ocr/sale-invoice")]
        [SwaggerOperation(
            Summary = "OCR hóa đơn bán hàng",
            Description = "Upload ảnh hóa đơn bán hàng do shop phát hành (hóa đơn đỏ khi bán) → GPT-4o Vision trích xuất tên người mua, số HĐ, ngày, SP/SL/đơn giá, VAT, tổng. Kết quả draft, không tự lưu.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> OcrSaleInvoice(IFormFile image, [FromForm] int locationId)
        {
            if (image == null || image.Length == 0)
                return BadRequest(MessageKeys.ValidationError);

            var userId = GetCurrentUserId();
            await _locationService.ValidateLocationAccessAsync(userId, locationId);

            using var stream = image.OpenReadStream();
            var result = await _aiServiceClient.OcrSaleInvoiceAsync(
                stream, image.ContentType ?? "image/jpeg", locationId);

            return Ok(result, MessageKeys.DataRetrievedSuccessfully);
        }

        // ── Dashboard Read Endpoints ─────────────────────────────────

        [HttpGet("forecast")]
        [SwaggerOperation(
            Summary = "Đọc dự báo doanh thu",
            Description = "Trả về dự báo 7 ngày tới (pre-computed bởi nightly job). Không gọi AI realtime.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetForecast([FromQuery] int locationId)
        {
            var userId = GetCurrentUserId();
            await _locationService.ValidateLocationAccessAsync(userId, locationId);

            var dto = await _aiDashboardService.GetForecastAsync(locationId);
            return Ok(dto, MessageKeys.DataRetrievedSuccessfully);
        }

        [HttpGet("reorder")]
        [SwaggerOperation(
            Summary = "Đọc gợi ý nhập hàng",
            Description = "Trả về danh sách sản phẩm cần nhập thêm, sắp xếp theo mức độ khẩn cấp. Pre-computed nightly.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetReorderSuggestions([FromQuery] int locationId)
        {
            var userId = GetCurrentUserId();
            await _locationService.ValidateLocationAccessAsync(userId, locationId);

            var dto = await _aiDashboardService.GetReorderSuggestionsAsync(locationId);
            return Ok(dto, MessageKeys.DataRetrievedSuccessfully);
        }

        [HttpGet("insights")]
        [SwaggerOperation(
            Summary = "Đọc phân tích hiệu suất sản phẩm",
            Description = "Top sellers, growth trends, promote candidates. Pre-computed nightly.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetProductInsights([FromQuery] int locationId)
        {
            var userId = GetCurrentUserId();
            await _locationService.ValidateLocationAccessAsync(userId, locationId);

            var dto = await _aiDashboardService.GetProductInsightsAsync(locationId);
            return Ok(dto, MessageKeys.DataRetrievedSuccessfully);
        }

        [HttpGet("anomalies")]
        [SwaggerOperation(
            Summary = "Đọc cảnh báo bất thường",
            Description = "Trả về danh sách cảnh báo. Dùng acknowledged=false để lọc chưa xác nhận.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAnomalies([FromQuery] int locationId, [FromQuery] bool? acknowledged = null)
        {
            var userId = GetCurrentUserId();
            await _locationService.ValidateLocationAccessAsync(userId, locationId);

            var dto = await _aiDashboardService.GetAnomaliesAsync(locationId, acknowledged);
            return Ok(dto, MessageKeys.DataRetrievedSuccessfully);
        }

        [HttpPost("anomalies/{id}/acknowledge")]
        [SwaggerOperation(
            Summary = "Xác nhận đã xem cảnh báo",
            Description = "Đánh dấu cảnh báo đã được chủ shop xem/dismiss.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> AcknowledgeAnomaly(string id, [FromQuery] int locationId)
        {
            var userId = GetCurrentUserId();
            await _locationService.ValidateLocationAccessAsync(userId, locationId);

            await _aiDashboardService.AcknowledgeAnomalyAsync(id, locationId);
            return Ok(MessageKeys.DataUpdatedSuccessfully);
        }

        [HttpPost("vector-store/backfill")]
        [SwaggerOperation(
            Summary = "Backfill toàn bộ sản phẩm của location vào ChromaDB",
            Description = "Đồng bộ tất cả sản phẩm Active của location vào vector store. Idempotent — an toàn để gọi lại nhiều lần. Dùng cho những sản phẩm tạo trước khi tích hợp AI.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> BackfillVectorStore([FromQuery] int locationId, CancellationToken ct)
        {
            var userId = GetCurrentUserId();
            await _locationService.ValidateLocationAccessAsync(userId, locationId);

            var result = await _aiServiceClient.TriggerVectorStoreBackfillAsync(locationId, ct);
            return Ok(result, MessageKeys.DataUpdatedSuccessfully);
        }

        private Guid GetCurrentUserId() => User.GetRequiredUserId();
    }
}
