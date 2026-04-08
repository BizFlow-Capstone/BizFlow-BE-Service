using BizFlow.Application.DTOs.BusinessType;
using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Application.Services;
using BizFlow.Domain.Entities;
using Moq;
using Xunit;

namespace BizFlow.Application.Tests.Services
{
    public class BusinessTypeServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly BusinessTypeService _businessTypeService;

        public BusinessTypeServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _businessTypeService = new BusinessTypeService(_mockUnitOfWork.Object);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsMappedBusinessTypes()
        {
            // Arrange
            var mockBusinessTypes = new List<BusinessType>
            {
                new BusinessType { BusinessTypeId = Guid.NewGuid(), Code = "RETAIL", Name = "Retail", Status = "Active" },
                new BusinessType { BusinessTypeId = Guid.NewGuid(), Code = "FNB", Name = "F&B", Status = "Active" }
            };

            _mockUnitOfWork.Setup(x => x.BusinessTypes.GetAllAsync())
                .ReturnsAsync(mockBusinessTypes);

            // Act
            var result = await _businessTypeService.GetAllAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            Assert.Contains(result, x => x.Code == "RETAIL");
            Assert.Contains(result, x => x.Code == "FNB");
        }
    }
}
