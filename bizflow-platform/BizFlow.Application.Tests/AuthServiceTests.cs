using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Exceptions;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.Auth;
using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Application.Interfaces.Services;
using BizFlow.Domain.Entities;
using BizFlow.Infrastructure.DataContext;
using BizFlow.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace BizFlow.Application.Tests;

public class AuthServiceTests : IDisposable
{
    private readonly BizFlowDbContext _db;
    private readonly Mock<IJwtService> _jwtService = new();
    private readonly Mock<ISubscriptionService> _subscriptionService = new();
    private readonly Mock<IImageService> _imageService = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IAccountRepository> _accountRepository = new();
    private readonly Mock<IOtpService> _otpService = new();
    private readonly Mock<IEmailSender> _emailSender = new();
    private readonly IOptions<GoogleAuthConfig> _googleConfig = Options.Create(new GoogleAuthConfig());
    private readonly IOptions<FirebaseAuthConfig> _firebaseConfig = Options.Create(new FirebaseAuthConfig());
    private readonly IOptions<AppPublicUrlsOptions> _appUrls = Options.Create(new AppPublicUrlsOptions());

    public AuthServiceTests()
    {
        var options = new DbContextOptionsBuilder<BizFlowDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        _db = new BizFlowDbContext(options);

        // Seed Roles
        var userRoleId = Guid.NewGuid();
        _db.Roles.AddRange(
            new Role { RoleId = userRoleId, Name = "user" },
            new Role { RoleId = Guid.NewGuid(), Name = "admin" },
            new Role { RoleId = Guid.NewGuid(), Name = "consultant" },
            new Role { RoleId = Guid.NewGuid(), Name = "owner" }
        );
        _db.SaveChanges();

        _jwtService.Setup(j => j.HashPassword(It.IsAny<string>())).Returns((string pwd) => $"hashed_{pwd}");
        _jwtService.Setup(j => j.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string raw, string hashed) => $"hashed_{raw}" == hashed);
        _jwtService.Setup(j => j.GenerateAccessToken(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<Guid?>()))
            .Returns("fake_access_token");
        _jwtService.Setup(j => j.GenerateRefreshToken())
            .Returns("fake_refresh_token");
        _jwtService.Setup(j => j.GenerateSalt()).Returns("salt");
        _jwtService.Setup(j => j.GetRefreshTokenExpiry()).Returns(DateTime.UtcNow.AddDays(7));
        _jwtService.Setup(j => j.HashToken(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string raw, string salt) => $"token_hash_{raw}_{salt}");

        _subscriptionService.Setup(s => s.EnsureFreeSubscriptionAsync(It.IsAny<Guid>()))
            .Returns(Task.CompletedTask);

