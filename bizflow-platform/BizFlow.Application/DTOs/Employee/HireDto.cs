namespace BizFlow.Application.DTOs.Employee
{
    public class HireDto
    {
        public int HireId { get; set; }
        public Guid OwnerId { get; set; }
        public Guid EmployeeId { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
    }
}
