using System.ComponentModel.DataAnnotations;

namespace BizFlow.Application.DTOs.Employee
{
    public class SendInvitationReplyNotificationRequest
    {
        [Required]
        public Guid OwnerUserId { get; set; }

        [Required]
        public bool IsAccepted { get; set; }

        [Required]
        [MaxLength(200)]
        public string EmployeeName { get; set; } = string.Empty;
    }
}
