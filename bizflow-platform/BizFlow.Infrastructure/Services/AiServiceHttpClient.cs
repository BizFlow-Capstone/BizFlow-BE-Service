using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.Ai;
using BizFlow.Application.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BizFlow.Infrastructure.Services
{
    public class AiServiceHttpClient : IAiServiceClient
    {
        private readonly HttpClient _httpClient;
        private readonly AiServiceSettings _settings;
        private readonly ILogger<AiServiceHttpClient> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public AiServiceHttpClient(
            IHttpClientFactory httpClientFactory,
            IOptions<AiServiceSettings> settings,
            ILogger<AiServiceHttpClient> logger)
        {
            _settings = settings.Value;
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient("AiService");
        }

        // ── User-facing (sync) ───────────────────────────────────

        public async Task<AiDraftOrderResultDto> ParseDraftOrderAsync(
            Stream audioStream, string mimeType, int locationId, CancellationToken ct = default)
        {
            using var content = new MultipartFormDataContent();
            content.Add(new StreamContent(audioStream), "audio", "audio.webm");
            content.Add(new StringContent(locationId.ToString()), "location_id");

            var response = await SendAsync(HttpMethod.Post, "/draft-order", content, ct);
            return await DeserializeAsync<AiDraftOrderResultDto>(response, ct);
        }

        public async Task<AiInvoiceResultDto> OcrInvoiceAsync(
            Stream imageStream, string mimeType, int locationId, CancellationToken ct = default)
        {
            using var content = new MultipartFormDataContent();
            var streamContent = new StreamContent(imageStream);
            streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(mimeType);
            content.Add(streamContent, "image", "image.jpg");
            content.Add(new StringContent(locationId.ToString()), "location_id");

            var response = await SendAsync(HttpMethod.Post, "/ocr/invoice", content, ct);
            return await DeserializeAsync<AiInvoiceResultDto>(response, ct);
        }

        public async Task<AiDeliveryNoteResultDto> OcrDeliveryNoteAsync(
            Stream imageStream, string mimeType, int locationId, CancellationToken ct = default)
        {
            using var content = new MultipartFormDataContent();
            var streamContent = new StreamContent(imageStream);
            streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(mimeType);
            content.Add(streamContent, "image", "image.jpg");
            content.Add(new StringContent(locationId.ToString()), "location_id");

            var response = await SendAsync(HttpMethod.Post, "/ocr/delivery-note", content, ct);
            return await DeserializeAsync<AiDeliveryNoteResultDto>(response, ct);
        }

        // ── Tier 1 anomaly ───────────────────────────────────────

        public async Task<AiCheckRecordResultDto> CheckAnomalyAsync(
            int locationId, string recordType, long recordId, CancellationToken ct = default)
        {
            var body = new
            {
                location_id = locationId.ToString(),
                record_type = recordType,
                record_id = recordId.ToString()
            };

            var response = await SendJsonAsync(HttpMethod.Post, "/anomaly/check-record", body, ct);
            return await DeserializeAsync<AiCheckRecordResultDto>(response, ct);
        }

        // ── Vector store sync ────────────────────────────────────

        public async Task TriggerVectorStoreSyncAsync(
            int locationId, long productId, string name, string unit, string? category, CancellationToken ct = default)
        {
            var body = new
            {
                location_id = locationId.ToString(),
                product_id = productId.ToString(),
                name,
                unit,
                category
            };

            await SendJsonAsync(HttpMethod.Post, "/vector-store/sync", body, ct);
        }

        public async Task TriggerVectorStoreDeleteAsync(
            int locationId, long productId, CancellationToken ct = default)
        {
            var body = new
            {
                location_id = locationId.ToString(),
                product_id = productId.ToString()
            };

            await SendJsonAsync(HttpMethod.Post, "/vector-store/delete", body, ct);
        }

        // ── Nightly batch jobs ───────────────────────────────────

        public async Task<AiBatchJobResultDto> TriggerForecastAsync(
            List<int> locationIds, CancellationToken ct = default)
        {
            var body = new { location_ids = locationIds.Select(id => id.ToString()).ToList() };
            var response = await SendJsonAsync(HttpMethod.Post, "/forecast", body, ct);
            return await DeserializeAsync<AiBatchJobResultDto>(response, ct);
        }

        public async Task<AiBatchJobResultDto> TriggerAnomalyPatternAsync(
            List<int> locationIds, CancellationToken ct = default)
        {
            var body = new { location_ids = locationIds.Select(id => id.ToString()).ToList() };
            var response = await SendJsonAsync(HttpMethod.Post, "/anomaly", body, ct);
            return await DeserializeAsync<AiBatchJobResultDto>(response, ct);
        }

        public async Task<AiBatchJobResultDto> TriggerReorderAsync(
            List<int> locationIds, CancellationToken ct = default)
        {
            var body = new { location_ids = locationIds.Select(id => id.ToString()).ToList() };
            var response = await SendJsonAsync(HttpMethod.Post, "/reorder", body, ct);
            return await DeserializeAsync<AiBatchJobResultDto>(response, ct);
        }

        public async Task<AiBatchJobResultDto> TriggerProductInsightsAsync(
            List<int> locationIds, CancellationToken ct = default)
        {
            var body = new { location_ids = locationIds.Select(id => id.ToString()).ToList() };
            var response = await SendJsonAsync(HttpMethod.Post, "/product-insights", body, ct);
            return await DeserializeAsync<AiBatchJobResultDto>(response, ct);
        }

        // ── Internal helpers ─────────────────────────────────────

        private async Task<HttpResponseMessage> SendAsync(
            HttpMethod method, string path, HttpContent content, CancellationToken ct)
        {
            using var request = new HttpRequestMessage(method, path);
            request.Headers.Add("X-Internal-Secret", _settings.InternalSecret);
            request.Content = content;

            var response = await _httpClient.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();
            return response;
        }

        private async Task<HttpResponseMessage> SendJsonAsync<T>(
            HttpMethod method, string path, T body, CancellationToken ct)
        {
            using var request = new HttpRequestMessage(method, path);
            request.Headers.Add("X-Internal-Secret", _settings.InternalSecret);
            request.Content = JsonContent.Create(body, options: JsonOptions);

            var response = await _httpClient.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();
            return response;
        }

        private static async Task<TResult> DeserializeAsync<TResult>(
            HttpResponseMessage response, CancellationToken ct)
            where TResult : new()
        {
            return await response.Content.ReadFromJsonAsync<TResult>(JsonOptions, ct) ?? new TResult();
        }
    }
}
