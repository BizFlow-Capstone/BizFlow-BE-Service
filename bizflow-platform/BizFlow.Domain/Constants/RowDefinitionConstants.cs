namespace BizFlow.Domain.Constants;

/// <summary>
/// Constants for TemplateRowDefinition fields. Values must match DB check constraints.
/// </summary>
public static class RowDefinitionConstants
{
    public static class RowType
    {
        public const string IndustryHeader = "industry_header";
        public const string DataPlaceholder = "data_placeholder";
        public const string Subtotal = "subtotal";
        public const string TaxLine = "tax_line";
        public const string GrandTotal = "grand_total";
        public const string SectionHeader = "section_header";
        public const string SectionSubtotal = "section_subtotal";
        public const string BalanceRow = "balance_row";
        public const string MonthlyTotal = "monthly_total";
        public const string QuarterlyTotal = "quarterly_total";
        public const string ProfitRow = "profit_row";

        public static readonly string[] All =
        {
            IndustryHeader, DataPlaceholder, Subtotal, TaxLine,
            GrandTotal, SectionHeader, SectionSubtotal,
            BalanceRow, MonthlyTotal, QuarterlyTotal, ProfitRow
        };
    }

    public static class Position
    {
        public const string PerGroup = "per_group";
        public const string PerSection = "per_section";
        public const string EndOfBook = "end_of_book";
        public const string StartOfBook = "start_of_book";

        public static readonly string[] All = { PerGroup, PerSection, EndOfBook, StartOfBook };
    }

    public static class SectionType
    {
        public const string IndustryGroup = "industry_group";
        public const string RevenueCost = "revenue_cost";
        public const string CashBank = "cash_bank";
        public const string PerProduct = "per_product";

        public static readonly string[] All = { IndustryGroup, RevenueCost, CashBank, PerProduct };
    }

    public static class TaxType
    {
        public const string Vat = "VAT";
        public const string Pit = "PIT";

        public static readonly string[] All = { Vat, Pit };
    }
}
