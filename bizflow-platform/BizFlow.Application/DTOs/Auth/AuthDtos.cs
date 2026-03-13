namespace BizFlow.Application.DTOs.Auth
{
    public class GoogleLoginRequest
    {
        public string IdToken { get; set; } = null!;
        public string? DeviceInfo { get; set; }
    }

    public class SetPasswordRequest
    {
        public string Password { get; set; } = null!;
    }

    public class RefreshTokenRequest
    {
        public string RefreshToken { get; set; } = null!;
        public string? DeviceInfo { get; set; }
    }

    public class AuthResponse
    {
        public string AccessToken { get; set; } = null!;
        public string RefreshToken { get; set; } = null!;
        public bool IsNewAccount { get; set; }
        public AccountInfo Account { get; set; } = null!;
    }

    public class AccountInfo
    {
        public Guid AccountId { get; set; }
        public Guid ProfileId { get; set; }
        public string FullName { get; set; } = null!;
        public string? AvatarUrl { get; set; }
        public string Role { get; set; } = null!;
        public bool HasPassword { get; set; }
        public List<CredentialInfo> Credentials { get; set; } = new();
    }

    public class CredentialInfo
    {
        public string Type { get; set; } = null!;
        public string Identifier { get; set; } = null!;
        public bool? EmailVerified { get; set; }
    }
}
