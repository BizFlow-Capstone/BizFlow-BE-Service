using System.Text.Json;
using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Exceptions;
using BizFlow.Application.DTOs.AccountingBook;
using BizFlow.Application.DTOs.Revenue;
using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Application.Interfaces.Services;
using BizFlow.Domain.Entities;
using BizFlow.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace BizFlow.Application.Services;

public class AccountingBookService : IAccountingBookService
{
    private readonly IUnitOfWork _uow;
    private readonly IBusinessLocationService _locationService;
    private readonly IBookRenderingService _renderingService;
    private readonly ILogger<AccountingBookService> _logger;

    public AccountingBookService(
        IUnitOfWork uow,
        IBusinessLocationService locationService,
        IBookRenderingService renderingService,
        ILogger<AccountingBookService> logger)
    {
        _uow = uow;
        _locationService = locationService;
        _renderingService = renderingService;
        _logger = logger;
    }

    // ────────────────────────────────────────────────────────
    // CREATE BOOKS
    // ────────────────────────────────────────────────────────
    public async Task<CreateBooksResponse> CreateBooksAsync(int locationId, Guid userId, CreateBooksRequest request)
    {
        await _locationService.ValidateOwnerAsync(userId, locationId);

        // 1. Validate period exists and is NOT finalized
        var period = await _uow.AccountingPeriods.GetByLocationAndIdAsync(locationId, request.PeriodId)
            ?? throw new NotFoundException("PERIOD_NOT_FOUND");
        if (period.Status == AccountingPeriodConstants.PeriodStatuses.Finalized)
            throw new BadRequestException("PERIOD_FINALIZED");

        // 2. Validate active ruleset
        var ruleset = await _uow.TaxRulesets.GetActiveRulesetAsync()
            ?? throw new BadRequestException("NO_ACTIVE_RULESET");

        // 3. Validate & get template versions
        var templateVersions = new List<(AccountingTemplate Template, AccountingTemplateVersion Version)>();
        foreach (var code in request.TemplateCodes.Distinct())
        {
            var template = await _uow.AccountingTemplates.GetByCodeAsync(code)
                ?? throw new BadRequestException($"TEMPLATE_NOT_FOUND:{code}");

            // Validate applicable group
            var groups = JsonSerializer.Deserialize<List<int>>(template.ApplicableGroups) ?? new();
            if (!groups.Contains(request.GroupNumber))
                throw new BadRequestException($"TEMPLATE_NOT_APPLICABLE_GROUP:{code}");

            // Validate applicable methods
            if (template.ApplicableMethods != null)
            {
                var methods = JsonSerializer.Deserialize<List<string>>(template.ApplicableMethods) ?? new();
                if (methods.Count > 0 && !methods.Contains(request.TaxMethod))
                    throw new BadRequestException($"TEMPLATE_NOT_APPLICABLE_METHOD:{code}");
            }

            var version = template.Versions.FirstOrDefault(v => v.IsActive)
                ?? throw new BadRequestException($"TEMPLATE_NO_ACTIVE_VERSION:{code}");

            templateVersions.Add((template, version));
        }

        // 4. Get business types linked to this location (with fallback)
        var businessTypeIds = await GetBusinessTypeIdsForLocation(locationId);
        if (!businessTypeIds.Any())
            throw new BadRequestException("NO_BUSINESS_TYPES_FOR_LOCATION");

        var allBusinessTypes = (await _uow.BusinessTypes.GetAllAsync())
            .ToDictionary(bt => bt.BusinessTypeId, bt => bt.Name);
        var locationBusinessTypes = businessTypeIds
            .Select(id => new BusinessTypeInBookDto
            {
                BusinessTypeId = id,
                Name = allBusinessTypes.GetValueOrDefault(id, string.Empty)
            })
            .ToList();

        // 5. Create books per template
        var createdBooks = new List<BookListItemDto>();

        await _uow.ExecuteResilientAsync(async ct =>
        {
            foreach (var (template, version) in templateVersions)
            {
                // Check if book already exists for this location + period + template.
                var exists = await _uow.AccountingBooks.ExistsForPeriodAsync(
                    locationId, request.PeriodId, version.TemplateVersionId);
                if (exists)
                {
                    _logger.LogWarning(
                        "Book already exists for location {LocationId}, period {PeriodId}, template {Code}",
                        locationId, request.PeriodId, template.TemplateCode);
                    continue;
                }

                var book = new AccountingBook
                {
                    BusinessLocationId = locationId,
                    PeriodId = request.PeriodId,
                    TemplateVersionId = version.TemplateVersionId,
                    GroupNumber = request.GroupNumber,
                    TaxMethod = request.TaxMethod,
                    RulesetId = ruleset.RulesetId,
                    Status = AccountingBookConstants.BookStatuses.Active,
                    CreatedByUserId = userId,
                    CreatedAt = DateTime.UtcNow
                };

                await _uow.AccountingBooks.AddAsync(book);
                await _uow.SaveChangesAsync(ct);

                createdBooks.Add(new BookListItemDto
                {
                    BookId = book.BookId,
                    TemplateCode = template.TemplateCode,
                    TemplateName = template.Name,
                    GroupNumber = book.GroupNumber,
                    TaxMethod = book.TaxMethod,
                    Status = book.Status,
                    TaxProfileKey = string.Empty,
                    BusinessTypes = locationBusinessTypes,
                    CreatedAt = book.CreatedAt
                });
            }
        });

        _logger.LogInformation(
            "Created {Count} accounting books for location {Location}, period {Period}, templates [{Templates}]",
            createdBooks.Count, locationId, request.PeriodId,
            string.Join(",", request.TemplateCodes));

        return new CreateBooksResponse { CreatedBooks = createdBooks };
    }

