using BizFlow.Application.DTOs.Hire;

namespace BizFlow.Application.DTOs.Location
{
    public class BusinessLocationDetailDto : BusinessLocationDto
    {
        public string? TaxCode { get; set; }
        public List<EmployeeSummaryDto> Employees { get; set; } = new();
    }
}
