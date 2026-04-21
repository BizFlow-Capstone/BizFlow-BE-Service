namespace BizFlow.Application.Common.Interfaces
{
    /// <summary>
    /// Resolves human-readable labels for reference/enum codes depending on
    /// the current request language (vi/en) derived from the Accept-Language
    /// header, mirroring the <see cref="IMessageService"/> mechanism.
    /// </summary>
    public interface IReferenceLabelService
    {
        /// <summary>
        /// Returns the label for the given <paramref name="category"/> and
        /// <paramref name="code"/> in the current request language. Falls back
        /// to the default language then to <paramref name="code"/> itself when
        /// no translation is configured.
        /// </summary>
        string GetLabel(string category, string code);
    }
}
