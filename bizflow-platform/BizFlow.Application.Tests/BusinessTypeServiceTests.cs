using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Application.Services;
using BizFlow.Domain.Entities;
using Moq;
using Xunit;

namespace BizFlow.Application.Tests;

public class BusinessTypeServiceTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IBusinessTypeRepository> _repo = new();

    public BusinessTypeServiceTests()
    {
        _uow.SetupGet(x => x.BusinessTypes).Returns(_repo.Object);
    }

    private BusinessTypeService BuildSut() => new(_uow.Object);

    [Fact]
    public async Task GetAllAsync_WhenNoRows_ShouldReturnEmpty()
    {
        _repo.Setup(r => r.GetAllAsync()).ReturnsAsync(Array.Empty<BusinessType>());

        var sut = BuildSut();
        var result = await sut.GetAllAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllAsync_ShouldMapFieldsCorrectly()
    {
        var id = Guid.NewGuid();
        _repo.Setup(r => r.GetAllAsync()).ReturnsAsync(new[]
        {
            new BusinessType
            {
                BusinessTypeId = id,
                Code = "GROCERY",
                Name = "Tap Hoa",
                Description = "Retail grocery",
                Status = "active"
            }
        });

        var sut = BuildSut();
        var result = (await sut.GetAllAsync()).ToList();

        Assert.Single(result);
        Assert.Equal(id, result[0].BusinessTypeId);
        Assert.Equal("GROCERY", result[0].Code);
        Assert.Equal("Tap Hoa", result[0].Name);
        Assert.Equal("active", result[0].Status);
    }

    [Fact]
    public async Task GetAllAsync_ShouldKeepInactiveRowsForReferenceScreen()
    {
        _repo.Setup(r => r.GetAllAsync()).ReturnsAsync(new[]
        {
            new BusinessType
            {
                BusinessTypeId = Guid.NewGuid(),
                Code = "INACTIVE_CODE",
                Name = "Old Type",
                Description = "Deprecated",
                Status = "inactive"
            }
        });

        var sut = BuildSut();
        var result = (await sut.GetAllAsync()).ToList();

        Assert.Single(result);
        Assert.Equal("inactive", result[0].Status);
    }
}
