using BizFlow.Application.Common.Models;
using System.ComponentModel.DataAnnotations;

namespace BizFlow.Application.DTOs.Revenue
{
    public class RevenueQueryParams : PaginationParams
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int BusinessLocationId { get; set; }

        public string? RevenueType { get; set; }
        public string? Status { get; set; }
        public string? MoneyChannel { get; set; }
        public DateOnly? FromDate { get; set; }
        public DateOnly? ToDate { get; set; }
    }
}
