using System.Text.Json;
using BizFlow.Application.Common.Interfaces;

namespace BizFlow.Infrastructure.Services
{
    /// <summary>
    /// Loads reference labels from per-language JSON resource files and
    /// resolves them based on the request language exposed by
    /// <see cref="IMessageService"/>.
    /// </summary>
    public class ReferenceLabelService : IReferenceLabelService
    {
        private const string DefaultLanguage = "vi";

        private readonly IMessageService _messageService;

        // language -> category -> code -> label
        private readonly Dictionary<string, Dictionary<string, Dictionary<string, string>>> _labels;

        public ReferenceLabelService(IMessageService messageService)
        {
            _messageService = messageService;
            _labels = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>(StringComparer.OrdinalIgnoreCase);
            LoadLabels();
        }

        private void LoadLabels()
        {
            LoadLanguage("vi", "ReferenceLabels.vi.json");
            LoadLanguage("en", "ReferenceLabels.en.json");
        }

        private void LoadLanguage(string language, string fileName)
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Common", "Resources", fileName);
            if (!File.Exists(path))
            {
                _labels[language] = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
                return;
            }

            var json = File.ReadAllText(path);
            var raw = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(json)
                      ?? new Dictionary<string, Dictionary<string, string>>();

            var normalized = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var (category, codes) in raw)
            {
                // Inner dictionary is case-insensitive so codes declared as
                // "DRAFT" / "draft" both resolve even if callers vary casing.
                normalized[category] = new Dictionary<string, string>(codes, StringComparer.OrdinalIgnoreCase);
            }

            _labels[language] = normalized;
        }

        public string GetLabel(string category, string code)
        {
            var lang = _messageService.GetCurrentLanguage();

            if (TryResolve(lang, category, code, out var label)) return label;
            if (!string.Equals(lang, DefaultLanguage, StringComparison.OrdinalIgnoreCase)
                && TryResolve(DefaultLanguage, category, code, out label))
            {
                return label;
            }

            // Fall back to the raw code so missing translations are obvious
            // instead of silently returning null/empty.
            return code;
        }

        private bool TryResolve(string language, string category, string code, out string label)
        {
            label = string.Empty;
            if (!_labels.TryGetValue(language, out var categories)) return false;
            if (!categories.TryGetValue(category, out var codes)) return false;
            if (!codes.TryGetValue(code, out var found)) return false;
            label = found;
            return true;
        }
    }
}
