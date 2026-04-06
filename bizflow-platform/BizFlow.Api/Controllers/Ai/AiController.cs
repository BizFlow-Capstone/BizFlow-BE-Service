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

        public AiController(
            IAiServiceClient aiServiceClient,
            IBusinessLocationService locationService,
            IMessageService messageService,
            ILogger<AiController> logger)
            : base(messageService, logger)
        {
            _aiServiceClient = aiServiceClient;
            _locationService = locationService;
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

        private Guid GetCurrentUserId() => User.GetRequiredUserId();
    }
}
