namespace BizFlow.Application.Common.Utilities;

/// <summary>
/// Masks email/phone/google identifiers for API responses (admin list/detail, account info).
/// </summary>
public static class CredentialMasking
{
    public static string MaskIdentifier(string type, string identifier)
    {
        return type switch
        {
            "email" => MaskEmail(identifier),
            "phone" => MaskPhone(identifier),
            "google" => "Connected",
            _ => "***"
        };
    }

    public static string MaskEmail(string email)
    {
        var parts = email.Split('@');
        if (parts.Length != 2) return "***@***";
        var name = parts[0];
        var masked = name.Length <= 2
            ? name + "***"
            : name[..2] + new string('*', name.Length - 2);
        return masked + "@" + parts[1];
    }

    public static string MaskPhone(string phone)
    {
        if (phone.Length <= 4) return "***";
        return phone[..4] + new string('*', phone.Length - 7) + phone[^3..];
    }
}
