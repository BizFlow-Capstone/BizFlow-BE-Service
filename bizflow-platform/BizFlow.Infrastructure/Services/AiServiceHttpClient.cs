using System.Net.Http.Json;
using System.Net.Http.Headers;
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
            var normalizedMimeType = NormalizeMimeType(mimeType);
            var streamContent = new StreamContent(audioStream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(normalizedMimeType);
            content.Add(streamContent, "audio", BuildAudioFileName(normalizedMimeType));
            content.Add(new StringContent(locationId.ToString()), "location_id");

            var response = await SendAsync(HttpMethod.Post, "/draft-order", content, ct);
            return await DeserializeAsync<AiDraftOrderResultDto>(response, ct);
        }

        public async Task<AiDraftRevenueResultDto> ParseDraftRevenueAsync(
            Stream audioStream, string mimeType, int locationId, CancellationToken ct = default)
        {
            using var content = new MultipartFormDataContent();
            var normalizedMimeType = NormalizeMimeType(mimeType);
            var streamContent = new StreamContent(audioStream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(normalizedMimeType);
            content.Add(streamContent, "audio", BuildAudioFileName(normalizedMimeType));
            content.Add(new StringContent(locationId.ToString()), "location_id");

            var response = await SendAsync(HttpMethod.Post, "/draft-revenue", content, ct);
            return await DeserializeAsync<AiDraftRevenueResultDto>(response, ct);
        }

        public async Task<AiDraftCostResultDto> ParseDraftCostAsync(
            Stream audioStream, string mimeType, int locationId, CancellationToken ct = default)
        {
            using var content = new MultipartFormDataContent();
            var normalizedMimeType = NormalizeMimeType(mimeType);
            var streamContent = new StreamContent(audioStream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(normalizedMimeType);
            content.Add(streamContent, "audio", BuildAudioFileName(normalizedMimeType));
            content.Add(new StringContent(locationId.ToString()), "location_id");

            var response = await SendAsync(HttpMethod.Post, "/draft-cost", content, ct);
            return await DeserializeAsync<AiDraftCostResultDto>(response, ct);
        }

        public async Task<AiPurchaseInvoiceResultDto> OcrPurchaseInvoiceAsync(
            Stream imageStream, string mimeType, int locationId, CancellationToken ct = default)
        {
            using var content = new MultipartFormDataContent();
            var streamContent = new StreamContent(imageStream);
            streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(mimeType);
            content.Add(streamContent, "image", "image.jpg");
            content.Add(new StringContent(locationId.ToString()), "location_id");

            var response = await SendAsync(HttpMethod.Post, "/ocr/purchase-invoice", content, ct);
            return await DeserializeAsync<AiPurchaseInvoiceResultDto>(response, ct);
        }

        public async Task<AiSaleInvoiceResultDto> OcrSaleInvoiceAsync(
            Stream imageStream, string mimeType, int locationId, CancellationToken ct = default)
        {
            using var content = new MultipartFormDataContent();
            var streamContent = new StreamContent(imageStream);
            streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(mimeType);
            content.Add(streamContent, "image", "image.jpg");
            content.Add(new StringContent(locationId.ToString()), "location_id");

            var response = await SendAsync(HttpMethod.Post, "/ocr/sale-invoice", content, ct);
            return await DeserializeAsync<AiSaleInvoiceResultDto>(response, ct);
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

        private static string NormalizeMimeType(string? mimeType)
        {
            if (string.IsNullOrWhiteSpace(mimeType))
            {
                return "audio/webm";
            }

            // Strip codec params: "audio/webm; codecs=opus" → "audio/webm"
            var normalized = mimeType.Trim().ToLowerInvariant();
            var semicolonIdx = normalized.IndexOf(';');
            if (semicolonIdx >= 0)
                normalized = normalized[..semicolonIdx].TrimEnd();

            return normalized switch
            {
                "audio/x-m4a" => "audio/m4a",
                "audio/mp4" => "audio/m4a",
                "audio/aac" => "audio/m4a",
                _ => normalized,
            };
        }

        private static string BuildAudioFileName(string mimeType)
        {
            return mimeType switch
            {
                "audio/m4a" => "audio.m4a",
                "audio/mp3" => "audio.mp3",
                "audio/wav" => "audio.wav",
                "audio/ogg" => "audio.ogg",
                _ => "audio.webm",
            };
        }
    }
}
