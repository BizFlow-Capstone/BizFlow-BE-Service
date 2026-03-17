namespace BizFlow.Application.DTOs.Debtor
{
    public class DebtorQueryParams
    {
        public List<int>? BusinessLocationIds { get; set; }
        public string? Search { get; set; }
        public bool? IsActive { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
