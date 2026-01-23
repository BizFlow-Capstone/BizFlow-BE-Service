namespace BizFlow.Application.Common.Exceptions;

public class ForbiddenException : Exception
{
    public string MessageKey { get; }

    public ForbiddenException(string messageKey = "COMMON_FORBIDDEN")
        : base($"Forbidden: {messageKey}")
    {
        MessageKey = messageKey;
    }
}