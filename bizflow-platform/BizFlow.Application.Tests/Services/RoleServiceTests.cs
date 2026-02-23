using BizFlow.Application.DTOs;
using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Application.Services;
using BizFlow.Application.Tests.Fixtures;
using BizFlow.Domain.Entities;
using Moq;
using Xunit;

namespace BizFlow.Application.Tests.Services
{
    public class RoleServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly RoleService _roleService;

        public RoleServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _roleService = new RoleService(_mockUnitOfWork.Object);
        }

        #region GetAllRolesAsync Tests

        [Fact]
        public async Task GetAllRolesAsync_WithValidRoles_ReturnsListOfRoleDtos()
        {
            // Arrange
            var testRoles = TestFixtures.CreateTestRoles(3);
            _mockUnitOfWork.Setup(x => x.Roles.GetAllAsync())
                .ReturnsAsync(testRoles);

            // Act
            var result = await _roleService.GetAllRolesAsync();

            // Assert
            Assert.NotNull(result);
            var roleDtos = result.ToList();
            Assert.Equal(3, roleDtos.Count);
            Assert.All(roleDtos, role => Assert.NotNull(role));
            _mockUnitOfWork.Verify(x => x.Roles.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetAllRolesAsync_WithNoRoles_ReturnsEmptyList()
        {
            // Arrange
            _mockUnitOfWork.Setup(x => x.Roles.GetAllAsync())
                .ReturnsAsync(new List<Role>());

            // Act
            var result = await _roleService.GetAllRolesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAllRolesAsync_MapsRolePropertiesToDto()
        {
            // Arrange
            var roleId = Guid.NewGuid();
            var testRoles = new List<Role>
            {
                new Role
                {
                    Id = roleId,
                    Name = "Admin",
                    Description = "Administrator Role",
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                    UpdatedAt = DateTime.UtcNow
                }
            };
            _mockUnitOfWork.Setup(x => x.Roles.GetAllAsync())
                .ReturnsAsync(testRoles);

            // Act
            var result = await _roleService.GetAllRolesAsync();
            var roleDto = result.First();

            // Assert
            Assert.Equal(roleId, roleDto.Id);
            Assert.Equal("Admin", roleDto.Name);
            Assert.Equal("Administrator Role", roleDto.Description);
        }

        #endregion

        #region GetRoleByIdAsync Tests

        [Fact]
        public async Task GetRoleByIdAsync_WithValidId_ReturnsRoleDto()
        {
            // Arrange
            var roleId = Guid.NewGuid();
            var testRole = TestFixtures.CreateTestRole(roleId, "Manager");
            _mockUnitOfWork.Setup(x => x.Roles.GetByIdAsync(roleId))
                .ReturnsAsync(testRole);

            // Act
            var result = await _roleService.GetRoleByIdAsync(roleId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(roleId, result.Id);
            Assert.Equal("Manager", result.Name);
        }

        [Fact]
        public async Task GetRoleByIdAsync_WithInvalidId_ReturnsNull()
        {
            // Arrange
            var roleId = Guid.NewGuid();
            _mockUnitOfWork.Setup(x => x.Roles.GetByIdAsync(roleId))
                .ReturnsAsync((Role?)null);

            // Act
            var result = await _roleService.GetRoleByIdAsync(roleId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetRoleByIdAsync_CallsRepositoryWithCorrectId()
        {
            // Arrange
            var roleId = Guid.NewGuid();
            _mockUnitOfWork.Setup(x => x.Roles.GetByIdAsync(roleId))
                .ReturnsAsync((Role?)null);

            // Act
            await _roleService.GetRoleByIdAsync(roleId);

            // Assert
            _mockUnitOfWork.Verify(x => x.Roles.GetByIdAsync(roleId), Times.Once);
        }

        #endregion

        #region GetRoleByNameAsync Tests

        [Fact]
        public async Task GetRoleByNameAsync_WithValidName_ReturnsRoleDto()
        {
            // Arrange
            var roleName = "Consultant";
            var testRole = TestFixtures.CreateTestRole(null, roleName);
            _mockUnitOfWork.Setup(x => x.Roles.GetByNameAsync(roleName))
                .ReturnsAsync(testRole);

            // Act
            var result = await _roleService.GetRoleByNameAsync(roleName);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(roleName, result.Name);
        }

        [Fact]
        public async Task GetRoleByNameAsync_WithInvalidName_ReturnsNull()
        {
            // Arrange
            var roleName = "NonExistentRole";
            _mockUnitOfWork.Setup(x => x.Roles.GetByNameAsync(roleName))
                .ReturnsAsync((Role?)null);

            // Act
            var result = await _roleService.GetRoleByNameAsync(roleName);

            // Assert
            Assert.Null(result);
        }

        [Theory]
        [InlineData("Admin")]
        [InlineData("Manager")]
        [InlineData("Employee")]
        [InlineData("Consultant")]
        public async Task GetRoleByNameAsync_WithDifferentNames_ReturnsCorrectRole(string name)
        {
            // Arrange
            var testRole = TestFixtures.CreateTestRole(null, name);
            _mockUnitOfWork.Setup(x => x.Roles.GetByNameAsync(name))
                .ReturnsAsync(testRole);

            // Act
            var result = await _roleService.GetRoleByNameAsync(name);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(name, result.Name);
        }

        #endregion
    }
}
