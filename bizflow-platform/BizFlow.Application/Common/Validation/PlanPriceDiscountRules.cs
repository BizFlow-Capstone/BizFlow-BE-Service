using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Exceptions;

namespace BizFlow.Application.Common.Validation
{
    /// <summary>
    /// Quy tắc cửa sổ giảm giá: không quá khứ; kết thúc phải sau bắt đầu (UTC).
    /// </summary>
    public static class PlanPriceDiscountRules
    {
        public static DateTime AsUtc(DateTime dt)
        {
            return dt.Kind switch
            {
                DateTimeKind.Utc => dt,
                DateTimeKind.Local => dt.ToUniversalTime(),
                _ => DateTime.SpecifyKind(dt, DateTimeKind.Utc),
            };
        }

        /// <summary>Áp dụng khi có <paramref name="discountedPrice"/> (kèm hoặc không kèm ngày).</summary>
        public static void ValidateOrThrow(decimal? discountedPrice, DateTime? discountStart, DateTime? discountEnd)
        {
            if (!discountedPrice.HasValue)
                return;

            var now = DateTime.UtcNow;

            if (discountStart.HasValue && AsUtc(discountStart.Value) < now)
                throw new BadRequestException(MessageKeys.SubscriptionDiscountStartInPast);

            if (discountEnd.HasValue && AsUtc(discountEnd.Value) < now)
                throw new BadRequestException(MessageKeys.SubscriptionDiscountEndInPast);

            if (discountStart.HasValue && discountEnd.HasValue
                && AsUtc(discountEnd.Value) <= AsUtc(discountStart.Value))
                throw new BadRequestException(MessageKeys.SubscriptionDiscountEndMustBeAfterStart);
        }
    }
}
