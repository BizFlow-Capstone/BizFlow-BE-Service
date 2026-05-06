namespace BizFlow.Application.DTOs.Ai
{
    // ── Draft Order ──────────────────────────────────────────────

    public class AiDraftOrderItemDto
    {
        public string? ProductId { get; set; }
        public string? SaleItemId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public bool Matched { get; set; }
        public double Quantity { get; set; }
        public string Unit { get; set; } = string.Empty;
        public double? UnitPrice { get; set; }
        public double? LineTotal { get; set; }
        public string? CustomerName { get; set; }
        public bool IsDebt { get; set; }
    }

    public class AiDraftOrderResultDto
    {
        public List<AiDraftOrderItemDto> Items { get; set; } = new();
        public string RawTranscript { get; set; } = string.Empty;
        public string Confidence { get; set; } = string.Empty;
        public double? TotalAmount { get; set; }
    }

    // ── OCR Purchase Invoice (hóa đơn nhập hàng) ──────────────────────────

    public class AiPurchaseInvoiceItemDto
    {
        public string ProductName { get; set; } = string.Empty;
        public double Quantity { get; set; }
        public string Unit { get; set; } = string.Empty;
        public double UnitPrice { get; set; }
    }

    public class AiPurchaseInvoiceResultDto
    {
        public string? SupplierName { get; set; }
        public string? InvoiceDate { get; set; }
        public List<AiPurchaseInvoiceItemDto> Items { get; set; } = new();
        public double? TotalAmount { get; set; }
        public string Confidence { get; set; } = string.Empty;
    }

    // ── OCR Sale Invoice (hóa đơn bán hàng) ─────────────────────────────────

    public class AiSaleInvoiceItemDto
    {
        public string ProductName { get; set; } = string.Empty;
        public double Quantity { get; set; }
        public string Unit { get; set; } = string.Empty;
        public double UnitPrice { get; set; }
    }

    public class AiSaleInvoiceResultDto
    {
        public string? BuyerName { get; set; }
        public string? InvoiceNumber { get; set; }
        public string? InvoiceDate { get; set; }
        public List<AiSaleInvoiceItemDto> Items { get; set; } = new();
        public double? VatAmount { get; set; }
        public double? TotalAmount { get; set; }
        public string Confidence { get; set; } = string.Empty;
    }

    // ── Draft Revenue (doanh thu từ giọng nói) ───────────────────────────────

    public class AiDraftRevenueItemDto
    {
        public double? Amount { get; set; }
        public string? Description { get; set; }
        public string? RevenueDate { get; set; }   // YYYY-MM-DD; null = today
        public string? MoneyChannel { get; set; }  // "cash" | "bank"
    }

    public class AiDraftRevenueResultDto
    {
        public List<AiDraftRevenueItemDto> Items { get; set; } = new();
        public string RawTranscript { get; set; } = string.Empty;
        public string Confidence { get; set; } = string.Empty;
    }

    // ── Draft Cost (chi phí từ giọng nói) ────────────────────────────────────

    public class AiDraftCostItemDto
    {
        public double? Amount { get; set; }
        public string? Description { get; set; }
        public string? CostDate { get; set; }       // YYYY-MM-DD; null = today
        public string? CostType { get; set; }
        public string? PaymentMethod { get; set; }  // "cash" | "bank"
    }

    public class AiDraftCostResultDto
    {
        public List<AiDraftCostItemDto> Items { get; set; } = new();
        public string RawTranscript { get; set; } = string.Empty;
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

    public class AiVectorStoreBackfillResultDto
    {
        public string LocationId { get; set; } = string.Empty;
        public int Synced { get; set; }
        public int Skipped { get; set; }
    }

    // ── Dashboard Read DTOs ──────────────────────────────────────

    public class AiForecastItemDto
    {
        public string ForecastDate { get; set; } = string.Empty;
        public double PredictedRevenue { get; set; }
        public double LowerBound { get; set; }
        public double UpperBound { get; set; }
        public string? TrendNote { get; set; }
        public DateTime GeneratedAt { get; set; }
    }

    public class AiForecastReadDto
    {
        public List<AiForecastItemDto> Forecasts { get; set; } = new();
    }

    public class AiAnomalyAlertReadDto
    {
        public string Id { get; set; } = string.Empty;
        public string AlertType { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Tier { get; set; } = string.Empty;
        public DateTime ReferenceDate { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? ReferenceId { get; set; }
        public string? RecordType { get; set; }
        public bool IsAcknowledged { get; set; }
        public DateTime GeneratedAt { get; set; }
    }

    public class AiReorderItemReadDto
    {
        public string ProductId { get; set; } = string.Empty;
        public double CurrentStock { get; set; }
        public int DaysUntilStockout { get; set; }
        public double SuggestedQuantity { get; set; }
        public double AvgDailySales { get; set; }
        public string Urgency { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; }
    }

    public class AiProductInsightReadDto
    {
        public string ProductId { get; set; } = string.Empty;
        public string InsightType { get; set; } = string.Empty;
        public int Rank { get; set; }
        public double MetricValue { get; set; }
        public int PeriodDays { get; set; }
        public DateTime GeneratedAt { get; set; }
    }
}
