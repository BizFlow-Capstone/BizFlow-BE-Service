namespace BizFlow.Application.DTOs.Reference
{
    /// <summary>
    /// Represents a reference / lookup option returned to the client.
    /// <para><c>Code</c> is the internal enum string used by the backend
    /// (e.g. <c>import_cost</c>) and is what the FE/client must send back
    /// in subsequent requests.</para>
    /// <para><c>Label</c> is the Vietnamese display text intended for UI only.</para>
    /// </summary>
    public sealed record ReferenceOptionDto(string Code, string Label);
}
