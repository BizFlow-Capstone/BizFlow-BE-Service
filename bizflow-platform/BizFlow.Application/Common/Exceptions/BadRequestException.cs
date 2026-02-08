namespace BizFlow.Application.Common.Exceptions;

public class BadRequestException : Exception
{
    public string MessageKey { get; }
    public object? Errors { get; }
    public object[] Args { get; }

    public BadRequestException(string messageKey, object? errors = null, params object[] args)
        : base($"Bad request: {messageKey}")
    {
        MessageKey = messageKey;
        Errors = errors;
        Args = args;
    }
}