using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Exceptions;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.Common.Models;
using BizFlow.Application.Common.Utilities;
using BizFlow.Application.DTOs.Admin;
using BizFlow.Application.DTOs.Auth;
using BizFlow.Application.DTOs.Otp;
using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Application.Interfaces.Services;
using BizFlow.Domain.Entities;
using BizFlow.Infrastructure.DataContext;
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth;
using Google.Apis.Auth.OAuth2;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;

namespace BizFlow.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly BizFlowDbContext _db;
        private readonly IJwtService _jwtService;
        private readonly ISubscriptionService _subscriptionService;
        private readonly GoogleAuthConfig _googleConfig;
        private readonly FirebaseAuthConfig _firebaseConfig;
        private readonly IImageService _imageService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IOtpService _otpService;
        private readonly IEmailSender _emailSender;
        private readonly IOptions<AppPublicUrlsOptions> _appPublicUrls;
        private readonly IAccountHardDeleteService _accountHardDeleteService;
        private readonly ILogger<AuthService> _logger;

        private const string DefaultRoleName = "user";
        private const string ConsultantRoleName = "consultant";
        private const string AdminRoleName = "admin";
        private const string ConsultantWelcomeTemplateAlias = "consultant-welcome";

        public AuthService(
            BizFlowDbContext db,
            IJwtService jwtService,
            ISubscriptionService subscriptionService,
            IOptions<GoogleAuthConfig> googleConfig,
            IOptions<FirebaseAuthConfig> firebaseConfig,
            IImageService imageService,
            IUnitOfWork unitOfWork,
            IOtpService otpService,
            IEmailSender emailSender,
            IOptions<AppPublicUrlsOptions> appPublicUrls,
            IAccountHardDeleteService accountHardDeleteService,
            ILogger<AuthService> logger)
        {
            _db = db;
            _jwtService = jwtService;
            _subscriptionService = subscriptionService;
            _googleConfig = googleConfig.Value;
            _firebaseConfig = firebaseConfig.Value;
            _imageService = imageService;
            _unitOfWork = unitOfWork;
            _otpService = otpService;
            _emailSender = emailSender;
            _appPublicUrls = appPublicUrls;
            _accountHardDeleteService = accountHardDeleteService;
            _logger = logger;
        }

        public async Task<AuthResponse> GoogleLoginAsync(string idToken, string? deviceInfo)
        {
            if (string.IsNullOrWhiteSpace(idToken))
            {
                throw new ArgumentException(MessageKeys.IdTokenRequired, nameof(idToken));
            }

            // 1. Verify Google ID token
            var payload = await VerifyGoogleTokenAsync(idToken);

            // 2. Check if Google credential already exists
            var credential = await _db.Credentials
                .Include(c => c.Account)
                    .ThenInclude(a => a.Profile)
                .Include(c => c.Account)
                    .ThenInclude(a => a.Role)
                .Include(c => c.Account)
                    .ThenInclude(a => a.Credentials)
                .FirstOrDefaultAsync(c => c.Type == "google" && c.Identifier == payload.Subject);

            bool isNewAccount;
            Account account;

            if (credential != null)
            {
                // Existing account — login
                account = credential.Account;
                isNewAccount = false;

                if (account.IsActive == false || account.DeletedAt != null)
                {
                    _logger.LogWarning("Google login rejected for inactive/deleted account. AccountId={AccountId}", account.AccountId);
                    throw new UnauthorizedAccessException(MessageKeys.AccountInactiveOrDeleted);
                }

                // Update last login
                account.LastLoginAt = DateTime.UtcNow;
                account.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                // New account — register
                var role = await _db.Roles.FirstOrDefaultAsync(r => r.Name == DefaultRoleName)
                    ?? throw new InvalidOperationException(
                        string.Format(CultureInfo.InvariantCulture, MessageKeys.DefaultRoleNotFound, DefaultRoleName));

                account = new Account
                {
                    AccountId = Guid.NewGuid(),
                    RoleId = role.RoleId,
                    PasswordHash = null, // Google-only, no password yet
                    IsActive = true,
                    MustChangePassword = false,
                    LastLoginAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var profile = new Profile
                {
                    ProfileId = Guid.NewGuid(),
                    AccountId = account.AccountId,
                    FullName = payload.Name ?? payload.Email?.Split('@')[0] ?? "user",
                    AvatarUrl = payload.Picture,
                    UpdatedAt = DateTime.UtcNow
                };

                var googleCredential = new Credential
                {
                    CredentialId = Guid.NewGuid(),
                    AccountId = account.AccountId,
                    Type = "google",
                    Identifier = payload.Subject,
                    GoogleEmail = payload.Email,
                    EmailVerified = false,
                    CreatedAt = DateTime.UtcNow
                };

                _db.Accounts.Add(account);
                _db.Profiles.Add(profile);
                _db.Credentials.Add(googleCredential);
                await _db.SaveChangesAsync();

                // Reload with navigation properties
                account = await _db.Accounts
                    .Include(a => a.Profile)
                    .Include(a => a.Role)
                    .Include(a => a.Credentials)
                    .FirstAsync(a => a.AccountId == account.AccountId);

                await _subscriptionService.EnsureFreeSubscriptionAsync(account.Profile!.ProfileId);
                isNewAccount = true;
            }

            // 3. Issue tokens
            var profile2 = account.Profile
                ?? throw new InvalidOperationException(MessageKeys.AccountHasNoProfile);

            var accessToken = _jwtService.GenerateAccessToken(
                account.AccountId, profile2.ProfileId, account.Role.Name);

            var refreshToken = _jwtService.GenerateRefreshToken();
            StoreRefreshToken(account.AccountId, refreshToken, deviceInfo);

            if (!isNewAccount)
            {
                await _db.SaveChangesAsync();
            }

            _logger.LogInformation("Google auth completed. AccountId={AccountId}, IsNewAccount={IsNewAccount}, Device={DeviceInfo}", account.AccountId, isNewAccount, deviceInfo ?? "unknown");

            return new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                IsNewAccount = isNewAccount,
                Account = MapAccountInfo(account)
            };
        }

        public async Task<AuthResponse> LoginWithEmailAsync(string email, string password, string? deviceInfo)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new ArgumentException(MessageKeys.EmailRequired, nameof(email));
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException(MessageKeys.PasswordRequired, nameof(password));
            }

            var normalizedEmail = email.Trim().ToLowerInvariant();
            var credential = await _db.Credentials
                .Include(c => c.Account)
                    .ThenInclude(a => a.Profile)
                .Include(c => c.Account)
                    .ThenInclude(a => a.Role)
                .Include(c => c.Account)
                    .ThenInclude(a => a.Credentials)
                .FirstOrDefaultAsync(c => c.Type == "email" && c.Identifier.ToLower() == normalizedEmail);

            if (credential == null)
            {
                _logger.LogWarning("Email login failed. Email credential not found: {Email}", normalizedEmail);
                throw new UnauthorizedAccessException(MessageKeys.InvalidCredentials);
            }

            return await LoginWithCredentialAsync(credential, password, deviceInfo, "email", normalizedEmail);
        }

        public async Task<AuthResponse> LoginWithPhoneAsync(string phone, string password, string? deviceInfo)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                throw new ArgumentException(MessageKeys.PhoneRequired, nameof(phone));
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException(MessageKeys.PasswordRequired, nameof(password));
            }

            var normalizedPhone = NormalizeVietnamPhone(phone);
            var credential = await _db.Credentials
                .Include(c => c.Account)
                    .ThenInclude(a => a.Profile)
                .Include(c => c.Account)
                    .ThenInclude(a => a.Role)
                .Include(c => c.Account)
                    .ThenInclude(a => a.Credentials)
                .FirstOrDefaultAsync(c => c.Type == "phone" && c.Identifier == normalizedPhone);

            if (credential == null)
            {
                _logger.LogWarning("Phone login failed. Phone credential not found: {Phone}", normalizedPhone);
                throw new UnauthorizedAccessException(MessageKeys.InvalidCredentials);
            }

            return await LoginWithCredentialAsync(credential, password, deviceInfo, "phone", normalizedPhone);
        }

        public async Task<AuthResponse> RegisterWithPhoneAsync(string phone, string password, string firebaseIdToken, string? fullName, string? deviceInfo)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                throw new ArgumentException(MessageKeys.PhoneRequired, nameof(phone));
            }

            if (string.IsNullOrWhiteSpace(firebaseIdToken))
            {
                throw new ArgumentException(MessageKeys.FirebaseIdTokenRequired, nameof(firebaseIdToken));
            }

            ValidatePasswordOrThrow(password);

            var normalizedPhone = NormalizeVietnamPhone(phone);
            var verifiedPhone = await VerifyFirebasePhoneTokenAsync(firebaseIdToken);

            if (!string.Equals(normalizedPhone, verifiedPhone, StringComparison.Ordinal))
            {
                _logger.LogWarning("Phone registration mismatch. RequestPhone={RequestPhone}, FirebasePhone={FirebasePhone}", normalizedPhone, verifiedPhone);
                throw new InvalidOperationException(MessageKeys.PhoneVerificationMismatch);
            }

            var phoneExists = await _db.Credentials.AnyAsync(c => c.Type == "phone" && c.Identifier == normalizedPhone);
            if (phoneExists)
            {
                _logger.LogWarning("Phone registration failed. Phone already exists: {Phone}", normalizedPhone);
                throw new InvalidOperationException(MessageKeys.PhoneAlreadyExists);
            }

            var role = await _db.Roles.FirstOrDefaultAsync(r => r.Name == DefaultRoleName)
                ?? throw new InvalidOperationException(
                    string.Format(CultureInfo.InvariantCulture, MessageKeys.DefaultRoleNotFound, DefaultRoleName));

            var account = new Account
            {
                AccountId = Guid.NewGuid(),
                RoleId = role.RoleId,
                PasswordHash = _jwtService.HashPassword(password),
                IsActive = true,
                MustChangePassword = false,
                LastLoginAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var profile = new Profile
            {
                ProfileId = Guid.NewGuid(),
                AccountId = account.AccountId,
                FullName = string.IsNullOrWhiteSpace(fullName) ? normalizedPhone : fullName.Trim(),
                UpdatedAt = DateTime.UtcNow
            };

            var phoneCredential = new Credential
            {
                CredentialId = Guid.NewGuid(),
                AccountId = account.AccountId,
                Type = "phone",
                Identifier = normalizedPhone,
                EmailVerified = false,
                CreatedAt = DateTime.UtcNow
            };

            _db.Accounts.Add(account);
            _db.Profiles.Add(profile);
            _db.Credentials.Add(phoneCredential);
            await _db.SaveChangesAsync();

            account = await _db.Accounts
                .Include(a => a.Profile)
                .Include(a => a.Role)
                .Include(a => a.Credentials)
                .FirstAsync(a => a.AccountId == account.AccountId);

            await _subscriptionService.EnsureFreeSubscriptionAsync(account.Profile!.ProfileId);

            var accessToken = _jwtService.GenerateAccessToken(account.AccountId, profile.ProfileId, account.Role.Name);
            var refreshToken = _jwtService.GenerateRefreshToken();
            StoreRefreshToken(account.AccountId, refreshToken, deviceInfo);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Phone registration success. AccountId={AccountId}, Phone={Phone}, Device={DeviceInfo}", account.AccountId, normalizedPhone, deviceInfo ?? "unknown");

            return new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                IsNewAccount = true,
                Account = MapAccountInfo(account)
            };
        }

        public async Task<List<CredentialInfo>> LinkPhoneAsync(Guid accountId, string phone, string firebaseIdToken, string? password)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                throw new ArgumentException(MessageKeys.PhoneRequired, nameof(phone));
            }

            if (string.IsNullOrWhiteSpace(firebaseIdToken))
            {
                throw new ArgumentException(MessageKeys.FirebaseIdTokenRequired, nameof(firebaseIdToken));
            }

            var account = await _db.Accounts
                .Include(a => a.Credentials)
                .Include(a => a.Role)
                .FirstOrDefaultAsync(a => a.AccountId == accountId)
                ?? throw new KeyNotFoundException(MessageKeys.AccountNotFound);

            if (string.Equals(account.Role.Name, ConsultantRoleName, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(MessageKeys.ConsultantCannotLinkPhone);
            }

            var alreadyLinked = account.Credentials.Any(c => c.Type == "phone");
            if (alreadyLinked)
            {
                throw new InvalidOperationException(MessageKeys.PhoneAlreadyLinked);
            }

            var normalizedPhone = NormalizeVietnamPhone(phone);
            var verifiedPhone = await VerifyFirebasePhoneTokenAsync(firebaseIdToken);

            if (!string.Equals(normalizedPhone, verifiedPhone, StringComparison.Ordinal))
            {
                _logger.LogWarning("Phone linking mismatch. RequestPhone={RequestPhone}, FirebasePhone={FirebasePhone}", normalizedPhone, verifiedPhone);
                throw new InvalidOperationException(MessageKeys.PhoneVerificationMismatch);
            }

            var usedByOtherAccount = await _db.Credentials.AnyAsync(c => c.Type == "phone" && c.Identifier == normalizedPhone && c.AccountId != accountId);
            if (usedByOtherAccount)
            {
                throw new InvalidOperationException(MessageKeys.PhoneAlreadyExists);
            }

            if (string.IsNullOrWhiteSpace(account.PasswordHash))
            {
                if (string.IsNullOrWhiteSpace(password))
                {
                    throw new ArgumentException(MessageKeys.PasswordRequiredWhenNoPassword, nameof(password));
                }

                ValidatePasswordOrThrow(password);
                account.PasswordHash = _jwtService.HashPassword(password);
            }

            var credential = new Credential
            {
                CredentialId = Guid.NewGuid(),
                AccountId = accountId,
                Type = "phone",
                Identifier = normalizedPhone,
                EmailVerified = false,
                CreatedAt = DateTime.UtcNow
            };

            account.UpdatedAt = DateTime.UtcNow;
            _db.Credentials.Add(credential);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Phone linked successfully. AccountId={AccountId}, Phone={Phone}", accountId, normalizedPhone);

            var credentials = await _db.Credentials
                .Where(c => c.AccountId == accountId)
                .ToListAsync();

            return credentials.Select(c => new CredentialInfo
            {
                Type = c.Type,
                Identifier = CredentialMasking.MaskIdentifier(c.Type, c.Identifier),
                EmailVerified = c.Type == "email" ? c.EmailVerified : null
            }).ToList();
        }

        public async Task<List<CredentialInfo>> LinkGoogleAsync(Guid accountId, string idToken)
        {
            if (string.IsNullOrWhiteSpace(idToken))
            {
                throw new ArgumentException(MessageKeys.IdTokenRequired, nameof(idToken));
            }

            var account = await _db.Accounts
                .Include(a => a.Credentials)
                .Include(a => a.Role)
                .FirstOrDefaultAsync(a => a.AccountId == accountId)
                ?? throw new KeyNotFoundException(MessageKeys.AccountNotFound);

            var alreadyLinked = account.Credentials.Any(c => c.Type == "google");
            if (alreadyLinked)
            {
                throw new InvalidOperationException(MessageKeys.GoogleAlreadyLinked);
            }

            var payload = await VerifyGoogleTokenAsync(idToken);

            var usedByOtherAccount = await _db.Credentials.AnyAsync(
                c => c.Type == "google" && c.Identifier == payload.Subject && c.AccountId != accountId);
            if (usedByOtherAccount)
            {
                throw new InvalidOperationException(MessageKeys.GoogleAlreadyExists);
            }

            var credential = new Credential
            {
                CredentialId = Guid.NewGuid(),
                AccountId = accountId,
                Type = "google",
                Identifier = payload.Subject,
                GoogleEmail = payload.Email,
                EmailVerified = false,
                CreatedAt = DateTime.UtcNow
            };

            account.UpdatedAt = DateTime.UtcNow;
            _db.Credentials.Add(credential);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Google linked successfully. AccountId={AccountId}, GoogleEmail={GoogleEmail}", accountId, payload.Email);

            var credentials = await _db.Credentials
                .Where(c => c.AccountId == accountId)
                .ToListAsync();

            return credentials.Select(c => new CredentialInfo
            {
                Type = c.Type,
                Identifier = CredentialMasking.MaskIdentifier(c.Type, c.Identifier),
                EmailVerified = c.Type == "email" ? c.EmailVerified : null
            }).ToList();
        }

        public async Task SetPasswordAsync(Guid accountId, string password)
        {
            ValidatePasswordOrThrow(password);

            var account = await _db.Accounts
                .Include(a => a.Credentials)
                .FirstOrDefaultAsync(a => a.AccountId == accountId)
                ?? throw new KeyNotFoundException(MessageKeys.AccountNotFound);

            if (account.PasswordHash != null)
                throw new InvalidOperationException(MessageKeys.PasswordAlreadySet);

            // Hash and set password
            account.PasswordHash = _jwtService.HashPassword(password);
            account.PasswordResetNonce = null;
            account.UpdatedAt = DateTime.UtcNow;

            // If account has a Google credential with email, auto-create email credential
            var googleCred = account.Credentials.FirstOrDefault(c => c.Type == "google");
            if (googleCred?.GoogleEmail != null)
            {
                var emailExists = await _db.Credentials
                    .AnyAsync(c => c.Type == "email" && c.Identifier == googleCred.GoogleEmail);

                if (!emailExists)
                {
                    var emailAlreadyLinked = account.Credentials.Any(c => c.Type == "email");
                    if (!emailAlreadyLinked)
                    {
                        var emailCredential = new Credential
                        {
                            CredentialId = Guid.NewGuid(),
                            AccountId = accountId,
                            Type = "email",
                            Identifier = googleCred.GoogleEmail,
                            EmailVerified = true, // Verified via Google
                            CreatedAt = DateTime.UtcNow
                        };
                        _db.Credentials.Add(emailCredential);
                    }
                }
            }

            await _db.SaveChangesAsync();
            _logger.LogInformation("Password set for account. AccountId={AccountId}", accountId);
        }

        public async Task<VerifyOtpResponse> VerifyEmailOtpForPasswordResetAsync(string email, string otpCode, CancellationToken ct = default)
        {
            var verified = await _otpService.VerifyEmailOtpForPasswordResetAsync(email, otpCode, ct);
            return BuildPasswordResetVerifyResponse(verified.AccountId, verified.ProfileId, verified.RoleName, verified.PasswordResetNonce);
        }

        public async Task<VerifyOtpResponse> VerifyOtpForPasswordResetAsync(VerifyOtpRequest request, CancellationToken ct = default)
        {
            if (request == null)
            {
                throw new BadRequestException(MessageKeys.ValidationError);
            }

            var hasFirebaseToken = !string.IsNullOrWhiteSpace(request.FirebaseIdToken);
            var hasEmailOrOtp = !string.IsNullOrWhiteSpace(request.Email) || !string.IsNullOrWhiteSpace(request.OtpCode);

            if (hasFirebaseToken && hasEmailOrOtp)
            {
                throw new BadRequestException(MessageKeys.ValidationError);
            }

            if (hasFirebaseToken)
            {
                return await VerifyFirebaseOtpForPasswordResetAsync(request.FirebaseIdToken!, ct);
            }

            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.OtpCode))
            {
                throw new BadRequestException(MessageKeys.ValidationError);
            }

            return await VerifyEmailOtpForPasswordResetAsync(request.Email, request.OtpCode, ct);
        }

        public async Task<VerifyOtpResponse> VerifyFirebaseOtpForPasswordResetAsync(string firebaseIdToken, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(firebaseIdToken))
            {
                throw new ArgumentException(MessageKeys.FirebaseIdTokenRequired, nameof(firebaseIdToken));
            }

            var verifiedPhone = await VerifyFirebasePhoneTokenAsync(firebaseIdToken);
            var normalizedPhone = NormalizeVietnamPhone(verifiedPhone);

            var credential = await _db.Credentials
                .Include(c => c.Account)
                    .ThenInclude(a => a.Profile)
                .Include(c => c.Account)
                    .ThenInclude(a => a.Role)
                .FirstOrDefaultAsync(
                    c => c.Type == "phone" && c.Identifier == normalizedPhone,
                    ct);

            if (credential == null)
            {
                throw new BadRequestException(MessageKeys.OtpInvalidOrExpired);
            }

            var account = credential.Account;
            if (string.Equals(account.Role.Name, AdminRoleName, StringComparison.OrdinalIgnoreCase))
            {
                throw new ForbiddenException(MessageKeys.AdminForgotPasswordNotAllowed);
            }

            if (account.IsActive == false || account.DeletedAt != null)
            {
                throw new UnauthorizedException(MessageKeys.AccountInactiveOrDeleted);
            }

            var profile = account.Profile
                ?? throw new BadRequestException(MessageKeys.AccountHasNoProfile);

            var nonce = Guid.NewGuid();
            account.PasswordResetNonce = nonce;
            account.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation("Firebase phone token verified for password reset. AccountId={AccountId}", account.AccountId);

            var accessToken = _jwtService.GenerateAccessToken(
                account.AccountId,
                profile.ProfileId,
                account.Role.Name,
                forPasswordReset: true,
                passwordResetNonce: nonce);

            return new VerifyOtpResponse { Verified = true, AccessToken = accessToken };
        }

        private VerifyOtpResponse BuildPasswordResetVerifyResponse(Guid accountId, Guid profileId, string roleName, Guid passwordResetNonce)
        {
            var accessToken = _jwtService.GenerateAccessToken(
                accountId,
                profileId,
                roleName,
                forPasswordReset: true,
                passwordResetNonce: passwordResetNonce);

            return new VerifyOtpResponse { Verified = true, AccessToken = accessToken };
        }

        public async Task ResetPasswordAfterForgotOtpAsync(Guid accountId, string newPassword, Guid passwordResetNonce)
        {
            if (string.IsNullOrWhiteSpace(newPassword))
                throw new BadRequestException(MessageKeys.PasswordRequired);
            if (newPassword.Length < 6 || newPassword.Length > 128)
                throw new BadRequestException(MessageKeys.PasswordInvalidFormat);

            var account = await _unitOfWork.Accounts.GetTrackedByIdAsync(accountId)
                ?? throw new NotFoundException(MessageKeys.AccountNotFound);

            if (account.PasswordResetNonce == null || account.PasswordResetNonce != passwordResetNonce)
                throw new BadRequestException(MessageKeys.PasswordResetTokenInvalidOrUsed);

            account.PasswordHash = _jwtService.HashPassword(newPassword);
            account.PasswordResetNonce = null;
            account.MustChangePassword = false;
            account.UpdatedAt = DateTime.UtcNow;

            await RevokeAllRefreshTokensAsync(accountId);

            _logger.LogInformation("Password reset completed after forgot flow. AccountId={AccountId}", accountId);
        }

        public async Task ChangePasswordAsync(Guid accountId, string? currentPassword, string newPassword)
        {
            ValidatePasswordOrThrow(newPassword);

            var account = await _db.Accounts
                .FirstOrDefaultAsync(a => a.AccountId == accountId)
                ?? throw new KeyNotFoundException(MessageKeys.AccountNotFound);

            if (string.IsNullOrWhiteSpace(account.PasswordHash))
            {
                _logger.LogWarning("Change password rejected: account has no password. AccountId={AccountId}", accountId);
                throw new InvalidOperationException(MessageKeys.NoPasswordToChange);
            }

            var currentProvided = !string.IsNullOrWhiteSpace(currentPassword);
            if (!currentProvided)
            {
                if (!account.MustChangePassword)
                {
                    throw new ArgumentException(MessageKeys.CurrentPasswordRequired, nameof(currentPassword));
                }
            }
            else
            {
                if (!_jwtService.VerifyPassword(currentPassword!, account.PasswordHash))
                {
                    _logger.LogWarning("Change password failed: incorrect current password. AccountId={AccountId}", accountId);
                    throw new UnauthorizedAccessException(MessageKeys.CurrentPasswordIncorrect);
                }
            }

            if (_jwtService.VerifyPassword(newPassword, account.PasswordHash))
            {
                throw new InvalidOperationException(MessageKeys.NewPasswordSameAsCurrent);
            }

            account.PasswordHash = _jwtService.HashPassword(newPassword);
            account.PasswordResetNonce = null;
            account.MustChangePassword = false;
            account.UpdatedAt = DateTime.UtcNow;

            await RevokeAllRefreshTokensAsync(accountId);

            _logger.LogInformation("Password changed. AccountId={AccountId}", accountId);
        }

        public async Task<UserProfileDto> GetProfileAsync(Guid profileId)
        {
            var profile = await _db.Profiles
                .AsNoTracking()
                .Include(p => p.Account)
                .FirstOrDefaultAsync(p => p.ProfileId == profileId)
                ?? throw new KeyNotFoundException(MessageKeys.NotFound);

            _logger.LogInformation("Profile retrieved. ProfileId={ProfileId}", profileId);

            return new UserProfileDto
            {
                ProfileId = profile.ProfileId,
                FullName = profile.FullName,
                AvatarUrl = profile.AvatarUrl,
                TaxCode = profile.TaxCode,
                MustChangePassword = profile.Account.MustChangePassword
            };
        }

        public async Task<UserProfileDto> UpdateProfileInfoAsync(Guid profileId, UpdateProfileInfoRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            // Distinguish: "not provided" vs "provided as null"
            var hasFullName = request.FullName.ValueKind != JsonValueKind.Undefined;
            var hasTaxCode = request.TaxCode.ValueKind != JsonValueKind.Undefined;

            if (!hasFullName && !hasTaxCode)
            {
                throw new ArgumentException(MessageKeys.ProfileAtLeastOneFieldRequired, nameof(request));
            }

            var profile = await _db.Profiles
                .Include(p => p.Account)
                .FirstOrDefaultAsync(p => p.ProfileId == profileId)
                ?? throw new KeyNotFoundException(MessageKeys.NotFound);

            if (hasFullName)
            {
                if (request.FullName.ValueKind == JsonValueKind.Null)
                {
                    profile.FullName = string.Empty;
                }
                else if (request.FullName.ValueKind == JsonValueKind.String)
                {
                    var rawName = request.FullName.GetString();
                    if (string.IsNullOrEmpty(rawName))
                    {
                        throw new ArgumentException(MessageKeys.ProfileFullNameRequired, nameof(request.FullName));
                    }

                    var trimmedName = rawName.Trim();
                    if (string.IsNullOrEmpty(trimmedName))
                    {
                        throw new ArgumentException(MessageKeys.ProfileFullNameRequired, nameof(request.FullName));
                    }

                    if (trimmedName.Length > 200)
                    {
                        throw new ArgumentException(MessageKeys.ProfileFullNameTooLong, nameof(request.FullName));
                    }

                    profile.FullName = trimmedName;
                }
                else
                {
                    throw new ArgumentException(MessageKeys.ProfileFullNameInvalidType, nameof(request.FullName));
                }
            }

            if (hasTaxCode)
            {
                if (request.TaxCode.ValueKind == JsonValueKind.Null)
                {
                    profile.TaxCode = null;
                }
                else if (request.TaxCode.ValueKind == JsonValueKind.String)
                {
                    var rawTax = request.TaxCode.GetString();
                    if (string.IsNullOrEmpty(rawTax))
                    {
                        profile.TaxCode = null;
                    }
                    else
                    {
                        var trimmedTax = rawTax.Trim();
                        if (trimmedTax.Length > 50)
                        {
                            throw new ArgumentException(MessageKeys.ProfileTaxCodeTooLong, nameof(request.TaxCode));
                        }
                        profile.TaxCode = trimmedTax;
                    }
                }
                else
                {
                    throw new ArgumentException(MessageKeys.ProfileTaxCodeInvalidType, nameof(request.TaxCode));
                }
            }

            profile.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            _logger.LogInformation("Profile info updated. ProfileId={ProfileId}", profileId);

            return new UserProfileDto
            {
                ProfileId = profile.ProfileId,
                FullName = profile.FullName,
                AvatarUrl = profile.AvatarUrl,
                TaxCode = profile.TaxCode,
                MustChangePassword = profile.Account.MustChangePassword
            };
        }

        public async Task<UserProfileDto> UpdateAvatarAsync(Guid profileId, UpdateAvatarRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var hasAvatarUpload = request.AvatarStream != null;
            var hasRemoveAvatar = request.RemoveAvatar;

            if (!hasAvatarUpload && !hasRemoveAvatar)
            {
                throw new ArgumentException(MessageKeys.ProfileAtLeastOneFieldRequired, nameof(request));
            }

            var profile = await _db.Profiles
                .Include(p => p.Account)
                .FirstOrDefaultAsync(p => p.ProfileId == profileId)
                ?? throw new KeyNotFoundException(MessageKeys.NotFound);

            if (hasAvatarUpload)
            {
                await using (var stream = request.AvatarStream!)
                {
                    var imageInfo = await _imageService.UploadImageAsync(
                        stream,
                        request.AvatarFileName,
                        ImageUploadTarget.Avatars);
                    profile.AvatarUrl = imageInfo.Url;
                }
            }
            else if (hasRemoveAvatar)
            {
                profile.AvatarUrl = null;
            }

            profile.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            _logger.LogInformation("Avatar updated. ProfileId={ProfileId}", profileId);

            return new UserProfileDto
            {
                ProfileId = profile.ProfileId,
                FullName = profile.FullName,
                AvatarUrl = profile.AvatarUrl,
                TaxCode = profile.TaxCode,
                MustChangePassword = profile.Account.MustChangePassword
            };
        }

        public async Task<AuthResponse> RefreshTokenAsync(string refreshToken, string? deviceInfo)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                throw new ArgumentException(MessageKeys.RefreshTokenRequired, nameof(refreshToken));
            }

            // Find matching token in DB
            var storedTokens = await _db.RefreshTokens
                .Include(rt => rt.Account)
                    .ThenInclude(a => a.Profile)
                .Include(rt => rt.Account)
                    .ThenInclude(a => a.Role)
                .Include(rt => rt.Account)
                    .ThenInclude(a => a.Credentials)
                .Where(rt => rt.ExpiresAt > DateTime.UtcNow)
                .ToListAsync();

            RefreshToken? matchedToken = null;
            foreach (var storedToken in storedTokens)
            {
                var hash = _jwtService.HashToken(refreshToken, storedToken.TokenSalt);
                if (hash == storedToken.TokenHash)
                {
                    matchedToken = storedToken;
                    break;
                }
            }

            if (matchedToken == null)
                throw new UnauthorizedAccessException(MessageKeys.RefreshTokenInvalidOrExpired);

            // Reuse detection: if token is already revoked, revoke ALL tokens for this account
            if (matchedToken.RevokedAt != null)
            {
                _logger.LogWarning("Refresh token reuse detected. AccountId={AccountId}", matchedToken.AccountId);
                await RevokeAllRefreshTokensAsync(matchedToken.AccountId);
                throw new UnauthorizedAccessException(MessageKeys.RefreshTokenReuseDetected);
            }

            var account = matchedToken.Account;

            // Revoke old token
            matchedToken.RevokedAt = DateTime.UtcNow;

            // Issue new tokens
            var profile = account.Profile
                ?? throw new InvalidOperationException(MessageKeys.AccountHasNoProfile);

            var newAccessToken = _jwtService.GenerateAccessToken(
                account.AccountId, profile.ProfileId, account.Role.Name);

            var newRefreshToken = _jwtService.GenerateRefreshToken();
            StoreRefreshToken(account.AccountId, newRefreshToken, deviceInfo);

            account.LastLoginAt = DateTime.UtcNow;
            account.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            _logger.LogInformation("Refresh token rotated. AccountId={AccountId}, Device={DeviceInfo}", account.AccountId, deviceInfo ?? "unknown");

            return new AuthResponse
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                IsNewAccount = false,
                Account = MapAccountInfo(account)
            };
        }

        public async Task RevokeRefreshTokenAsync(string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                throw new ArgumentException(MessageKeys.RefreshTokenRequired, nameof(refreshToken));
            }

            var storedTokens = await _db.RefreshTokens
                .Where(rt => rt.RevokedAt == null && rt.ExpiresAt > DateTime.UtcNow)
                .ToListAsync();

            foreach (var storedToken in storedTokens)
            {
                var hash = _jwtService.HashToken(refreshToken, storedToken.TokenSalt);
                if (hash == storedToken.TokenHash)
                {
                    storedToken.RevokedAt = DateTime.UtcNow;
                    await _db.SaveChangesAsync();
                    _logger.LogInformation("Refresh token revoked for one session. AccountId={AccountId}", storedToken.AccountId);
                    return;
                }
            }

            throw new UnauthorizedAccessException(MessageKeys.InvalidRefreshToken);
        }

        public async Task RevokeAllRefreshTokensAsync(Guid accountId)
        {
            var revokedCount = await MarkAllRefreshTokensRevokedAsync(accountId);
            await _db.SaveChangesAsync();
            _logger.LogInformation("All refresh tokens revoked. AccountId={AccountId}, Count={Count}", accountId, revokedCount);
        }

        /// <summary>
        /// Marks every non-revoked refresh token for the account as revoked (does not call SaveChanges).
        /// </summary>
        private async Task<int> MarkAllRefreshTokensRevokedAsync(Guid accountId)
        {
            var tokens = await _db.RefreshTokens
                .Where(rt => rt.AccountId == accountId && rt.RevokedAt == null)
                .ToListAsync();

            var now = DateTime.UtcNow;
            foreach (var token in tokens)
            {
                token.RevokedAt = now;
            }

            return tokens.Count;
        }

        public async Task DeleteAccountAsync(Guid accountId, string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException(MessageKeys.PasswordRequired, nameof(password));
            }

            var account = await _db.Accounts
                .FirstOrDefaultAsync(a => a.AccountId == accountId)
                ?? throw new KeyNotFoundException(MessageKeys.AccountNotFound);

            if (account.IsActive == false || account.DeletedAt != null)
            {
                _logger.LogWarning("Delete account rejected for inactive/deleted account. AccountId={AccountId}", accountId);
                throw new UnauthorizedAccessException(MessageKeys.AccountInactiveOrDeleted);
            }

            if (string.IsNullOrWhiteSpace(account.PasswordHash))
            {
                _logger.LogWarning("Delete account rejected because account has no password. AccountId={AccountId}", accountId);
                throw new InvalidOperationException(MessageKeys.NoPasswordToDelete);
            }

            var passwordOk = _jwtService.VerifyPassword(password, account.PasswordHash);
            if (!passwordOk)
            {
                _logger.LogWarning("Delete account failed due to incorrect password. AccountId={AccountId}", accountId);
                throw new UnauthorizedAccessException(MessageKeys.CurrentPasswordIncorrect);
            }

            var deleted = await _accountHardDeleteService.HardDeleteAccountNowAsync(accountId);
            if (!deleted)
                throw new KeyNotFoundException(MessageKeys.AccountNotFound);

            _logger.LogInformation(
                "Account hard-deleted immediately. AccountId={AccountId}",
                accountId);
        }

        public async Task DeleteConsultantByAdminAsync(Guid accountId)
        {
            var account = await _db.Accounts
                .Include(a => a.Role)
                .FirstOrDefaultAsync(a => a.AccountId == accountId)
                ?? throw new KeyNotFoundException(MessageKeys.AccountNotFound);

            if (!string.Equals(account.Role.Name, ConsultantRoleName, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(MessageKeys.AccountIsNotConsultant);
            }

            var deleted = await _accountHardDeleteService.HardDeleteAccountNowAsync(accountId);
            if (!deleted)
            {
                throw new KeyNotFoundException(MessageKeys.AccountNotFound);
            }

            _logger.LogInformation(
                "Consultant hard-deleted by admin. AccountId={AccountId}",
                accountId);
        }

        public async Task<List<CredentialInfo>> GetCredentialsAsync(Guid accountId)
        {
            var credentials = await _db.Credentials
                .Where(c => c.AccountId == accountId)
                .ToListAsync();

            return credentials.Select(c => new CredentialInfo
            {
                Type = c.Type,
                Identifier = CredentialMasking.MaskIdentifier(c.Type, c.Identifier),
                EmailVerified = c.Type == "email" ? c.EmailVerified : null
            }).ToList();
        }

        public async Task<FirebaseCustomTokenResponse> CreateFirebaseCustomTokenAsync(Guid profileId)
        {
            var profileExists = await _db.Profiles
                .AsNoTracking()
                .AnyAsync(p => p.ProfileId == profileId);

            if (!profileExists)
            {
                throw new KeyNotFoundException(MessageKeys.NotFound);
            }

            var firebaseAuth = GetFirebaseAuth();
            var claims = new Dictionary<string, object>
            {
                ["profileId"] = profileId.ToString()
            };

            var customToken = await firebaseAuth.CreateCustomTokenAsync(profileId.ToString(), claims);

            _logger.LogInformation("Firebase custom token created. ProfileId={ProfileId}", profileId);

            return new FirebaseCustomTokenResponse
            {
                ProfileId = profileId,
                CustomToken = customToken
            };
        }

        // ===== Private helpers =====

        private async Task<GoogleJsonWebSignature.Payload> VerifyGoogleTokenAsync(string idToken)
        {
            var audiences = new List<string>();
            if (!string.IsNullOrWhiteSpace(_googleConfig.ClientId))
            {
                audiences.Add(_googleConfig.ClientId);
            }
            if (!string.IsNullOrWhiteSpace(_googleConfig.AndroidClientId))
            {
                audiences.Add(_googleConfig.AndroidClientId);
            }

            if (audiences.Count == 0)
            {
                throw new InvalidOperationException(MessageKeys.GoogleAuthClientIdsNotConfigured);
            }

            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = audiences.Distinct().ToArray()
            };

            try
            {
                return await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
            }
            catch (InvalidJwtException ex)
            {
                _logger.LogWarning(ex, "Invalid Google ID token");
                throw new UnauthorizedAccessException(MessageKeys.InvalidGoogleToken);
            }
        }

        private void StoreRefreshToken(Guid accountId, string rawToken, string? deviceInfo)
        {
            var salt = _jwtService.GenerateSalt();
            var hash = _jwtService.HashToken(rawToken, salt);

            var refreshToken = new RefreshToken
            {
                RefreshTokenId = Guid.NewGuid(),
                AccountId = accountId,
                TokenHash = hash,
                TokenSalt = salt,
                DeviceInfo = deviceInfo,
                ExpiresAt = _jwtService.GetRefreshTokenExpiry(),
                CreatedAt = DateTime.UtcNow
            };

            _db.RefreshTokens.Add(refreshToken);
        }

        private static AccountInfo MapAccountInfo(Account account)
        {
            return new AccountInfo
            {
                AccountId = account.AccountId,
                ProfileId = account.Profile!.ProfileId,
                FullName = account.Profile.FullName,
                AvatarUrl = account.Profile.AvatarUrl,
                Role = account.Role.Name,
                HasPassword = account.PasswordHash != null,
                MustChangePassword = account.MustChangePassword,
                Credentials = account.Credentials.Select(c => new CredentialInfo
                {
                    Type = c.Type,
                    Identifier = CredentialMasking.MaskIdentifier(c.Type, c.Identifier),
                    EmailVerified = c.Type == "email" ? c.EmailVerified : null
                }).ToList()
            };
        }

        public async Task<CreateConsultantResponse> CreateConsultantByAdminAsync(string email, string? fullName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new ArgumentException(MessageKeys.EmailRequired, nameof(email));
            }

            var normalizedEmail = email.Trim().ToLowerInvariant();
            var emailTaken = await _db.Credentials.AnyAsync(
                c => c.Type == "email" && c.Identifier.ToLower() == normalizedEmail,
                cancellationToken);
            if (emailTaken)
            {
                throw new InvalidOperationException(MessageKeys.EmailAlreadyExists);
            }

            var role = await _db.Roles.FirstOrDefaultAsync(r => r.Name == ConsultantRoleName, cancellationToken)
                ?? throw new InvalidOperationException(MessageKeys.ConsultantRoleNotFound);

            var plainPassword = GenerateConsultantTemporaryPassword();
            ValidatePasswordOrThrow(plainPassword);

            var displayName = string.IsNullOrWhiteSpace(fullName)
                ? normalizedEmail.Split('@')[0]
                : fullName.Trim();

            var account = new Account
            {
                AccountId = Guid.NewGuid(),
                RoleId = role.RoleId,
                PasswordHash = _jwtService.HashPassword(plainPassword),
                IsActive = true,
                MustChangePassword = true,
                LastLoginAt = null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var profile = new Profile
            {
                ProfileId = Guid.NewGuid(),
                AccountId = account.AccountId,
                FullName = displayName,
                UpdatedAt = DateTime.UtcNow
            };

            var emailCredential = new Credential
            {
                CredentialId = Guid.NewGuid(),
                AccountId = account.AccountId,
                Type = "email",
                Identifier = normalizedEmail,
                EmailVerified = true,
                CreatedAt = DateTime.UtcNow
            };

            _db.Accounts.Add(account);
            _db.Profiles.Add(profile);
            _db.Credentials.Add(emailCredential);
            await _db.SaveChangesAsync(cancellationToken);

            account = await _db.Accounts
                .Include(a => a.Profile)
                .Include(a => a.Role)
                .Include(a => a.Credentials)
                .FirstAsync(a => a.AccountId == account.AccountId, cancellationToken);

            await _subscriptionService.EnsureFreeSubscriptionAsync(account.Profile!.ProfileId);

            var loginUrl = (_appPublicUrls.Value.LoginUrl ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(loginUrl))
            {
                _logger.LogWarning("AppPublicUrls:LoginUrl is not configured; consultant welcome email CTA may be empty. AccountId={AccountId}", account.AccountId);
            }

            var variables = new Dictionary<string, string>
            {
                ["consultant_email"] = normalizedEmail,
                ["login_url"] = loginUrl,
                ["temporary_password"] = plainPassword
            };

            var sendResult = await _emailSender.SendTemplateAsync(
                normalizedEmail,
                ConsultantWelcomeTemplateAlias,
                variables,
                idempotencyKey: $"consultant-welcome/{account.AccountId}",
                cancellationToken).ConfigureAwait(false);

            if (!sendResult.Success)
            {
                _logger.LogWarning(
                    "Consultant welcome email failed for AccountId={AccountId}. Reason={Reason}",
                    account.AccountId,
                    sendResult.ErrorDetail ?? "unknown");
            }

            return new CreateConsultantResponse
            {
                AccountId = account.AccountId,
                ProfileId = account.Profile.ProfileId,
                Email = normalizedEmail
            };
        }

        private static string GenerateConsultantTemporaryPassword()
        {
            const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            const string lower = "abcdefghijkmnopqrstuvwxyz";
            const string digits = "23456789";
            var all = string.Concat(upper, lower, digits);
            Span<byte> randomBytes = stackalloc byte[48];
            RandomNumberGenerator.Fill(randomBytes);
            var chars = new char[14];
            chars[0] = upper[randomBytes[0] % upper.Length];
            chars[1] = lower[randomBytes[1] % lower.Length];
            chars[2] = digits[randomBytes[2] % digits.Length];
            for (var i = 3; i < 14; i++)
            {
                chars[i] = all[randomBytes[i] % all.Length];
            }

            for (var i = chars.Length - 1; i > 0; i--)
            {
                var j = randomBytes[i + 16] % (i + 1);
                (chars[i], chars[j]) = (chars[j], chars[i]);
            }

            return new string(chars);
        }

        private static void ValidatePasswordOrThrow(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException(MessageKeys.PasswordRequired, nameof(password));
            }

            if (password.Length < 6 || password.Length > 128)
            {
                throw new ArgumentException(MessageKeys.PasswordInvalidFormat, nameof(password));
            }
        }

        private async Task<AuthResponse> LoginWithCredentialAsync(
            Credential credential,
            string password,
            string? deviceInfo,
            string method,
            string identifier)
        {
            var account = credential.Account;

            if (account.IsActive == false || account.DeletedAt != null)
            {
                _logger.LogWarning("{Method} login rejected for inactive/deleted account. AccountId={AccountId}", method, account.AccountId);
                throw new UnauthorizedAccessException(MessageKeys.InvalidCredentials);
            }

            if (string.IsNullOrWhiteSpace(account.PasswordHash))
            {
                _logger.LogWarning("{Method} login rejected because account has no password. AccountId={AccountId}", method, account.AccountId);
                throw new UnauthorizedAccessException(MessageKeys.InvalidCredentials);
            }

            var passwordOk = _jwtService.VerifyPassword(password, account.PasswordHash);
            if (!passwordOk)
            {
                _logger.LogWarning("{Method} login failed due to wrong password. Identifier={Identifier}", method, identifier);
                throw new UnauthorizedAccessException(MessageKeys.InvalidCredentials);
            }

            var profile = account.Profile
                ?? throw new InvalidOperationException(MessageKeys.AccountHasNoProfile);

            var accessToken = _jwtService.GenerateAccessToken(account.AccountId, profile.ProfileId, account.Role.Name);
            var refreshToken = _jwtService.GenerateRefreshToken();
            StoreRefreshToken(account.AccountId, refreshToken, deviceInfo);

            account.LastLoginAt = DateTime.UtcNow;
            account.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            _logger.LogInformation("{Method} login success. AccountId={AccountId}, Device={DeviceInfo}", method, account.AccountId, deviceInfo ?? "unknown");

            return new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                IsNewAccount = false,
                Account = MapAccountInfo(account)
            };
        }

        private static string NormalizeVietnamPhone(string phone)
        {
            var cleaned = phone.Trim().Replace(" ", string.Empty);
            if (cleaned.StartsWith("+84") && cleaned.Length == 12)
            {
                var localPart = cleaned[3..];
                if (localPart.All(char.IsDigit))
                {
                    return cleaned;
                }
            }

            if (cleaned.Length == 10 && cleaned.StartsWith("0") && cleaned.All(char.IsDigit))
            {
                return "+84" + cleaned[1..];
            }

            throw new ArgumentException(MessageKeys.PhoneInvalidFormat, nameof(phone));
        }

        private async Task<string> VerifyFirebasePhoneTokenAsync(string firebaseIdToken)
        {
            var firebaseAuth = GetFirebaseAuth();

            try
            {
                var decoded = await firebaseAuth.VerifyIdTokenAsync(firebaseIdToken);
                if (decoded == null || decoded.Claims == null)
                {
                    throw new UnauthorizedAccessException(MessageKeys.FirebaseTokenPayloadInvalid);
                }

                if (!decoded.Claims.TryGetValue("phone_number", out var phoneObj) || phoneObj == null)
                {
                    throw new UnauthorizedAccessException(MessageKeys.FirebaseTokenNoPhoneNumber);
                }

                var firebasePhone = phoneObj.ToString();
                if (string.IsNullOrWhiteSpace(firebasePhone))
                {
                    throw new UnauthorizedAccessException(MessageKeys.FirebasePhoneNumberEmpty);
                }

                return firebasePhone.Trim();
            }
            catch (FirebaseAuthException ex)
            {
                _logger.LogWarning(ex, "Invalid Firebase ID token");
                throw new UnauthorizedAccessException(MessageKeys.InvalidFirebaseToken);
            }
            catch (NullReferenceException ex)
            {
                _logger.LogWarning(ex, "Firebase ID token payload is malformed or incomplete");
                throw new UnauthorizedAccessException(MessageKeys.InvalidFirebaseToken);
            }
        }

        private FirebaseAuth GetFirebaseAuth()
        {
            var app = FirebaseApp.DefaultInstance;
            if (app == null)
            {
                if (string.IsNullOrWhiteSpace(_firebaseConfig.ProjectId))
                {
                    throw new InvalidOperationException(MessageKeys.FirebaseProjectIdNotConfigured);
                }

                try
                {
                    app = FirebaseApp.Create(new AppOptions
                    {
                        Credential = ResolveFirebaseCredential(),
                        ProjectId = _firebaseConfig.ProjectId
                    });
                }
                catch (ArgumentException)
                {
                    // Another request may have initialized Firebase concurrently.
                    app = FirebaseApp.DefaultInstance;
                }
            }

            if (app == null)
            {
                throw new InvalidOperationException(MessageKeys.FirebaseAppNotInitialized);
            }

            return FirebaseAuth.GetAuth(app);
        }

        private GoogleCredential ResolveFirebaseCredential()
        {
            var envPath = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
            var configPath = string.IsNullOrWhiteSpace(_firebaseConfig.ServiceAccountPath)
                ? null
                : _firebaseConfig.ServiceAccountPath.Trim();

            var candidatePath = !string.IsNullOrWhiteSpace(envPath) ? envPath : configPath;
            if (!string.IsNullOrWhiteSpace(candidatePath))
            {
                if (File.Exists(candidatePath))
                {
                    return GoogleCredential.FromFile(candidatePath);
                }

                _logger.LogError(
                    "Firebase service account file not found at '{CandidatePath}'. GOOGLE_APPLICATION_CREDENTIALS='{EnvPath}', FirebaseAuth:ServiceAccountPath='{ConfigPath}'.",
                    candidatePath,
                    envPath,
                    configPath);
                throw new InvalidOperationException(MessageKeys.FirebaseServiceAccountFileNotFound);
            }

            return GoogleCredential.GetApplicationDefault();
        }
    }
}
