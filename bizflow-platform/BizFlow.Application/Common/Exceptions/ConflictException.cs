namespace BizFlow.Application.Common.Exceptions;

public class ConflictException : Exception
{
    public string MessageKey { get; }
    public object[] Args { get; }

    public ConflictException(string messageKey, params object[] args)
        : base($"Conflict: {messageKey}")
    {
        MessageKey = messageKey;
        Args = args;
    }
}