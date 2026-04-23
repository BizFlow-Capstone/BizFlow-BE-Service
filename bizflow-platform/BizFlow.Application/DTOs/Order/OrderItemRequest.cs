using System.ComponentModel.DataAnnotations;

namespace BizFlow.Application.DTOs.Order
{
    public class OrderItemRequest
    {
        [Range(1, long.MaxValue)]
        public long SaleItemId { get; set; }

        [Range(typeof(decimal), "0.01", "999999999999.99")]
        public decimal Quantity { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Discount { get; set; } = 0;
    }
}
