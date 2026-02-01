using System.ComponentModel.DataAnnotations;

namespace BizFlow.Application.DTOs.Location
{
    /// <summary>
    /// Request DTO for creating a new Business Location
    /// </summary>
    public class CreateLocationRequest
    {
        [Required]
        [StringLength(255, MinimumLength = 1)]
        public string Name { get; set; } = null!;

        [Required]
        public string Address { get; set; } = null!;

        [StringLength(100)]
        public string? District { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(20)]
        public string? Phone { get; set; }

        [StringLength(50)]
        public string? TaxCode { get; set; }

        /// <summary>
        /// Optional: List of hired employee IDs to assign to this location
        /// Employees must be hired by the owner (exist in Hire table) to be assigned
        /// </summary>
        public List<Guid>? EmployeeIds { get; set; }
    }
}