    // ────────────────────────────────────────────────────────
    // LIST BOOKS
    // ────────────────────────────────────────────────────────
    public async Task<List<BookListItemDto>> ListBooksAsync(int locationId, Guid userId, long periodId)
    {
        await _locationService.ValidateOwnerAsync(userId, locationId);

        var books = await _uow.AccountingBooks.GetByLocationAndPeriodAsync(locationId, periodId);
        var locationBusinessTypes = await GetBusinessTypesForLocation(locationId);

        return books.Select(b => new BookListItemDto
        {
            BookId = b.BookId,
            TemplateCode = b.TemplateVersion?.Template?.TemplateCode ?? "",
            TemplateName = b.TemplateVersion?.Template?.Name ?? "",
            GroupNumber = b.GroupNumber,
            TaxMethod = b.TaxMethod,
            Status = b.Status,
            TaxProfileKey = string.Empty,
            BusinessTypes = locationBusinessTypes,
            CreatedAt = b.CreatedAt
        }).ToList();
    }

    // ────────────────────────────────────────────────────────
    // GET BOOK SUMMARY (fast — KPIs only)
    // ────────────────────────────────────────────────────────
    public async Task<BookSummaryResponse> GetBookSummaryAsync(int locationId, Guid userId, long bookId)
    {
        await _locationService.ValidateOwnerAsync(userId, locationId);

        var book = await _uow.AccountingBooks.GetByIdWithBusinessTypesAsync(bookId)
            ?? throw new NotFoundException("BOOK_NOT_FOUND");
        if (book.BusinessLocationId != locationId)
            throw new ForbiddenException("COMMON_FORBIDDEN");

        var template = book.TemplateVersion?.Template;
        var mappings = book.TemplateVersion?.FieldMappings?.OrderBy(m => m.SortOrder).ToList() ?? new();
        var businessTypeIds = await GetBusinessTypeIdsForLocation(locationId);

        // Build render context
        var period = await _uow.AccountingPeriods.GetByLocationAndIdAsync(locationId, book.PeriodId);
        var renderCtx = BuildRenderContext(book, period, businessTypeIds);

        // Compute formula summary (KPIs)
        var formulaSummary = await _renderingService.ComputeSummaryAsync(renderCtx);

        var rulesetRates = await _uow.TaxRulesets.GetTaxRatesByBusinessTypeIdsAsync(book.RulesetId, businessTypeIds);
        var allBusinessTypes = (await _uow.BusinessTypes.GetAllAsync())
            .ToDictionary(bt => bt.BusinessTypeId, bt => bt);

        var allFormulas = await _uow.FormulaDefinitions.GetActiveAsync();
        var mappedFormulaIds = mappings
            .Where(m => m.FormulaId != null)
            .Select(m => m.FormulaId!.Value)
            .ToHashSet();
        var templatePrefix = $"{(template?.TemplateCode ?? string.Empty).ToUpperInvariant()}_";
        var templateFormulas = allFormulas
            .Where(f => mappedFormulaIds.Contains(f.FormulaId)
                        || (!string.IsNullOrWhiteSpace(templatePrefix)
                            && f.Code.StartsWith(templatePrefix, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(f => f.FormulaId)
            .ToList();

        var formulaCodeById = templateFormulas.ToDictionary(f => f.FormulaId, f => f.Code);

        return new BookSummaryResponse
        {
            BookId = book.BookId,
            TemplateCode = template?.TemplateCode ?? "",
            TemplateName = template?.Name ?? "",
            TotalRows = formulaSummary.TotalRows,
            TotalRevenue = formulaSummary.TotalRevenue,
            TotalCost = formulaSummary.TotalCost,
            TotalTax = formulaSummary.TotalTax,
            FormulaValues = formulaSummary.FormulaValues,
            Notes = new List<string>
            {
                $"Ky tinh: {renderCtx.PeriodStart:yyyy-MM-dd} -> {renderCtx.PeriodEnd:yyyy-MM-dd}",
                $"Tax method: {book.TaxMethod}",
                $"Ruleset: {book.RulesetId}",
                $"So nganh trong so: {businessTypeIds.Count}"
            },
            BusinessTypeTaxes = businessTypeIds.Select(btId =>
            {
                allBusinessTypes.TryGetValue(btId, out var btInfo);
                return new BookBusinessTypeTaxDto
                {
                    BusinessTypeId = btId,
                    Code = btInfo?.Code ?? string.Empty,
                    Name = btInfo?.Name ?? string.Empty,
                    TaxRates = rulesetRates
                        .Where(r => r.BusinessTypeId == btId)
                        .OrderBy(r => r.TaxType)
                        .Select(r => new BookTaxRateItemDto
                        {
                            TaxType = r.TaxType,
                            TaxRate = r.TaxRate,
                            Description = r.Description
                        }).ToList()
                };
            }).ToList(),
            FormulaDetails = templateFormulas.Select(f => new BookFormulaExplainDto
            {
                FormulaId = f.FormulaId,
                Code = f.Code,
                Name = f.Name,
                Description = f.Description,
                ExpressionJson = f.ExpressionJson,
                Value = formulaSummary.FormulaValues?.GetValueOrDefault(f.Code)
            }).ToList(),
            FormulaBindings = mappings
                .Where(m => m.SourceType == "formula")
                .Select(m =>
                {
                    string? formulaCode = null;
                    decimal? value = null;

                    if (m.FormulaId.HasValue && formulaCodeById.TryGetValue(m.FormulaId.Value, out var resolvedCode))
                    {
                        formulaCode = resolvedCode;
                        value = formulaSummary.FormulaValues?.GetValueOrDefault(resolvedCode);
                    }
                    else if (formulaSummary.FormulaValues?.TryGetValue(m.FieldCode, out var fieldCodeValue) == true)
                    {
                        formulaCode = m.FieldCode;
                        value = fieldCodeValue;
                    }

                    return new BookFieldFormulaBindingDto
                    {
                        FieldCode = m.FieldCode,
                        FieldLabel = m.FieldLabel,
                        FormulaId = m.FormulaId,
                        FormulaCode = formulaCode,
                        FormulaExpression = m.FormulaExpression,
                        Value = value
                    };
                }).ToList(),
            Columns = mappings.Select(m => new BookColumnDto
            {
                FieldCode = m.FieldCode,
                Label = m.FieldLabel,
                FieldType = m.FieldType,
                ExportColumn = m.ExportColumn
            }).ToList()
        };
    }

    // ────────────────────────────────────────────────────────
    // GET BOOK ROWS (cursor-based, batch 200)
    // ────────────────────────────────────────────────────────
    public async Task<BookRowsResponse> GetBookRowsAsync(
        int locationId, Guid userId, long bookId, string? cursor, int batchSize = 200)
    {
        await _locationService.ValidateOwnerAsync(userId, locationId);

        var book = await _uow.AccountingBooks.GetByIdWithBusinessTypesAsync(bookId)
            ?? throw new NotFoundException("BOOK_NOT_FOUND");
        if (book.BusinessLocationId != locationId)
            throw new ForbiddenException("COMMON_FORBIDDEN");

        // Cap batch size
        batchSize = Math.Min(batchSize, 200);

        // Build render context
        var period = await _uow.AccountingPeriods.GetByLocationAndIdAsync(locationId, book.PeriodId);
        var businessTypeIds = await GetBusinessTypeIdsForLocation(locationId);
        var renderCtx = BuildRenderContext(book, period, businessTypeIds);

        // Render live rows via engine
        var renderResult = await _renderingService.RenderRowsAsync(renderCtx, cursor, batchSize);

        return new BookRowsResponse
        {
            Rows = renderResult.Rows,
            HasMore = renderResult.HasMore,
            NextCursor = renderResult.NextCursor,
            LoadedCount = renderResult.LoadedCount,
            TotalEstimated = renderResult.TotalEstimated
        };
    }

    // ────────────────────────────────────────────────────────
    // GET BOOK SECTIONS (structure + formula values)
    // ────────────────────────────────────────────────────────
    public async Task<BookSectionsResponse> GetBookSectionsAsync(int locationId, Guid userId, long bookId)
    {
        await _locationService.ValidateOwnerAsync(userId, locationId);

        var book = await _uow.AccountingBooks.GetByIdWithBusinessTypesAsync(bookId)
            ?? throw new NotFoundException("BOOK_NOT_FOUND");
        if (book.BusinessLocationId != locationId)
            throw new ForbiddenException("COMMON_FORBIDDEN");

        var period = await _uow.AccountingPeriods.GetByLocationAndIdAsync(locationId, book.PeriodId);
        var businessTypeIds = await GetBusinessTypeIdsForLocation(locationId);
        var renderCtx = BuildRenderContext(book, period, businessTypeIds);

        var renderResult = await _renderingService.RenderSectionsAsync(renderCtx);
        var template = book.TemplateVersion?.Template;

        return new BookSectionsResponse
        {
            BookId = book.BookId,
            TemplateCode = template?.TemplateCode ?? "",
            TemplateName = template?.Name ?? "",
            LastCalculatedAt = DateTime.UtcNow,
            Columns = renderResult.Columns,
            Sections = renderResult.Sections.Select(s => new BookSectionResponseDto
            {
                SectionType = s.SectionType,
                BusinessTypeId = s.GroupKey,
                BusinessTypeName = s.GroupName,
                GroupIndex = s.GroupIndex,
                Rows = s.Rows.Select(r => MapToSectionRow(r)).ToList()
            }).ToList(),
            FooterRows = renderResult.FooterRows.Select(r => MapToSectionRow(r)).ToList()
        };
    }

    private static SectionRowDto MapToSectionRow(Dictionary<string, object?> raw)
    {
        var lineType = raw.GetValueOrDefault("lineType")?.ToString() ?? "unknown";
        var values = new Dictionary<string, object?>(raw);
        values.Remove("lineType");
        values.Remove("dataFilter");
        values.Remove("taxMetadata");

        DataFilterDto? dataFilter = null;
        if (raw.GetValueOrDefault("dataFilter") is Dictionary<string, object?> df)
        {
            dataFilter = new DataFilterDto
            {
                BusinessTypeId = df.GetValueOrDefault("businessTypeId")?.ToString(),
                Section = df.GetValueOrDefault("section")?.ToString()
            };
        }

        TaxMetadataDto? taxMeta = null;
        if (raw.GetValueOrDefault("taxMetadata") is Dictionary<string, object?> tm)
        {
            taxMeta = new TaxMetadataDto
            {
                TaxType = tm.GetValueOrDefault("taxType")?.ToString() ?? "",
                Rate = tm.GetValueOrDefault("rate") is decimal r ? r : 0m,
                Source = tm.GetValueOrDefault("source")?.ToString() ?? "DEFAULT"
            };
        }

        return new SectionRowDto
        {
            LineType = lineType,
            Values = values,
            DataFilter = dataFilter,
            TaxMetadata = taxMeta
        };
    }

    // ────────────────────────────────────────────────────────
    // PRIVATE HELPERS
    // ────────────────────────────────────────────────────────

    private BookRenderContext BuildRenderContext(AccountingBook book, AccountingPeriod? period, List<Guid> businessTypeIds)
    {
        return new BookRenderContext
        {
            BookId = book.BookId,
            BusinessLocationId = book.BusinessLocationId,
            PeriodId = book.PeriodId,
            PeriodStart = period?.StartDate ?? DateOnly.MinValue,
            PeriodEnd = period?.EndDate ?? DateOnly.MaxValue,
            TemplateVersionId = book.TemplateVersionId,
            TemplateCode = book.TemplateVersion?.Template?.TemplateCode ?? "",
            DataSourceType = book.TemplateVersion?.Template?.DataSourceType ?? "revenues",
            GroupNumber = book.GroupNumber,
            TaxMethod = book.TaxMethod,
            RulesetId = book.RulesetId,
            BusinessTypeIds = businessTypeIds
        };
    }

    private async Task<List<BusinessTypeInBookDto>> GetBusinessTypesForLocation(int locationId)
    {
        var ids = await GetBusinessTypeIdsForLocation(locationId);
        if (!ids.Any())
            return new List<BusinessTypeInBookDto>();

        var map = (await _uow.BusinessTypes.GetAllAsync())
            .ToDictionary(bt => bt.BusinessTypeId, bt => bt.Name);

        return ids
            .Select(id => new BusinessTypeInBookDto
            {
                BusinessTypeId = id,
                Name = map.GetValueOrDefault(id, string.Empty)
            })
            .ToList();
    }

    private async Task<List<Guid>> GetBusinessTypeIdsForLocation(int locationId)
    {
        var products = await _uow.Products.QuickSearchByLocationAsync(locationId, null);

        // Primary source: product business types in this location (ignore status to support legacy/inactive products).
        var ids = products
            .Where(p => p.DeletedAt == null)
            .Select(p => p.BusinessTypeId)
            .Distinct()
            .ToList();

        if (ids.Any())
            return ids;

        // Fallback 1: derive from revenue classifications if location has no products.
        var revenueQuery = new RevenueQueryParams
        {
            BusinessLocationId = locationId,
            PageNumber = 1,
            PageSize = int.MaxValue
        };
        var (revenues, _) = await _uow.Revenues.SearchAsync(revenueQuery);
        ids = revenues
            .Where(r => r.DeletedAt == null && r.BusinessTypeId.HasValue)
            .Select(r => r.BusinessTypeId!.Value)
            .Distinct()
            .ToList();

        if (ids.Any())
        {
            _logger.LogWarning(
                "No product-linked business type for location {LocationId}, fallback to revenue-linked business types.",
                locationId);
            return ids;
        }

        // Fallback 2: avoid hard-failing new/empty locations by using active business types.
        var businessTypes = await _uow.BusinessTypes.GetAllAsync();
        ids = businessTypes
            .Where(bt => string.Equals(bt.Status, ProductStatus.Active, StringComparison.OrdinalIgnoreCase))
            .Select(bt => bt.BusinessTypeId)
            .Distinct()
            .ToList();

        if (ids.Any())
        {
            _logger.LogWarning(
                "No product-linked business type for location {LocationId}, fallback to active business types.",
                locationId);
        }

        return ids;
    }

}
