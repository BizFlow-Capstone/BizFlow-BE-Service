using BizFlow.Application.DTOs.Hire;

namespace BizFlow.Application.DTOs.Location
{
    public class BusinessLocationDetailDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Address { get; set; } = null!;
        public string? District { get; set; }
        public string? City { get; set; }
        public string? Phone { get; set; }
        public string? TaxCode { get; set; }
        public bool IsActive { get; set; }
        public string? OwnerName { get; set; }
        public List<EmployeeSummaryDto> Employees { get; set; } = new();
    }
}
