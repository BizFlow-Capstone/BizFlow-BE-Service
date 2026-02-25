using System.ComponentModel.DataAnnotations;

namespace BizFlow.Application.DTOs.Location
{
    /// <summary>
    /// Request DTO for updating Business Location info
    /// </summary>
    public class UpdateLocationRequest
    {
        [Required(ErrorMessage = "Name is required")]
        [StringLength(255, MinimumLength = 1)]
        public string Name { get; set; } = null!;

        [Required(ErrorMessage = "Address is required")]
        public string Address { get; set; } = null!;

        [StringLength(100)]
        public string? District { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(10)]
        public string? Phone { get; set; }

        [StringLength(50)]
        public string? TaxCode { get; set; }
    }
}
