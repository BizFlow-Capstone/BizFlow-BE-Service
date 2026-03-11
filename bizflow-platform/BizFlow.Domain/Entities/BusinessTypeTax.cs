using System;
using System.Collections.Generic;

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

    /// <summary>
    /// Calculation base: price, revenue
    /// </summary>
    public string CalculationBase { get; set; } = null!;

    public DateOnly EffectiveFrom { get; set; }

    public DateOnly? EffectiveTo { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual BusinessType BusinessType { get; set; } = null!;

    public virtual Profile? CreatedByNavigation { get; set; }
}
