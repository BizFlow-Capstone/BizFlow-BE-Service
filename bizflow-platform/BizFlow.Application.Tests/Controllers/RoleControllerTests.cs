using BizFlow.Api.Controllers;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.DTOs;
using BizFlow.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BizFlow.Application.Tests.Controllers
{
    public class RoleControllerTests
    {
        // ─── Mock dependencies ──────────────────────────────────────────
        private readonly Mock<IRoleService>    _mockRoleService;
        private readonly Mock<IMessageService> _mockMessageService;
        private readonly Mock<ILogger<RoleController>> _mockLogger;
        private readonly RoleController _controller;

        public RoleControllerTests()
        {
            _mockRoleService    = new Mock<IRoleService>();
            _mockMessageService = new Mock<IMessageService>();
            _mockLogger         = new Mock<ILogger<RoleController>>();

            _mockMessageService.Setup(m => m.GetMessage(It.IsAny<string>())).Returns("ok");
            _mockMessageService.Setup(m => m.GetMessage(It.IsAny<string>(), It.IsAny<object[]>())).Returns("ok");

            _controller = new RoleController(
                _mockRoleService.Object,
                _mockMessageService.Object,
                _mockLogger.Object);
        }

        // ================================================================
        // GET /api/roles
        // ================================================================

        [Fact]
        public async Task GetAllRoles_ReturnsOk_WithRoleList()
        {
            // Arrange
            var roles = new List<RoleDto>
            {
                new() { Id = Guid.NewGuid(), Name = "Admin" },
                new() { Id = Guid.NewGuid(), Name = "Manager" }
            };
            _mockRoleService.Setup(s => s.GetAllRolesAsync()).ReturnsAsync(roles);

            // Act
            var result = await _controller.GetAllRoles();

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, ok.StatusCode);
            _mockRoleService.Verify(s => s.GetAllRolesAsync(), Times.Once);
        }

        [Fact]
        public async Task GetAllRoles_ReturnsOk_WithEmptyList()
        {
            // Arrange
            _mockRoleService.Setup(s => s.GetAllRolesAsync())
                .ReturnsAsync(Enumerable.Empty<RoleDto>());

            // Act
            var result = await _controller.GetAllRoles();

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, ok.StatusCode);
        }

        // ================================================================
        // GET /api/roles/{id}
        // ================================================================

        [Fact]
        public async Task GetRoleById_ReturnsOk_WhenRoleExists()
        {
            // Arrange
            var roleId = Guid.NewGuid();
            var dto    = new RoleDto { Id = roleId, Name = "Admin" };
            _mockRoleService.Setup(s => s.GetRoleByIdAsync(roleId)).ReturnsAsync(dto);

            // Act
            var result = await _controller.GetRoleById(roleId);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, ok.StatusCode);
            _mockRoleService.Verify(s => s.GetRoleByIdAsync(roleId), Times.Once);
        }

        [Fact]
        public async Task GetRoleById_Returns404_WhenRoleNotFound()
        {
            // Arrange
            var roleId = Guid.NewGuid();
            _mockRoleService.Setup(s => s.GetRoleByIdAsync(roleId)).ReturnsAsync((RoleDto?)null);

            // Act
            var result = await _controller.GetRoleById(roleId);

            // Assert
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(404, notFound.StatusCode);
        }

        // ================================================================
        // GET /api/roles/by-name/{name}
        // ================================================================

        [Fact]
        public async Task GetRoleByName_ReturnsOk_WhenRoleExists()
        {
            // Arrange
            var roleName = "Manager";
            var dto      = new RoleDto { Id = Guid.NewGuid(), Name = roleName };
            _mockRoleService.Setup(s => s.GetRoleByNameAsync(roleName)).ReturnsAsync(dto);

            // Act
            var result = await _controller.GetRoleByName(roleName);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, ok.StatusCode);
            _mockRoleService.Verify(s => s.GetRoleByNameAsync(roleName), Times.Once);
        }

        [Fact]
        public async Task GetRoleByName_Returns404_WhenRoleNotFound()
        {
            // Arrange
            var roleName = "NonExistentRole";
            _mockRoleService.Setup(s => s.GetRoleByNameAsync(roleName)).ReturnsAsync((RoleDto?)null);

            // Act
            var result = await _controller.GetRoleByName(roleName);

            // Assert
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(404, notFound.StatusCode);
        }
    }
}
