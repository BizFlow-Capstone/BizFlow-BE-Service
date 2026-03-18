namespace BizFlow.Application.DTOs.Debtor
{
    public class DebtorSummaryDto
    {
        public long DebtorId { get; set; }
        public int BusinessLocationId { get; set; }
        public string Name { get; set; } = null!;
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? Notes { get; set; }
        public decimal? CreditLimit { get; set; }
        public decimal CurrentBalance { get; set; }
        public bool IsActive { get; set; }
    }

    public class DebtorDetailDto : DebtorSummaryDto
    {
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        // Inherits all properties from DebtorSummaryDto
    }
}
