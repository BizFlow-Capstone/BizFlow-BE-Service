using BizFlow.Application.DTOs.Hire;
using BizFlow.Application.DTOs.Location;
using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Exceptions;
using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Application.Interfaces.Services;
using BizFlow.Application.Services;
using BizFlow.Domain.Entities;
using AutoMapper;
using Moq;
using Xunit;

namespace BizFlow.Application.Tests.Services
{
    public class BusinessLocationServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IHireService> _mockHireService;
        private readonly Mock<IMapper> _mockMapper;
        private readonly BusinessLocationService _businessLocationService;

        public BusinessLocationServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockHireService = new Mock<IHireService>();
            _mockMapper = new Mock<IMapper>();
            
            // Setup SaveChangesAsync to always return 1 by default
            _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);
            
            _businessLocationService = new BusinessLocationService(
                _mockUnitOfWork.Object,
                _mockHireService.Object,
                _mockMapper.Object);
        }

        #region GetOwnedLocationsAsync Tests

        [Fact]
        public async Task GetOwnedLocationsAsync_WithValidUserId_ReturnsOwnedLocations()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var location1 = new BusinessLocation { BusinessLocationId = 1, Name = "Location 1", Address = "Address 1" };
            var location2 = new BusinessLocation { BusinessLocationId = 2, Name = "Location 2", Address = "Address 2" };
            var locations = new List<BusinessLocation> { location1, location2 };

            _mockUnitOfWork.Setup(x => x.BusinessLocations.GetOwnedByUserIdAsync(userId))
                .ReturnsAsync(locations);
            _mockUnitOfWork.Setup(x => x.BusinessLocations.GetByIdWithOwnerAsync(1))
                .ReturnsAsync((location1, "John Doe"));
            _mockUnitOfWork.Setup(x => x.BusinessLocations.GetByIdWithOwnerAsync(2))
                .ReturnsAsync((location2, "John Doe"));
            _mockMapper.Setup(x => x.Map<BusinessLocationDto>(location1))
                .Returns(new BusinessLocationDto { Id = 1, Name = "Location 1", Address = "Address 1" });
            _mockMapper.Setup(x => x.Map<BusinessLocationDto>(location2))
                .Returns(new BusinessLocationDto { Id = 2, Name = "Location 2", Address = "Address 2" });

            // Act
            var result = await _businessLocationService.GetOwnedLocationsAsync(userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            Assert.Equal("John Doe", result.First().OwnerName);
            _mockUnitOfWork.Verify(x => x.BusinessLocations.GetOwnedByUserIdAsync(userId), Times.Once);
        }

        [Fact]
        public async Task GetOwnedLocationsAsync_WithNoLocations_ReturnsEmptyList()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _mockUnitOfWork.Setup(x => x.BusinessLocations.GetOwnedByUserIdAsync(userId))
                .ReturnsAsync(new List<BusinessLocation>());

            // Act
            var result = await _businessLocationService.GetOwnedLocationsAsync(userId);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetOwnedLocationsAsync_IncludesOwnerName_InMappedDto()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var location = new BusinessLocation { BusinessLocationId = 1, Name = "Test Location", Address = "Test Address" };
            var ownerName = "Jane Smith";

            _mockUnitOfWork.Setup(x => x.BusinessLocations.GetOwnedByUserIdAsync(userId))
                .ReturnsAsync(new List<BusinessLocation> { location });
            _mockUnitOfWork.Setup(x => x.BusinessLocations.GetByIdWithOwnerAsync(1))
                .ReturnsAsync((location, ownerName));
            _mockMapper.Setup(x => x.Map<BusinessLocationDto>(It.IsAny<BusinessLocation>()))
                .Returns(new BusinessLocationDto { Id = 1, Name = "Test Location", Address = "Test Address" });

            // Act
            var result = await _businessLocationService.GetOwnedLocationsAsync(userId);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            var dto = result.First();
            Assert.Equal(ownerName, dto.OwnerName);
        }

        [Fact]
        public async Task GetOwnedLocationsAsync_WithMultipleLocations_ReturnsAllWithOwnerNames()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var location1 = new BusinessLocation { BusinessLocationId = 1, Name = "Location A", Address = "Addr A" };
            var location2 = new BusinessLocation { BusinessLocationId = 2, Name = "Location B", Address = "Addr B" };
            var location3 = new BusinessLocation { BusinessLocationId = 3, Name = "Location C", Address = "Addr C" };
            var locations = new List<BusinessLocation> { location1, location2, location3 };

            _mockUnitOfWork.Setup(x => x.BusinessLocations.GetOwnedByUserIdAsync(userId))
                .ReturnsAsync(locations);
            _mockUnitOfWork.Setup(x => x.BusinessLocations.GetByIdWithOwnerAsync(1))
                .ReturnsAsync((location1, "Owner"));
            _mockUnitOfWork.Setup(x => x.BusinessLocations.GetByIdWithOwnerAsync(2))
                .ReturnsAsync((location2, "Owner"));
            _mockUnitOfWork.Setup(x => x.BusinessLocations.GetByIdWithOwnerAsync(3))
                .ReturnsAsync((location3, "Owner"));
            _mockMapper.Setup(x => x.Map<BusinessLocationDto>(location1))
                .Returns(new BusinessLocationDto { Id = 1, Name = "Location A", Address = "Addr A" });
            _mockMapper.Setup(x => x.Map<BusinessLocationDto>(location2))
                .Returns(new BusinessLocationDto { Id = 2, Name = "Location B", Address = "Addr B" });
            _mockMapper.Setup(x => x.Map<BusinessLocationDto>(location3))
                .Returns(new BusinessLocationDto { Id = 3, Name = "Location C", Address = "Addr C" });

            // Act
            var result = await _businessLocationService.GetOwnedLocationsAsync(userId);

            // Assert
            var resultList = result.ToList();
            Assert.Equal(3, resultList.Count);
            Assert.Equal("Location A", resultList[0].Name);
            Assert.Equal("Location B", resultList[1].Name);
            Assert.Equal("Location C", resultList[2].Name);
            Assert.All(resultList, dto => Assert.Equal("Owner", dto.OwnerName));
        }

        #endregion

        #region GetWorkLocationsAsync Tests

        [Fact]
        public async Task GetWorkLocationsAsync_WithValidUserId_ReturnsWorkLocations()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var location1 = new BusinessLocation { BusinessLocationId = 5, Name = "Work Location 1", Address = "Work Addr 1" };
            var location2 = new BusinessLocation { BusinessLocationId = 6, Name = "Work Location 2", Address = "Work Addr 2" };
            var locations = new List<BusinessLocation> { location1, location2 };

            _mockUnitOfWork.Setup(x => x.BusinessLocations.GetWorkLocationsByUserIdAsync(userId))
                .ReturnsAsync(locations);
            _mockUnitOfWork.Setup(x => x.BusinessLocations.GetByIdWithOwnerAsync(5))
                .ReturnsAsync((location1, "Owner 1"));
            _mockUnitOfWork.Setup(x => x.BusinessLocations.GetByIdWithOwnerAsync(6))
                .ReturnsAsync((location2, "Owner 2"));
            _mockMapper.Setup(x => x.Map<BusinessLocationDto>(location1))
                .Returns(new BusinessLocationDto { Id = 5, Name = "Work Location 1", Address = "Work Addr 1" });
            _mockMapper.Setup(x => x.Map<BusinessLocationDto>(location2))
                .Returns(new BusinessLocationDto { Id = 6, Name = "Work Location 2", Address = "Work Addr 2" });

            // Act
            var result = await _businessLocationService.GetWorkLocationsAsync(userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            var resultList = result.ToList();
            Assert.Equal("Work Location 1", resultList[0].Name);
            Assert.Equal("Owner 1", resultList[0].OwnerName);
            _mockUnitOfWork.Verify(x => x.BusinessLocations.GetWorkLocationsByUserIdAsync(userId), Times.Once);
        }

        [Fact]
        public async Task GetWorkLocationsAsync_WithNoWorkLocations_ReturnsEmptyList()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _mockUnitOfWork.Setup(x => x.BusinessLocations.GetWorkLocationsByUserIdAsync(userId))
                .ReturnsAsync(new List<BusinessLocation>());

            // Act
            var result = await _businessLocationService.GetWorkLocationsAsync(userId);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        #endregion

        #region UpdateLocationStatusAsync Tests

        [Fact]
        public async Task UpdateLocationStatusAsync_WithValidOwner_UpdatesStatus()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var locationId = 1;
            var location = new BusinessLocation { BusinessLocationId = locationId, Name = "Test", IsActive = true };

            _mockUnitOfWork.Setup(x => x.BusinessLocations.IsOwnerOfLocationAsync(userId, locationId))
                .ReturnsAsync(true);
            _mockUnitOfWork.Setup(x => x.BusinessLocations.GetByIdAsync(locationId))
                .ReturnsAsync(location);

            // Act
            var result = await _businessLocationService.UpdateLocationStatusAsync(userId, locationId, false);

            // Assert
            Assert.True(result);
            Assert.False(location.IsActive);
            _mockUnitOfWork.Verify(x => x.BusinessLocations.Update(location), Times.Once);
        }

        [Fact]
        public async Task UpdateLocationStatusAsync_WithoutOwnership_ReturnsFalse()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var locationId = 1;

            _mockUnitOfWork.Setup(x => x.BusinessLocations.IsOwnerOfLocationAsync(userId, locationId))
                .ReturnsAsync(false);

            // Act
            var result = await _businessLocationService.UpdateLocationStatusAsync(userId, locationId, false);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region UpdateLocationAsync Tests

        [Fact]
        public async Task UpdateLocationAsync_WithValidOwnerAndUniqueName_UpdatesLocation()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var locationId = 1;
            var location = new BusinessLocation { BusinessLocationId = locationId, Name = "Old Name", Address = "Old Addr" };
            var request = new UpdateLocationRequest { Name = "New Name", Address = "New Addr" };

            _mockUnitOfWork.Setup(x => x.BusinessLocations.GetByIdAsync(locationId))
                .ReturnsAsync(location);
            _mockUnitOfWork.Setup(x => x.BusinessLocations.IsOwnerOfLocationAsync(userId, locationId))
                .ReturnsAsync(true);
            _mockUnitOfWork.Setup(x => x.BusinessLocations.IsExistedByNameAsync(userId, "New Name"))
                .ReturnsAsync(false);
            // Don't use callback with Map - just track the call
            _mockMapper.Setup(x => x.Map(It.IsAny<UpdateLocationRequest>(), It.IsAny<BusinessLocation>()));

            // Act
            var result = await _businessLocationService.UpdateLocationAsync(userId, locationId, request);

            // Assert
            Assert.True(result);
            _mockUnitOfWork.Verify(x => x.BusinessLocations.Update(location), Times.Once);
        }

        [Fact]
        public async Task UpdateLocationAsync_WithDuplicateName_ThrowsConflictException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var locationId = 1;
            var location = new BusinessLocation { BusinessLocationId = locationId, Name = "Existing Name" };
            var request = new UpdateLocationRequest { Name = "Duplicate Name" };

            _mockUnitOfWork.Setup(x => x.BusinessLocations.GetByIdAsync(locationId))
                .ReturnsAsync(location);
            _mockUnitOfWork.Setup(x => x.BusinessLocations.IsOwnerOfLocationAsync(userId, locationId))
                .ReturnsAsync(true);
            _mockUnitOfWork.Setup(x => x.BusinessLocations.IsExistedByNameAsync(userId, "Duplicate Name"))
                .ReturnsAsync(true);

            // Act & Assert
            await Assert.ThrowsAsync<ConflictException>(
                () => _businessLocationService.UpdateLocationAsync(userId, locationId, request));
        }

        [Fact]
        public async Task UpdateLocationAsync_WithoutOwnership_ReturnsFalse()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var locationId = 1;
            var location = new BusinessLocation { BusinessLocationId = locationId, Name = "Test" };
            var request = new UpdateLocationRequest { Name = "Updated" };

            _mockUnitOfWork.Setup(x => x.BusinessLocations.GetByIdAsync(locationId))
                .ReturnsAsync(location);
            _mockUnitOfWork.Setup(x => x.BusinessLocations.IsOwnerOfLocationAsync(userId, locationId))
                .ReturnsAsync(false);

            // Act
            var result = await _businessLocationService.UpdateLocationAsync(userId, locationId, request);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region AddEmployeesToLocationAsync Tests

        [Fact]
        public async Task AddEmployeesToLocationAsync_WithValidEmployees_AddsSuccessfully()
        {
            // Arrange
            var ownerId = Guid.NewGuid();
            var locationId = 1;
            var employee1 = Guid.NewGuid();
            var employee2 = Guid.NewGuid();
            var employeeIds = new List<Guid> { employee1, employee2 };
            var location = new BusinessLocation { BusinessLocationId = locationId };

            _mockUnitOfWork.Setup(x => x.BusinessLocations.IsOwnerOfLocationAsync(ownerId, locationId))
                .ReturnsAsync(true);
            _mockUnitOfWork.Setup(x => x.BusinessLocations.GetByIdAsync(locationId))
                .ReturnsAsync(location);
            _mockUnitOfWork.Setup(x => x.BusinessLocations.GetAssignedEmployeeIdsAsync(locationId))
                .ReturnsAsync(new List<Guid>());
            _mockHireService.Setup(x => x.ValidateEmployeesForAssignmentAsync(ownerId, employeeIds))
                .ReturnsAsync(new EmployeeValidationResult 
                { 
                    ValidEmployeeIds = employeeIds, 
                    InvalidEmployeeIds = new List<Guid>() 
                });

            // Act
            var result = await _businessLocationService.AddEmployeesToLocationAsync(ownerId, locationId, employeeIds);

            // Assert
            Assert.True(result);
            _mockUnitOfWork.Verify(x => x.BusinessLocations.AddUserLocationAssignmentAsync(It.IsAny<UserLocationAssignment>()), Times.Exactly(2));
        }

        [Fact]
        public async Task AddEmployeesToLocationAsync_WithInvalidEmployees_ThrowsBadRequestException()
        {
            // Arrange
            var ownerId = Guid.NewGuid();
            var locationId = 1;
            var invalidEmployee = Guid.NewGuid();
            var employeeIds = new List<Guid> { invalidEmployee };

            _mockUnitOfWork.Setup(x => x.BusinessLocations.IsOwnerOfLocationAsync(ownerId, locationId))
                .ReturnsAsync(true);
            _mockUnitOfWork.Setup(x => x.BusinessLocations.GetByIdAsync(locationId))
                .ReturnsAsync(new BusinessLocation { BusinessLocationId = locationId });
            _mockHireService.Setup(x => x.ValidateEmployeesForAssignmentAsync(ownerId, employeeIds))
                .ReturnsAsync(new EmployeeValidationResult 
                { 
                    ValidEmployeeIds = new List<Guid>(), 
                    InvalidEmployeeIds = employeeIds 
                });

            // Act & Assert
            await Assert.ThrowsAsync<BadRequestException>(
                () => _businessLocationService.AddEmployeesToLocationAsync(ownerId, locationId, employeeIds));
        }

        [Fact]
        public async Task AddEmployeesToLocationAsync_WithoutOwnership_ReturnsFalse()
        {
            // Arrange
            var ownerId = Guid.NewGuid();
            var locationId = 1;
            var employeeIds = new List<Guid> { Guid.NewGuid() };

            _mockUnitOfWork.Setup(x => x.BusinessLocations.IsOwnerOfLocationAsync(ownerId, locationId))
                .ReturnsAsync(false);

            // Act
            var result = await _businessLocationService.AddEmployeesToLocationAsync(ownerId, locationId, employeeIds);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region GetEmployeesByLocationAsync Tests

        [Fact]
        public async Task GetEmployeesByLocationAsync_WithValidOwner_ReturnsEmployees()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var locationId = 1;
            var empId1 = Guid.NewGuid();
            var empId2 = Guid.NewGuid();
            var employees = new List<(Guid UserId, string FullName, string Email, string Phone)>
            {
                (empId1, "Employee 1", "emp1@test.com", "123"),
                (empId2, "Employee 2", "emp2@test.com", "456")
            };

            _mockUnitOfWork.Setup(x => x.BusinessLocations.IsOwnerOfLocationAsync(userId, locationId))
                .ReturnsAsync(true);
            _mockUnitOfWork.Setup(x => x.BusinessLocations.GetEmployeesByLocationIdAsync(locationId))
                .ReturnsAsync(employees);
            _mockMapper.Setup(x => x.Map<List<EmployeeSummaryDto>>(It.IsAny<IEnumerable<(Guid, string, string, string)>>()))
                .Returns(new List<EmployeeSummaryDto>
                {
                    new() { UserId = empId1.ToString(), UserName = "Employee 1" },
                    new() { UserId = empId2.ToString(), UserName = "Employee 2" }
                });

            // Act
            var result = await _businessLocationService.GetEmployeesByLocationAsync(userId, locationId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Employees.Count);
            _mockUnitOfWork.Verify(x => x.BusinessLocations.IsOwnerOfLocationAsync(userId, locationId), Times.Once);
        }

        [Fact]
        public async Task GetEmployeesByLocationAsync_WithoutOwnership_ThrowsForbiddenException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var locationId = 1;

            _mockUnitOfWork.Setup(x => x.BusinessLocations.IsOwnerOfLocationAsync(userId, locationId))
                .ReturnsAsync(false);

            // Act & Assert
            await Assert.ThrowsAsync<ForbiddenException>(
                () => _businessLocationService.GetEmployeesByLocationAsync(userId, locationId));
        }

        [Fact]
        public async Task GetEmployeesByLocationAsync_WithNoEmployees_ReturnsEmptyList()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var locationId = 1;
            var emptyEmployees = new List<(Guid UserId, string FullName, string Email, string Phone)>();

            _mockUnitOfWork.Setup(x => x.BusinessLocations.IsOwnerOfLocationAsync(userId, locationId))
                .ReturnsAsync(true);
            _mockUnitOfWork.Setup(x => x.BusinessLocations.GetEmployeesByLocationIdAsync(locationId))
                .ReturnsAsync(emptyEmployees);
            _mockMapper.Setup(x => x.Map<List<EmployeeSummaryDto>>(It.IsAny<IEnumerable<(Guid, string, string, string)>>()))
                .Returns(new List<EmployeeSummaryDto>());

            // Act
            var result = await _businessLocationService.GetEmployeesByLocationAsync(userId, locationId);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Employees);
        }

        #endregion

        #region DeleteLocationAsync Tests

        [Fact]
        public async Task DeleteLocationAsync_WithValidOwner_SoftDeletesLocation()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var locationId = 1;
            var location = new BusinessLocation { BusinessLocationId = locationId, Name = "To Delete", DeletedAt = null };

            _mockUnitOfWork.Setup(x => x.BusinessLocations.IsOwnerOfLocationAsync(userId, locationId))
                .ReturnsAsync(true);
            _mockUnitOfWork.Setup(x => x.BusinessLocations.GetByIdAsync(locationId))
                .ReturnsAsync(location);

            // Act
            var result = await _businessLocationService.DeleteLocationAsync(userId, locationId);

            // Assert
            Assert.True(result);
            Assert.NotNull(location.DeletedAt);
            _mockUnitOfWork.Verify(x => x.BusinessLocations.Update(location), Times.Once);
        }

        [Fact]
        public async Task DeleteLocationAsync_WithoutOwnership_ReturnsFalse()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var locationId = 1;

            _mockUnitOfWork.Setup(x => x.BusinessLocations.IsOwnerOfLocationAsync(userId, locationId))
                .ReturnsAsync(false);

            // Act
            var result = await _businessLocationService.DeleteLocationAsync(userId, locationId);

            // Assert
            Assert.False(result);
        }

        #endregion
    }
}
