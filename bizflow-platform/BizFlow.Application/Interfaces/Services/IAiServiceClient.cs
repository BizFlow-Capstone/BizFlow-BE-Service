using BizFlow.Application.DTOs.Ai;

namespace BizFlow.Application.Interfaces.Services
{
    public interface IAiServiceClient
    {
        // ── User-facing (sync) ───────────────────────────────────
        Task<AiDraftOrderResultDto> ParseDraftOrderAsync(Stream audioStream, string mimeType, int locationId, CancellationToken ct = default);
        Task<AiInvoiceResultDto> OcrInvoiceAsync(Stream imageStream, string mimeType, int locationId, CancellationToken ct = default);
        Task<AiDeliveryNoteResultDto> OcrDeliveryNoteAsync(Stream imageStream, string mimeType, int locationId, CancellationToken ct = default);

        // ── Tier 1 anomaly (fire-and-forget via Hangfire) ────────
        Task<AiCheckRecordResultDto> CheckAnomalyAsync(int locationId, string recordType, long recordId, CancellationToken ct = default);

        // ── Vector store sync (fire-and-forget) ──────────────────
        Task TriggerVectorStoreSyncAsync(int locationId, long productId, string name, string unit, string? category, CancellationToken ct = default);
        Task TriggerVectorStoreDeleteAsync(int locationId, long productId, CancellationToken ct = default);

        // ── Nightly batch jobs ───────────────────────────────────
        Task<AiBatchJobResultDto> TriggerForecastAsync(List<int> locationIds, CancellationToken ct = default);
        Task<AiBatchJobResultDto> TriggerAnomalyPatternAsync(List<int> locationIds, CancellationToken ct = default);
        Task<AiBatchJobResultDto> TriggerReorderAsync(List<int> locationIds, CancellationToken ct = default);
        Task<AiBatchJobResultDto> TriggerProductInsightsAsync(List<int> locationIds, CancellationToken ct = default);
    }
}
