using System.Collections.Generic;
using BizFlow.Application.Common.Models;
using BizFlow.Application.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Resend;

namespace BizFlow.Infrastructure.Services
{
    public class ResendEmailSender : IEmailSender
    {
        private readonly IResend _resend;
        private readonly ResendSettings _settings;
        private readonly ILogger<ResendEmailSender> _logger;

        public ResendEmailSender(
            IResend resend,
            IOptions<ResendSettings> settings,
            ILogger<ResendEmailSender> logger)
        {
            _resend = resend;
            _settings = settings.Value;
            _logger = logger;
        }

        public bool IsReady =>
            _settings.Enabled
            && !string.IsNullOrWhiteSpace(_settings.ApiKey)
            && !string.IsNullOrWhiteSpace(_settings.DefaultFrom);

        public async Task<EmailSendResult> SendAsync(EmailSendRequest request, CancellationToken cancellationToken = default)
        {
            if (!IsReady)
            {
                return EmailSendResult.Fail("Resend disabled or not configured.");
            }

            if (string.IsNullOrWhiteSpace(request.To)
                || string.IsNullOrWhiteSpace(request.Subject)
                || string.IsNullOrWhiteSpace(request.HtmlBody)
                || string.IsNullOrWhiteSpace(request.IdempotencyKey))
            {
                return EmailSendResult.Fail("Invalid email request.");
            }

            var message = new EmailMessage
            {
                From = _settings.DefaultFrom.Trim(),
                Subject = request.Subject.Trim(),
                HtmlBody = request.HtmlBody,
                TextBody = request.TextBody
            };

            message.To.Add(request.To.Trim());

            var replyTo = string.IsNullOrWhiteSpace(request.ReplyToOverride)
                ? _settings.ReplyTo
                : request.ReplyToOverride;
            if (!string.IsNullOrWhiteSpace(replyTo))
            {
                message.ReplyTo = replyTo.Trim();
            }

            if (request.Tags is { Count: > 0 })
            {
                message.Tags ??= new List<EmailTag>();
                foreach (var kv in request.Tags)
                {
                    message.Tags.Add(new EmailTag { Name = kv.Key, Value = kv.Value });
                }
            }

            try
            {
                var response = await _resend.EmailSendAsync(request.IdempotencyKey.Trim(), message, cancellationToken)
                    .ConfigureAwait(false);

                if (!response.Success)
                {
                    var err = response.Exception?.Message ?? "Resend API error.";
                    _logger.LogWarning("Resend send failed: {Error}", err);
                    return EmailSendResult.Fail(err);
                }

                return EmailSendResult.Ok(response.Content.ToString());
            }
            catch (ResendException ex)
            {
                _logger.LogWarning(ex, "Resend send failed.");
                return EmailSendResult.Fail(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error sending email via Resend.");
                return EmailSendResult.Fail(ex.Message);
            }
        }

        public async Task<EmailSendResult> SendTemplateAsync(
            string to,
            string templateIdOrAlias,
            IReadOnlyDictionary<string, string> variables,
            string idempotencyKey,
            CancellationToken cancellationToken = default)
        {
            if (!IsReady)
            {
                return EmailSendResult.Fail("Resend disabled or not configured.");
            }

            if (string.IsNullOrWhiteSpace(to) || string.IsNullOrWhiteSpace(templateIdOrAlias))
            {
                return EmailSendResult.Fail("Invalid email template request.");
            }

            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _settings.ApiKey);
                
                if (!string.IsNullOrWhiteSpace(idempotencyKey))
                {
                    client.DefaultRequestHeaders.Add("Idempotency-Key", idempotencyKey.Trim());
                }

                var payload = new
                {
                    from = _settings.DefaultFrom.Trim(),
                    to = new[] { to.Trim() },
                    template_id = templateIdOrAlias,
                    headers = new { },
                    tags = new object[] { },
                    text = "",
                    template_data = variables
                };

                // Filter out empty text because Resend API mutual exclusion might reject if "text" is also passed.
                var cleanPayload = new
                {
                    from = _settings.DefaultFrom.Trim(),
                    to = new[] { to.Trim() },
                    template_id = templateIdOrAlias,
                    tags = new object[] { },
                }; // Let's use a simpler structure that works.

                var dictPayload = new Dictionary<string, object>
                {
                    { "from", _settings.DefaultFrom.Trim() },
                    { "to", new[] { to.Trim() } }
                };

                var templateObject = new Dictionary<string, object>
                {
                    { "id", templateIdOrAlias }
                };

                if (variables != null && variables.Count > 0)
                {
                    templateObject.Add("variables", variables);
                }

                dictPayload.Add("template", templateObject);

                var json = System.Text.Json.JsonSerializer.Serialize(dictPayload);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await client.PostAsync("https://api.resend.com/emails", content, cancellationToken).ConfigureAwait(false);
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Resend template send failed: {StatusCode} {Error}", response.StatusCode, responseContent);
                    return EmailSendResult.Fail($"Resend API error: {responseContent}");
                }

                return EmailSendResult.Ok(responseContent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error sending template email via Resend.");
                return EmailSendResult.Fail(ex.Message);
            }
        }
    }
}
