using System.ComponentModel.DataAnnotations;

namespace BizFlow.Application.DTOs.Otp
{
    /// <summary>Request to send an OTP by email.</summary>
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

    /// <summary>Request to verify the OTP received by email.</summary>
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
    }
}
