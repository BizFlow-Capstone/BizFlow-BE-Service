using BizFlow.Application.DTOs.Otp;
using System.Threading;
using System.Threading.Tasks;

namespace BizFlow.Application.Interfaces.Services
{
    public interface IOtpService
    {
        Task<SendOtpResponse> SendOtpAsync(SendOtpRequest request, CancellationToken ct = default);
        Task<VerifyOtpResponse> VerifyOtpAsync(VerifyOtpRequest request, CancellationToken ct = default);
    }
}
