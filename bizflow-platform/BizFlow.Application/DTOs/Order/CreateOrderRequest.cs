using System.ComponentModel.DataAnnotations;

namespace BizFlow.Application.DTOs.Order
{
    public class CreateOrderRequest
    {
        [Range(1, int.MaxValue)]
        public int BusinessLocationId { get; set; }

        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public long? DebtorId { get; set; }
        public string? Note { get; set; }
        public string? BillMetadata { get; set; }

        [Range(0, double.MaxValue)]
        public decimal CashAmount { get; set; }

        [Range(0, double.MaxValue)]
        public decimal BankAmount { get; set; }

        [Range(0, double.MaxValue)]
        public decimal DebtAmount { get; set; }

        public bool ConfirmLowStock { get; set; } = false;
        public bool ConfirmCreditLimitExceeded { get; set; } = false;

        [Required]
        public List<OrderItemRequest> Items { get; set; } = new();
    }
}
