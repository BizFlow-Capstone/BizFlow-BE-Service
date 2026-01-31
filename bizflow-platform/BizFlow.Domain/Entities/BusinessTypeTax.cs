namespace BizFlow.Domain.Entities;

public partial class BusinessTypeTax
{
    public Guid BusinessTypeTaxId { get; set; }

    public Guid BusinessTypeId { get; set; }

    /// <summary>
    /// VAT, PIT
    /// </summary>
    public string TaxType { get; set; } = null!;

    /// <summary>
    /// Tax rate percentage
    /// </summary>
    public decimal TaxRate { get; set; }

    public bool? CalculateOnPrice { get; set; }

    public DateOnly EffectiveFrom { get; set; }

    public DateOnly? EffectiveTo { get; set; }

    public Guid? CreatedById { get; set; }

    public DateTime CreatedDate { get; set; }

    public virtual BusinessType BusinessType { get; set; } = null!;

    public virtual User? CreatedBy { get; set; }
}
