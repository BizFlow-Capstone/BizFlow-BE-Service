using BizFlow.Api.Controllers.Location;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.DTOs.Hire;
using BizFlow.Application.DTOs.Location;
using BizFlow.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BizFlow.Application.Tests.Controllers
{
    public class BusinessLocationControllerTests
    {
        // ─── Mock dependencies ──────────────────────────────────────────
        private readonly Mock<IBusinessLocationService> _mockLocationService;
        private readonly Mock<IMessageService> _mockMessageService;
        private readonly Mock<ILogger<BusinessLocationController>> _mockLogger;
        private readonly BusinessLocationController _controller;

        // Mock user IDs that match the controller's static fields
        private static readonly Guid _mockOwnerUserId = Guid.Parse("550e8400-e29b-41d4-a716-446655440001");
        private static readonly Guid _mockEmployeeId  = Guid.Parse("550e8400-e29b-41d4-a716-446655440003");

        public BusinessLocationControllerTests()
        {
            _mockLocationService = new Mock<IBusinessLocationService>();
            _mockMessageService  = new Mock<IMessageService>();
            _mockLogger          = new Mock<ILogger<BusinessLocationController>>();

            _mockMessageService.Setup(m => m.GetMessage(It.IsAny<string>())).Returns("ok");
            _mockMessageService.Setup(m => m.GetMessage(It.IsAny<string>(), It.IsAny<object[]>())).Returns("ok");

            _controller = new BusinessLocationController(
                _mockLocationService.Object,
                _mockMessageService.Object,
                _mockLogger.Object);
        }

        // ================================================================
        // GET /api/location/me/owned
        // ================================================================

        [Fact]
        public async Task GetOwnedLocations_ReturnsOk_WithLocationList()
        {
            // Arrange
            var locations = new List<BusinessLocationDto>
            {
                new() { Id = 1, Name = "Location A", Address = "123 St", IsActive = true },
                new() { Id = 2, Name = "Location B", Address = "456 Rd", IsActive = false }
            };
            _mockLocationService.Setup(s => s.GetOwnedLocationsAsync(_mockOwnerUserId))
                .ReturnsAsync(locations);

            // Act
            var result = await _controller.GetOwnedLocations();

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, ok.StatusCode);
            _mockLocationService.Verify(s => s.GetOwnedLocationsAsync(_mockOwnerUserId), Times.Once);
        }

        // ================================================================
        // POST /api/location/create
        // ================================================================

        [Fact]
        public async Task CreateLocation_Returns201_WhenCreatedSuccessfully()
        {
            // Arrange
            var request = new CreateLocationRequest { Name = "New Location", Address = "789 Ave" };
            var dto = new BusinessLocationDto { Id = 3, Name = "New Location", Address = "789 Ave", IsActive = true };
            _mockLocationService.Setup(s => s.CreateLocationAsync(_mockOwnerUserId, request))
                .ReturnsAsync(dto);

            // Act
            var result = await _controller.CreateLocation(request);

            // Assert
            var created = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(201, created.StatusCode);
            _mockLocationService.Verify(s => s.CreateLocationAsync(_mockOwnerUserId, request), Times.Once);
        }

        // ================================================================
        // PUT /api/location/me/owned/{id}/status
        // ================================================================

        [Fact]
        public async Task UpdateLocationStatus_Returns200_WhenServiceReturnsTrue()
        {
            // Arrange
            var locationId = 1;
            var request = new UpdateLocationStatusRequest { IsActive = true };
            _mockLocationService.Setup(s => s.UpdateLocationStatusAsync(_mockOwnerUserId, locationId, true))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.UpdateLocationStatus(locationId, request);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, ok.StatusCode);
        }

        [Fact]
        public async Task UpdateLocationStatus_Returns403_WhenServiceReturnsFalse()
        {
            // Arrange
            var locationId = 1;
            var request = new UpdateLocationStatusRequest { IsActive = false };
            _mockLocationService.Setup(s => s.UpdateLocationStatusAsync(_mockOwnerUserId, locationId, false))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.UpdateLocationStatus(locationId, request);

            // Assert
            var statusResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(403, statusResult.StatusCode);
        }

        // ================================================================
        // PUT /api/location/me/owned/{id}
        // ================================================================

        [Fact]
        public async Task UpdateLocation_Returns200_WhenSuccessful()
        {
            // Arrange
            var locationId = 1;
            var request = new UpdateLocationRequest { Name = "Updated Name", Address = "New Address" };
            _mockLocationService.Setup(s => s.UpdateLocationAsync(_mockOwnerUserId, locationId, request))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.UpdateLocation(locationId, request);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, ok.StatusCode);
        }

        [Fact]
        public async Task UpdateLocation_Returns403_WhenAccessDenied()
        {
            // Arrange
            var locationId = 1;
            var request = new UpdateLocationRequest { Name = "Updated Name", Address = "New Address" };
            _mockLocationService.Setup(s => s.UpdateLocationAsync(_mockOwnerUserId, locationId, request))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.UpdateLocation(locationId, request);

            // Assert
            var statusResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(403, statusResult.StatusCode);
        }

        // ================================================================
        // POST /api/location/{locationId}/employees
        // ================================================================

        [Fact]
        public async Task AddEmployeesToLocation_Returns200_WhenSuccessful()
        {
            // Arrange
            var locationId = 1;
            var employeeIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
            _mockLocationService.Setup(s => s.AddEmployeesToLocationAsync(_mockOwnerUserId, locationId, employeeIds))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.AddEmployeesToLocation(locationId, employeeIds);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, ok.StatusCode);
        }

        [Fact]
        public async Task AddEmployeesToLocation_Returns403_WhenAccessDenied()
        {
            // Arrange
            var locationId = 1;
            var employeeIds = new List<Guid> { Guid.NewGuid() };
            _mockLocationService.Setup(s => s.AddEmployeesToLocationAsync(_mockOwnerUserId, locationId, employeeIds))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.AddEmployeesToLocation(locationId, employeeIds);

            // Assert
            var statusResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(403, statusResult.StatusCode);
        }

        // ================================================================
        // DELETE /api/location/me/owned/{id}
        // ================================================================

        [Fact]
        public async Task DeleteLocation_Returns200_WhenSuccessful()
        {
            // Arrange
            var locationId = 1;
            _mockLocationService.Setup(s => s.DeleteLocationAsync(_mockOwnerUserId, locationId))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteLocation(locationId);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, ok.StatusCode);
            _mockLocationService.Verify(s => s.DeleteLocationAsync(_mockOwnerUserId, locationId), Times.Once);
        }

        [Fact]
        public async Task DeleteLocation_Returns403_WhenAccessDenied()
        {
            // Arrange
            var locationId = 1;
            _mockLocationService.Setup(s => s.DeleteLocationAsync(_mockOwnerUserId, locationId))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.DeleteLocation(locationId);

            // Assert
            var statusResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(403, statusResult.StatusCode);
        }

        // ================================================================
        // GET /api/location/me/owned/{locationId}/employees
        // ================================================================

        [Fact]
        public async Task GetEmployeesByLocation_ReturnsOk_WithEmployeeList()
        {
            // Arrange
            var locationId = 1;
            var summaryList = new EmployeeSummaryListDto
            {
                Employees = new List<EmployeeSummaryDto>
                {
                    new() { UserId = Guid.NewGuid().ToString(), UserName = "emp_a" }
                }
            };
            _mockLocationService.Setup(s => s.GetEmployeesByLocationAsync(_mockOwnerUserId, locationId))
                .ReturnsAsync(summaryList);

            // Act
            var result = await _controller.GetEmployeesByLocation(locationId);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, ok.StatusCode);
        }

        // ================================================================
        // GET /api/location/work-at-locations  (uses employee ID)
        // ================================================================

        [Fact]
        public async Task GetWorkAtLocations_ReturnsOk_WithLocationList()
        {
            // Arrange — controller uses _employeeId internally, not _mockCurrentUserId
            var locations = new List<BusinessLocationDto>
            {
                new() { Id = 5, Name = "Shop X", Address = "1 Road", IsActive = true }
            };
            _mockLocationService.Setup(s => s.GetWorkLocationsAsync(_mockEmployeeId))
                .ReturnsAsync(locations);

            // Act
            var result = await _controller.GetWorkAtLocations();

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, ok.StatusCode);
            _mockLocationService.Verify(s => s.GetWorkLocationsAsync(_mockEmployeeId), Times.Once);
        }
    }
}
