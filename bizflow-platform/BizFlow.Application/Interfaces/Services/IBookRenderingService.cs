namespace BizFlow.Application.Interfaces.Services;

using BizFlow.Application.DTOs.AccountingBook;

/// <summary>
/// Render engine: query live data + evaluate formulas → produce rows for an accounting book.
/// </summary>
public interface IBookRenderingService
{
    /// <summary>
    /// Render data rows for a book, applying cursor-based pagination.
    /// </summary>
    Task<BookRenderResult> RenderRowsAsync(BookRenderContext context, string? cursor, int batchSize);

    /// <summary>
    /// Compute formula summary values for a book (KPIs).
    /// </summary>
    Task<BookFormulaSummary> ComputeSummaryAsync(BookRenderContext context);

    /// <summary>
    /// Render book sections structure with formula values.
    /// </summary>
    Task<BookSectionsRenderResult> RenderSectionsAsync(BookRenderContext context);
}

/// <summary>
/// Context required to render a book — loaded from DB once.
/// </summary>
public class BookRenderContext
{
    public long BookId { get; set; }
    public int BusinessLocationId { get; set; }
    public long PeriodId { get; set; }
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public int TemplateVersionId { get; set; }
    public string TemplateCode { get; set; } = null!;
    public string DataSourceType { get; set; } = "revenues";
    public int GroupNumber { get; set; }
    public string? TaxMethod { get; set; }
    public int RulesetId { get; set; }
    public List<Guid> BusinessTypeIds { get; set; } = new();
}

public class BookRenderResult
{
    public List<Dictionary<string, object?>> Rows { get; set; } = new();
    public bool HasMore { get; set; }
    public string? NextCursor { get; set; }
    public int LoadedCount { get; set; }
    public int? TotalEstimated { get; set; }
}

public class BookFormulaSummary
{
    public Dictionary<string, decimal> FormulaValues { get; set; } = new();
    public int TotalRows { get; set; }
    public decimal? TotalRevenue { get; set; }
    public decimal? TotalCost { get; set; }
    public decimal? TotalTax { get; set; }
}

public class BookSectionsRenderResult
{
    public List<BookColumnDto> Columns { get; set; } = new();
    public List<BookSectionDto> Sections { get; set; } = new();
    public List<Dictionary<string, object?>> FooterRows { get; set; } = new();
}

public class BookSectionDto
{
    public string SectionType { get; set; } = null!;
    public string? GroupKey { get; set; }
    public string? GroupName { get; set; }
    public int GroupIndex { get; set; }
    public List<Dictionary<string, object?>> Rows { get; set; } = new();
}
