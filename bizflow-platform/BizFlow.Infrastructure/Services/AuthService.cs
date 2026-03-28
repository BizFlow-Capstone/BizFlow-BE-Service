using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.Auth;
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

namespace BizFlow.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly BizFlowDbContext _db;
        private readonly IJwtService _jwtService;
        private readonly GoogleAuthConfig _googleConfig;
        private readonly FirebaseAuthConfig _firebaseConfig;
        private readonly ILogger<AuthService> _logger;

        private const string DefaultRoleName = "user";

        public AuthService(
            BizFlowDbContext db,
            IJwtService jwtService,
            IOptions<GoogleAuthConfig> googleConfig,
            IOptions<FirebaseAuthConfig> firebaseConfig,
            ILogger<AuthService> logger)
        {
            _db = db;
            _jwtService = jwtService;
            _googleConfig = googleConfig.Value;
            _firebaseConfig = firebaseConfig.Value;
            _logger = logger;
        }

        public async Task<AuthResponse> GoogleLoginAsync(string idToken, string? deviceInfo)
        {
            if (string.IsNullOrWhiteSpace(idToken))
            {
                throw new ArgumentException("Id token is required", nameof(idToken));
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
                    throw new UnauthorizedAccessException("Account is inactive or deleted");
                }

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
                throw new ArgumentException("Email is required", nameof(email));
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException("Password is required", nameof(password));
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
                throw new UnauthorizedAccessException("Invalid credentials");
            }

            return await LoginWithCredentialAsync(credential, password, deviceInfo, "email", normalizedEmail);
        }

        public async Task<AuthResponse> LoginWithPhoneAsync(string phone, string password, string? deviceInfo)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                throw new ArgumentException("Phone is required", nameof(phone));
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException("Password is required", nameof(password));
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
                throw new UnauthorizedAccessException("Invalid credentials");
            }

            return await LoginWithCredentialAsync(credential, password, deviceInfo, "phone", normalizedPhone);
        }

        public async Task<AuthResponse> RegisterWithPhoneAsync(string phone, string password, string firebaseIdToken, string? fullName, string? deviceInfo)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                throw new ArgumentException("Phone is required", nameof(phone));
            }

            if (string.IsNullOrWhiteSpace(firebaseIdToken))
            {
                throw new ArgumentException("Firebase token is required", nameof(firebaseIdToken));
            }

            ValidatePasswordOrThrow(password);

            var normalizedPhone = NormalizeVietnamPhone(phone);
            var verifiedPhone = await VerifyFirebasePhoneTokenAsync(firebaseIdToken);

            if (!string.Equals(normalizedPhone, verifiedPhone, StringComparison.Ordinal))
            {
                _logger.LogWarning("Phone registration mismatch. RequestPhone={RequestPhone}, FirebasePhone={FirebasePhone}", normalizedPhone, verifiedPhone);
                throw new InvalidOperationException("PHONE_VERIFICATION_MISMATCH");
            }

            var phoneExists = await _db.Credentials.AnyAsync(c => c.Type == "phone" && c.Identifier == normalizedPhone);
            if (phoneExists)
            {
                _logger.LogWarning("Phone registration failed. Phone already exists: {Phone}", normalizedPhone);
                throw new InvalidOperationException("PHONE_ALREADY_EXISTS");
            }

            var role = await _db.Roles.FirstOrDefaultAsync(r => r.Name == DefaultRoleName)
                ?? throw new InvalidOperationException($"Default role '{DefaultRoleName}' not found");

            var account = new Account
            {
                AccountId = Guid.NewGuid(),
                RoleId = role.RoleId,
                PasswordHash = _jwtService.HashPassword(password),
                IsActive = true,
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
                throw new ArgumentException("Phone is required", nameof(phone));
            }

            if (string.IsNullOrWhiteSpace(firebaseIdToken))
            {
                throw new ArgumentException("Firebase token is required", nameof(firebaseIdToken));
            }

            var account = await _db.Accounts
                .Include(a => a.Credentials)
                .FirstOrDefaultAsync(a => a.AccountId == accountId)
                ?? throw new KeyNotFoundException("Account not found");

            var alreadyLinked = account.Credentials.Any(c => c.Type == "phone");
            if (alreadyLinked)
            {
                throw new InvalidOperationException("PHONE_ALREADY_LINKED");
            }

            var normalizedPhone = NormalizeVietnamPhone(phone);
            var verifiedPhone = await VerifyFirebasePhoneTokenAsync(firebaseIdToken);

            if (!string.Equals(normalizedPhone, verifiedPhone, StringComparison.Ordinal))
            {
                _logger.LogWarning("Phone linking mismatch. RequestPhone={RequestPhone}, FirebasePhone={FirebasePhone}", normalizedPhone, verifiedPhone);
                throw new InvalidOperationException("PHONE_VERIFICATION_MISMATCH");
            }

            var usedByOtherAccount = await _db.Credentials.AnyAsync(c => c.Type == "phone" && c.Identifier == normalizedPhone && c.AccountId != accountId);
            if (usedByOtherAccount)
            {
                throw new InvalidOperationException("PHONE_ALREADY_EXISTS");
            }

            if (string.IsNullOrWhiteSpace(account.PasswordHash))
            {
                if (string.IsNullOrWhiteSpace(password))
                {
                    throw new ArgumentException("Password is required when account has no password", nameof(password));
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
                Identifier = MaskIdentifier(c.Type, c.Identifier),
                EmailVerified = c.Type == "email" ? c.EmailVerified : null
            }).ToList();
        }

        public async Task SetPasswordAsync(Guid accountId, string password)
        {
            ValidatePasswordOrThrow(password);

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
            _logger.LogInformation("Password set for account. AccountId={AccountId}", accountId);
        }

        public async Task<AuthResponse> RefreshTokenAsync(string refreshToken, string? deviceInfo)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                throw new ArgumentException("Refresh token is required", nameof(refreshToken));
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
                throw new UnauthorizedAccessException("Invalid or expired refresh token");

            // Reuse detection: if token is already revoked, revoke ALL tokens for this account
            if (matchedToken.RevokedAt != null)
            {
                _logger.LogWarning("Refresh token reuse detected. AccountId={AccountId}", matchedToken.AccountId);
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
                throw new ArgumentException("Refresh token is required", nameof(refreshToken));
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

            throw new UnauthorizedAccessException("Invalid refresh token");
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
            _logger.LogInformation("All refresh tokens revoked. AccountId={AccountId}, Count={Count}", accountId, activeTokens.Count);
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

        public async Task<FirebaseCustomTokenResponse> CreateFirebaseCustomTokenAsync(Guid profileId)
        {
            var profileExists = await _db.Profiles
                .AsNoTracking()
                .AnyAsync(p => p.ProfileId == profileId);

            if (!profileExists)
            {
                throw new KeyNotFoundException("Profile not found");
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

        private static void ValidatePasswordOrThrow(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException("Password is required", nameof(password));
            }

            if (password.Length < 6 || password.Length > 128)
            {
                throw new ArgumentException("Password must be between 6 and 128 characters", nameof(password));
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
                throw new UnauthorizedAccessException("Invalid credentials");
            }

            if (string.IsNullOrWhiteSpace(account.PasswordHash))
            {
                _logger.LogWarning("{Method} login rejected because account has no password. AccountId={AccountId}", method, account.AccountId);
                throw new UnauthorizedAccessException("Invalid credentials");
            }

            var passwordOk = _jwtService.VerifyPassword(password, account.PasswordHash);
            if (!passwordOk)
            {
                _logger.LogWarning("{Method} login failed due to wrong password. Identifier={Identifier}", method, identifier);
                throw new UnauthorizedAccessException("Invalid credentials");
            }

            var profile = account.Profile
                ?? throw new InvalidOperationException("Account has no profile");

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

            throw new ArgumentException("Phone number must be in format 0xxxxxxxxx or +84xxxxxxxxx", nameof(phone));
        }

        private async Task<string> VerifyFirebasePhoneTokenAsync(string firebaseIdToken)
        {
            var firebaseAuth = GetFirebaseAuth();

            try
            {
                var decoded = await firebaseAuth.VerifyIdTokenAsync(firebaseIdToken);
                if (decoded == null || decoded.Claims == null)
                {
                    throw new UnauthorizedAccessException("Invalid Firebase token payload");
                }

                if (!decoded.Claims.TryGetValue("phone_number", out var phoneObj) || phoneObj == null)
                {
                    throw new UnauthorizedAccessException("Firebase token has no verified phone number");
                }

                var firebasePhone = phoneObj.ToString();
                if (string.IsNullOrWhiteSpace(firebasePhone))
                {
                    throw new UnauthorizedAccessException("Firebase phone number is empty");
                }

                return firebasePhone.Trim();
            }
            catch (FirebaseAuthException ex)
            {
                _logger.LogWarning(ex, "Invalid Firebase ID token");
                throw new UnauthorizedAccessException("Invalid Firebase token");
            }
            catch (NullReferenceException ex)
            {
                _logger.LogWarning(ex, "Firebase ID token payload is malformed or incomplete");
                throw new UnauthorizedAccessException("Invalid Firebase token");
            }
        }

        private FirebaseAuth GetFirebaseAuth()
        {
            var app = FirebaseApp.DefaultInstance;
            if (app == null)
            {
                if (string.IsNullOrWhiteSpace(_firebaseConfig.ProjectId))
                {
                    throw new InvalidOperationException("FirebaseAuth:ProjectId is not configured");
                }

                try
                {
                    app = FirebaseApp.Create(new AppOptions
                    {
                        Credential = GoogleCredential.GetApplicationDefault(),
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
                throw new InvalidOperationException("Firebase app is not initialized");
            }

            return FirebaseAuth.GetAuth(app);
        }
    }
}
