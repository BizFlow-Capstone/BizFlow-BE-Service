using Microsoft.AspNetCore.Mvc;

namespace BizFlow.Api.Common.Filters
{
    /// <summary>
    /// Enforces feature quota based on subscription.
    /// By default requires <c>locationId</c> / <c>businessLocationId</c> in route, query, or body.
    /// Set <paramref name="useOwnerScope"/> = true for APIs without a location yet (e.g. creating a new store): checks access by owner (CheckFeatureAccessByOwnerAsync).
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
