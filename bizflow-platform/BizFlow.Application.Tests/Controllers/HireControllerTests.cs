using BizFlow.Api.Controllers;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.DTOs.Hire;
using BizFlow.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BizFlow.Application.Tests.Controllers
{
    public class HireControllerTests
    {
        // ─── Mock dependencies ──────────────────────────────────────────
        private readonly Mock<IHireService> _mockHireService;
        private readonly Mock<IMessageService> _mockMessageService;
        private readonly Mock<ILogger<HireController>> _mockLogger;
        private readonly HireController _controller;

        // Mock user ID that matches the controller's field
        private static readonly Guid _mockUserId = Guid.Parse("550e8400-e29b-41d4-a716-446655440001");

        public HireControllerTests()
        {
            _mockHireService = new Mock<IHireService>();
            _mockMessageService = new Mock<IMessageService>();
            _mockLogger = new Mock<ILogger<HireController>>();

            _mockMessageService.Setup(m => m.GetMessage(It.IsAny<string>())).Returns("ok");
            _mockMessageService.Setup(m => m.GetMessage(It.IsAny<string>(), It.IsAny<object[]>())).Returns("ok");

            _controller = new HireController(
                _mockHireService.Object,
                _mockMessageService.Object,
                _mockLogger.Object);
        }

        // ================================================================
        // GET /api/my-employee/employees
        // ================================================================

        [Fact]
        public async Task GetMyHiredEmployees_ReturnsOk_WithEmployeeList()
        {
            // Arrange
            var summaryList = new EmployeeSummaryListDto
            {
                Employees = new List<EmployeeSummaryDto>
                {
                    new() { UserId = Guid.NewGuid().ToString(), UserName = "employee_a" },
                    new() { UserId = Guid.NewGuid().ToString(), UserName = "employee_b" }
                }
            };
            _mockHireService.Setup(s => s.GetEmployeeSummariesAsync(_mockUserId))
                .ReturnsAsync(summaryList);

            // Act
            var result = await _controller.GetMyHiredEmployees();

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, ok.StatusCode);
            _mockHireService.Verify(s => s.GetEmployeeSummariesAsync(_mockUserId), Times.Once);
        }

        [Fact]
        public async Task GetMyHiredEmployees_ReturnsOk_WithEmptyList()
        {
            // Arrange
            var emptyList = new EmployeeSummaryListDto { Employees = new List<EmployeeSummaryDto>() };
            _mockHireService.Setup(s => s.GetEmployeeSummariesAsync(_mockUserId))
                .ReturnsAsync(emptyList);

            // Act
            var result = await _controller.GetMyHiredEmployees();

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, ok.StatusCode);
        }

        // ================================================================
        // GET /api/my-employee/employees/details
        // ================================================================

        [Fact]
        public async Task GetMyHiredEmployeeDetails_ReturnsOk_WithDetailList()
        {
            // Arrange
            var details = new List<HiredEmployeeDto>
            {
                new() { EmployeeId = Guid.NewGuid(), FullName = "Employee A", Email = "a@test.com" },
                new() { EmployeeId = Guid.NewGuid(), FullName = "Employee B", Email = "b@test.com" }
            };
            _mockHireService.Setup(s => s.GetHiredEmployeeDetailsAsync(_mockUserId))
                .ReturnsAsync(details);

            // Act
            var result = await _controller.GetMyHiredEmployeeDetails();

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, ok.StatusCode);
            _mockHireService.Verify(s => s.GetHiredEmployeeDetailsAsync(_mockUserId), Times.Once);
        }

        [Fact]
        public async Task GetMyHiredEmployeeDetails_ReturnsOk_WithEmptyList()
        {
            // Arrange
            _mockHireService.Setup(s => s.GetHiredEmployeeDetailsAsync(_mockUserId))
                .ReturnsAsync(Enumerable.Empty<HiredEmployeeDto>());

            // Act
            var result = await _controller.GetMyHiredEmployeeDetails();

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, ok.StatusCode);
        }
    }
}
