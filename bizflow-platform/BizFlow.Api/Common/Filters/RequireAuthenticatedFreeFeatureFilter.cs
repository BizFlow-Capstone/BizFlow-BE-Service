using BizFlow.Api.Common.Extensions;
using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.Common.Models;
using BizFlow.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BizFlow.Api.Common.Filters
{
    public class RequireAuthenticatedFreeFeatureFilter : IAsyncAuthorizationFilter
    {
        private readonly ISubscriptionService _subscriptionService;
        private readonly IMessageService _messageService;

        public RequireAuthenticatedFreeFeatureFilter(
            ISubscriptionService subscriptionService,
            IMessageService messageService)
        {
            _subscriptionService = subscriptionService;
            _messageService = messageService;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            if (context.Filters.OfType<IAllowAnonymousFilter>().Any())
            {
                return;
            }

            var endpoint = context.HttpContext.GetEndpoint();
            if (endpoint?.Metadata.GetMetadata<IAllowAnonymous>() != null)
            {
                return;
            }

            var hasAuthorizeMetadata =
                context.Filters.OfType<AuthorizeFilter>().Any()
                || endpoint?.Metadata.GetOrderedMetadata<IAuthorizeData>().Any() == true;

            if (!hasAuthorizeMetadata)
            {
                return;
            }

            Guid profileId;
            try
            {
                profileId = context.HttpContext.User.GetRequiredUserId();
            }
            catch
            {
                return;
            }

            var hasAccess = await _subscriptionService.HasActiveSubscriptionAsync(profileId);
            if (hasAccess)
            {
                return;
            }

            context.Result = new ObjectResult(ApiResponse.ErrorResponse(
                MessageKeys.Forbidden,
                _messageService.GetMessage(MessageKeys.Forbidden),
                new
                {
                    reason = "Active subscription is required for authenticated APIs."
                }))
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
    }
}
