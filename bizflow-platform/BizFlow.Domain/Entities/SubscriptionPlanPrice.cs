namespace BizFlow.Domain.Entities;

/// <summary>
/// Price history for subscription plans
/// </summary>
public partial class SubscriptionPlanPrice
{
    public int PriceId { get; set; }

    public int SubscriptionPlanId { get; set; }

    public decimal BasePrice { get; set; }

    public decimal? DiscountedPrice { get; set; }

    public DateTime? DiscountStart { get; set; }

    public DateTime? DiscountEnd { get; set; }

    public bool IsDiscountActive { get; set; }

    public bool IsActive { get; set; }

    public string Currency { get; set; } = "VND";

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual SubscriptionPlan SubscriptionPlan { get; set; } = null!;

    /// <summary>
    /// Computes the effective price based on discount rules
    /// </summary>
    public decimal GetEffectivePrice(DateTime? atTime = null)
    {
        var now = atTime ?? DateTime.UtcNow;

        if (IsDiscountActive && DiscountedPrice.HasValue
            && (DiscountStart == null || now >= DiscountStart)
            && (DiscountEnd == null || now <= DiscountEnd))
        {
            return DiscountedPrice.Value;
        }

        return BasePrice;
    }
}
