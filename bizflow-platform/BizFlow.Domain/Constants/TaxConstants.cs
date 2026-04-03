namespace BizFlow.Domain.Constants;

/// <summary>
/// Constants cho IndustryTaxRates — TaxType tokens phải khớp chính xác với giá trị
/// được dùng trong FormulaEngine lookup (case-sensitive, Ordinal compare).
/// </summary>
public static class IndustryTaxRateConstants
{
    /// <summary>Thuế GTGT — áp dụng cho tất cả nhóm chịu thuế.</summary>
    public const string Vat = "VAT";

    /// <summary>Thuế TNCN Cách 1 — % trên DT vượt 500tr, chỉ Nhóm 2.</summary>
    public const string PitMethod1 = "PIT_METHOD_1";

    /// <summary>
    /// Tập hợp các TaxType hợp lệ cho IndustryTaxRates.
    /// Dùng để validate khi admin tạo/cập nhật tax rate.
    /// </summary>
    public static readonly HashSet<string> AllowedTaxTypes = new(StringComparer.Ordinal)
    {
        Vat,
        PitMethod1
    };

    /// <summary>Giá trị nhỏ nhất của TaxRate (dạng fraction, 0 = 0%).</summary>
    public const decimal MinRate = 0m;

    /// <summary>Giá trị lớn nhất của TaxRate (dạng fraction, 1 = 100%).</summary>
    public const decimal MaxRate = 1m;
}

/// <summary>Constants cho BusinessType entity.</summary>
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
