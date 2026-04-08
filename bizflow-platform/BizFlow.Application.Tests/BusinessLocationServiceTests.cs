using AutoMapper;
using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Exceptions;
using BizFlow.Application.DTOs.Hire;
using BizFlow.Application.DTOs.Location;
using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Application.Interfaces.Services;
using BizFlow.Application.Mappers;
using BizFlow.Application.Services;
using BizFlow.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace BizFlow.Application.Tests;

public class BusinessLocationServiceTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IBusinessLocationRepository> _locationRepo = new();
    private readonly Mock<IHireService> _hireService = new();
    private readonly IMapper _mapper;

    public BusinessLocationServiceTests()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<LocationProfile>();
        }, NullLoggerFactory.Instance);
        _mapper = config.CreateMapper();

        _uow.SetupGet(x => x.BusinessLocations).Returns(_locationRepo.Object);

        _uow.Setup(x => x.ExecuteResilientAsync(It.IsAny<Func<CancellationToken, Task<int>>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<CancellationToken, Task<int>> action, CancellationToken ct) => action(ct));

        _uow.Setup(x => x.ExecuteResilientAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<CancellationToken, Task> action, CancellationToken ct) => action(ct));
    }

    private BusinessLocationService BuildSut() => new(_uow.Object, _hireService.Object, _mapper);

    [Fact]
    public async Task GetLocationDetailAsync_WhenNoAccess_ShouldThrowForbidden()
    {
        var userId = Guid.NewGuid();
        const int locationId = 11;

        _locationRepo.Setup(r => r.GetByIdAsync(locationId))
            .ReturnsAsync(new BusinessLocation { BusinessLocationId = locationId, LocationName = "A", Address = "B", Status = "active", IsActive = true });
        _locationRepo.Setup(r => r.HasAccessToLocationAsync(userId, locationId)).ReturnsAsync(false);

        var sut = BuildSut();

        await Assert.ThrowsAsync<ForbiddenException>(() => sut.GetLocationDetailAsync(userId, locationId));
    }

    [Fact]
    public async Task GetLocationDetailAsync_WhenDetailMissing_ShouldThrowNotFound()
    {
        var userId = Guid.NewGuid();
        const int locationId = 12;

        _locationRepo.Setup(r => r.GetByIdAsync(locationId))
            .ReturnsAsync(new BusinessLocation { BusinessLocationId = locationId, LocationName = "A", Address = "B", Status = "active", IsActive = true });
        _locationRepo.Setup(r => r.HasAccessToLocationAsync(userId, locationId)).ReturnsAsync(true);
        _locationRepo.Setup(r => r.IsOwnerOfLocationAsync(userId, locationId)).ReturnsAsync(true);
        _locationRepo.Setup(r => r.GetLocationDetailByIdAsync(locationId)).ReturnsAsync((BusinessLocationDetailDto?)null);

        var sut = BuildSut();

        await Assert.ThrowsAsync<NotFoundException>(() => sut.GetLocationDetailAsync(userId, locationId));
    }

    [Fact]
    public async Task CreateLocationAsync_WithValidRequest_ShouldCreateAndReturnDto()
    {
        var userId = Guid.NewGuid();
        var employeeA = Guid.NewGuid();
        var employeeB = Guid.NewGuid();

        var request = new CreateLocationRequest
        {
            Name = "Chi nhanh Q1",
            Address = "1 Le Loi",
            EmployeeIds = new List<Guid> { employeeA, employeeB }
        };

        _locationRepo.Setup(r => r.IsExistedByNameAsync(userId, request.Name)).ReturnsAsync(false);

        BusinessLocation? createdEntity = null;
        _locationRepo.Setup(r => r.AddAsync(It.IsAny<BusinessLocation>()))
            .Callback<BusinessLocation>(x =>
            {
                x.BusinessLocationId = 99;
                createdEntity = x;
            })
            .ReturnsAsync((BusinessLocation x) => x);

        _hireService.Setup(s => s.ValidateEmployeesForAssignmentAsync(userId, It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(new EmployeeValidationResult
            {
                ValidEmployeeIds = new List<Guid> { employeeA, employeeB },
                InvalidEmployeeIds = new List<Guid>()
            });

        _locationRepo.Setup(r => r.GetAssignedEmployeeIdsAsync(99)).ReturnsAsync(Array.Empty<Guid>());
        _locationRepo.Setup(r => r.GetLocationDtoByUserAndIdAsync(userId, 99))
            .ReturnsAsync(new BusinessLocationDto
            {
                Id = 99,
                Name = request.Name,
                Address = request.Address,
                IsActive = true,
                OwnerProfileId = userId
            });

        _uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = BuildSut();
        var result = await sut.CreateLocationAsync(userId, request);

        Assert.NotNull(createdEntity);
        Assert.Equal(99, result.Id);
        _locationRepo.Verify(r => r.AddUserLocationAssignmentAsync(It.Is<UserLocationAssignment>(a => a.UserId == userId && a.IsOwner)), Times.Once);
        _locationRepo.Verify(r => r.AddUserLocationAssignmentAsync(It.Is<UserLocationAssignment>(a => a.UserId == employeeA && !a.IsOwner)), Times.Once);
        _locationRepo.Verify(r => r.AddUserLocationAssignmentAsync(It.Is<UserLocationAssignment>(a => a.UserId == employeeB && !a.IsOwner)), Times.Once);
    }

    [Fact]
    public async Task CreateLocationAsync_WhenNameDuplicated_ShouldThrowConflict()
    {
        var userId = Guid.NewGuid();
        var request = new CreateLocationRequest { Name = "Trung lap", Address = "A" };

        _locationRepo.Setup(r => r.IsExistedByNameAsync(userId, request.Name)).ReturnsAsync(true);

        var sut = BuildSut();

        var ex = await Assert.ThrowsAsync<ConflictException>(() => sut.CreateLocationAsync(userId, request));
        Assert.Equal(MessageKeys.LocationAlreadyExists, ex.MessageKey);
        _locationRepo.Verify(r => r.AddAsync(It.IsAny<BusinessLocation>()), Times.Never);
    }

    [Fact]
    public async Task AddEmployeesToLocationAsync_WhenEmployeeNotHired_ShouldThrowBadRequest()
    {
        var ownerId = Guid.NewGuid();
        const int locationId = 17;
        var unknownEmployee = Guid.NewGuid();

        _locationRepo.Setup(r => r.GetByIdAsync(locationId))
            .ReturnsAsync(new BusinessLocation { BusinessLocationId = locationId, LocationName = "A", Address = "B", Status = "active", IsActive = true });
        _locationRepo.Setup(r => r.IsOwnerOfLocationAsync(ownerId, locationId)).ReturnsAsync(true);

        _hireService.Setup(s => s.ValidateEmployeesForAssignmentAsync(ownerId, It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(new EmployeeValidationResult
            {
                ValidEmployeeIds = new List<Guid>(),
                InvalidEmployeeIds = new List<Guid> { unknownEmployee }
            });

        var sut = BuildSut();

        await Assert.ThrowsAsync<BadRequestException>(() => sut.AddEmployeesToLocationAsync(ownerId, locationId, new List<Guid> { unknownEmployee }));
        _locationRepo.Verify(r => r.AddUserLocationAssignmentAsync(It.IsAny<UserLocationAssignment>()), Times.Never);
    }

    [Fact]
    public async Task AddEmployeesToLocationAsync_WhenEmployeeAlreadyAssigned_ShouldThrowBadRequest()
    {
        var ownerId = Guid.NewGuid();
        const int locationId = 18;
        var employee = Guid.NewGuid();

        _locationRepo.Setup(r => r.GetByIdAsync(locationId))
            .ReturnsAsync(new BusinessLocation { BusinessLocationId = locationId, LocationName = "A", Address = "B", Status = "active", IsActive = true });
        _locationRepo.Setup(r => r.IsOwnerOfLocationAsync(ownerId, locationId)).ReturnsAsync(true);

        _hireService.Setup(s => s.ValidateEmployeesForAssignmentAsync(ownerId, It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(new EmployeeValidationResult
            {
                ValidEmployeeIds = new List<Guid> { employee },
                InvalidEmployeeIds = new List<Guid>()
            });

        _locationRepo.Setup(r => r.GetAssignedEmployeeIdsAsync(locationId)).ReturnsAsync(new[] { employee });

        var sut = BuildSut();

        var ex = await Assert.ThrowsAsync<BadRequestException>(() => sut.AddEmployeesToLocationAsync(ownerId, locationId, new List<Guid> { employee }));
        Assert.Equal(MessageKeys.EmployeesAlreadyAssigned, ex.MessageKey);
    }

    [Fact]
    public async Task DeleteLocationAsync_WhenHasRelatedData_ShouldSoftDelete()
    {
        var userId = Guid.NewGuid();
        const int locationId = 21;
        var location = new BusinessLocation
        {
            BusinessLocationId = locationId,
            LocationName = "Store A",
            Address = "Addr",
            Status = "active",
            IsActive = true
        };

        _locationRepo.Setup(r => r.GetByIdAsync(locationId)).ReturnsAsync(location);
        _locationRepo.Setup(r => r.IsOwnerOfLocationAsync(userId, locationId)).ReturnsAsync(true);
        _locationRepo.Setup(r => r.HasRelatedDataAsync(locationId)).ReturnsAsync(true);
        _uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = BuildSut();
        await sut.DeleteLocationAsync(userId, locationId);

        Assert.NotNull(location.DeletedAt);
        _locationRepo.Verify(r => r.Update(location), Times.Once);
        _locationRepo.Verify(r => r.Delete(It.IsAny<BusinessLocation>()), Times.Never);
    }

    [Fact]
    public async Task DeleteLocationAsync_WhenNoRelatedData_ShouldHardDelete()
    {
        var userId = Guid.NewGuid();
        const int locationId = 22;
        var location = new BusinessLocation
        {
            BusinessLocationId = locationId,
            LocationName = "Store B",
            Address = "Addr",
            Status = "active",
            IsActive = true
        };

        _locationRepo.Setup(r => r.GetByIdAsync(locationId)).ReturnsAsync(location);
        _locationRepo.Setup(r => r.IsOwnerOfLocationAsync(userId, locationId)).ReturnsAsync(true);
        _locationRepo.Setup(r => r.HasRelatedDataAsync(locationId)).ReturnsAsync(false);
        _uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = BuildSut();
        await sut.DeleteLocationAsync(userId, locationId);

        _locationRepo.Verify(r => r.Delete(location), Times.Once);
        _locationRepo.Verify(r => r.Update(It.IsAny<BusinessLocation>()), Times.Never);
    }

    [Fact]
    public async Task ValidateLocationAccessAsync_WhenEmployeeAccessInactiveLocation_ShouldThrowForbidden()
    {
        var userId = Guid.NewGuid();
        const int locationId = 30;

        _locationRepo.Setup(r => r.GetByIdAsync(locationId))
            .ReturnsAsync(new BusinessLocation
            {
                BusinessLocationId = locationId,
                LocationName = "Store",
                Address = "Addr",
                Status = "inactive",
                IsActive = false
            });
        _locationRepo.Setup(r => r.HasAccessToLocationAsync(userId, locationId)).ReturnsAsync(true);
        _locationRepo.Setup(r => r.IsOwnerOfLocationAsync(userId, locationId)).ReturnsAsync(false);

        var sut = BuildSut();

        var ex = await Assert.ThrowsAsync<ForbiddenException>(() => sut.ValidateLocationAccessAsync(userId, locationId));
        Assert.Equal(MessageKeys.LocationInactive, ex.MessageKey);
    }

    [Fact]
    public async Task RemoveEmployeeFromLocationAsync_ShouldCallRepositoryAndSave()
    {
        var ownerId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        const int locationId = 35;

        _locationRepo.Setup(r => r.GetByIdAsync(locationId))
            .ReturnsAsync(new BusinessLocation { BusinessLocationId = locationId, LocationName = "Store", Address = "Addr", Status = "active", IsActive = true });
        _locationRepo.Setup(r => r.IsOwnerOfLocationAsync(ownerId, locationId)).ReturnsAsync(true);
        _uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = BuildSut();
        await sut.RemoveEmployeeFromLocationAsync(ownerId, locationId, employeeId);

        _locationRepo.Verify(r => r.RemoveEmployeeFromLocationAsync(locationId, employeeId), Times.Once);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}