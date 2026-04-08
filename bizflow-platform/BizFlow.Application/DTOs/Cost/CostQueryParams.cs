using BizFlow.Application.Common.Models;
using System.ComponentModel.DataAnnotations;

namespace BizFlow.Application.DTOs.Cost
{
    public class CostQueryParams : PaginationParams
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int BusinessLocationId { get; set; }

        /// <summary>
        /// Filter by business type (industry)
        /// </summary>
        public Guid? BusinessTypeId { get; set; }

        /// <summary>
        /// Filter by cost type (see CostType enum for valid values)
        /// </summary>
        public string? CostType { get; set; }

        /// <summary>
        /// Filter by payment method (cash | bank)
        /// </summary>
        public string? PaymentMethod { get; set; }
        public DateOnly? FromDate { get; set; }
        public DateOnly? ToDate { get; set; }
    }
}
