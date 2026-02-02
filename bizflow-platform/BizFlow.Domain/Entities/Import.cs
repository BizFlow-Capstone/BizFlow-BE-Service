namespace BizFlow.Domain.Entities;

public partial class Import
{
    public long ImportId { get; set; }

    /// <summary>
    /// Import data schema
    /// </summary>
    public string? SchemaJson { get; set; }

    /// <summary>
    /// Total amount
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Import date
    /// </summary>
    public DateTime Date { get; set; }

    public string? Description { get; set; }

    public virtual ICollection<ProductImport> ProductsImports { get; set; } = new List<ProductImport>();
}
