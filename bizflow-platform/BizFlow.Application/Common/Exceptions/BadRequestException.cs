namespace BizFlow.Application.Common.Exceptions;

public class BadRequestException : Exception
{
    public string MessageKey { get; }
    public object? Errors { get; }

    public BadRequestException(string messageKey, object? errors = null)
        : base($"Bad request: {messageKey}")
    {
        MessageKey = messageKey;
        Errors = errors;
    }
}