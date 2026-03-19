namespace BizFlow.Application.DTOs.Employee
{
    public class EmployeeInvitationDto
    {
        public int HireId { get; set; }
        public Guid OwnerId { get; set; }
        public string OwnerName { get; set; } = string.Empty;
        public DateTime InvitedAt { get; set; }
    }
}
