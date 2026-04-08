namespace BizFlow.Domain.Constants;

/// <summary>
/// Constants for IndustryTaxRates — TaxType tokens must match exactly the values
/// used in FormulaEngine lookup (case-sensitive, Ordinal compare).
/// </summary>
public static class IndustryTaxRateConstants
{
    /// <summary>VAT — applies to all taxable groups.</summary>
    public const string Vat = "VAT";

    /// <summary>PIT Method 1 — percentage on revenue above 500M threshold; Group 2 only.</summary>
    public const string PitMethod1 = "PIT_METHOD_1";

    /// <summary>
    /// Allowed TaxType values for IndustryTaxRates.
    /// Used to validate when an admin creates or updates a tax rate.
    /// </summary>
    public static readonly HashSet<string> AllowedTaxTypes = new(StringComparer.Ordinal)
    {
        Vat,
        PitMethod1
    };

    /// <summary>Minimum TaxRate value (fraction, 0 = 0%).</summary>
    public const decimal MinRate = 0m;

    /// <summary>Maximum TaxRate value (fraction, 1 = 100%).</summary>
    public const decimal MaxRate = 1m;
}

/// <summary>Constants for the BusinessType entity.</summary>
public static class BusinessTypeConstants
{
    public const string Active = "active";
    public const string Inactive = "inactive";

    public static readonly HashSet<string> AllowedStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        Active,
        Inactive
    };
}
