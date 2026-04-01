using BizFlow.Api.Common.Extensions;
using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.Common.Models;
using BizFlow.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BizFlow.Api.Common.Filters
{
    public class RequireFeatureFilter : IAsyncActionFilter
    {
        private readonly string _featureCode;
        private readonly bool _incrementUsage;
        private readonly string? _locationKey;
        private readonly bool _useOwnerScope;
        private readonly ISubscriptionService _subscriptionService;
        private readonly IMessageService _messageService;

        public RequireFeatureFilter(
            string featureCode,
            bool incrementUsage,
            string? locationKey,
            bool useOwnerScope,
            ISubscriptionService subscriptionService,
            IMessageService messageService
        )
        {
            _featureCode = featureCode;
            _incrementUsage = incrementUsage;
            _locationKey = locationKey;
            _useOwnerScope = useOwnerScope;
            _subscriptionService = subscriptionService;
            _messageService = messageService;
        }

        public async Task OnActionExecutionAsync(
            ActionExecutingContext context,
            ActionExecutionDelegate next
        )
        {
            Guid profileId;
            try
            {
                profileId = context.HttpContext.User.GetRequiredUserId();
            }
            catch
            {
                context.Result = new UnauthorizedObjectResult(
                    ApiResponse.ErrorResponse(
                        MessageKeys.Unauthorized,
                        _messageService.GetMessage(MessageKeys.Unauthorized)
                    )
                );
                return;
            }

            if (string.IsNullOrWhiteSpace(_featureCode))
            {
                context.Result = new BadRequestObjectResult(
                    ApiResponse.ErrorResponse(
                        MessageKeys.ValidationError,
                        _messageService.GetMessage(MessageKeys.ValidationError),
                        new { field = "featureCode", message = "featureCode is required" }
                    )
                );
                return;
            }

            if (_useOwnerScope)
            {
                var evalOwner = await _subscriptionService.EvaluateFeatureAccessByOwnerAsync(
                    profileId,
                    _featureCode
                );
                if (!evalOwner.Allowed)
                {
                    var msgKey = ResolveDeniedMessageKey(evalOwner.DenialReason);
                    context.Result = new ObjectResult(
                        ApiResponse.ErrorResponse(msgKey, GetDeniedMessage(evalOwner))
                    )
                    {
                        StatusCode = StatusCodes.Status403Forbidden,
                    };
                    return;
                }

                var executedOwner = await next();

                if (!_incrementUsage)
                {
                    return;
                }

                var statusOwner =
                    executedOwner.HttpContext.Response?.StatusCode ?? StatusCodes.Status200OK;
                if (
                    executedOwner.Exception == null
                    && statusOwner < StatusCodes.Status400BadRequest
                )
                {
                    await _subscriptionService.CheckFeatureAccessByOwnerAsync(
                        profileId,
                        _featureCode,
                        incrementUsage: true
                    );
                }

                return;
            }

            var locationId = ResolveLocationId(context, _locationKey);

            if (!locationId.HasValue || locationId.Value <= 0)
            {
                context.Result = new BadRequestObjectResult(
                    ApiResponse.ErrorResponse(
                        MessageKeys.ValidationError,
                        _messageService.GetMessage(MessageKeys.ValidationError),
                        new
                        {
                            field = "locationId",
                            message = _messageService.GetMessage(MessageKeys.LocationIdRequired),
                        }
                    )
                );
                return;
            }

            var evalLoc = await _subscriptionService.EvaluateFeatureAccessAsync(
                profileId,
                locationId.Value,
                _featureCode
            );

            if (!evalLoc.Allowed)
            {
                var msgKeyLoc = ResolveDeniedMessageKey(evalLoc.DenialReason);
                context.Result = new ObjectResult(
                    ApiResponse.ErrorResponse(msgKeyLoc, GetDeniedMessage(evalLoc))
                )
                {
                    StatusCode = StatusCodes.Status403Forbidden,
                };
                return;
            }

            var executedContext = await next();

            if (!_incrementUsage)
            {
                return;
            }

            var statusCode =
                executedContext.HttpContext.Response?.StatusCode ?? StatusCodes.Status200OK;
            if (executedContext.Exception == null && statusCode < StatusCodes.Status400BadRequest)
            {
                // Increment usage only after successful action execution.
                await _subscriptionService.CheckFeatureAccessAsync(
                    profileId,
                    locationId.Value,
                    _featureCode,
                    incrementUsage: true
                );
            }
        }

        private static int? ResolveLocationId(ActionExecutingContext context, string? locationKey)
        {
            var candidateKeys = BuildCandidateKeys(locationKey);

            foreach (var key in candidateKeys)
            {
                if (context.ActionArguments.TryGetValue(key, out var arg) && arg != null)
                {
                    var parsed = TryParseInt(arg);
                    if (parsed.HasValue && parsed.Value > 0)
                    {
                        return parsed;
                    }
                }
            }

            foreach (var value in context.ActionArguments.Values)
            {
                if (value == null)
                {
                    continue;
                }

                var properties = value.GetType().GetProperties();
                var matchingProperty = properties.FirstOrDefault(p =>
                    candidateKeys.Contains(p.Name, StringComparer.OrdinalIgnoreCase)
                );
                if (matchingProperty?.GetValue(value) is object propertyValue)
                {
                    var parsed = TryParseInt(propertyValue);
                    if (parsed.HasValue && parsed.Value > 0)
                    {
                        return parsed;
                    }
                }
            }

            foreach (var key in candidateKeys)
            {
                var queryValue = context.HttpContext.Request.Query[key].ToString();
                if (int.TryParse(queryValue, out var parsedQuery) && parsedQuery > 0)
                {
                    return parsedQuery;
                }
            }

            foreach (var key in candidateKeys)
            {
                if (
                    context.RouteData.Values.TryGetValue(key, out var routeValue)
                    && routeValue != null
                )
                {
                    var parsed = TryParseInt(routeValue);
                    if (parsed.HasValue && parsed.Value > 0)
                    {
                        return parsed;
                    }
                }
            }

            return null;
        }

        private static string ResolveDeniedMessageKey(FeatureAccessDenialReason reason)
        {
            return reason == FeatureAccessDenialReason.UsageLimitReached
                ? MessageKeys.SubscriptionFeatureUsageLimitReached
                : MessageKeys.SubscriptionFeatureAccessDenied;
        }

        private string GetDeniedMessage(FeatureAccessEvaluationResult eval)
        {
            if (
                eval.DenialReason == FeatureAccessDenialReason.UsageLimitReached
                && eval.Used.HasValue
                && eval.Limit.HasValue
            )
            {
                return _messageService.GetMessage(
                    MessageKeys.SubscriptionFeatureUsageLimitReached,
                    eval.Used.Value,
                    eval.Limit.Value
                );
            }

            return _messageService.GetMessage(ResolveDeniedMessageKey(eval.DenialReason));
        }

        private static int? TryParseInt(object value)
        {
            return value switch
            {
                int intValue => intValue,
                string s when int.TryParse(s, out var parsed) => parsed,
                _ => null,
            };
        }

        private static List<string> BuildCandidateKeys(string? locationKey)
        {
            var keys = new List<string>
            {
                "locationId",
                "businessLocationId",
                "LocationId",
                "BusinessLocationId",
            };

            if (!string.IsNullOrWhiteSpace(locationKey))
            {
                keys.Insert(0, locationKey);
            }

            return keys.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }
    }
}
