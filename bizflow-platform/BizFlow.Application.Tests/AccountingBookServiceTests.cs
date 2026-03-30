using BizFlow.Application.Common.Exceptions;
using BizFlow.Application.DTOs.AccountingBook;
using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Application.Interfaces.Services;
using BizFlow.Application.Services;
using BizFlow.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BizFlow.Application.Tests;

public class AccountingBookServiceTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IBusinessLocationService> _locationService = new();
    private readonly Mock<IBookRenderingService> _renderingService = new();
    private readonly Mock<ILogger<AccountingBookService>> _logger = new();
    private readonly Mock<IAccountingBookRepository> _bookRepo = new();
    private readonly Mock<IAccountingTemplateRepository> _templateRepo = new();
    private readonly Mock<ITaxRulesetRepository> _rulesetRepo = new();
    private readonly Mock<IAccountingPeriodRepository> _periodRepo = new();
    private readonly Mock<IBusinessLocationRepository> _locationRepo = new();
    private readonly Mock<IBusinessTypeRepository> _businessTypeRepo = new();

    private readonly Guid _userId = Guid.NewGuid();
    private const int LocationId = 6;

    public AccountingBookServiceTests()
    {
        _uow.SetupGet(x => x.AccountingBooks).Returns(_bookRepo.Object);
        _uow.SetupGet(x => x.AccountingTemplates).Returns(_templateRepo.Object);
        _uow.SetupGet(x => x.TaxRulesets).Returns(_rulesetRepo.Object);
        _uow.SetupGet(x => x.AccountingPeriods).Returns(_periodRepo.Object);
        _uow.SetupGet(x => x.BusinessLocations).Returns(_locationRepo.Object);
        _uow.SetupGet(x => x.BusinessTypes).Returns(_businessTypeRepo.Object);
    }

    private AccountingBookService BuildSut() => new(
        _uow.Object, _locationService.Object, _renderingService.Object, _logger.Object);

    // ═══════════════════════════════════════════════════
    // CREATE BOOKS
    // ═══════════════════════════════════════════════════

    [Fact]
    public async Task CreateBooksAsync_WhenPeriodFinalized_ShouldThrowBadRequest()
    {
        // Arrange
        _periodRepo.Setup(r => r.GetByLocationAndIdAsync(LocationId, 1))
            .ReturnsAsync(new AccountingPeriod { PeriodId = 1, Status = "finalized" });

        var request = new CreateBooksRequest
        {
            PeriodId = 1,
            GroupNumber = 2,
            TaxMethod = "method_1",
            TemplateCodes = new List<string> { "S2a" }
        };

        var sut = BuildSut();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BadRequestException>(
            () => sut.CreateBooksAsync(LocationId, _userId, request));
        Assert.Equal("PERIOD_FINALIZED", ex.Message);
    }

    [Fact]
    public async Task CreateBooksAsync_WhenPeriodNotFound_ShouldThrowNotFound()
    {
        _periodRepo.Setup(r => r.GetByLocationAndIdAsync(LocationId, 999))
            .ReturnsAsync((AccountingPeriod?)null);

        var request = new CreateBooksRequest
        {
            PeriodId = 999,
            GroupNumber = 2,
            TaxMethod = "method_1",
            TemplateCodes = new List<string> { "S2a" }
        };

        var sut = BuildSut();

        await Assert.ThrowsAsync<NotFoundException>(
            () => sut.CreateBooksAsync(LocationId, _userId, request));
    }

    [Fact]
    public async Task CreateBooksAsync_WhenNoActiveRuleset_ShouldThrowBadRequest()
    {
        _periodRepo.Setup(r => r.GetByLocationAndIdAsync(LocationId, 1))
            .ReturnsAsync(new AccountingPeriod { PeriodId = 1, Status = "open" });
        _rulesetRepo.Setup(r => r.GetActiveRulesetAsync())
            .ReturnsAsync((TaxRuleset?)null);

        var request = new CreateBooksRequest
        {
            PeriodId = 1,
            GroupNumber = 2,
            TaxMethod = "method_1",
            TemplateCodes = new List<string> { "S2a" }
        };

        var sut = BuildSut();

        var ex = await Assert.ThrowsAsync<BadRequestException>(
            () => sut.CreateBooksAsync(LocationId, _userId, request));
        Assert.Equal("NO_ACTIVE_RULESET", ex.Message);
    }

    [Fact]
    public async Task CreateBooksAsync_WithInvalidTemplateCode_ShouldThrowBadRequest()
    {
        _periodRepo.Setup(r => r.GetByLocationAndIdAsync(LocationId, 1))
            .ReturnsAsync(new AccountingPeriod { PeriodId = 1, Status = "open" });
        _rulesetRepo.Setup(r => r.GetActiveRulesetAsync())
            .ReturnsAsync(new TaxRuleset { RulesetId = 1 });
        _templateRepo.Setup(r => r.GetByCodeAsync("INVALID"))
            .ReturnsAsync((AccountingTemplate?)null);

        var request = new CreateBooksRequest
        {
            PeriodId = 1,
            GroupNumber = 2,
            TaxMethod = "method_1",
            TemplateCodes = new List<string> { "INVALID" }
        };

        var sut = BuildSut();

        var ex = await Assert.ThrowsAsync<BadRequestException>(
            () => sut.CreateBooksAsync(LocationId, _userId, request));
        Assert.Contains("TEMPLATE_NOT_FOUND", ex.Message);
    }

    [Fact]
    public async Task CreateBooksAsync_WithWrongGroup_ShouldThrowBadRequest()
    {
        _periodRepo.Setup(r => r.GetByLocationAndIdAsync(LocationId, 1))
            .ReturnsAsync(new AccountingPeriod { PeriodId = 1, Status = "open" });
        _rulesetRepo.Setup(r => r.GetActiveRulesetAsync())
            .ReturnsAsync(new TaxRuleset { RulesetId = 1 });
        _templateRepo.Setup(r => r.GetByCodeAsync("S2a"))
            .ReturnsAsync(new AccountingTemplate
            {
                TemplateCode = "S2a",
                Name = "Test",
                ApplicableGroups = "[2,3,4]",        // Group 1 NOT applicable
                ApplicableMethods = "[\"method_1\"]",
                Versions = new List<AccountingTemplateVersion>
                {
                    new() { TemplateVersionId = 1, IsActive = true }
                }
            });

        var request = new CreateBooksRequest
        {
            PeriodId = 1,
            GroupNumber = 1, // Wrong group!
            TaxMethod = "method_1",
            TemplateCodes = new List<string> { "S2a" }
        };

        var sut = BuildSut();

        var ex = await Assert.ThrowsAsync<BadRequestException>(
            () => sut.CreateBooksAsync(LocationId, _userId, request));
        Assert.Contains("TEMPLATE_NOT_APPLICABLE_GROUP", ex.Message);
    }

    // ═══════════════════════════════════════════════════
    // LIST BOOKS
    // ═══════════════════════════════════════════════════

    [Fact]
    public async Task ListBooksAsync_ShouldReturnMappedDtos()
    {
        var books = new List<AccountingBook>
        {
            new()
            {
                BookId = 1,
                BusinessLocationId = LocationId,
                GroupNumber = 2,
                TaxMethod = "method_1",
                Status = "active",
                CreatedAt = DateTime.UtcNow,
                TemplateVersion = new AccountingTemplateVersion
                {
                    Template = new AccountingTemplate
                    {
                        TemplateCode = "S2a",
                        Name = "Sổ doanh thu"
                    }
                },
                BookBusinessTypes = new List<AccountingBookBusinessType>
                {
                    new() { BusinessTypeId = Guid.NewGuid(), TaxProfileKey = "VAT_1.00|PIT_0.50|METHOD_method_1" }
                }
            }
        };

        _bookRepo.Setup(r => r.GetByLocationAndPeriodAsync(LocationId, 1))
            .ReturnsAsync(books);

        var sut = BuildSut();
        var result = await sut.ListBooksAsync(LocationId, _userId, 1);

        Assert.Single(result);
        Assert.Equal("S2a", result[0].TemplateCode);
        Assert.Equal("Sổ doanh thu", result[0].TemplateName);
        Assert.Equal(2, result[0].GroupNumber);
    }

    // ═══════════════════════════════════════════════════
    // SUMMARY — delegates to render engine
    // ═══════════════════════════════════════════════════

    [Fact]
    public async Task GetBookSummaryAsync_WhenBookNotFound_ShouldThrowNotFound()
    {
        _bookRepo.Setup(r => r.GetByIdWithBusinessTypesAsync(999))
            .ReturnsAsync((AccountingBook?)null);

        var sut = BuildSut();

        await Assert.ThrowsAsync<NotFoundException>(
            () => sut.GetBookSummaryAsync(LocationId, _userId, 999));
    }

    [Fact]
    public async Task GetBookSummaryAsync_WhenWrongLocation_ShouldThrowForbidden()
    {
        _bookRepo.Setup(r => r.GetByIdWithBusinessTypesAsync(1))
            .ReturnsAsync(new AccountingBook
            {
                BookId = 1,
                BusinessLocationId = 999, // Different location!
                PeriodId = 1,
                TemplateVersionId = 1,
                TemplateVersion = new AccountingTemplateVersion
                {
                    Template = new AccountingTemplate { TemplateCode = "S2a", Name = "test" },
                    FieldMappings = new List<TemplateFieldMapping>()
                },
                BookBusinessTypes = new List<AccountingBookBusinessType>()
            });

        var sut = BuildSut();

        await Assert.ThrowsAsync<ForbiddenException>(
            () => sut.GetBookSummaryAsync(LocationId, _userId, 1));
    }
}
