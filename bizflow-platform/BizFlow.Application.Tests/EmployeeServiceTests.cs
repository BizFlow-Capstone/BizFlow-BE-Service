using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Exceptions;
using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Application.Interfaces.Services;
using BizFlow.Application.Services;
using BizFlow.Domain.Entities;
using Moq;
using Xunit;

namespace BizFlow.Application.Tests;

public class EmployeeServiceTests
{
    private readonly Mock<IEmployeeRepository> _employeeRepo = new();
    private readonly Mock<INotificationService> _notificationService = new();

    private EmployeeService BuildSut() => new(_employeeRepo.Object, _notificationService.Object);

    // ═══════════════════════════════════════════════════
    // SEARCH USER BY CONTACT
    // ═══════════════════════════════════════════════════

    [Fact]
    public async Task SearchUserByContactAsync_WhenQueryEmpty_ShouldReturnEmpty()
    {
        var sut = BuildSut();

        var result = await sut.SearchUserByContactAsync(Guid.NewGuid(), "   ");

        Assert.Empty(result);
    }

    // ═══════════════════════════════════════════════════
    // INVITE EMPLOYEE
    // ═══════════════════════════════════════════════════

    [Fact]
    public async Task InviteEmployeeAsync_WhenOwnerInvitesSelf_ShouldThrowBadRequest()
    {
        var userId = Guid.NewGuid();
        var sut = BuildSut();

        var ex = await Assert.ThrowsAsync<BadRequestException>(() => sut.InviteEmployeeAsync(userId, userId));
        Assert.Equal(MessageKeys.BadRequest, ex.MessageKey);
    }

