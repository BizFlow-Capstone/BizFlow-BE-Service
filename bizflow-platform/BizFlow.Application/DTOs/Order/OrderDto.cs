using BizFlow.Application.DTOs.Reference;

namespace BizFlow.Application.DTOs.Order
{
    public class OrderDto
    {
        public long OrderId { get; set; }
        public string OrderCode { get; set; } = null!;
        public long? RefOrderId { get; set; }
        public long? DebtorId { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public decimal SubTotal { get; set; }
        public decimal Discount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal CashAmount { get; set; }
        public decimal BankAmount { get; set; }
        public decimal DebtAmount { get; set; }
        /// <summary>
        /// Order status. <c>Code</c> ∈ {<c>pending</c>, <c>completed</c>, <c>cancelled</c>}.
        /// <c>Label</c> is localized by <c>Accept-Language</c>.
        /// </summary>
        public ReferenceOptionDto Status { get; set; } = null!;
        public string? Note { get; set; }
        public Guid? CreatedByProfileId { get; set; }
        public string? CreatedByProfileFullName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? CancelledAt { get; set; }
        public string? CancelReason { get; set; }
        public List<OrderDetailDto> Items { get; set; } = new();
    }
}
