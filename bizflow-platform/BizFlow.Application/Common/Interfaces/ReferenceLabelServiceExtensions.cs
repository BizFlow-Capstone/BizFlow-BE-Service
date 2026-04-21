using BizFlow.Application.DTOs.Reference;

namespace BizFlow.Application.Common.Interfaces
{
    /// <summary>
    /// Convenience helpers to build <see cref="ReferenceOptionDto"/> from
    /// raw enum codes without repeating null / whitespace checks at call sites.
    /// </summary>
    public static class ReferenceLabelServiceExtensions
    {
        /// <summary>
        /// Returns a <see cref="ReferenceOptionDto"/> whose <c>code</c> is the
        /// original backend value and <c>label</c> is resolved via the current
        /// request language. Used for required enum fields.
        /// </summary>
        public static ReferenceOptionDto ToOption(
            this IReferenceLabelService labelService,
            string category,
            string code)
        {
            return new ReferenceOptionDto(code, labelService.GetLabel(category, code));
        }

        /// <summary>
        /// Same as <see cref="ToOption"/> but returns <c>null</c> when the
        /// backing code is null or whitespace. Used for optional enum fields.
        /// </summary>
        public static ReferenceOptionDto? ToOptionOrNull(
            this IReferenceLabelService labelService,
            string category,
            string? code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return null;

            return new ReferenceOptionDto(code, labelService.GetLabel(category, code));
        }
    }
}
