using Hangfire.Dashboard;

namespace BizFlow.Api.Common.Filters;

/// <summary>
/// Allows access to Hangfire Dashboard (for local dev/test environments).
/// </summary>
public sealed class AllowHangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context) => true;
}

