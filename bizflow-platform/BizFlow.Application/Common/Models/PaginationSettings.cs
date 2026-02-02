namespace BizFlow.Application.Common.Models
{
    /// <summary>
    /// Pagination settings configured via appsettings.json
    /// </summary>
    public class PaginationSettings
    {
        public const string SectionName = "Pagination";

        /// <summary>
        /// Default page number when not specified
        /// </summary>
        public int DefaultPageNumber { get; set; } = 1;

        /// <summary>
        /// Default page size when not specified
        /// </summary>
        public int DefaultPageSize { get; set; } = 10;

        /// <summary>
        /// Maximum allowed page size to prevent performance issues
        /// </summary>
        public int MaxPageSize { get; set; } = 100;
    }
}
