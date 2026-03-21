using BizFlow.Application.Common.Models;
using System.ComponentModel.DataAnnotations;

namespace BizFlow.Application.DTOs.Order
{
    public class OrderQueryParams : PaginationParams
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int BusinessLocationId { get; set; }

        public string? Status { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? Search { get; set; }
    }
}
