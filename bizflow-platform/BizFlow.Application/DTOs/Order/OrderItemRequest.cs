using System.ComponentModel.DataAnnotations;

namespace BizFlow.Application.DTOs.Order
{
    public class OrderItemRequest
    {
        [Range(1, long.MaxValue)]
        public long SaleItemId { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [Range(0, double.MaxValue)]
        public decimal UnitPrice { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Discount { get; set; } = 0;
    }
}
