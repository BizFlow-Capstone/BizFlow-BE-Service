namespace BizFlow.Application.Common.Exceptions;

public class NotFoundException : Exception
{
    public string MessageKey { get; }
    public object[] Args { get; }

    public NotFoundException(string messageKey, params object[] args)
        : base($"Resource not found: {messageKey}")
    {
        MessageKey = messageKey;
        Args = args;
    }
}