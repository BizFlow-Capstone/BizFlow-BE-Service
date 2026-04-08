using AutoMapper;
using BizFlow.Application.DTOs.Hire;
using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Application.Mappers;
using BizFlow.Application.Services;
using BizFlow.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace BizFlow.Application.Tests;

public class HireServiceTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IHireRepository> _hireRepo = new();
    private readonly IMapper _mapper;

    public HireServiceTests()
    {
        var cfg = new MapperConfiguration(c => c.AddProfile<HireProfile>(), NullLoggerFactory.Instance);
        _mapper = cfg.CreateMapper();

        _uow.SetupGet(x => x.Hires).Returns(_hireRepo.Object);
    }

    private HireService BuildSut() => new(_uow.Object, _mapper);

    [Fact]
    public async Task GetEmployeeSummariesAsync_WhenNoEmployees_ShouldReturnEmptyList()
    {
        _hireRepo.Setup(r => r.GetHiredEmployeesWithDetailsAsync(It.IsAny<Guid>()))
            .ReturnsAsync(Array.Empty<(Hire hire, string fullName, string email, string? phone, string? avatarUrl)>());

        var sut = BuildSut();
        var result = await sut.GetEmployeeSummariesAsync(Guid.NewGuid());

        Assert.NotNull(result);
        Assert.Empty(result.Employees);
    }

    [Fact]
    public async Task GetEmployeeSummariesAsync_ShouldMapStatusAndBasicFields()
    {
        var employeeId = Guid.NewGuid();
        IEnumerable<(Hire hire, string fullName, string email, string? phone, string? avatarUrl)> rows =
            new List<(Hire hire, string fullName, string email, string? phone, string? avatarUrl)>
            {
                (
                    new Hire
                    {
                        EmployeeId = employeeId,
                        Status = "accepted",
                        IsActive = true,
                        StartAt = DateTime.UtcNow.AddDays(-10)
                    },
                    "Nguyen Van A",
                    "a@test.com",
                    "0909000001",
                    "https://avatar"
                )
            };
        _hireRepo.Setup(r => r.GetHiredEmployeesWithDetailsAsync(It.IsAny<Guid>()))
            .ReturnsAsync(rows);

        var sut = BuildSut();
        var result = await sut.GetEmployeeSummariesAsync(Guid.NewGuid());

        Assert.Single(result.Employees);
        Assert.Equal(employeeId.ToString(), result.Employees[0].ProfileId);
        Assert.Equal("Nguyen Van A", result.Employees[0].UserName);
        Assert.Equal("a@test.com", result.Employees[0].Email);
        Assert.True(result.Employees[0].IsAlreadyHired);
    }

    [Fact]
    public async Task GetHiredEmployeeDetailsAsync_ShouldMapAllRows()
    {
        var e1 = Guid.NewGuid();
        var e2 = Guid.NewGuid();
        IEnumerable<(Hire hire, string fullName, string email, string? phone, string? avatarUrl)> rows =
            new List<(Hire hire, string fullName, string email, string? phone, string? avatarUrl)>
            {
                (
                    new Hire { EmployeeId = e1, Status = "accepted", IsActive = true, StartAt = DateTime.UtcNow.AddDays(-5) },
                    "Emp One", "one@test.com", "0901", null
                ),
                (
                    new Hire { EmployeeId = e2, Status = "pending", IsActive = false },
                    "Emp Two", "two@test.com", "0902", null
                )
            };
        _hireRepo.Setup(r => r.GetHiredEmployeesWithDetailsAsync(It.IsAny<Guid>()))
            .ReturnsAsync(rows);

        var sut = BuildSut();
        var result = (await sut.GetHiredEmployeeDetailsAsync(Guid.NewGuid())).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal(e1, result[0].EmployeeId);
        Assert.Equal("Emp One", result[0].FullName);
        Assert.Equal("accepted", result[0].Status);
        Assert.Equal("pending", result[1].Status);
    }

    [Fact]
    public async Task ValidateEmployeesForAssignmentAsync_WhenAllValid_ShouldReturnAllValid()
    {
        var ownerId = Guid.NewGuid();
        var e1 = Guid.NewGuid();
        var e2 = Guid.NewGuid();

        _hireRepo.Setup(r => r.GetHiredEmployeeIdsAsync(ownerId)).ReturnsAsync(new[] { e1, e2 });

        var sut = BuildSut();
        var result = await sut.ValidateEmployeesForAssignmentAsync(ownerId, new[] { e1, e2 });

        Assert.True(result.AllValid);
        Assert.Equal(2, result.ValidEmployeeIds.Count);
        Assert.Empty(result.InvalidEmployeeIds);
    }

    [Fact]
    public async Task ValidateEmployeesForAssignmentAsync_WhenMixedAndDuplicated_ShouldDistinctAndSplit()
    {
        var ownerId = Guid.NewGuid();
        var validEmployee = Guid.NewGuid();
        var invalidEmployee = Guid.NewGuid();

        _hireRepo.Setup(r => r.GetHiredEmployeeIdsAsync(ownerId)).ReturnsAsync(new[] { validEmployee });

        var sut = BuildSut();
        var result = await sut.ValidateEmployeesForAssignmentAsync(ownerId, new[] { validEmployee, validEmployee, invalidEmployee, invalidEmployee });

        Assert.False(result.AllValid);
        Assert.Single(result.ValidEmployeeIds);
        Assert.Single(result.InvalidEmployeeIds);
        Assert.Equal(validEmployee, result.ValidEmployeeIds[0]);
        Assert.Equal(invalidEmployee, result.InvalidEmployeeIds[0]);
    }

    [Fact]
    public async Task ValidateEmployeesForAssignmentAsync_WhenInputEmpty_ShouldReturnEmptyResult()
    {
        var ownerId = Guid.NewGuid();
        _hireRepo.Setup(r => r.GetHiredEmployeeIdsAsync(ownerId)).ReturnsAsync(Array.Empty<Guid>());

        var sut = BuildSut();
        var result = await sut.ValidateEmployeesForAssignmentAsync(ownerId, Array.Empty<Guid>());

        Assert.True(result.AllValid);
        Assert.Empty(result.ValidEmployeeIds);
        Assert.Empty(result.InvalidEmployeeIds);
    }
}
