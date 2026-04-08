namespace BizFlow.Application.Common.Models
{
    public sealed class EmailSendResult
    {
        public bool Success { get; init; }
        public string? ProviderMessageId { get; init; }
        public string? ErrorDetail { get; init; }

        public static EmailSendResult Ok(string? providerMessageId) =>
            new() { Success = true, ProviderMessageId = providerMessageId };

        public static EmailSendResult Fail(string? errorDetail) =>
            new() { Success = false, ErrorDetail = errorDetail };
    }
}
