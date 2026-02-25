namespace BizFlow.Application.DTOs.BusinessType
{
    public class BusinessTypeDto
    {
        public Guid BusinessTypeId { get; set; }
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string Status { get; set; } = null!;
    }
}
