namespace BizFlow.Application.DTOs.Location
{
    /// <summary>
    /// Response DTO for Business Location
    /// </summary>
    public class BusinessLocationDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Address { get; set; } = null!;
        public string? District { get; set; }
        public string? City { get; set; }
        public string? Phone { get; set; }
        public bool IsActive { get; set; }
        public Guid OwnerProfileId { get; set; }
        public string? OwnerName { get; set; }
    }
}
