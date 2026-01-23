namespace BizFlow.Application.Common.Exceptions;

public class UnauthorizedException : Exception
{
    public string MessageKey { get; }

    public UnauthorizedException(string messageKey = "COMMON_UNAUTHORIZED")
        : base($"Unauthorized: {messageKey}")
    {
        MessageKey = messageKey;
    }
}