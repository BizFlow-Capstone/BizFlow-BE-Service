using BizFlow.Application.Common.Models;

namespace BizFlow.Application.Interfaces.Services
{
    /// <summary>Sends transactional email (Resend or another provider).</summary>
    public interface IEmailSender
    {
        /// <summary>True when enabled and minimally configured (API key, DefaultFrom).</summary>
        bool IsReady { get; }

        Task<EmailSendResult> SendAsync(EmailSendRequest request, CancellationToken cancellationToken = default);

        /// <summary>Sends email using a published Resend template (no HtmlBody).</summary>
        Task<EmailSendResult> SendTemplateAsync(
            string to,
            string templateIdOrAlias,
            IReadOnlyDictionary<string, string> variables,
            string idempotencyKey,
            CancellationToken cancellationToken = default);
    }
}
