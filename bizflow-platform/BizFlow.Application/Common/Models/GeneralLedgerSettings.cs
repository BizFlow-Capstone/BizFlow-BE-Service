namespace BizFlow.Application.Common.Models
{
    public class GeneralLedgerSettings
    {
        public const string SectionName = "GeneralLedger";

        /// <summary>
        /// day | month | year
        /// </summary>
        public string LookbackUnit { get; set; } = "year";

        /// <summary>
        /// Number of units to look back.
        /// Examples: 1 year, 3 months, 30 days.
        /// </summary>
        public int LookbackValue { get; set; } = 1;
    }
}
