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
    /// Giảm giá đang hiệu lực tại <paramref name="atUtc"/> khi có <see cref="DiscountedPrice"/>
    /// và thời điểm nằm trong [DiscountStart, DiscountEnd] (null = không giới hạn phía đó).
    /// </summary>
    public bool IsDiscountPeriodActive(DateTime? atUtc = null)
    {
        if (!DiscountedPrice.HasValue)
            return false;

        var now = atUtc ?? DateTime.UtcNow;
        if (DiscountStart.HasValue && now < NormalizeToUtc(DiscountStart.Value))
            return false;
        if (DiscountEnd.HasValue && now > NormalizeToUtc(DiscountEnd.Value))
            return false;

        return true;
    }

    /// <summary>
    /// Giá thu thực tế: dùng giá giảm chỉ khi <see cref="IsDiscountPeriodActive"/> tại thời điểm đó.
    /// </summary>
    public decimal GetEffectivePrice(DateTime? atTime = null)
    {
        if (!DiscountedPrice.HasValue)
            return BasePrice;

        return IsDiscountPeriodActive(atTime) ? DiscountedPrice.Value : BasePrice;
    }

    private static DateTime NormalizeToUtc(DateTime dt)
    {
        return dt.Kind switch
        {
            DateTimeKind.Utc => dt,
            DateTimeKind.Local => dt.ToUniversalTime(),
            _ => DateTime.SpecifyKind(dt, DateTimeKind.Utc),
        };
    }
}
