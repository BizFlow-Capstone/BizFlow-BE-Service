namespace BizFlow.Domain.Entities;

public partial class DeviceToken
{
    public Guid DeviceTokenId { get; set; }
    public Guid ProfileId { get; set; }
    public string Token { get; set; } = string.Empty;
    public string? DeviceName { get; set; }
    public string Platform { get; set; } = "Unknown";
    public DateTime RegisteredAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public bool IsActive { get; set; }

    public virtual Profile Profile { get; set; } = null!;
}
