using System.ComponentModel.DataAnnotations;

namespace BizFlow.Application.DTOs.Otp
{
    /// <summary>Request body for <c>POST /api/auth/forgot-password/send-otp</c>.</summary>
    public class SendOtpRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;
    }

    public class SendOtpResponse
    {
        public string Destination { get; set; } = null!;
        public int ExpiryMinutes { get; set; }
    }

    /// <summary>Request body for <c>POST /api/auth/forgot-password/verify-otp</c>.</summary>
    public class VerifyOtpRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;

        [Required]
        [RegularExpression("^[0-9]{6}$", ErrorMessage = "OTP must be 6 digits")]
        public string OtpCode { get; set; } = null!;
    }

    public class VerifyOtpResponse
    {
        public bool Verified { get; set; }

        /// <summary>JWT for <c>POST /api/auth/forgot-password/reset</c> only (no refresh token).</summary>
        public string AccessToken { get; set; } = null!;
    }

    /// <summary>After a valid OTP: nonce is persisted; auth layer uses this to sign the password-reset JWT.</summary>
    public sealed class PasswordResetOtpVerifiedResult
    {
        public Guid AccountId { get; init; }
        public Guid ProfileId { get; init; }
        public string RoleName { get; init; } = null!;
        public Guid PasswordResetNonce { get; init; }
    }
}
