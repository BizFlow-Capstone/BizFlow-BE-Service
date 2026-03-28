namespace BizFlow.Application.Interfaces.Services
{
    public interface INotificationService
    {
        Task RegisterDeviceTokenAsync(Guid userId, string token, string? deviceName, string platform);
        Task UnregisterDeviceTokenAsync(Guid userId, string token);
        Task SendToAllDevicesAsync(Guid userId, string title, string body);
        Task SendSilentNotificationAsync(Guid userId, Dictionary<string, string> data);
        Task SendEmployeeInviteAsync(Guid employeeId, string ownerName);
        Task NotifyEmployeeRemovedAsync(Guid employeeId, string businessName);
        Task NotifySubscriptionExpiringAsync(Guid ownerId, string planName, int daysRemaining, decimal price);
        Task NotifySubscriptionExpiredAsync(Guid ownerId, string planName);
    }
}
