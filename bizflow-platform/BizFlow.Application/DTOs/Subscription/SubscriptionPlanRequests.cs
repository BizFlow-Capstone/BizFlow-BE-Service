using System.ComponentModel.DataAnnotations;

namespace BizFlow.Application.DTOs.Subscription
{
    public class CreateSubscriptionPlanRequest
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = null!;

        [Required]
        [StringLength(5000, MinimumLength = 1)]
        public string Description { get; set; } = null!;

        [Required]
        [Range(1, int.MaxValue)]
        public int DurationDays { get; set; }

        public CreatePlanPriceRequest? Price { get; set; }

        public List<PlanFeatureRequest>? Features { get; set; }
    }

    public class UpdateSubscriptionPlanRequest
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = null!;

        [Required]
        [StringLength(5000, MinimumLength = 1)]
        public string Description { get; set; } = null!;

        [Required]
        [Range(1, int.MaxValue)]
        public int DurationDays { get; set; }

        public UpdatePlanPriceRequest? Price { get; set; }

        public List<PlanFeatureRequest> Features { get; set; }
    }

    public class CreatePlanPriceRequest
    {
        [Required]
        [Range(0, double.MaxValue)]
        public decimal BasePrice { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? DiscountedPrice { get; set; }

        public DateTime? DiscountStart { get; set; }
        public DateTime? DiscountEnd { get; set; }
    }

    public class UpdatePlanPriceRequest
    {
        [Required]
        [Range(0, double.MaxValue)]
        public decimal BasePrice { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? DiscountedPrice { get; set; }

        public DateTime? DiscountStart { get; set; }
        public DateTime? DiscountEnd { get; set; }
    }

    public class SetPlanStatusRequest
    {
        [Required]
        public bool IsActive { get; set; }
    }

    public class PlanFeatureRequest
    {
        [Required]
        public int FeatureId { get; set; }

        [Required]
        public int UsageLimit { get; set; } = -1;
    }
}
