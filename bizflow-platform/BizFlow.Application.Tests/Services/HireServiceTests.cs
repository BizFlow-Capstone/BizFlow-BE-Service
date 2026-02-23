using BizFlow.Application.DTOs.Hire;
using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Application.Services;
using BizFlow.Domain.Entities;
using AutoMapper;
using Moq;
using Xunit;

namespace BizFlow.Application.Tests.Services
{
    public class HireServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IMapper> _mockMapper;
        private readonly HireService _hireService;

        public HireServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockMapper = new Mock<IMapper>();
            _hireService = new HireService(_mockUnitOfWork.Object, _mockMapper.Object);
        }

        #region GetEmployeeSummariesAsync Tests

        [Fact]
        public async Task GetEmployeeSummariesAsync_WithValidOwnerId_ReturnsEmployeeSummaryList()
        {
            // Arrange
            var ownerId = Guid.NewGuid();
            var employeeId1 = Guid.NewGuid();
            var employeeId2 = Guid.NewGuid();
            
            var employees = new List<(Hire, string, string, string?)>
            {
                (new Hire { EmployeeId = employeeId1 }, "John Doe", "john@test.com", "1234567890"),
                (new Hire { EmployeeId = employeeId2 }, "Jane Smith", "jane@test.com", "0987654321")
            };

            var mappedDtos = new List<EmployeeSummaryDto>
            {
                new() { UserId = employeeId1.ToString(), UserName = "John Doe", Phone = "1234567890" },
                new() { UserId = employeeId2.ToString(), UserName = "Jane Smith", Phone = "0987654321" }
            };

            _mockUnitOfWork.Setup(x => x.Hires.GetHiredEmployeesWithDetailsAsync(ownerId))
                .ReturnsAsync(employees);
            _mockMapper.Setup(x => x.Map<List<EmployeeSummaryDto>>(employees))
                .Returns(mappedDtos);

            // Act
            var result = await _hireService.GetEmployeeSummariesAsync(ownerId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Employees.Count);
            Assert.Equal("John Doe", result.Employees[0].UserName);
            Assert.Equal("Jane Smith", result.Employees[1].UserName);
            _mockUnitOfWork.Verify(x => x.Hires.GetHiredEmployeesWithDetailsAsync(ownerId), Times.Once);
        }

        [Fact]
        public async Task GetEmployeeSummariesAsync_WithNoEmployees_ReturnsEmptyList()
        {
            // Arrange
            var ownerId = Guid.NewGuid();
            var emptyEmployees = new List<(Hire, string, string, string?)>();

            _mockUnitOfWork.Setup(x => x.Hires.GetHiredEmployeesWithDetailsAsync(ownerId))
                .ReturnsAsync(emptyEmployees);
            _mockMapper.Setup(x => x.Map<List<EmployeeSummaryDto>>(emptyEmployees))
                .Returns(new List<EmployeeSummaryDto>());

            // Act
            var result = await _hireService.GetEmployeeSummariesAsync(ownerId);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Employees);
        }

        #endregion

        #region GetHiredEmployeeDetailsAsync Tests

        [Fact]
        public async Task GetHiredEmployeeDetailsAsync_WithValidOwnerId_ReturnsHiredEmployeeDetails()
        {
            // Arrange
            var ownerId = Guid.NewGuid();
            var employeeId = Guid.NewGuid();
            var now = DateTime.UtcNow;

            var employees = new List<(Hire, string, string, string?)>
            {
                (new Hire { EmployeeId = employeeId, StartAt = now }, "John Doe", "john@test.com", "1234567890")
            };

            var mappedDtos = new List<HiredEmployeeDto>
            {
                new() { EmployeeId = employeeId, FullName = "John Doe", Email = "john@test.com", Phone = "1234567890", StartAt = now }
            };

            _mockUnitOfWork.Setup(x => x.Hires.GetHiredEmployeesWithDetailsAsync(ownerId))
                .ReturnsAsync(employees);
            _mockMapper.Setup(x => x.Map<IEnumerable<HiredEmployeeDto>>(employees))
                .Returns(mappedDtos);

            // Act
            var result = await _hireService.GetHiredEmployeeDetailsAsync(ownerId);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            var employee = result.First();
            Assert.Equal(employeeId, employee.EmployeeId);
            Assert.Equal("John Doe", employee.FullName);
            Assert.Equal("john@test.com", employee.Email);
            _mockUnitOfWork.Verify(x => x.Hires.GetHiredEmployeesWithDetailsAsync(ownerId), Times.Once);
        }

        [Fact]
        public async Task GetHiredEmployeeDetailsAsync_WithNoEmployees_ReturnsEmptyList()
        {
            // Arrange
            var ownerId = Guid.NewGuid();
            var emptyEmployees = new List<(Hire, string, string, string?)>();

            _mockUnitOfWork.Setup(x => x.Hires.GetHiredEmployeesWithDetailsAsync(ownerId))
                .ReturnsAsync(emptyEmployees);
            _mockMapper.Setup(x => x.Map<IEnumerable<HiredEmployeeDto>>(emptyEmployees))
                .Returns(new List<HiredEmployeeDto>());

            // Act
            var result = await _hireService.GetHiredEmployeeDetailsAsync(ownerId);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        #endregion

        #region ValidateEmployeesForAssignmentAsync Tests

        [Fact]
        public async Task ValidateEmployeesForAssignmentAsync_WithAllValidEmployees_ReturnsAllValid()
        {
            // Arrange
            var ownerId = Guid.NewGuid();
            var employee1 = Guid.NewGuid();
            var employee2 = Guid.NewGuid();
            var employeeIds = new List<Guid> { employee1, employee2 };
            var hiredEmployeeIds = new List<Guid> { employee1, employee2, Guid.NewGuid() };

            _mockUnitOfWork.Setup(x => x.Hires.GetHiredEmployeeIdsAsync(ownerId))
                .ReturnsAsync(hiredEmployeeIds);

            // Act
            var result = await _hireService.ValidateEmployeesForAssignmentAsync(ownerId, employeeIds);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.ValidEmployeeIds.Count);
            Assert.Empty(result.InvalidEmployeeIds);
            Assert.Contains(employee1, result.ValidEmployeeIds);
            Assert.Contains(employee2, result.ValidEmployeeIds);
            Assert.True(result.AllValid);
        }

        [Fact]
        public async Task ValidateEmployeesForAssignmentAsync_WithMixedEmployees_SeparatesValidAndInvalid()
        {
            // Arrange
            var ownerId = Guid.NewGuid();
            var validEmployee = Guid.NewGuid();
            var invalidEmployee = Guid.NewGuid();
            var employeeIds = new List<Guid> { validEmployee, invalidEmployee };
            var hiredEmployeeIds = new List<Guid> { validEmployee };

            _mockUnitOfWork.Setup(x => x.Hires.GetHiredEmployeeIdsAsync(ownerId))
                .ReturnsAsync(hiredEmployeeIds);

            // Act
            var result = await _hireService.ValidateEmployeesForAssignmentAsync(ownerId, employeeIds);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.ValidEmployeeIds);
            Assert.Single(result.InvalidEmployeeIds);
            Assert.Contains(validEmployee, result.ValidEmployeeIds);
            Assert.Contains(invalidEmployee, result.InvalidEmployeeIds);
            Assert.False(result.AllValid);
        }

        [Fact]
        public async Task ValidateEmployeesForAssignmentAsync_WithDuplicateIds_DeduplicatesAndValidates()
        {
            // Arrange
            var ownerId = Guid.NewGuid();
            var employee = Guid.NewGuid();
            var employeeIds = new List<Guid> { employee, employee, employee };
            var hiredEmployeeIds = new List<Guid> { employee };

            _mockUnitOfWork.Setup(x => x.Hires.GetHiredEmployeeIdsAsync(ownerId))
                .ReturnsAsync(hiredEmployeeIds);

            // Act
            var result = await _hireService.ValidateEmployeesForAssignmentAsync(ownerId, employeeIds);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.ValidEmployeeIds);
            Assert.Empty(result.InvalidEmployeeIds);
            Assert.True(result.AllValid);
        }

        [Fact]
        public async Task ValidateEmployeesForAssignmentAsync_WithNoValidEmployees_ReturnsAllInvalid()
        {
            // Arrange
            var ownerId = Guid.NewGuid();
            var employee1 = Guid.NewGuid();
            var employee2 = Guid.NewGuid();
            var employeeIds = new List<Guid> { employee1, employee2 };
            var hiredEmployeeIds = new List<Guid>();

            _mockUnitOfWork.Setup(x => x.Hires.GetHiredEmployeeIdsAsync(ownerId))
                .ReturnsAsync(hiredEmployeeIds);

            // Act
            var result = await _hireService.ValidateEmployeesForAssignmentAsync(ownerId, employeeIds);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.ValidEmployeeIds);
            Assert.Equal(2, result.InvalidEmployeeIds.Count);
            Assert.False(result.AllValid);
        }

        #endregion
    }
}
