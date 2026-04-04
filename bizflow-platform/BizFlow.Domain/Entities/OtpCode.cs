using System;

namespace BizFlow.Domain.Entities
{
    public class OtpCode
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Email { get; set; } = null!;
        public string Code { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiredAt { get; set; }
        public bool IsUsed { get; set; }
    }
}
