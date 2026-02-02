using System.ComponentModel.DataAnnotations;

namespace BizFlow.Application.DTOs.Product
{
    /// <summary>
    /// Request to update product status
    /// </summary>
    public class UpdateProductStatusRequest
    {
        [Required]
        public string Status { get; set; } = null!;
    }
}
