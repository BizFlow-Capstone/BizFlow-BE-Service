namespace BizFlow.Application.DTOs.Product
{
    public class BulkAdjustSellingPriceRequest
    {
        /// <summary>
        /// Positive value increases price, negative value decreases price.
        /// </summary>
        public decimal DeltaAmount { get; set; }

        /// <summary>
        /// Target sale items to adjust.
        /// </summary>
        public List<long> SaleItemIds { get; set; } = new();
    }
}
