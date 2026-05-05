using System.Security.Cryptography;
using Microsoft.Extensions.Logging;

namespace BizFlow.Application.Common.Helpers;

public static class EntityCodeGenerator
{
    private static readonly ILogger Logger =
        LoggerFactory
            .Create(builder => builder.AddConsole())
            .CreateLogger("EntityCodeGenerator");

    public static string Generate(string prefix, DateTime date, int locationId, int randomDigits)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            throw new ArgumentException("Prefix is required.", nameof(prefix));
        if (randomDigits <= 0)
            throw new ArgumentOutOfRangeException(nameof(randomDigits), "Random digits must be greater than zero.");

        var normalizedPrefix = prefix.Trim().ToUpperInvariant();
        var randomPart = GenerateRandomDigits(randomDigits);
        return $"{normalizedPrefix}-{date:ddMMyyyy}-{locationId}{randomPart}";
    }

    private static string GenerateRandomDigits(int digits)
    {
        var chars = new char[digits];
        for (var i = 0; i < digits; i++)
        {
            chars[i] = (char)('0' + RandomNumberGenerator.GetInt32(0, 10));
        }

        return new string(chars);
    }

    public static async Task<T> ExecuteWithDuplicateKeyRetryAsync<T>(Func<Task<T>> operation, int maxAttempts = 5)
    {
        if (maxAttempts < 1)
            throw new ArgumentOutOfRangeException(nameof(maxAttempts), "Max attempts must be greater than zero.");

        Exception? lastDuplicateException = null;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex) when (IsDuplicateKeyException(ex))
            {
                lastDuplicateException = ex;
                Logger.LogWarning(
                    ex,
                    "Duplicate key detected when generating entity code. Retry attempt {Attempt}/{MaxAttempts}.",
                    attempt,
                    maxAttempts);
                if (attempt == maxAttempts)
                    throw;
            }
        }

        throw lastDuplicateException ?? new InvalidOperationException("Retry operation failed unexpectedly.");
    }

    public static async Task ExecuteWithDuplicateKeyRetryAsync(Func<Task> operation, int maxAttempts = 5)
    {
        if (maxAttempts < 1)
            throw new ArgumentOutOfRangeException(nameof(maxAttempts), "Max attempts must be greater than zero.");

        Exception? lastDuplicateException = null;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await operation();
                return;
            }
            catch (Exception ex) when (IsDuplicateKeyException(ex))
            {
                lastDuplicateException = ex;
                Logger.LogWarning(
                    ex,
                    "Duplicate key detected when generating entity code. Retry attempt {Attempt}/{MaxAttempts}.",
                    attempt,
                    maxAttempts);
                if (attempt == maxAttempts)
                    throw;
            }
        }

        throw lastDuplicateException ?? new InvalidOperationException("Retry operation failed unexpectedly.");
    }

    private static bool IsDuplicateKeyException(Exception ex)
    {
        for (var current = ex; current != null; current = current.InnerException)
        {
            var message = current.Message;
            if (message.Contains("Duplicate entry", StringComparison.OrdinalIgnoreCase)
                || message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase)
                || message.Contains("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
