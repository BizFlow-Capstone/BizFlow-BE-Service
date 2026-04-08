using BizFlow.Application.Interfaces.Repositories;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace BizFlow.Infrastructure.Jobs
{
    public class OtpCleanupJob
    {
        private readonly IOtpCodeRepository _otpCodeRepository;
        private readonly ILogger<OtpCleanupJob> _logger;

        public OtpCleanupJob(
            IOtpCodeRepository otpCodeRepository,
            ILogger<OtpCleanupJob> logger)
        {
            _otpCodeRepository = otpCodeRepository;
            _logger = logger;
        }

        public async Task ExecuteAsync()
        {
            try
            {
                _logger.LogInformation("Starting DB cleanup for expired OTP codes.");
                
                // Keep expired ones for 24h as trail, delete anything older
                var before = DateTime.UtcNow.AddHours(-24);
                
                await _otpCodeRepository.DeleteExpiredOtpsAsync(before);
                
                _logger.LogInformation("Completed DB cleanup for expired OTP codes.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while cleaning up expired OTP codes.");
            }
        }
    }
}
