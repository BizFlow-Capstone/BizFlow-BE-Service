using BizFlow.Application.Common.Models;
using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Interfaces;
using BizFlow.Application.Interfaces.Repositories;
using BizFlow.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace BizFlow.Api.Controllers.Subscription
{
    [ApiController]
    [Route("api/webhooks")]
    public class StripeWebhookController : ControllerBase
    {
        private readonly ISubscriptionService _subscriptionService;
        private readonly IStripeService _stripeService;
        private readonly ILogger<StripeWebhookController> _logger;
        private readonly StripeSettings _stripeSettings;
        private readonly IMessageService _messageService;
        private readonly IStripeWebhookEventRepository _stripeWebhookEventRepository;

        public StripeWebhookController(
            ISubscriptionService subscriptionService,
            IStripeService stripeService,
            IStripeWebhookEventRepository stripeWebhookEventRepository,
            IMessageService messageService,
            IOptions<StripeSettings> stripeSettings,
            ILogger<StripeWebhookController> logger)
        {
            _subscriptionService = subscriptionService;
            _stripeService = stripeService;
            _stripeWebhookEventRepository = stripeWebhookEventRepository;
            _messageService = messageService;
            _logger = logger;
            _stripeSettings = stripeSettings.Value;
        }

        [HttpPost("stripe")]
        [AllowAnonymous]
        public async Task<IActionResult> HandleStripeWebhook()
        {
            var payload = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            var signature = Request.Headers["Stripe-Signature"].ToString();

            Event stripeEvent;
            try
            {
                stripeEvent = _stripeService.ConstructEvent(payload, signature, _stripeSettings.WebhookSecret);
            }
            catch (StripeException ex)
            {
                _logger.LogWarning(ex, "Invalid Stripe webhook signature.");
                return BadRequest(ApiResponse.ErrorResponse(
                    MessageKeys.InvalidWebhookSignature,
                    _messageService.GetMessage(MessageKeys.InvalidWebhookSignature)));
            }

            var stripeCreatedAtUtc = stripeEvent.Created.ToUniversalTime();
            var canProcess = await _stripeWebhookEventRepository.TryCreateReceivedAsync(
                stripeEvent.Id,
                stripeEvent.Type,
                stripeCreatedAtUtc);

            if (!canProcess)
            {
                _logger.LogInformation("Duplicate Stripe event skipped: {EventId} ({Type})", stripeEvent.Id, stripeEvent.Type);
                return Ok();
            }

            try
            {
                switch (stripeEvent.Type)
                {
                    case "checkout.session.completed":
                    {
                        if (stripeEvent.Data.Object is Session completedSession)
                        {
                            await _subscriptionService.HandleCheckoutCompletedAsync(MapCheckoutSession(completedSession));
                        }
                        break;
                    }
                    case "checkout.session.expired":
                    {
                        if (stripeEvent.Data.Object is Session expiredSession)
                        {
                            await _subscriptionService.HandleCheckoutExpiredAsync(MapCheckoutSession(expiredSession));
                        }
                        break;
                    }
                    case "payment_intent.payment_failed":
                    {
                        if (stripeEvent.Data.Object is PaymentIntent paymentIntent)
                        {
                            await _subscriptionService.HandlePaymentFailedAsync(MapPaymentIntent(paymentIntent));
                        }
                        break;
                    }
                    case "charge.refunded":
                    {
                        if (stripeEvent.Data.Object is Charge charge && !string.IsNullOrWhiteSpace(charge.PaymentIntentId))
                        {
                            await _subscriptionService.HandleChargeRefundedAsync(charge.PaymentIntentId);
                        }
                        break;
                    }
                    default:
                        _logger.LogInformation("Unhandled Stripe event type: {Type}", stripeEvent.Type);
                        break;
                }

                await _stripeWebhookEventRepository.MarkProcessedAsync(stripeEvent.Id);
                return Ok();
            }
            catch (Exception ex)
            {
                await _stripeWebhookEventRepository.MarkFailedAsync(stripeEvent.Id, ex.Message);
                _logger.LogError(ex, "Failed to process Stripe event {EventId} ({Type})", stripeEvent.Id, stripeEvent.Type);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        private static StripeCheckoutSessionPayload MapCheckoutSession(Session session)
        {
            return new StripeCheckoutSessionPayload
            {
                SessionId = session.Id,
                PaymentIntentId = session.PaymentIntentId,
                PaymentStatus = session.PaymentStatus,
                Status = session.Status,
                Metadata = session.Metadata != null
                    ? new Dictionary<string, string>(session.Metadata)
                    : null
            };
        }

        private static StripePaymentIntentPayload MapPaymentIntent(PaymentIntent paymentIntent)
        {
            return new StripePaymentIntentPayload
            {
                PaymentIntentId = paymentIntent.Id,
                Status = paymentIntent.Status
            };
        }
    }
}
