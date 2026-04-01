using Microsoft.AspNetCore.Mvc;

namespace BizFlow.Api.Common.Filters
{
    /// <summary>
    /// Kiểm tra quota feature theo subscription.
    /// Mặc định cần <c>locationId</c> / <c>businessLocationId</c> trong route/query/body.
    /// Đặt <paramref name="useOwnerScope"/> = true cho API chưa có location (ví dụ tạo cửa hàng mới): kiểm tra theo owner (CheckFeatureAccessByOwnerAsync).
    /// </summary>
    public class RequireFeatureAttribute : TypeFilterAttribute
    {
        public RequireFeatureAttribute(
            string featureCode,
            bool incrementUsage = true,
            string? locationKey = null,
            bool useOwnerScope = false)
            : base(typeof(RequireFeatureFilter))
        {
            Arguments = new object[] { featureCode, incrementUsage, locationKey ?? string.Empty, useOwnerScope };
        }
    }
}