        _uow.SetupGet(x => x.Accounts).Returns(_accountRepository.Object);
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }

    private AuthService BuildSut() => new(
        _db,
        _jwtService.Object,
        _subscriptionService.Object,
        _googleConfig,
        _firebaseConfig,
        _imageService.Object,
        _uow.Object,
        _otpService.Object,
        _emailSender.Object,
        _appUrls,
        NullLogger<AuthService>.Instance);

    // ═══════════════════════════════════════════════════
    // LOGIN WITH EMAIL
    // ═══════════════════════════════════════════════════

    [Fact]
    public async Task LoginWithEmailAsync_WhenNotFound_ShouldThrowUnauthorized()
    {
        var sut = BuildSut();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => sut.LoginWithEmailAsync("notfound@mail.com", "pass", null));
    }

    [Fact]
    public async Task LoginWithEmailAsync_WhenWrongPassword_ShouldThrowUnauthorized()
    {
        // Seed Account
        var accountId = Guid.NewGuid();
        _db.Accounts.Add(new Account
        {
            AccountId = accountId, IsActive = true, PasswordHash = "hashed_correct", RoleId = _db.Roles.First(r => r.Name == "user").RoleId,
            Profile = new Profile { ProfileId = Guid.NewGuid(), FullName = "Test" },
            Credentials = new List<Credential> { new Credential { Type = "email", Identifier = "test@mail.com" } }
        });
        await _db.SaveChangesAsync();

        var sut = BuildSut();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => sut.LoginWithEmailAsync("test@mail.com", "wrong", null));
    }

    [Fact]
    public async Task LoginWithEmailAsync_WhenValid_ShouldReturnTokens()
    {
        var accountId = Guid.NewGuid();
        _db.Accounts.Add(new Account
        {
            AccountId = accountId, IsActive = true, PasswordHash = "hashed_correct", RoleId = _db.Roles.First(r => r.Name == "user").RoleId,
            Profile = new Profile { ProfileId = Guid.NewGuid(), FullName = "Test" },
            Credentials = new List<Credential> { new Credential { Type = "email", Identifier = "test@mail.com" } }
        });
        await _db.SaveChangesAsync();

        var sut = BuildSut();
        var result = await sut.LoginWithEmailAsync("test@mail.com", "correct", "web");

        Assert.Equal("fake_access_token", result.AccessToken);
        Assert.Equal("fake_refresh_token", result.RefreshToken);
        Assert.False(result.IsNewAccount);
        
        var account = await _db.Accounts.FindAsync(accountId);
        Assert.NotNull(account);
        Assert.Single(_db.RefreshTokens);
    }

    // ═══════════════════════════════════════════════════
    // PASSWORD MANAGEMENT
    // ═══════════════════════════════════════════════════

    [Fact]
    public async Task SetPasswordAsync_WhenAlreadySet_ShouldThrowInvalidOperation()
    {
        var accountId = Guid.NewGuid();
        _db.Accounts.Add(new Account { AccountId = accountId, PasswordHash = "existing", IsActive = true, RoleId = _db.Roles.First(r => r.Name == "user").RoleId });
        await _db.SaveChangesAsync();

        var sut = BuildSut();
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.SetPasswordAsync(accountId, "newpass123"));
    }

    [Fact]
    public async Task ChangePasswordAsync_WhenNoPasswordSet_ShouldThrowInvalidOperation()
    {
        var accountId = Guid.NewGuid();
        _db.Accounts.Add(new Account { AccountId = accountId, PasswordHash = null, IsActive = true, RoleId = _db.Roles.First(r => r.Name == "user").RoleId });
        await _db.SaveChangesAsync();

        var sut = BuildSut();
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ChangePasswordAsync(accountId, "old", "newpass123"));
    }

    [Fact]
    public async Task ChangePasswordAsync_WhenValid_ShouldChangeAndRevokeTokens()
    {
        var accountId = Guid.NewGuid();
        _db.Accounts.Add(new Account { AccountId = accountId, PasswordHash = "hashed_oldpass", IsActive = true, RoleId = _db.Roles.First(r => r.Name == "user").RoleId });
        _db.RefreshTokens.Add(new RefreshToken { RefreshTokenId = Guid.NewGuid(), AccountId = accountId, ExpiresAt = DateTime.UtcNow.AddDays(1), TokenSalt = "salt", TokenHash = "token_hash" });
        await _db.SaveChangesAsync();

        var sut = BuildSut();
        await sut.ChangePasswordAsync(accountId, "oldpass", "newpass123");

        var token = await _db.RefreshTokens.FirstAsync();
        var account = await _db.Accounts.FirstAsync();

        Assert.NotNull(token.RevokedAt); // Verify tokens revoked
        Assert.Equal("hashed_newpass123", account.PasswordHash);
    }

    // ═══════════════════════════════════════════════════
    // PROFILE UPDATES
    // ═══════════════════════════════════════════════════

    [Fact]
    public async Task UpdateProfileInfoAsync_WhenEmptyName_ShouldThrowArgumentException()
    {
        var profileId = Guid.NewGuid();
        _db.Profiles.Add(new Profile { ProfileId = profileId, FullName = "Old", Account = new Account { IsActive = true } });
        await _db.SaveChangesAsync();

        var req = new UpdateProfileInfoRequest { FullName = System.Text.Json.JsonDocument.Parse("\"   \"").RootElement };

        var sut = BuildSut();
        await Assert.ThrowsAsync<ArgumentException>(() => sut.UpdateProfileInfoAsync(profileId, req));
    }

    // ═══════════════════════════════════════════════════
    // REFRESH TOKEN & SESSION SECURITY
    // ═══════════════════════════════════════════════════

    [Fact]
    public async Task RefreshTokenAsync_WhenInvalidToken_ShouldThrowUnauthorized()
    {
        var accountId = Guid.NewGuid();
        var roleId = _db.Roles.First(r => r.Name == "user").RoleId;
        _db.Accounts.Add(new Account
        {
            AccountId = accountId,
            RoleId = roleId,
            IsActive = true,
            PasswordHash = "hashed_password",
            Profile = new Profile { ProfileId = Guid.NewGuid(), FullName = "User" }
        });
        await _db.SaveChangesAsync();

        _db.RefreshTokens.Add(new RefreshToken
        {
            RefreshTokenId = Guid.NewGuid(),
            AccountId = accountId,
            TokenSalt = "salt",
            TokenHash = "token_hash_existing_token_salt",
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        });
        await _db.SaveChangesAsync();

        var sut = BuildSut();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => sut.RefreshTokenAsync("unknown_token", "web"));
    }

    [Fact]
    public async Task RefreshTokenAsync_WhenValid_ShouldRotateTokenAndRevokeOldOne()
    {
        var accountId = Guid.NewGuid();
        var roleId = _db.Roles.First(r => r.Name == "user").RoleId;
        _db.Accounts.Add(new Account
        {
            AccountId = accountId,
            RoleId = roleId,
            IsActive = true,
            PasswordHash = "hashed_password",
            Profile = new Profile { ProfileId = Guid.NewGuid(), FullName = "User" },
            Credentials = new List<Credential> { new Credential { Type = "email", Identifier = "u@mail.com" } }
        });
        await _db.SaveChangesAsync();

        var originalToken = new RefreshToken
        {
            RefreshTokenId = Guid.NewGuid(),
            AccountId = accountId,
            TokenSalt = "salt",
            TokenHash = "token_hash_old_token_salt",
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };
        _db.RefreshTokens.Add(originalToken);
        await _db.SaveChangesAsync();

        var sut = BuildSut();
        var result = await sut.RefreshTokenAsync("old_token", "mobile-ios");

        Assert.Equal("fake_access_token", result.AccessToken);
        Assert.Equal("fake_refresh_token", result.RefreshToken);

        var tokens = await _db.RefreshTokens.Where(t => t.AccountId == accountId).OrderBy(t => t.CreatedAt).ToListAsync();
        Assert.Equal(2, tokens.Count);
        Assert.NotNull(tokens[0].RevokedAt);
        Assert.Null(tokens[1].RevokedAt);
        Assert.Equal("mobile-ios", tokens[1].DeviceInfo);
    }

    [Fact]
    public async Task RefreshTokenAsync_WhenReuseDetected_ShouldRevokeAllAndThrowUnauthorized()
    {
        var accountId = Guid.NewGuid();
        var roleId = _db.Roles.First(r => r.Name == "user").RoleId;
        _db.Accounts.Add(new Account
        {
            AccountId = accountId,
            RoleId = roleId,
            IsActive = true,
            PasswordHash = "hashed_password",
            Profile = new Profile { ProfileId = Guid.NewGuid(), FullName = "User" },
            Credentials = new List<Credential> { new Credential { Type = "email", Identifier = "u@mail.com" } }
        });
        await _db.SaveChangesAsync();

        _db.RefreshTokens.AddRange(
            new RefreshToken
            {
                RefreshTokenId = Guid.NewGuid(),
                AccountId = accountId,
                TokenSalt = "salt",
                TokenHash = "token_hash_reused_token_salt",
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                RevokedAt = DateTime.UtcNow.AddMinutes(-1)
            },
            new RefreshToken
            {
                RefreshTokenId = Guid.NewGuid(),
                AccountId = accountId,
                TokenSalt = "salt",
                TokenHash = "token_hash_other_active_salt",
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            });
        await _db.SaveChangesAsync();

        var sut = BuildSut();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => sut.RefreshTokenAsync("reused_token", "android"));

        var allTokens = await _db.RefreshTokens.Where(t => t.AccountId == accountId).ToListAsync();
        Assert.All(allTokens, t => Assert.NotNull(t.RevokedAt));
    }

    [Fact]
    public async Task RevokeRefreshTokenAsync_WhenValid_ShouldRevokeOnlyMatchedToken()
    {
        var accountId = Guid.NewGuid();
        var roleId = _db.Roles.First(r => r.Name == "user").RoleId;
        _db.Accounts.Add(new Account
        {
            AccountId = accountId,
            RoleId = roleId,
            IsActive = true,
            PasswordHash = "hashed_password",
            Profile = new Profile { ProfileId = Guid.NewGuid(), FullName = "User" }
        });

        var tokenA = new RefreshToken
        {
            RefreshTokenId = Guid.NewGuid(),
            AccountId = accountId,
            TokenSalt = "salt",
            TokenHash = "token_hash_logout_me_salt",
            ExpiresAt = DateTime.UtcNow.AddHours(2)
        };
        var tokenB = new RefreshToken
        {
            RefreshTokenId = Guid.NewGuid(),
            AccountId = accountId,
            TokenSalt = "salt",
            TokenHash = "token_hash_keep_alive_salt",
            ExpiresAt = DateTime.UtcNow.AddHours(2)
        };
        _db.RefreshTokens.AddRange(tokenA, tokenB);
        await _db.SaveChangesAsync();

        var sut = BuildSut();
        await sut.RevokeRefreshTokenAsync("logout_me");

        var refreshedA = await _db.RefreshTokens.FirstAsync(t => t.RefreshTokenId == tokenA.RefreshTokenId);
        var refreshedB = await _db.RefreshTokens.FirstAsync(t => t.RefreshTokenId == tokenB.RefreshTokenId);

        Assert.NotNull(refreshedA.RevokedAt);
        Assert.Null(refreshedB.RevokedAt);
    }

    [Fact]
    public async Task RevokeAllRefreshTokensAsync_ShouldRevokeOnlyTargetAccountTokens()
    {
        var roleId = _db.Roles.First(r => r.Name == "user").RoleId;
        var accountA = Guid.NewGuid();
        var accountB = Guid.NewGuid();

        _db.Accounts.AddRange(
            new Account { AccountId = accountA, RoleId = roleId, IsActive = true, PasswordHash = "hashed_a", Profile = new Profile { ProfileId = Guid.NewGuid(), FullName = "A" } },
            new Account { AccountId = accountB, RoleId = roleId, IsActive = true, PasswordHash = "hashed_b", Profile = new Profile { ProfileId = Guid.NewGuid(), FullName = "B" } });

        _db.RefreshTokens.AddRange(
            new RefreshToken { RefreshTokenId = Guid.NewGuid(), AccountId = accountA, TokenSalt = "salt", TokenHash = "ha", ExpiresAt = DateTime.UtcNow.AddHours(2) },
            new RefreshToken { RefreshTokenId = Guid.NewGuid(), AccountId = accountA, TokenSalt = "salt", TokenHash = "hb", ExpiresAt = DateTime.UtcNow.AddHours(2) },
            new RefreshToken { RefreshTokenId = Guid.NewGuid(), AccountId = accountB, TokenSalt = "salt", TokenHash = "hc", ExpiresAt = DateTime.UtcNow.AddHours(2) });
        await _db.SaveChangesAsync();

        var sut = BuildSut();
        await sut.RevokeAllRefreshTokensAsync(accountA);

        var tokensA = await _db.RefreshTokens.Where(t => t.AccountId == accountA).ToListAsync();
        var tokensB = await _db.RefreshTokens.Where(t => t.AccountId == accountB).ToListAsync();

        Assert.All(tokensA, t => Assert.NotNull(t.RevokedAt));
        Assert.All(tokensB, t => Assert.Null(t.RevokedAt));
    }

    // ═══════════════════════════════════════════════════
    // FORGOT PASSWORD / OTP NONCE
    // ═══════════════════════════════════════════════════

    [Fact]
    public async Task VerifyEmailOtpForPasswordResetAsync_WhenOtpValid_ShouldReturnPasswordResetAccessToken()
    {
        var accountId = Guid.NewGuid();
        var profileId = Guid.NewGuid();
        var nonce = Guid.NewGuid();

        _otpService.Setup(s => s.VerifyEmailOtpForPasswordResetAsync("reset@mail.com", "123456", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BizFlow.Application.DTOs.Otp.PasswordResetOtpVerifiedResult
            {
                AccountId = accountId,
                ProfileId = profileId,
                RoleName = "user",
                PasswordResetNonce = nonce
            });

        var sut = BuildSut();
        var result = await sut.VerifyEmailOtpForPasswordResetAsync("reset@mail.com", "123456", CancellationToken.None);

        Assert.True(result.Verified);
        Assert.Equal("fake_access_token", result.AccessToken);
        _jwtService.Verify(j => j.GenerateAccessToken(accountId, profileId, "user", true, nonce), Times.Once);
    }

    [Fact]
    public async Task ResetPasswordAfterForgotOtpAsync_WhenNonceMismatch_ShouldThrowBadRequest()
    {
        var account = new Account
        {
            AccountId = Guid.NewGuid(),
            RoleId = _db.Roles.First(r => r.Name == "user").RoleId,
            IsActive = true,
            PasswordHash = "hashed_old",
            PasswordResetNonce = Guid.NewGuid(),
            Profile = new Profile { ProfileId = Guid.NewGuid(), FullName = "Reset User" }
        };

        _accountRepository.Setup(r => r.GetTrackedByIdAsync(account.AccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var sut = BuildSut();
        await Assert.ThrowsAsync<BadRequestException>(() =>
            sut.ResetPasswordAfterForgotOtpAsync(account.AccountId, "newpass123", Guid.NewGuid()));
    }

    [Fact]
    public async Task ResetPasswordAfterForgotOtpAsync_WhenValid_ShouldUpdatePasswordClearNonceAndRevokeAllTokens()
    {
        var accountId = Guid.NewGuid();
        var nonce = Guid.NewGuid();
        var roleId = _db.Roles.First(r => r.Name == "user").RoleId;

        _db.Accounts.Add(new Account
        {
            AccountId = accountId,
            RoleId = roleId,
            IsActive = true,
            PasswordHash = "hashed_old",
            PasswordResetNonce = nonce,
            MustChangePassword = true,
            Profile = new Profile { ProfileId = Guid.NewGuid(), FullName = "Reset User" }
        });

        _db.RefreshTokens.AddRange(
            new RefreshToken { RefreshTokenId = Guid.NewGuid(), AccountId = accountId, TokenSalt = "salt", TokenHash = "h1", ExpiresAt = DateTime.UtcNow.AddHours(1) },
            new RefreshToken { RefreshTokenId = Guid.NewGuid(), AccountId = accountId, TokenSalt = "salt", TokenHash = "h2", ExpiresAt = DateTime.UtcNow.AddHours(1) });
        await _db.SaveChangesAsync();

        var tracked = await _db.Accounts.FirstAsync(a => a.AccountId == accountId);
        _accountRepository.Setup(r => r.GetTrackedByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tracked);

        var sut = BuildSut();
        await sut.ResetPasswordAfterForgotOtpAsync(accountId, "newpass123", nonce);

        var dbAccount = await _db.Accounts.FirstAsync(a => a.AccountId == accountId);
        Assert.Equal("hashed_newpass123", dbAccount.PasswordHash);
        Assert.Null(dbAccount.PasswordResetNonce);
        Assert.False(dbAccount.MustChangePassword);

        var tokens = await _db.RefreshTokens.Where(t => t.AccountId == accountId).ToListAsync();
        Assert.All(tokens, t => Assert.NotNull(t.RevokedAt));
    }

    // ═══════════════════════════════════════════════════
    // CHANGE PASSWORD / ACCOUNT SECURITY
    // ═══════════════════════════════════════════════════

    [Fact]
    public async Task ChangePasswordAsync_WhenMustChangePasswordAndCurrentMissing_ShouldAllowChange()
    {
        var accountId = Guid.NewGuid();
        var roleId = _db.Roles.First(r => r.Name == "user").RoleId;
        _db.Accounts.Add(new Account
        {
            AccountId = accountId,
            RoleId = roleId,
            IsActive = true,
            PasswordHash = "hashed_temp123",
            MustChangePassword = true
        });
        _db.RefreshTokens.Add(new RefreshToken
        {
            RefreshTokenId = Guid.NewGuid(),
            AccountId = accountId,
            TokenSalt = "salt",
            TokenHash = "h",
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        });
        await _db.SaveChangesAsync();

        var sut = BuildSut();
        await sut.ChangePasswordAsync(accountId, null, "finalpass123");

        var account = await _db.Accounts.FirstAsync(a => a.AccountId == accountId);
        var token = await _db.RefreshTokens.FirstAsync(t => t.AccountId == accountId);
        Assert.Equal("hashed_finalpass123", account.PasswordHash);
        Assert.False(account.MustChangePassword);
        Assert.NotNull(token.RevokedAt);
    }

    [Fact]
    public async Task ChangePasswordAsync_WhenNewPasswordSameAsCurrent_ShouldThrowInvalidOperation()
    {
        var accountId = Guid.NewGuid();
        _db.Accounts.Add(new Account
        {
            AccountId = accountId,
            RoleId = _db.Roles.First(r => r.Name == "user").RoleId,
            IsActive = true,
            PasswordHash = "hashed_samepass"
        });
        await _db.SaveChangesAsync();

        var sut = BuildSut();
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ChangePasswordAsync(accountId, "samepass", "samepass"));
    }

    [Fact]
    public async Task DeleteAccountAsync_WhenValid_ShouldSoftDeleteAndRevokeAllTokens()
    {
        var accountId = Guid.NewGuid();
        _db.Accounts.Add(new Account
        {
            AccountId = accountId,
            RoleId = _db.Roles.First(r => r.Name == "user").RoleId,
            IsActive = true,
            PasswordHash = "hashed_secret123"
        });
        _db.RefreshTokens.AddRange(
            new RefreshToken { RefreshTokenId = Guid.NewGuid(), AccountId = accountId, TokenSalt = "salt", TokenHash = "h1", ExpiresAt = DateTime.UtcNow.AddHours(1) },
            new RefreshToken { RefreshTokenId = Guid.NewGuid(), AccountId = accountId, TokenSalt = "salt", TokenHash = "h2", ExpiresAt = DateTime.UtcNow.AddHours(1) });
        await _db.SaveChangesAsync();

        var sut = BuildSut();
        await sut.DeleteAccountAsync(accountId, "secret123");

        var account = await _db.Accounts.FirstAsync(a => a.AccountId == accountId);
        var tokens = await _db.RefreshTokens.Where(t => t.AccountId == accountId).ToListAsync();

        Assert.False(account.IsActive ?? true);
        Assert.NotNull(account.DeletedAt);
        Assert.All(tokens, t => Assert.NotNull(t.RevokedAt));
    }

    [Fact]
    public async Task SetPasswordAsync_WhenGoogleCredentialExists_ShouldSetPasswordAndCreateEmailCredential()
    {
        var accountId = Guid.NewGuid();
        var googleEmail = "new-google@mail.com";
        _db.Accounts.Add(new Account
        {
            AccountId = accountId,
            RoleId = _db.Roles.First(r => r.Name == "user").RoleId,
            IsActive = true,
            PasswordHash = null,
            Credentials = new List<Credential>
            {
                new Credential { CredentialId = Guid.NewGuid(), AccountId = accountId, Type = "google", Identifier = "google-sub", GoogleEmail = googleEmail }
            }
        });
        await _db.SaveChangesAsync();

        var sut = BuildSut();
        await sut.SetPasswordAsync(accountId, "newpass123");

        var account = await _db.Accounts.Include(a => a.Credentials).FirstAsync(a => a.AccountId == accountId);
        Assert.Equal("hashed_newpass123", account.PasswordHash);
        Assert.Contains(account.Credentials, c => c.Type == "email" && c.Identifier == googleEmail && c.EmailVerified);
    }

    [Fact]
    public async Task RevokeRefreshTokenAsync_WhenTokenInvalid_ShouldThrowUnauthorized()
    {
        var accountId = Guid.NewGuid();
        _db.Accounts.Add(new Account
        {
            AccountId = accountId,
            RoleId = _db.Roles.First(r => r.Name == "user").RoleId,
            IsActive = true,
            PasswordHash = "hashed_any"
        });
        _db.RefreshTokens.Add(new RefreshToken
        {
            RefreshTokenId = Guid.NewGuid(),
            AccountId = accountId,
            TokenSalt = "salt",
            TokenHash = "token_hash_other_salt",
            ExpiresAt = DateTime.UtcNow.AddHours(2)
        });
        await _db.SaveChangesAsync();

        var sut = BuildSut();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => sut.RevokeRefreshTokenAsync("not_found_token"));
    }

    [Fact]
    public async Task DeleteAccountAsync_WhenWrongPassword_ShouldThrowUnauthorized()
    {
        var accountId = Guid.NewGuid();
        _db.Accounts.Add(new Account
        {
            AccountId = accountId,
            RoleId = _db.Roles.First(r => r.Name == "user").RoleId,
            IsActive = true,
            PasswordHash = "hashed_correct_pass"
        });
        await _db.SaveChangesAsync();

        var sut = BuildSut();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => sut.DeleteAccountAsync(accountId, "wrong_pass"));
    }

    [Fact]
    public async Task DeleteAccountAsync_WhenNoPasswordSet_ShouldThrowInvalidOperation()
    {
        var accountId = Guid.NewGuid();
        _db.Accounts.Add(new Account
        {
            AccountId = accountId,
            RoleId = _db.Roles.First(r => r.Name == "user").RoleId,
            IsActive = true,
            PasswordHash = null
        });
        await _db.SaveChangesAsync();

        var sut = BuildSut();
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.DeleteAccountAsync(accountId, "secret123"));
    }

    [Fact]
    public async Task DeleteAccountAsync_WhenAccountInactive_ShouldThrowUnauthorized()
    {
        var accountId = Guid.NewGuid();
        _db.Accounts.Add(new Account
        {
            AccountId = accountId,
            RoleId = _db.Roles.First(r => r.Name == "user").RoleId,
            IsActive = false,
            PasswordHash = "hashed_secret123"
        });
        await _db.SaveChangesAsync();

        var sut = BuildSut();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => sut.DeleteAccountAsync(accountId, "secret123"));
    }

    [Fact]
    public async Task CreateFirebaseCustomTokenAsync_WhenProfileNotFound_ShouldThrowKeyNotFound()
    {
        var sut = BuildSut();
        await Assert.ThrowsAsync<KeyNotFoundException>(() => sut.CreateFirebaseCustomTokenAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetProfileAsync_WhenNotFound_ShouldThrowKeyNotFound()
    {
        var sut = BuildSut();
        await Assert.ThrowsAsync<KeyNotFoundException>(() => sut.GetProfileAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetCredentialsAsync_WhenHasCredentials_ShouldReturnMaskedList()
    {
        var accountId = Guid.NewGuid();
        _db.Credentials.AddRange(
            new Credential { CredentialId = Guid.NewGuid(), AccountId = accountId, Type = "email", Identifier = "mask@mail.com", EmailVerified = true },
            new Credential { CredentialId = Guid.NewGuid(), AccountId = accountId, Type = "phone", Identifier = "+84912345678", EmailVerified = false });
        await _db.SaveChangesAsync();

        var sut = BuildSut();
        var result = await sut.GetCredentialsAsync(accountId);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, c => c.Type == "email" && c.EmailVerified == true);
        Assert.Contains(result, c => c.Type == "phone" && c.EmailVerified == null);
    }

    [Fact]
    public async Task UpdateAvatarAsync_WhenNoFieldProvided_ShouldThrowArgumentException()
    {
        var accountId = Guid.NewGuid();
        var profileId = Guid.NewGuid();
        _db.Accounts.Add(new Account
        {
            AccountId = accountId,
            RoleId = _db.Roles.First(r => r.Name == "user").RoleId,
            IsActive = true,
            PasswordHash = "hashed_x",
            Profile = new Profile { ProfileId = profileId, FullName = "Avatar User" }
        });
        await _db.SaveChangesAsync();

        var sut = BuildSut();
        await Assert.ThrowsAsync<ArgumentException>(() => sut.UpdateAvatarAsync(profileId, new UpdateAvatarRequest()));
    }

    [Fact]
    public async Task CreateConsultantByAdminAsync_WhenEmailTaken_ShouldThrowInvalidOperation()
    {
        var existingAccountId = Guid.NewGuid();
        _db.Accounts.Add(new Account
        {
            AccountId = existingAccountId,
            RoleId = _db.Roles.First(r => r.Name == "user").RoleId,
            IsActive = true,
            PasswordHash = "hashed_exist",
            Profile = new Profile { ProfileId = Guid.NewGuid(), FullName = "Existing" },
            Credentials = new List<Credential>
            {
                new Credential { CredentialId = Guid.NewGuid(), AccountId = existingAccountId, Type = "email", Identifier = "taken@mail.com", EmailVerified = true }
            }
        });
        await _db.SaveChangesAsync();

        var sut = BuildSut();
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.CreateConsultantByAdminAsync("taken@mail.com", "Consultant"));
    }

    [Fact]
    public async Task GoogleLoginAsync_WhenTokenMissing_ShouldThrowArgumentException()
    {
        var sut = BuildSut();
        await Assert.ThrowsAsync<ArgumentException>(() => sut.GoogleLoginAsync("", "web"));
    }

    [Fact]
    public async Task LoginWithPhoneAsync_WhenPhoneMissing_ShouldThrowArgumentException()
    {
        var sut = BuildSut();
        await Assert.ThrowsAsync<ArgumentException>(() => sut.LoginWithPhoneAsync("", "pass123", "web"));
    }

    [Fact]
    public async Task RegisterWithPhoneAsync_WhenFirebaseTokenMissing_ShouldThrowArgumentException()
    {
        var sut = BuildSut();
        await Assert.ThrowsAsync<ArgumentException>(() => sut.RegisterWithPhoneAsync("0912345678", "pass123", "", "User", "web"));
    }

    [Fact]
    public async Task LinkPhoneAsync_WhenAccountNotFound_ShouldThrowKeyNotFound()
    {
        var sut = BuildSut();
        await Assert.ThrowsAsync<KeyNotFoundException>(() => sut.LinkPhoneAsync(Guid.NewGuid(), "0912345678", "token", "pass123"));
    }
}
