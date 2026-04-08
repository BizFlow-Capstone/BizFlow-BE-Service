namespace BizFlow.Application.DTOs.Debtor
{
    public class DebtorMinimalDto
    {
        public long DebtorId { get; set; }
        public string Name { get; set; } = null!;
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public decimal? CreditLimit { get; set; }
        public decimal CurrentBalance { get; set; }
    }
}
