using System.Text;

namespace BizFlow.Application.Common.Utilities;

/// <summary>
/// Builds string variants for matching phone numbers stored in <c>Credential.Identifier</c>
/// (E.164 or near E.164) when the user types different formats (0..., +84..., +1..., digits-only, etc.).
/// </summary>
public static class PhoneSearchNormalizer
{
    /// <summary>
    /// Returns substrings to use with <c>Identifier.Contains(v)</c>.
    /// Duplicates are removed and empty strings omitted.
    /// </summary>
    public static IReadOnlyList<string> GetSearchVariants(string? searchInput)
    {
        if (string.IsNullOrWhiteSpace(searchInput))
            return Array.Empty<string>();

        var trimmed = searchInput.Trim();
        var set = new HashSet<string>(StringComparer.Ordinal);

        void Add(string? s)
        {
            if (string.IsNullOrEmpty(s))
                return;
            set.Add(s);
        }

        Add(trimmed);

        var digits = ExtractDigits(trimmed);
        if (digits.Length > 0)
        {
            Add(digits);
            Add("+" + digits);

            // Vietnam: 0... ↔ +84... / 84... (any length after 0, e.g. 0999 → +84999)
            if (digits.StartsWith('0'))
            {
                var national = digits[1..];
                Add("+84" + national);
                Add("84" + national);
            }

            // Already starts with country code 84 (no leading +)
            if (digits.StartsWith("84", StringComparison.Ordinal))
            {
                Add("+" + digits);
                var after84 = digits[2..];
                Add("0" + after84);
            }
        }

        return set.Where(s => s.Length > 0).ToList();
    }

    private static string ExtractDigits(string input)
    {
        var sb = new StringBuilder(input.Length);
        foreach (var ch in input)
        {
            if (char.IsDigit(ch))
                sb.Append(ch);
        }

        return sb.ToString();
    }
}
