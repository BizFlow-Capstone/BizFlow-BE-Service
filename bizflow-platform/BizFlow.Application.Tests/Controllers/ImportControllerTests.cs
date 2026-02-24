using BizFlow.Api.Controllers.Import;
using BizFlow.Application.Common.Exceptions;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.Common.Models;
using BizFlow.Application.DTOs.Import;
using BizFlow.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace BizFlow.Application.Tests.Controllers
{
    public class ImportControllerTests
    {
        // ─── Mock dependencies ──────────────────────────────────────────
        private readonly Mock<IImportService> _mockImportService;
        private readonly Mock<IMessageService> _mockMessageService;
        private readonly Mock<ILogger<ImportController>> _mockLogger;
        private readonly IOptions<PaginationSettings> _paginationOptions;
        private readonly ImportController _controller;

        // Mock user ID that matches the controller's static field
        private static readonly Guid _mockUserId = Guid.Parse("550e8400-e29b-41d4-a716-446655440001");

        public ImportControllerTests()
        {
            _mockImportService  = new Mock<IImportService>();
            _mockMessageService = new Mock<IMessageService>();
            _mockLogger         = new Mock<ILogger<ImportController>>();

            _mockMessageService.Setup(m => m.GetMessage(It.IsAny<string>())).Returns("ok");
            _mockMessageService.Setup(m => m.GetMessage(It.IsAny<string>(), It.IsAny<object[]>())).Returns("ok");

            _paginationOptions = Options.Create(new PaginationSettings
            {
                DefaultPageNumber = 1,
                DefaultPageSize   = 10,
                MaxPageSize       = 100
            });

            _controller = new ImportController(
                _mockImportService.Object,
                _mockMessageService.Object,
                _paginationOptions,
                _mockLogger.Object);
        }

        // ================================================================
        // GET /api/my-business/accounting/import-template
        // ================================================================

        [Fact]
        public async Task GetTemplate_ReturnsOk_WhenTemplateExists()
        {
            // Arrange
            var schema = new ImportSchemaDto { SchemaJson = "{}" };
            _mockImportService.Setup(s => s.GetTemplateAsync()).ReturnsAsync(schema);

            // Act
            var result = await _controller.GetTemplate();

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, ok.StatusCode);
            _mockImportService.Verify(s => s.GetTemplateAsync(), Times.Once);
        }

        [Fact]
        public async Task GetTemplate_Returns404_WhenTemplateNotFound()
        {
            // Arrange
            _mockImportService.Setup(s => s.GetTemplateAsync())
                .ThrowsAsync(new NotFoundException("ImportTemplateNotFound"));

            // Act
            var result = await _controller.GetTemplate();

            // Assert
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(404, notFound.StatusCode);
        }

        // ================================================================
        // POST /api/my-business/accounting/import
        // ================================================================

        [Fact]
        public async Task CreateImport_Returns201_WhenCreatedSuccessfully()
        {
            // Arrange
            var request = new CreateImportRequest
            {
                ImportType         = "INVOICE",
                BusinessLocationId = 1,
                Items              = new List<ImportItemRequest>
                {
                    new() { ProductId = 1, Quantity = 10, CostPrice = 50m }
                }
            };
            var summary = new ImportSummaryDto { ImportId = 10, ImportType = "INVOICE", Status = "DRAFT" };
            _mockImportService.Setup(s => s.CreateImportAsync(_mockUserId, request)).ReturnsAsync(summary);

            // Act
            var result = await _controller.CreateImport(request);

            // Assert
            var created = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(201, created.StatusCode);
            _mockImportService.Verify(s => s.CreateImportAsync(_mockUserId, request), Times.Once);
        }

        [Fact]
        public async Task CreateImport_Returns404_WhenProductNotFound()
        {
            // Arrange
            var request = new CreateImportRequest { ImportType = "INVOICE", BusinessLocationId = 1 };
            _mockImportService.Setup(s => s.CreateImportAsync(_mockUserId, request))
                .ThrowsAsync(new NotFoundException("ProductNotFound"));

            // Act
            var result = await _controller.CreateImport(request);

            // Assert
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(404, notFound.StatusCode);
        }

        [Fact]
        public async Task CreateImport_Returns400_WhenBadRequest()
        {
            // Arrange
            var request = new CreateImportRequest { ImportType = "INVOICE", BusinessLocationId = 1 };
            _mockImportService.Setup(s => s.CreateImportAsync(_mockUserId, request))
                .ThrowsAsync(new BadRequestException("InvalidImportData"));

            // Act
            var result = await _controller.CreateImport(request);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequest.StatusCode);
        }

        // ================================================================
        // PUT /api/my-business/accounting/import/{importId}
        // ================================================================

        [Fact]
        public async Task UpdateImport_ReturnsOk_WhenUpdatedSuccessfully()
        {
            // Arrange
            var importId = 10L;
            var request  = new UpdateImportRequest { Supplier = "New Supplier" };
            var summary  = new ImportSummaryDto { ImportId = importId, Status = "DRAFT" };
            _mockImportService.Setup(s => s.UpdateImportAsync(_mockUserId, importId, request))
                .ReturnsAsync(summary);

            // Act
            var result = await _controller.UpdateImport(importId, request);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, ok.StatusCode);
        }

        [Fact]
        public async Task UpdateImport_Returns404_WhenImportNotFound()
        {
            // Arrange
            var importId = 999L;
            var request  = new UpdateImportRequest();
            _mockImportService.Setup(s => s.UpdateImportAsync(_mockUserId, importId, request))
                .ThrowsAsync(new NotFoundException("ImportNotFound"));

            // Act
            var result = await _controller.UpdateImport(importId, request);

            // Assert
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(404, notFound.StatusCode);
        }

        [Fact]
        public async Task UpdateImport_Returns400_WhenBadRequest()
        {
            // Arrange
            var importId = 10L;
            var request  = new UpdateImportRequest();
            _mockImportService.Setup(s => s.UpdateImportAsync(_mockUserId, importId, request))
                .ThrowsAsync(new BadRequestException("CannotUpdateConfirmedImport"));

            // Act
            var result = await _controller.UpdateImport(importId, request);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequest.StatusCode);
        }

        // ================================================================
        // PATCH /api/my-business/accounting/import/{importId}
        // ================================================================

        [Fact]
        public async Task PatchImport_ReturnsOk_WhenPatchedSuccessfully()
        {
            // Arrange
            var importId = 10L;
            var request  = new PatchImportRequest { ReceivedAt = DateTime.UtcNow };
            var result_  = new ImportPatchResultDto { ImportId = importId, Status = "CONFIRMED" };
            _mockImportService.Setup(s => s.PatchImportAsync(_mockUserId, importId, request))
                .ReturnsAsync(result_);

            // Act
            var result = await _controller.PatchImport(importId, request);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, ok.StatusCode);
        }

        [Fact]
        public async Task PatchImport_Returns404_WhenImportNotFound()
        {
            // Arrange
            var importId = 999L;
            var request  = new PatchImportRequest();
            _mockImportService.Setup(s => s.PatchImportAsync(_mockUserId, importId, request))
                .ThrowsAsync(new NotFoundException("ImportNotFound"));

            // Act
            var result = await _controller.PatchImport(importId, request);

            // Assert
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(404, notFound.StatusCode);
        }

        [Fact]
        public async Task PatchImport_Returns400_WhenBadRequest()
        {
            // Arrange
            var importId = 10L;
            var request  = new PatchImportRequest();
            _mockImportService.Setup(s => s.PatchImportAsync(_mockUserId, importId, request))
                .ThrowsAsync(new BadRequestException("ReceivedAtIsRequired"));

            // Act
            var result = await _controller.PatchImport(importId, request);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequest.StatusCode);
        }

        // ================================================================
        // GET /api/my-business/accounting/imports
        // ================================================================

        [Fact]
        public async Task ListImports_ReturnsOk_WithPaginatedResult()
        {
            // Arrange
            var query = new ImportQueryParams { PageNumber = 1, PageSize = 10 };
            var paged = new PaginatedResponse<ImportSummaryDto>(
                items: new List<ImportSummaryDto> { new() { ImportId = 1 } },
                count: 1,
                pageNumber: 1,
                pageSize: 10);
            _mockImportService.Setup(s => s.ListImportsAsync(_mockUserId, query)).ReturnsAsync(paged);

            // Act
            var result = await _controller.ListImports(query);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, ok.StatusCode);
        }

        // ================================================================
        // GET /api/my-business/accounting/import/{importId}
        // ================================================================

        [Fact]
        public async Task GetImportDetail_ReturnsOk_WhenImportExists()
        {
            // Arrange
            var importId = 10L;
            var detail   = new ImportDetailDto { ImportId = importId, Status = "DRAFT" };
            _mockImportService.Setup(s => s.GetImportDetailAsync(_mockUserId, importId))
                .ReturnsAsync(detail);

            // Act
            var result = await _controller.GetImportDetail(importId);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, ok.StatusCode);
        }

        [Fact]
        public async Task GetImportDetail_Returns404_WhenNotFound()
        {
            // Arrange
            var importId = 999L;
            _mockImportService.Setup(s => s.GetImportDetailAsync(_mockUserId, importId))
                .ThrowsAsync(new NotFoundException("ImportNotFound"));

            // Act
            var result = await _controller.GetImportDetail(importId);

            // Assert
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(404, notFound.StatusCode);
        }

        // ================================================================
        // DELETE /api/my-business/accounting/import/{importId}
        // ================================================================

        [Fact]
        public async Task DeleteImport_ReturnsOk_WhenDeleted()
        {
            // Arrange
            var importId = 10L;
            _mockImportService.Setup(s => s.DeleteImportAsync(_mockUserId, importId))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.DeleteImport(importId);

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, ok.StatusCode);
            _mockImportService.Verify(s => s.DeleteImportAsync(_mockUserId, importId), Times.Once);
        }

        [Fact]
        public async Task DeleteImport_Returns404_WhenImportNotFound()
        {
            // Arrange
            var importId = 999L;
            _mockImportService.Setup(s => s.DeleteImportAsync(_mockUserId, importId))
                .ThrowsAsync(new NotFoundException("ImportNotFound"));

            // Act
            var result = await _controller.DeleteImport(importId);

            // Assert
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(404, notFound.StatusCode);
        }
    }
}
