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
        _jwtService.Setup(j => j.HashToken(It.IsAny<string>(), It.IsAny<string>())).Returns("token_hash");

        _subscriptionService.Setup(s => s.EnsureFreeSubscriptionAsync(It.IsAny<Guid>()))
            .Returns(Task.CompletedTask);
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
}
