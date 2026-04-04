namespace BizFlow.Application.DTOs.Ai
{
    // ── Draft Order ──────────────────────────────────────────────

    public class AiDraftOrderItemDto
    {
        public string? ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public bool Matched { get; set; }
        public double Quantity { get; set; }
        public string Unit { get; set; } = string.Empty;
        public string? CustomerName { get; set; }
        public bool IsDebt { get; set; }
    }

    public class AiDraftOrderResultDto
    {
        public List<AiDraftOrderItemDto> Items { get; set; } = new();
        public string RawTranscript { get; set; } = string.Empty;
        public string Confidence { get; set; } = string.Empty;
    }

    // ── OCR Invoice ──────────────────────────────────────────────

    public class AiInvoiceItemDto
    {
        public string ProductName { get; set; } = string.Empty;
        public double Quantity { get; set; }
        public string Unit { get; set; } = string.Empty;
        public double UnitPrice { get; set; }
    }

    public class AiInvoiceResultDto
    {
        public string? SupplierName { get; set; }
        public string? InvoiceDate { get; set; }
        public List<AiInvoiceItemDto> Items { get; set; } = new();
        public double? TotalAmount { get; set; }
        public string Confidence { get; set; } = string.Empty;
    }

    // ── OCR Delivery Note ────────────────────────────────────────

    public class AiDeliveryItemDto
    {
        public string ProductName { get; set; } = string.Empty;
        public double Quantity { get; set; }
        public string Unit { get; set; } = string.Empty;
    }

    public class AiDeliveryNoteResultDto
    {
        public List<AiDeliveryItemDto> Items { get; set; } = new();
        public string Confidence { get; set; } = string.Empty;
    }

    // ── Anomaly Check ────────────────────────────────────────────

    public class AiAlertDetailDto
    {
        public string AlertType { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class AiCheckRecordResultDto
    {
        public int AlertsCreated { get; set; }
        public bool HasCritical { get; set; }
        public List<AiAlertDetailDto> Alerts { get; set; } = new();
    }

    // ── Vector Store ─────────────────────────────────────────────

    public class AiVectorSyncRequest
    {
        public string LocationId { get; set; } = string.Empty;
        public string ProductId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public string? Category { get; set; }
    }

    // ── Batch Job Results ────────────────────────────────────────

    public class AiBatchJobResultDto
    {
        public int Processed { get; set; }
        public int Skipped { get; set; }
    }
}
