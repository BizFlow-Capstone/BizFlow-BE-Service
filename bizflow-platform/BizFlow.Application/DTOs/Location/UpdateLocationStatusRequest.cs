using System.ComponentModel.DataAnnotations;

namespace BizFlow.Application.DTOs.Location
{
    /// <summary>
    /// Request DTO for updating Business Location status
    /// </summary>
    public class UpdateLocationStatusRequest
    {
        [Required]
        public bool IsActive { get; set; }
    }
}
