namespace BizFlow.Application.Common.Models
{
    /// <summary>Transactional email send request (not tied to the Resend SDK).</summary>
    public sealed class EmailSendRequest
    {
        public string To { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string HtmlBody { get; set; } = string.Empty;
        public string? TextBody { get; set; }
        public string IdempotencyKey { get; set; } = string.Empty;
        public string? ReplyToOverride { get; set; }
        public IReadOnlyDictionary<string, string>? Tags { get; set; }
    }
}