    [Fact]
    public async Task InviteEmployeeAsync_WhenEmployeeProfileNotFound_ShouldThrowNotFound()
    {
        var ownerId = Guid.NewGuid();
        var empId = Guid.NewGuid();
        _employeeRepo.Setup(r => r.ProfileExistsAsync(empId)).ReturnsAsync(false);

        var sut = BuildSut();

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => sut.InviteEmployeeAsync(ownerId, empId));
        Assert.Equal(MessageKeys.UserNotFound, ex.MessageKey);
    }

    [Fact]
    public async Task InviteEmployeeAsync_WhenAlreadyPendingOrAccepted_ShouldThrowConflict()
    {
        var ownerId = Guid.NewGuid();
        var empId = Guid.NewGuid();
        _employeeRepo.Setup(r => r.ProfileExistsAsync(empId)).ReturnsAsync(true);
        _employeeRepo.Setup(r => r.GetOpenHireAsync(ownerId, empId))
            .ReturnsAsync(new Hire { Status = "pending" });

        var sut = BuildSut();

        var ex = await Assert.ThrowsAsync<ConflictException>(() => sut.InviteEmployeeAsync(ownerId, empId));
        Assert.Equal(MessageKeys.EmployeeAlreadyHired, ex.MessageKey);
    }

    [Fact]
    public async Task InviteEmployeeAsync_WhenAlreadyAccepted_ShouldThrowConflict()
    {
        var ownerId = Guid.NewGuid();
        var empId = Guid.NewGuid();
        _employeeRepo.Setup(r => r.ProfileExistsAsync(empId)).ReturnsAsync(true);
        _employeeRepo.Setup(r => r.GetOpenHireAsync(ownerId, empId))
            .ReturnsAsync(new Hire { Status = "accepted" });

        var sut = BuildSut();

        var ex = await Assert.ThrowsAsync<ConflictException>(() => sut.InviteEmployeeAsync(ownerId, empId));
        Assert.Equal(MessageKeys.EmployeeAlreadyHired, ex.MessageKey);
    }

    [Fact]
    public async Task InviteEmployeeAsync_WhenValid_ShouldCreateHireAndSendNotification()
    {
        var ownerId = Guid.NewGuid();
        var empId = Guid.NewGuid();
        _employeeRepo.Setup(r => r.ProfileExistsAsync(empId)).ReturnsAsync(true);
        _employeeRepo.Setup(r => r.GetOpenHireAsync(ownerId, empId)).ReturnsAsync((Hire?)null);
        _employeeRepo.Setup(r => r.CreateHireAsync(It.IsAny<Hire>()))
            .ReturnsAsync(new Hire { HireId = 1, OwnerId = ownerId, EmployeeId = empId, Status = "pending" });
        _notificationService.Setup(n => n.SendEmployeeInviteAsync(empId, It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var sut = BuildSut();
        var result = await sut.InviteEmployeeAsync(ownerId, empId);

        Assert.Equal(ownerId, result.OwnerId);
        Assert.Equal(empId, result.EmployeeId);
        _notificationService.Verify(n => n.SendEmployeeInviteAsync(empId, It.IsAny<string>()), Times.Once);
    }

    // ═══════════════════════════════════════════════════
    // REMOVE EMPLOYEE
    // ═══════════════════════════════════════════════════

    [Fact]
    public async Task RemoveEmployeeAsync_WhenHireNotFound_ShouldThrowNotFound()
    {
        var ownerId = Guid.NewGuid();
        var empId = Guid.NewGuid();
        _employeeRepo.Setup(r => r.GetActiveHireAsync(ownerId, empId)).ReturnsAsync((Hire?)null);

        var sut = BuildSut();

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => sut.RemoveEmployeeAsync(ownerId, empId));
        Assert.Equal(MessageKeys.UserNotFound, ex.MessageKey);
    }

    [Fact]
    public async Task RemoveEmployeeAsync_WhenHasActiveAssignments_ShouldThrowBadRequest()
    {
        var ownerId = Guid.NewGuid();
        var empId = Guid.NewGuid();
        _employeeRepo.Setup(r => r.GetActiveHireAsync(ownerId, empId))
            .ReturnsAsync(new Hire { HireId = 1, Status = "accepted" });
        _employeeRepo.Setup(r => r.HasActiveAssignmentsAsync(empId)).ReturnsAsync(true);

        var sut = BuildSut();

        var ex = await Assert.ThrowsAsync<BadRequestException>(() => sut.RemoveEmployeeAsync(ownerId, empId));
        Assert.Equal(MessageKeys.EmployeeHasActiveAssignments, ex.MessageKey);
    }

    // ═══════════════════════════════════════════════════
    // ACCEPT / REJECT INVITATION
    // ═══════════════════════════════════════════════════

    [Fact]
    public async Task AcceptInvitationAsync_WhenInvitationNotFound_ShouldThrowNotFound()
    {
        var empId = Guid.NewGuid();
        _employeeRepo.Setup(r => r.GetPendingInvitationByIdAsync(empId, 99)).ReturnsAsync((Hire?)null);

        var sut = BuildSut();

        await Assert.ThrowsAsync<NotFoundException>(() => sut.AcceptInvitationAsync(empId, 99));
    }

    [Fact]
    public async Task AcceptInvitationAsync_WhenValid_ShouldSetAcceptedAndSave()
    {
        var empId = Guid.NewGuid();
        var hire = new Hire { HireId = 5, Status = "pending", IsActive = false };
        _employeeRepo.Setup(r => r.GetPendingInvitationByIdAsync(empId, 5)).ReturnsAsync(hire);
        _employeeRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var sut = BuildSut();
        await sut.AcceptInvitationAsync(empId, 5);

        Assert.Equal("accepted", hire.Status);
        Assert.True(hire.IsActive);
        _employeeRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task RejectInvitationAsync_WhenInvitationNotFound_ShouldThrowNotFound()
    {
        var empId = Guid.NewGuid();
        _employeeRepo.Setup(r => r.GetPendingInvitationByIdAsync(empId, 77)).ReturnsAsync((Hire?)null);

        var sut = BuildSut();

        await Assert.ThrowsAsync<NotFoundException>(() => sut.RejectInvitationAsync(empId, 77));
    }

    [Fact]
    public async Task RejectInvitationAsync_WhenValid_ShouldSetRejectedAndSave()
    {
        var empId = Guid.NewGuid();
        var hire = new Hire { HireId = 6, Status = "pending", IsActive = false };
        _employeeRepo.Setup(r => r.GetPendingInvitationByIdAsync(empId, 6)).ReturnsAsync(hire);
        _employeeRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var sut = BuildSut();
        await sut.RejectInvitationAsync(empId, 6);

        Assert.Equal("rejected", hire.Status);
        Assert.False(hire.IsActive);
        _employeeRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }
}
