namespace BizFlow.Application.DTOs.AccountingBook;

public class CreateBooksRequest
{
    public long PeriodId { get; set; }
    public int GroupNumber { get; set; }
    public string TaxMethod { get; set; } = null!;
    public List<string> TemplateCodes { get; set; } = new();
}

public class CreateBooksResponse
{
    public List<BookListItemDto> CreatedBooks { get; set; } = new();
}

public class BookListItemDto
{
    public long BookId { get; set; }
    public string TemplateCode { get; set; } = null!;
    public string TemplateName { get; set; } = null!;
    public int GroupNumber { get; set; }
    public string? TaxMethod { get; set; }
    public string Status { get; set; } = null!;
    public string TaxProfileKey { get; set; } = null!;
    public List<BusinessTypeInBookDto> BusinessTypes { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class BusinessTypeInBookDto
{
    public Guid BusinessTypeId { get; set; }
    public string Name { get; set; } = null!;
}

public class BookSummaryResponse
{
    public long BookId { get; set; }
    public string TemplateCode { get; set; } = null!;
    public string TemplateName { get; set; } = null!;
    public int TotalRows { get; set; }
    public decimal? TotalRevenue { get; set; }
    public decimal? TotalCost { get; set; }
    public decimal? TotalTax { get; set; }
    public Dictionary<string, decimal>? FormulaValues { get; set; }
    public List<BookColumnDto> Columns { get; set; } = new();
    public List<string> Notes { get; set; } = new();
    public List<BookBusinessTypeTaxDto> BusinessTypeTaxes { get; set; } = new();
    public List<BookFormulaExplainDto> FormulaDetails { get; set; } = new();
    public List<BookFieldFormulaBindingDto> FormulaBindings { get; set; } = new();
}

public class BookBusinessTypeTaxDto
{
    public Guid BusinessTypeId { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public List<BookTaxRateItemDto> TaxRates { get; set; } = new();
}

public class BookTaxRateItemDto
{
    public string TaxType { get; set; } = null!;
    public decimal TaxRate { get; set; }
    public string? Description { get; set; }
}

public class BookFormulaExplainDto
{
    public long FormulaId { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string ExpressionJson { get; set; } = null!;
    public decimal? Value { get; set; }
}

public class BookFieldFormulaBindingDto
{
    public string FieldCode { get; set; } = null!;
    public string FieldLabel { get; set; } = null!;
    public long? FormulaId { get; set; }
    public string? FormulaCode { get; set; }
    public string? FormulaExpression { get; set; }
    public decimal? Value { get; set; }
}

public class BookColumnDto
{
    public string FieldCode { get; set; } = null!;
    public string Label { get; set; } = null!;
    public string FieldType { get; set; } = null!;
    public string? ExportColumn { get; set; }
}

public class BookRowsResponse
{
    public List<Dictionary<string, object?>> Rows { get; set; } = new();
    public bool HasMore { get; set; }
    public string? NextCursor { get; set; }
    public int LoadedCount { get; set; }
    public int? TotalEstimated { get; set; }
}

// ── GET /sections Response ──
public class BookSectionsResponse
{
    public long BookId { get; set; }
    public string TemplateCode { get; set; } = null!;
    public string TemplateName { get; set; } = null!;

    /// <summary>
    /// Timestamp when section data was computed. Helps client detect staleness.
    /// </summary>
    public DateTime LastCalculatedAt { get; set; }

    public List<BookColumnDto> Columns { get; set; } = new();
    public List<BookSectionResponseDto> Sections { get; set; } = new();
    public List<SectionRowDto> FooterRows { get; set; } = new();
}

public class BookSectionResponseDto
{
    public string SectionType { get; set; } = null!;
    public string? BusinessTypeId { get; set; }
    public string? BusinessTypeName { get; set; }
    public int GroupIndex { get; set; }
    public List<SectionRowDto> Rows { get; set; } = new();
}

public class SectionRowDto
{
    public string LineType { get; set; } = null!;
    public Dictionary<string, object?> Values { get; set; } = new();
    public DataFilterDto? DataFilter { get; set; }
    public TaxMetadataDto? TaxMetadata { get; set; }

    /// <summary>
    /// Human-readable explanation of how this row's value was computed.
    /// Includes formula breakdown, threshold info, and proration details.
    /// </summary>
    public string? Explanation { get; set; }

    /// <summary>
    /// Per-industry tax breakdown for multi-industry books.
    /// Present only on tax_line rows where per-industry computation applies.
    /// </summary>
    public List<TaxBreakdownItemDto>? TaxBreakdown { get; set; }
}

public class TaxBreakdownItemDto
{
    public Guid BusinessTypeId { get; set; }
    public string BusinessTypeName { get; set; } = null!;
    public decimal Revenue { get; set; }
    public decimal Cost { get; set; }
    public decimal Profit { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public string? Explanation { get; set; }
}

public class DataFilterDto
{
    public string? BusinessTypeId { get; set; }
    public string? Section { get; set; }
}

public class TaxMetadataDto
{
    public string TaxType { get; set; } = null!;
    public decimal Rate { get; set; }
    public string Source { get; set; } = "DEFAULT";
}
