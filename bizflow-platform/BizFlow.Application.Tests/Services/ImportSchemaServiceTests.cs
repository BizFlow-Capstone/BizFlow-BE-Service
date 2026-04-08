using AutoMapper;
using BizFlow.Application.Common.Exceptions;
using BizFlow.Application.DTOs.ImportSchema;
using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Application.Services;
using BizFlow.Domain.Entities;
using Moq;
using Xunit;

namespace BizFlow.Application.Tests.Services
{
    public class ImportSchemaServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IMapper> _mockMapper;
        private readonly ImportSchemaService _service;

        public ImportSchemaServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockMapper = new Mock<IMapper>();

            _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            _service = new ImportSchemaService(_mockUnitOfWork.Object, _mockMapper.Object);
        }

        [Fact]
        public async Task CreateAsync_WithExistingTemplateCode_ThrowsBadRequestException()
        {
            // Arrange
            var request = new CreateImportSchemaRequest { TemplateCode = "EXISTING_CODE", Name = "Test", SchemaJson = "{}" };
            _mockUnitOfWork.Setup(x => x.ImportSchemas.ExistsWithTemplateCodeAsync(request.TemplateCode, It.IsAny<int?>()))
                .ReturnsAsync(true);

            // Act & Assert
            await Assert.ThrowsAsync<BadRequestException>(() => _service.CreateAsync(request));
        }

        [Fact]
        public async Task DeleteAsync_WhenSchemaIsActive_ThrowsBadRequestException()
        {
            // Arrange
            var schemaId = 1;
            var schema = new ImportSchema { ImportSchemaId = schemaId, IsActive = true };
            
            _mockUnitOfWork.Setup(x => x.ImportSchemas.GetByIdAsync(schemaId))
                .ReturnsAsync(schema);

            // Act & Assert
            await Assert.ThrowsAsync<BadRequestException>(() => _service.DeleteAsync(schemaId));
        }

        [Fact]
        public async Task UpdateAsync_WithNewJsonBody_CreatesNewVersion()
        {
            // Arrange
            var schemaId = 1;
            var schema = new ImportSchema 
            { 
                ImportSchemaId = schemaId,
                ImportSchemaVersions = new List<ImportSchemaVersion>
                {
                    new ImportSchemaVersion { SchemaJson = "{\"old\":true}", IsActive = true }
                }
            };
            
            var request = new UpdateImportSchemaRequest { SchemaJson = "{\"new\":true}" };

            _mockUnitOfWork.Setup(x => x.ImportSchemas.GetByIdWithVersionsAsync(schemaId))
                .ReturnsAsync(schema);
            _mockMapper.Setup(x => x.Map<ImportSchemaResponse>(It.IsAny<ImportSchema>()))
                .Returns(new ImportSchemaResponse());

            // Act
            await _service.UpdateAsync(schemaId, request);

            // Assert
            Assert.Equal(2, schema.ImportSchemaVersions.Count);
            Assert.False(schema.ImportSchemaVersions.First().IsActive);
            Assert.True(schema.ImportSchemaVersions.Last().IsActive);
            Assert.Equal("{\"new\":true}", schema.ImportSchemaVersions.Last().SchemaJson);
        }
    }
}
