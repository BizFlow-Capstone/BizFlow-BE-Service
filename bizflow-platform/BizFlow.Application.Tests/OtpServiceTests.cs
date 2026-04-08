using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Exceptions;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.DTOs.Otp;
using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Application.Interfaces.Services;
using BizFlow.Application.Services;
using BizFlow.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace BizFlow.Application.Tests;

public class OtpServiceTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IAccountRepository> _accountRepo = new();
    private readonly Mock<IOtpCodeRepository> _otpRepo = new();
    private readonly Mock<IEmailSender> _emailSender = new();
    private readonly Mock<IMessageService> _messageService = new();

    public OtpServiceTests()
    {
        _uow.SetupGet(u => u.Accounts).Returns(_accountRepo.Object);
        _uow.SetupGet(u => u.OtpCodes).Returns(_otpRepo.Object);

        _messageService.Setup(m => m.GetMessage(It.IsAny<string>()))
            .Returns((string key) => key);
        _messageService.Setup(m => m.GetMessage(It.IsAny<string>(), It.IsAny<object[]>()))
            .Returns((string key, object[] _) => key);
    }

    private OtpService BuildSut() => new(
        _uow.Object,
        _emailSender.Object,
        _messageService.Object,
        NullLogger<OtpService>.Instance);

    // ═══════════════════════════════════════════════════
    // SEND OTP ASYNC
    // ═══════════════════════════════════════════════════

    [Fact]
    public async Task SendOtpAsync_WhenAccountNotFound_ShouldThrowBadRequest()
    {
        _accountRepo.Setup(r => r.GetWithProfileAndRoleByNormalizedEmailCredentialAsync("test@mail.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account?)null);

        var sut = BuildSut();

        await Assert.ThrowsAsync<BadRequestException>(() => sut.SendOtpAsync(new SendOtpRequest { Email = "test@mail.com" }));
    }

    [Fact]
    public async Task SendOtpAsync_WhenRoleIsAdmin_ShouldThrowForbidden()
    {
        var account = new Account { Role = new Role { Name = "admin" } };
        _accountRepo.Setup(r => r.GetWithProfileAndRoleByNormalizedEmailCredentialAsync("admin@mail.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var sut = BuildSut();

        await Assert.ThrowsAsync<ForbiddenException>(() => sut.SendOtpAsync(new SendOtpRequest { Email = "admin@mail.com" }));
    }

    [Fact]
    public async Task SendOtpAsync_WhenAccountInactive_ShouldThrowUnauthorized()
    {
        var account = new Account { IsActive = false, Role = new Role { Name = "user" } };
        _accountRepo.Setup(r => r.GetWithProfileAndRoleByNormalizedEmailCredentialAsync("user@mail.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var sut = BuildSut();

        await Assert.ThrowsAsync<UnauthorizedException>(() => sut.SendOtpAsync(new SendOtpRequest { Email = "user@mail.com" }));
    }

    [Fact]
    public async Task SendOtpAsync_WhenRateLimited_ShouldThrowBadRequest()
    {
        var account = new Account { IsActive = true, Role = new Role { Name = "user" } };
        _accountRepo.Setup(r => r.GetWithProfileAndRoleByNormalizedEmailCredentialAsync("user@mail.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var recentOtp = new OtpCode { CreatedAt = DateTime.UtcNow.AddSeconds(-30) }; // Under 1 minute
        _otpRepo.Setup(r => r.GetLatestActiveOtpAsync("user@mail.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(recentOtp);

        var sut = BuildSut();

        await Assert.ThrowsAsync<BadRequestException>(() => sut.SendOtpAsync(new SendOtpRequest { Email = "user@mail.com" }));
    }

    [Fact]
    public async Task SendOtpAsync_WhenValid_ShouldCreateOtpAndSendEmail()
    {
        var email = "valid@mail.com";
        var account = new Account { IsActive = true, Role = new Role { Name = "user" }, Profile = new Profile { FullName = "Test User" } };
        
        _accountRepo.Setup(r => r.GetWithProfileAndRoleByNormalizedEmailCredentialAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        // Not rate limited
        var oldOtp = new OtpCode { CreatedAt = DateTime.UtcNow.AddMinutes(-5) };
        _otpRepo.Setup(r => r.GetLatestActiveOtpAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(oldOtp);

        _emailSender.Setup(s => s.SendTemplateAsync(email, "password-reset", It.IsAny<Dictionary<string, string>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BizFlow.Application.Common.Models.EmailSendResult { Success = true });

        var sut = BuildSut();
        var result = await sut.SendOtpAsync(new SendOtpRequest { Email = email });

        Assert.Equal(email, result.Destination);
        Assert.Equal(5, result.ExpiryMinutes);

        _otpRepo.Verify(r => r.DisableAllActiveOtpsAsync(email, It.IsAny<CancellationToken>()), Times.Once);
        _otpRepo.Verify(r => r.AddAsync(It.IsAny<OtpCode>()), Times.Once);
        _emailSender.Verify(s => s.SendTemplateAsync(email, "password-reset", It.IsNotNull<Dictionary<string, string>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ═══════════════════════════════════════════════════
    // VERIFY OTP FOR PASSWORD RESET
    // ═══════════════════════════════════════════════════

    [Theory]
    [InlineData("", "123456")]
    [InlineData("test@mail.com", "")]
    public async Task VerifyEmailOtp_WhenInputInvalid_ShouldThrowBadRequest(string email, string code)
    {
        var sut = BuildSut();
        await Assert.ThrowsAsync<BadRequestException>(() => sut.VerifyEmailOtpForPasswordResetAsync(email, code));
    }

    [Fact]
    public async Task VerifyEmailOtp_WhenInvalidOtp_ShouldThrowBadRequest()
    {
        var email = "test@mail.com";
        var code = "123456";
        var account = new Account { IsActive = true, Role = new Role { Name = "user" } };
        
        _accountRepo.Setup(r => r.GetWithProfileAndRoleByNormalizedEmailCredentialAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        _otpRepo.Setup(r => r.TryConsumeActiveOtpAsync(email, code, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var sut = BuildSut();
        await Assert.ThrowsAsync<BadRequestException>(() => sut.VerifyEmailOtpForPasswordResetAsync(email, code));
    }

    [Fact]
    public async Task VerifyEmailOtp_WhenValid_ShouldSetNonceAndReturnResult()
    {
        var email = "test@mail.com";
        var code = "123456";
        var accountId = Guid.NewGuid();
        var profileId = Guid.NewGuid();
        
        var account = new Account 
        { 
            AccountId = accountId,
            IsActive = true, 
            Role = new Role { Name = "user" },
            Profile = new Profile { ProfileId = profileId }
        };
        
        _accountRepo.Setup(r => r.GetWithProfileAndRoleByNormalizedEmailCredentialAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        _otpRepo.Setup(r => r.TryConsumeActiveOtpAsync(email, code, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = BuildSut();
        var result = await sut.VerifyEmailOtpForPasswordResetAsync(email, code);

        Assert.Equal(accountId, result.AccountId);
        Assert.Equal(profileId, result.ProfileId);
        Assert.NotEqual(Guid.Empty, result.PasswordResetNonce);
        Assert.Equal(result.PasswordResetNonce, account.PasswordResetNonce); // Account mutated

        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
