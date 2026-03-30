namespace BizFlow.Application.DTOs.Cost
{
    public class CostDto
    {
        public long CostId { get; set; }
        public int BusinessLocationId { get; set; }
        public string CostType { get; set; } = null!;
        public string Description { get; set; } = null!;
        public decimal Amount { get; set; }
        public DateOnly CostDate { get; set; }
        public string? PaymentMethod { get; set; }
        public string? DocumentUrl { get; set; }
        public string? DocumentPublicId { get; set; }
        public string? DocumentNumber { get; set; }
        public DateOnly? DocumentDate { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
