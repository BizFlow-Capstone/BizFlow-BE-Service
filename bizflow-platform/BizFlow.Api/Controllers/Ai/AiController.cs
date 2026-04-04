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

        [HttpPost("ocr/invoice")]
        [SwaggerOperation(
            Summary = "OCR hóa đơn nhập hàng",
            Description = "Upload ảnh hóa đơn → GPT-4o Vision trích xuất SP, SL, đơn giá. Kết quả draft, không tự lưu.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> OcrInvoice(IFormFile image, [FromForm] int locationId)
        {
            if (image == null || image.Length == 0)
                return BadRequest(MessageKeys.ValidationError);

            var userId = GetCurrentUserId();
            await _locationService.ValidateLocationAccessAsync(userId, locationId);

            using var stream = image.OpenReadStream();
            var result = await _aiServiceClient.OcrInvoiceAsync(
                stream, image.ContentType ?? "image/jpeg", locationId);

            return Ok(result, MessageKeys.DataRetrievedSuccessfully);
        }

        [HttpPost("ocr/delivery-note")]
        [SwaggerOperation(
            Summary = "OCR phiếu giao hàng",
            Description = "Upload ảnh phiếu giao hàng → GPT-4o Vision trích xuất SP, SL. Kết quả draft, không tự lưu.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> OcrDeliveryNote(IFormFile image, [FromForm] int locationId)
        {
            if (image == null || image.Length == 0)
                return BadRequest(MessageKeys.ValidationError);

            var userId = GetCurrentUserId();
            await _locationService.ValidateLocationAccessAsync(userId, locationId);

            using var stream = image.OpenReadStream();
            var result = await _aiServiceClient.OcrDeliveryNoteAsync(
                stream, image.ContentType ?? "image/jpeg", locationId);

            return Ok(result, MessageKeys.DataRetrievedSuccessfully);
        }

        private Guid GetCurrentUserId() => User.GetRequiredUserId();
    }
}
