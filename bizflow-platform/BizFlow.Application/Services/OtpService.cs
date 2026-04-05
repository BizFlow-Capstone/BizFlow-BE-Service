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
using System.Threading;
using System.Threading.Tasks;

namespace BizFlow.Application.Services
{
    public class OtpService : IOtpService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailSender _emailSender;
        private readonly IMessageService _messageService;
        private readonly ILogger<OtpService> _logger;

        private const int ExpirationMinutes = 5;
        private const int RateLimitMinutes  = 1;
        private const string EmailTemplateAlias = "password-reset";

        private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

        public OtpService(
            IUnitOfWork unitOfWork,
            IEmailSender emailSender,
            IMessageService messageService,
            ILogger<OtpService> logger)
        {
            _unitOfWork     = unitOfWork;
            _emailSender       = emailSender;
            _messageService    = messageService;
            _logger            = logger;
        }

        public async Task<SendOtpResponse> SendOtpAsync(SendOtpRequest request, CancellationToken ct = default)
        {
            var normalizedEmail = NormalizeEmail(request.Email);

            var fullName = await _unitOfWork.Profiles.GetFullNameForEligibleForgotPasswordEmailAsync(normalizedEmail, ct);
            if (fullName == null)
                throw new BadRequestException(MessageKeys.ForgotPasswordEmailNotRegistered);

            // Rate-limit: do not allow resend within RateLimitMinutes
            var existing = await _unitOfWork.OtpCodes.GetLatestActiveOtpAsync(normalizedEmail, ct);
            if (existing != null)
            {
                var elapsed = DateTime.UtcNow - existing.CreatedAt;
                if (elapsed.TotalMinutes < RateLimitMinutes)
                    throw new BadRequestException(MessageKeys.OtpTooManyRequests);
            }

            // Invalidate older OTPs that are still active
            await _unitOfWork.OtpCodes.DisableAllActiveOtpsAsync(normalizedEmail, ct);

            // Generate new code and persist
            var code = GenerateCode();
            var otpCode = new OtpCode
            {
                Email     = normalizedEmail,
                Code      = code,
                ExpiredAt = DateTime.UtcNow.AddMinutes(ExpirationMinutes)
            };
            await _unitOfWork.OtpCodes.AddAsync(otpCode);

            var userName = !string.IsNullOrWhiteSpace(fullName) ? fullName : normalizedEmail.Split('@')[0];

            // Send email via Resend template
            var variables = new Dictionary<string, string> 
            { 
                { "OTP_CODE", code },
                { "EXPIRY_MINUTES", ExpirationMinutes.ToString() },
                { "USER_NAME", userName }
            };
            var sendResult = await _emailSender.SendTemplateAsync(
                normalizedEmail,
                EmailTemplateAlias,
                variables,
                idempotencyKey: otpCode.Id.ToString(),
                ct);

            if (!sendResult.Success)
                _logger.LogWarning("Failed to send OTP email to {Email}. Reason: {Error}", normalizedEmail, sendResult.ErrorDetail);

            return new SendOtpResponse
            {
                Destination  = normalizedEmail,
                ExpiryMinutes = ExpirationMinutes
            };
        }

        public async Task<PasswordResetOtpVerifiedResult> VerifyEmailOtpForPasswordResetAsync(string email, string otpCode, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new BadRequestException(MessageKeys.EmailRequired);
            if (string.IsNullOrWhiteSpace(otpCode))
                throw new BadRequestException(MessageKeys.OtpInvalidOrExpired);

            var normalizedEmail = NormalizeEmail(email);

            if (!await _unitOfWork.OtpCodes.TryConsumeActiveOtpAsync(normalizedEmail, otpCode, ct))
                throw new BadRequestException(MessageKeys.OtpInvalidOrExpired);

            var account = await _unitOfWork.Accounts.GetWithProfileAndRoleByNormalizedEmailCredentialAsync(normalizedEmail, ct);
            if (account == null)
                throw new BadRequestException(MessageKeys.OtpInvalidOrExpired);
            if (account.IsActive == false || account.DeletedAt != null)
                throw new UnauthorizedException(MessageKeys.AccountInactiveOrDeleted);

            var profile = account.Profile
                ?? throw new BadRequestException(MessageKeys.AccountHasNoProfile);

            var nonce = Guid.NewGuid();
            account.PasswordResetNonce = nonce;
            await _unitOfWork.SaveChangesAsync(ct);

            _logger.LogInformation("Email OTP verified for password reset. AccountId={AccountId}", account.AccountId);

            return new PasswordResetOtpVerifiedResult
            {
                AccountId = account.AccountId,
                ProfileId = profile.ProfileId,
                RoleName = account.Role.Name,
                PasswordResetNonce = nonce
            };
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
