namespace BizFlow.Application.DTOs.Product
{
    /// <summary>
    /// Request to update an existing product
    /// </summary>
    public class UpdateProductRequest : CreateProductRequest
    {
        /// <summary>
        /// If true, removes the product image (sets to null)
        /// </summary>
        public bool RemoveImage { get; set; }
    }
}
