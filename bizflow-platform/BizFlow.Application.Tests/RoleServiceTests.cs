using BizFlow.Application.Services;
using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Domain.Entities;
using Moq;
using Xunit;

namespace BizFlow.Application.Tests;

public class RoleServiceTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IRoleRepository> _roleRepo = new();

    public RoleServiceTests()
    {
        _uow.SetupGet(x => x.Roles).Returns(_roleRepo.Object);
    }

    private RoleService BuildSut() => new(_uow.Object);

    [Fact]
    public async Task GetAllRolesAsync_WhenEmpty_ShouldReturnEmpty()
    {
        _roleRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(Array.Empty<Role>());

        var sut = BuildSut();
        var result = await sut.GetAllRolesAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllRolesAsync_WhenHasData_ShouldMapAllFields()
    {
        var now = DateTime.UtcNow;
        _roleRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new[]
        {
            new Role { RoleId = Guid.NewGuid(), Name = "owner", Description = "Owner role", CreateAt = now.AddDays(-1), UpdateAt = now },
            new Role { RoleId = Guid.NewGuid(), Name = "employee", Description = "Employee role", CreateAt = now.AddDays(-2), UpdateAt = now.AddHours(-1) }
        });

        var sut = BuildSut();
        var result = (await sut.GetAllRolesAsync()).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal("owner", result[0].Name);
        Assert.Equal("Owner role", result[0].Description);
        Assert.Equal("employee", result[1].Name);
    }

    [Fact]
    public async Task GetRoleByIdAsync_WhenNotFound_ShouldReturnNull()
    {
        _roleRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Role?)null);

        var sut = BuildSut();
        var result = await sut.GetRoleByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetRoleByIdAsync_WhenFound_ShouldMap()
    {
        var roleId = Guid.NewGuid();
        _roleRepo.Setup(r => r.GetByIdAsync(roleId)).ReturnsAsync(new Role
        {
            RoleId = roleId,
            Name = "admin",
            Description = "Admin role",
            CreateAt = DateTime.UtcNow.AddDays(-2),
            UpdateAt = DateTime.UtcNow.AddDays(-1)
        });

        var sut = BuildSut();
        var result = await sut.GetRoleByIdAsync(roleId);

        Assert.NotNull(result);
        Assert.Equal(roleId, result!.Id);
        Assert.Equal("admin", result.Name);
    }

    [Fact]
    public async Task GetRoleByNameAsync_WhenNotFound_ShouldReturnNull()
    {
        _roleRepo.Setup(r => r.GetByNameAsync("missing")).ReturnsAsync((Role?)null);

        var sut = BuildSut();
        var result = await sut.GetRoleByNameAsync("missing");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetRoleByNameAsync_WhenFound_ShouldMap()
    {
        _roleRepo.Setup(r => r.GetByNameAsync("owner")).ReturnsAsync(new Role
        {
            RoleId = Guid.NewGuid(),
            Name = "owner",
            Description = "Main owner",
            CreateAt = DateTime.UtcNow.AddDays(-7),
            UpdateAt = DateTime.UtcNow
        });

        var sut = BuildSut();
        var result = await sut.GetRoleByNameAsync("owner");

        Assert.NotNull(result);
        Assert.Equal("owner", result!.Name);
        Assert.Equal("Main owner", result.Description);
    }
}
