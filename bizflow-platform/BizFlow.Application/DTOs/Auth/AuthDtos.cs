namespace BizFlow.Application.DTOs.Auth
{
    using System.ComponentModel.DataAnnotations;

    public class GoogleLoginRequest
    {
        [Required]
        [MinLength(20)]
        public string IdToken { get; set; } = null!;

        [MaxLength(500)]
        public string? DeviceInfo { get; set; }
    }

    public class SetPasswordRequest
    {
        [Required]
        [MinLength(6)]
        [MaxLength(128)]
        public string Password { get; set; } = null!;
    }

    public class LoginWithEmailRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;

        [Required]
        [MinLength(6)]
        [MaxLength(128)]
        public string Password { get; set; } = null!;

        [MaxLength(500)]
        public string? DeviceInfo { get; set; }
    }

    public class LoginWithPhoneRequest
    {
        [Required]
        [MinLength(10)]
        [MaxLength(12)]
        public string Phone { get; set; } = null!;

        [Required]
        [MinLength(6)]
        [MaxLength(128)]
        public string Password { get; set; } = null!;

        [MaxLength(500)]
        public string? DeviceInfo { get; set; }
    }

    public class RegisterWithPhoneRequest
    {
        [Required]
        [MinLength(10)]
        [MaxLength(12)]
        public string Phone { get; set; } = null!;

        [Required]
        [MinLength(6)]
        [MaxLength(128)]
        public string Password { get; set; } = null!;

        [Required]
        [MinLength(50)]
        public string FirebaseIdToken { get; set; } = null!;

        [MaxLength(200)]
        public string? FullName { get; set; }

        [MaxLength(500)]
        public string? DeviceInfo { get; set; }
    }

    public class LinkPhoneRequest
    {
        [Required]
        [MinLength(10)]
        [MaxLength(12)]
        public string Phone { get; set; } = null!;

        [Required]
        [MinLength(50)]
        public string FirebaseIdToken { get; set; } = null!;

        [MinLength(6)]
        [MaxLength(128)]
        public string? Password { get; set; }
    }

    public class RefreshTokenRequest
    {
        [Required]
        [MinLength(20)]
        public string RefreshToken { get; set; } = null!;

        [MaxLength(500)]
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
