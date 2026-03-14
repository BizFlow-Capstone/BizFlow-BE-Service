using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.Auth;
using BizFlow.Application.Interfaces.Services;
using BizFlow.Domain.Entities;
using BizFlow.Infrastructure.DataContext;
using Google.Apis.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BizFlow.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly BizFlowDbContext _db;
        private readonly IJwtService _jwtService;
        private readonly GoogleAuthConfig _googleConfig;
        private readonly ILogger<AuthService> _logger;

        private const string DefaultRoleName = "user";

        public AuthService(
            BizFlowDbContext db,
            IJwtService jwtService,
            IOptions<GoogleAuthConfig> googleConfig,
            ILogger<AuthService> logger)
        {
            _db = db;
            _jwtService = jwtService;
            _googleConfig = googleConfig.Value;
            _logger = logger;
        }

        public async Task<AuthResponse> GoogleLoginAsync(string idToken, string? deviceInfo)
        {
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

                // Update last login
                account.LastLoginAt = DateTime.UtcNow;
                account.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                // New account — register
                var role = await _db.Roles.FirstOrDefaultAsync(r => r.Name == DefaultRoleName)
                    ?? throw new InvalidOperationException($"Default role '{DefaultRoleName}' not found");

                account = new Account
                {
                    AccountId = Guid.NewGuid(),
                    RoleId = role.RoleId,
                    PasswordHash = null, // Google-only, no password yet
                    IsActive = true,
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

                isNewAccount = true;
            }

            // 3. Issue tokens
            var profile2 = account.Profile
                ?? throw new InvalidOperationException("Account has no profile");

            var accessToken = _jwtService.GenerateAccessToken(
                account.AccountId, profile2.ProfileId, account.Role.Name);

            var refreshToken = _jwtService.GenerateRefreshToken();
            StoreRefreshToken(account.AccountId, refreshToken, deviceInfo);

            if (!isNewAccount)
            {
                await _db.SaveChangesAsync();
            }

            return new AuthResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                IsNewAccount = isNewAccount,
                Account = MapAccountInfo(account)
            };
        }

        public async Task SetPasswordAsync(Guid accountId, string password)
        {
            var account = await _db.Accounts
                .Include(a => a.Credentials)
                .FirstOrDefaultAsync(a => a.AccountId == accountId)
                ?? throw new KeyNotFoundException("Account not found");

            if (account.PasswordHash != null)
                throw new InvalidOperationException("Password is already set. Use change password instead.");

            // Hash and set password
            account.PasswordHash = _jwtService.HashPassword(password);
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
        }

        public async Task<AuthResponse> RefreshTokenAsync(string refreshToken, string? deviceInfo)
        {
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
                throw new UnauthorizedAccessException("Invalid or expired refresh token");

            // Reuse detection: if token is already revoked, revoke ALL tokens for this account
            if (matchedToken.RevokedAt != null)
            {
                await RevokeAllRefreshTokensAsync(matchedToken.AccountId);
                throw new UnauthorizedAccessException("Refresh token reuse detected. All sessions revoked.");
            }

            var account = matchedToken.Account;

            // Revoke old token
            matchedToken.RevokedAt = DateTime.UtcNow;

            // Issue new tokens
            var profile = account.Profile
                ?? throw new InvalidOperationException("Account has no profile");

            var newAccessToken = _jwtService.GenerateAccessToken(
                account.AccountId, profile.ProfileId, account.Role.Name);

            var newRefreshToken = _jwtService.GenerateRefreshToken();
            StoreRefreshToken(account.AccountId, newRefreshToken, deviceInfo);

            account.LastLoginAt = DateTime.UtcNow;
            account.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

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
                    return;
                }
            }
        }

        public async Task RevokeAllRefreshTokensAsync(Guid accountId)
        {
            var activeTokens = await _db.RefreshTokens
                .Where(rt => rt.AccountId == accountId && rt.RevokedAt == null)
                .ToListAsync();

            foreach (var token in activeTokens)
            {
                token.RevokedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();
        }

        public async Task<List<CredentialInfo>> GetCredentialsAsync(Guid accountId)
        {
            var credentials = await _db.Credentials
                .Where(c => c.AccountId == accountId)
                .ToListAsync();

            return credentials.Select(c => new CredentialInfo
            {
                Type = c.Type,
                Identifier = MaskIdentifier(c.Type, c.Identifier),
                EmailVerified = c.Type == "email" ? c.EmailVerified : null
            }).ToList();
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
                throw new InvalidOperationException("GoogleAuth client IDs are not configured");
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
                throw new UnauthorizedAccessException("Invalid Google token");
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
                Credentials = account.Credentials.Select(c => new CredentialInfo
                {
                    Type = c.Type,
                    Identifier = MaskIdentifier(c.Type, c.Identifier),
                    EmailVerified = c.Type == "email" ? c.EmailVerified : null
                }).ToList()
            };
        }

        private static string MaskIdentifier(string type, string identifier)
        {
            return type switch
            {
                "email" => MaskEmail(identifier),
                "phone" => MaskPhone(identifier),
                "google" => "Connected",
                _ => "***"
            };
        }

        private static string MaskEmail(string email)
        {
            var parts = email.Split('@');
            if (parts.Length != 2) return "***@***";
            var name = parts[0];
            var masked = name.Length <= 2
                ? name + "***"
                : name[..2] + new string('*', name.Length - 2);
            return masked + "@" + parts[1];
        }

        private static string MaskPhone(string phone)
        {
            if (phone.Length <= 4) return "***";
            return phone[..4] + new string('*', phone.Length - 7) + phone[^3..];
        }
    }
}
