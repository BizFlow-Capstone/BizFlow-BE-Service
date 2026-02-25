namespace BizFlow.Application.DTOs.Import
{
    /// <summary>
    /// A single item line in an import request
    /// </summary>
    public class ImportItemRequest
    {
        public long ProductId { get; set; }

        /// <summary>
        /// Quantity in base unit
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Cost price per base unit
        /// </summary>
        public decimal CostPrice { get; set; }
    }
}
