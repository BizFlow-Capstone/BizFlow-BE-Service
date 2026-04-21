namespace BizFlow.Application.DTOs.Reference
{
    /// <summary>
    /// Standardized response shape for enum-like reference fields
    /// (e.g. statuses, types, payment methods).
    /// <para>
    /// <c>Code</c> is the stable internal identifier used by the backend
    /// (for example <c>import_cost</c>, <c>pending</c>, <c>cash</c>).
    /// Clients MUST echo this exact value back on create/update/filter requests —
    /// the backend only accepts <c>Code</c>, never <c>Label</c>.
    /// </para>
    /// <para>
    /// <c>Label</c> is the human-readable display text resolved by the
    /// <c>IReferenceLabelService</c> based on the <c>Accept-Language</c> request
    /// header (<c>vi</c> / <c>en</c>). It is intended for UI rendering only
    /// and MUST NOT be used as a business key or stored on the client side as
    /// the source of truth.
    /// </para>
    /// </summary>
    /// <param name="Code">Stable backend code (language-independent).</param>
    /// <param name="Label">Localized display text for UI only.</param>
    public sealed record ReferenceOptionDto(string Code, string Label);
}
