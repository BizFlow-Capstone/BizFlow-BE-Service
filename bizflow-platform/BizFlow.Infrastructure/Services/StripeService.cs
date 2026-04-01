using BizFlow.Application.Common.Exceptions;
using BizFlow.Application.Common.Constants;
using BizFlow.Application.Common.Models;
using BizFlow.Application.Interfaces.Services;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace BizFlow.Infrastructure.Services
{
    public class StripeService : IStripeService
    {
        private readonly StripeSettings _settings;

        public StripeService(IOptions<StripeSettings> settings)
        {
            _settings = settings.Value;

            if (!string.IsNullOrWhiteSpace(_settings.SecretKey))
            {
                StripeConfiguration.ApiKey = _settings.SecretKey;
            }
        }

        public bool IsConfigured => !string.IsNullOrWhiteSpace(_settings.SecretKey);

        public async Task<Session> CreateCheckoutSessionAsync(
            string? stripeCustomerId,
            string stripePriceId,
            Guid transactionId,
            Guid profileId,
            string idempotencyKey,
            string platform = "web",
            int quantity = 1)
        {
            if (string.IsNullOrWhiteSpace(_settings.SecretKey))
            {
                throw new BadRequestException(MessageKeys.StripeSecretMissing);
            }

            if (string.IsNullOrWhiteSpace(stripePriceId))
            {
                throw new BadRequestException(MessageKeys.StripePriceNotConfigured);
            }

            var successUrl = string.IsNullOrWhiteSpace(_settings.SuccessCallbackUrl)
                ? _settings.WebSuccessUrl + "?session_id={CHECKOUT_SESSION_ID}"
                : _settings.SuccessCallbackUrl + "?session_id={CHECKOUT_SESSION_ID}";

            var cancelUrl = string.IsNullOrWhiteSpace(_settings.CancelCallbackUrl)
                ? _settings.WebCancelUrl
                : _settings.CancelCallbackUrl;

            var safeQuantity = Math.Max(1, quantity);

            var options = new SessionCreateOptions
            {
                Mode = "payment",
                Customer = string.IsNullOrWhiteSpace(stripeCustomerId) ? null : stripeCustomerId,
                CustomerCreation = string.IsNullOrWhiteSpace(stripeCustomerId) ? "always" : null,
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl,
                LineItems =
                [
                    new SessionLineItemOptions
                    {
                        Price = stripePriceId,
                        Quantity = safeQuantity
                    }
                ],
                Metadata = new Dictionary<string, string>
                {
                    ["transactionId"] = transactionId.ToString(),
                    ["profileId"] = profileId.ToString(),
                    ["platform"] = platform,
                    ["quantity"] = safeQuantity.ToString()
                }
            };

            var requestOptions = new RequestOptions
            {
                IdempotencyKey = idempotencyKey
            };

            var service = new SessionService();
            return await service.CreateAsync(options, requestOptions);
        }

        public async Task<Session> CreateCheckoutSessionForTotalAmountAsync(
            string? stripeCustomerId,
            long totalAmountVnd,
            string productName,
            Guid transactionId,
            Guid profileId,
            string idempotencyKey,
            string platform = "web",
            int quantity = 1)
        {
            if (string.IsNullOrWhiteSpace(_settings.SecretKey))
            {
                throw new BadRequestException(MessageKeys.StripeSecretMissing);
            }

            if (totalAmountVnd <= 0)
            {
                throw new BadRequestException(MessageKeys.BadRequest);
            }

            var successUrl = string.IsNullOrWhiteSpace(_settings.SuccessCallbackUrl)
                ? _settings.WebSuccessUrl + "?session_id={CHECKOUT_SESSION_ID}"
                : _settings.SuccessCallbackUrl + "?session_id={CHECKOUT_SESSION_ID}";

            var cancelUrl = string.IsNullOrWhiteSpace(_settings.CancelCallbackUrl)
                ? _settings.WebCancelUrl
                : _settings.CancelCallbackUrl;

            var safeQuantity = Math.Max(1, quantity);
            var displayName = string.IsNullOrWhiteSpace(productName) ? "BizFlow subscription" : productName;

            var options = new SessionCreateOptions
            {
                Mode = "payment",
                Customer = string.IsNullOrWhiteSpace(stripeCustomerId) ? null : stripeCustomerId,
                CustomerCreation = string.IsNullOrWhiteSpace(stripeCustomerId) ? "always" : null,
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl,
                LineItems =
                [
                    new SessionLineItemOptions
                    {
                        Quantity = 1,
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = "vnd",
                            UnitAmount = totalAmountVnd,
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = displayName
                            }
                        }
                    }
                ],
                Metadata = new Dictionary<string, string>
                {
                    ["transactionId"] = transactionId.ToString(),
                    ["profileId"] = profileId.ToString(),
                    ["platform"] = platform,
                    ["quantity"] = safeQuantity.ToString(),
                    ["billingMode"] = "customTotalVnd"
                }
            };

            var requestOptions = new RequestOptions
            {
                IdempotencyKey = idempotencyKey
            };

            var service = new SessionService();
            return await service.CreateAsync(options, requestOptions);
        }

        public Event ConstructEvent(string json, string stripeSignature, string webhookSecret)
        {
            return EventUtility.ConstructEvent(json, stripeSignature, webhookSecret);
        }

        public async Task<Refund?> RefundPaymentIntentAsync(
            string paymentIntentId,
            string idempotencyKey,
            string? reason = null,
            Dictionary<string, string>? metadata = null)
        {
            if (string.IsNullOrWhiteSpace(_settings.SecretKey) || string.IsNullOrWhiteSpace(paymentIntentId))
            {
                return null;
            }

            var refundService = new RefundService();
            var options = new RefundCreateOptions
            {
                PaymentIntent = paymentIntentId,
                Metadata = metadata
            };

            if (string.Equals(reason, "fraudulent", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(reason, "requested_by_customer", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(reason, "duplicate", StringComparison.OrdinalIgnoreCase))
            {
                options.Reason = reason;
            }

            var requestOptions = new RequestOptions
            {
                IdempotencyKey = idempotencyKey
            };

            return await refundService.CreateAsync(options, requestOptions);
        }

        public async Task<Session?> GetCheckoutSessionAsync(string checkoutSessionId)
        {
            if (string.IsNullOrWhiteSpace(_settings.SecretKey) || string.IsNullOrWhiteSpace(checkoutSessionId))
            {
                return null;
            }

            var service = new SessionService();
            var options = new SessionGetOptions
            {
                Expand = ["payment_intent"]
            };

            try
            {
                return await service.GetAsync(checkoutSessionId, options);
            }
            catch (StripeException)
            {
                return null;
            }
        }

        public async Task<PaymentIntent?> GetPaymentIntentAsync(string paymentIntentId)
        {
            if (string.IsNullOrWhiteSpace(_settings.SecretKey) || string.IsNullOrWhiteSpace(paymentIntentId))
            {
                return null;
            }

            var service = new PaymentIntentService();
            try
            {
                return await service.GetAsync(paymentIntentId);
            }
            catch (StripeException)
            {
                return null;
            }
        }

        public async Task<IReadOnlyList<Refund>> ListRecentRefundsAsync(DateTime fromUtc, int limit = 100)
        {
            if (string.IsNullOrWhiteSpace(_settings.SecretKey))
            {
                return [];
            }

            var refundService = new RefundService();
            var safeLimit = Math.Clamp(limit, 1, 100);

            var options = new RefundListOptions
            {
                Limit = safeLimit
            };

            var list = await refundService.ListAsync(options);
            return list.Data
                .Where(x => x.Created >= fromUtc)
                .OrderByDescending(x => x.Created)
                .ToList();
        }

        // ===================== Product & Price catalog sync =====================

        public async Task<Product> CreateProductAsync(string name, string? description, Dictionary<string, string>? metadata = null)
        {
            var service = new ProductService();
            var options = new ProductCreateOptions
            {
                Name = name,
                Description = description,
                Metadata = metadata
            };
            return await service.CreateAsync(options);
        }

        public async Task<Product> UpdateProductAsync(string productId, string name, string? description, bool? active = null)
        {
            var service = new ProductService();
            var options = new ProductUpdateOptions
            {
                Name = name,
                Description = description
            };
            if (active.HasValue)
                options.Active = active.Value;

            return await service.UpdateAsync(productId, options);
        }

        public async Task ArchiveProductAsync(string productId)
        {
            var service = new ProductService();
            await service.UpdateAsync(productId, new ProductUpdateOptions { Active = false });
        }

        /// <summary>
        /// Creates a one-time Stripe Price. VND is a zero-decimal currency so unitAmount maps 1:1.
        /// </summary>
        public async Task<Price> CreatePriceAsync(string productId, long unitAmount, string currency = "vnd")
        {
            var service = new PriceService();
            var options = new PriceCreateOptions
            {
                Product = productId,
                UnitAmount = unitAmount,
                Currency = currency
            };
            return await service.CreateAsync(options);
        }

        public async Task ArchivePriceAsync(string priceId)
        {
            var service = new PriceService();
            await service.UpdateAsync(priceId, new PriceUpdateOptions { Active = false });
        }

        public async Task<Price?> GetPriceAsync(string priceId)
        {
            if (string.IsNullOrWhiteSpace(_settings.SecretKey) || string.IsNullOrWhiteSpace(priceId))
                return null;

            try
            {
                return await new PriceService().GetAsync(priceId);
            }
            catch (StripeException)
            {
                return null;
            }
        }

        public async Task<Price> ReactivatePriceAsync(string priceId)
        {
            var service = new PriceService();
            return await service.UpdateAsync(priceId, new PriceUpdateOptions { Active = true });
        }
    }
}
