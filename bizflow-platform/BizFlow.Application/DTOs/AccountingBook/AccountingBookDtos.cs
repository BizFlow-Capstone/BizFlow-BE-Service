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
