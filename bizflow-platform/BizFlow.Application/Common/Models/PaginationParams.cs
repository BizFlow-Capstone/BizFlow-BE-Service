namespace BizFlow.Application.Common.Models
{
    /// <summary>
    /// Base pagination parameters - separated from query-specific params
    /// </summary>
    public class PaginationParams
    {
        public int? PageNumber { get; set; }
        public int? PageSize { get; set; }
    }
}
