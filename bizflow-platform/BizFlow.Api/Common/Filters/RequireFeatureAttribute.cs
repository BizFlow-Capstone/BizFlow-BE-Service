using Microsoft.AspNetCore.Mvc;

namespace BizFlow.Api.Common.Filters
{
    public class RequireFeatureAttribute : TypeFilterAttribute
    {
        public RequireFeatureAttribute(
            string featureCode,
            bool incrementUsage = true,
            string? locationKey = null)
            : base(typeof(RequireFeatureFilter))
        {
            Arguments = new object[] { featureCode, incrementUsage, locationKey ?? string.Empty };
        }
    }
}
