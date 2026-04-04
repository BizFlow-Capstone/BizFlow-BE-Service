using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Exceptions;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.DTOs.Otp;
using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Application.Interfaces.Services;
using BizFlow.Domain.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BizFlow.Application.Services
{
    public class OtpService : IOtpService
    {
        private readonly IOtpCodeRepository _otpCodeRepository;
        private readonly IProfileRepository _profileRepository;
        private readonly IEmailSender _emailSender;
        private readonly IMessageService _messageService;
        private readonly ILogger<OtpService> _logger;

        private const int ExpirationMinutes = 5;
        private const int RateLimitMinutes  = 1;
        private const string EmailTemplateAlias = "password-reset";

        public OtpService(
            IOtpCodeRepository otpCodeRepository,
            IProfileRepository profileRepository,
            IEmailSender emailSender,
            IMessageService messageService,
            ILogger<OtpService> logger)
        {
            _otpCodeRepository = otpCodeRepository;
            _profileRepository = profileRepository;
            _emailSender       = emailSender;
            _messageService    = messageService;
            _logger            = logger;
        }

        public async Task<SendOtpResponse> SendOtpAsync(SendOtpRequest request, CancellationToken ct = default)
        {
            // Rate-limit: do not allow resend within RateLimitMinutes
            var existing = await _otpCodeRepository.GetLatestActiveOtpAsync(request.Email, ct);
            if (existing != null)
            {
                var elapsed = DateTime.UtcNow - existing.CreatedAt;
                if (elapsed.TotalMinutes < RateLimitMinutes)
                    throw new BadRequestException(_messageService.GetMessage(MessageKeys.OtpTooManyRequests));
            }

            // Invalidate older OTPs that are still active
            await _otpCodeRepository.DisableAllActiveOtpsAsync(request.Email, ct);

            // Generate new code and persist
            var code = GenerateCode();
            var otpCode = new OtpCode
            {
                Email     = request.Email,
                Code      = code,
                ExpiredAt = DateTime.UtcNow.AddMinutes(ExpirationMinutes)
            };
            await _otpCodeRepository.AddAsync(otpCode);

            // Get FullName from Profile if exists, otherwise fallback to Email prefix
            var fullName = await _profileRepository.GetFullNameByEmailAsync(request.Email, ct);
            var userName = !string.IsNullOrWhiteSpace(fullName) ? fullName : request.Email.Split('@')[0];

            // Send email via Resend template
            var variables = new Dictionary<string, string> 
            { 
                { "OTP_CODE", code },
                { "EXPIRY_MINUTES", ExpirationMinutes.ToString() },
                { "USER_NAME", userName }
            };
            var sendResult = await _emailSender.SendTemplateAsync(
                request.Email,
                EmailTemplateAlias,
                variables,
                idempotencyKey: otpCode.Id.ToString(),
                ct);

            if (!sendResult.Success)
                _logger.LogWarning("Failed to send OTP email to {Email}. Reason: {Error}", request.Email, sendResult.ErrorDetail);

            return new SendOtpResponse
            {
                Destination  = request.Email,
                ExpiryMinutes = ExpirationMinutes
            };
        }

        public async Task<VerifyOtpResponse> VerifyOtpAsync(VerifyOtpRequest request, CancellationToken ct = default)
        {
            var activeOtp = await _otpCodeRepository.GetLatestActiveOtpAsync(request.Email, ct);

            if (activeOtp == null || activeOtp.Code != request.OtpCode)
                throw new BadRequestException(_messageService.GetMessage(MessageKeys.OtpInvalidOrExpired));

            // Mark as used
            activeOtp.IsUsed = true;
            await _otpCodeRepository.UpdateAsync(activeOtp);

            return new VerifyOtpResponse { Verified = true };
        }

        // Random 6-digit code using RandomNumberGenerator to avoid bias
        private static string GenerateCode()
        {
            Span<byte> bytes = stackalloc byte[6];
            System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
            var digits = new char[6];
            for (int i = 0; i < 6; i++)
                digits[i] = (char)('0' + bytes[i] % 10);
            return new string(digits);
        }
    }
}
