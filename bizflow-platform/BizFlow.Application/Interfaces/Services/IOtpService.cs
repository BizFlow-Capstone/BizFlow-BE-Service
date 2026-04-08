using BizFlow.Application.DTOs.Otp;
using System.Threading;
using System.Threading.Tasks;

namespace BizFlow.Application.Interfaces.Services
{
    public interface IOtpService
    {
        Task<SendOtpResponse> SendOtpAsync(SendOtpRequest request, CancellationToken ct = default);

        /// <summary>
        /// Verifies forgot-password email OTP, sets <c>PasswordResetNonce</c>, and saves. Does not issue a JWT.
        /// </summary>
        Task<PasswordResetOtpVerifiedResult> VerifyEmailOtpForPasswordResetAsync(string email, string otpCode, CancellationToken ct = default);
    }
}
