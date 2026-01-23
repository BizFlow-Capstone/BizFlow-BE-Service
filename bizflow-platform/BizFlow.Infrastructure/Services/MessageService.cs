using BizFlow.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace BizFlow.Infrastructure.Services
{
    public class MessageService : IMessageService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly Dictionary<string, Dictionary<string, string>> _messages;
        private const string DefaultLanguage = "vi";
        private const string LanguageHeader = "Accept-Language";

        public MessageService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
            _messages = new Dictionary<string, Dictionary<string, string>>();
            LoadMessages();
        }

        private void LoadMessages()
        {
            // Load Vietnamese messages
            var viPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Common", "Resources", "Messages.vi.json");
            if (File.Exists(viPath))
            {
                var viJson = File.ReadAllText(viPath);
                _messages["vi"] = JsonSerializer.Deserialize<Dictionary<string, string>>(viJson) ?? new();
            }

            // Load English messages
            var enPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Common", "Resources", "Messages.en.json");
            if (File.Exists(enPath))
            {
                var enJson = File.ReadAllText(enPath);
                _messages["en"] = JsonSerializer.Deserialize<Dictionary<string, string>>(enJson) ?? new();
            }
        }

        public string GetCurrentLanguage()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) return DefaultLanguage;

            // header Accept-Language
            var acceptLanguage = httpContext.Request.Headers[LanguageHeader].FirstOrDefault();
            if (!string.IsNullOrEmpty(acceptLanguage))
            {
                var lang = acceptLanguage.Split(',').FirstOrDefault()?.Trim().ToLower();
                if (lang != null && (lang.StartsWith("vi") || lang.StartsWith("en")))
                {
                    return lang.StartsWith("vi") ? "vi" : "en";
                }
            }

            // query string (fallback)
            var queryLang = httpContext.Request.Query["lang"].FirstOrDefault();
            if (!string.IsNullOrEmpty(queryLang))
            {
                return queryLang.ToLower() == "en" ? "en" : "vi";
            }

            return DefaultLanguage;
        }

        public void SetLanguage(string languageCode)
        {
            // if need to save to session/cookie
        }

        public string GetMessage(string key)
        {
            var lang = GetCurrentLanguage();
            if (_messages.TryGetValue(lang, out var messages) && messages.TryGetValue(key, out var message))
            {
                return message;
            }

            // Fallback to Vietnamese
            if (_messages.TryGetValue(DefaultLanguage, out var fallbackMessages) && fallbackMessages.TryGetValue(key, out var fallbackMessage))
            {
                return fallbackMessage;
            }

            return key; // Return key if message not found
        }

        public string GetMessage(string key, params object[] args)
        {
            var message = GetMessage(key);
            try
            {
                return string.Format(message, args);
            }
            catch
            {
                return message;
            }
        }
    }
}
