namespace BizFlow.Application.DTOs.GeneralLedger
{
    /// <summary>
    /// Tổng doanh thu và tổng chi phí lấy trực tiếp từ sổ cái (theo <c>reference_type</c> revenue / cost).
    /// </summary>
    public class GeneralLedgerTotalsDto
    {
        public decimal TotalRevenue { get; set; }
        public decimal TotalCost { get; set; }
    }
}
