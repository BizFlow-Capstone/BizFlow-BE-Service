namespace BizFlow.Application.DTOs.Revenue
{
    public class RevenueDto
    {
        public long RevenueId { get; set; }
        public int BusinessLocationId { get; set; }
        public string RevenueType { get; set; } = null!;
        public decimal Amount { get; set; }
        public DateOnly RevenueDate { get; set; }
        public string Description { get; set; } = null!;
        public string? MoneyChannel { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
