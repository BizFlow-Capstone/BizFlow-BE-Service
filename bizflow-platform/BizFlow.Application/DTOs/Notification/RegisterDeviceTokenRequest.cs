namespace BizFlow.Application.DTOs.Notification
{
    public class RegisterDeviceTokenRequest
    {
        public string Token { get; set; } = string.Empty;
        public string? DeviceName { get; set; }
        public string Platform { get; set; } = "Unknown";
    }
}
