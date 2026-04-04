namespace BizFlow.Application.Common.Models
{
    /// <summary>
    /// Resend settings for transactional email. ApiKey is mapped to the Resend client token when registering DI.
    /// </summary>
    public class ResendSettings
    {
        public const string SectionName = "Resend";

        /// <summary>API key (re_...). Do not commit real values to git.</summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>Sender address, e.g. <c>BizFlow &lt;noreply@yourdomain.com&gt;</c>. Domain must be verified in Resend to avoid 403 and improve deliverability.</summary>
        public string DefaultFrom { get; set; } = string.Empty;

        /// <summary>Optional Reply-To (single email address).</summary>
        public string? ReplyTo { get; set; }

        /// <summary>Enable sending via Resend (when false, all sends are skipped).</summary>
        public bool Enabled { get; set; } = true;
    }
}
