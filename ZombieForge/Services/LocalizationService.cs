using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Windows.ApplicationModel.Resources;
using Windows.Globalization;
using Windows.Storage;
using ZombieForge.Models;

namespace ZombieForge.Services
{
    /// <summary>
    /// Manages UI language selection. Call <see cref="Initialize"/> once at startup,
    /// before the first window is created.
    /// </summary>
    public static class LocalizationService
    {
        private const string SettingsKey = "LanguageOverride";

        // Single authoritative list of named languages (excludes the system-default entry).
        // Add a new LanguageOption here to register it; validation and SupportedLanguages are derived from this.
        private static readonly LanguageOption[] _namedLanguages = [LanguageOption.English, LanguageOption.German];
        private static readonly ILogger _logger = App.LoggerFactory.CreateLogger(nameof(LocalizationService));
        private static bool _didLogUnpackagedSettingsWarning;

        private static ResourceLoader? _loader;

        public static IReadOnlyList<LanguageOption> SupportedLanguages { get; private set; } = [];

        /// <summary>
        /// The override that was actually applied at startup (set once; never changes at runtime).
        /// </summary>
        public static string ActiveOverride { get; private set; } = string.Empty;

        /// <summary>
        /// The currently saved override tag ("en-US", "de-DE", or "" for system default).
        /// Updated whenever the user changes the setting.
        /// </summary>
        public static string CurrentOverride { get; private set; } = string.Empty;

        /// <summary>
        /// Sets <see cref="ApplicationLanguages.PrimaryLanguageOverride"/> from the saved
        /// user preference (or empty string so the OS language is used with automatic
        /// WinRT fallback to the default package language).
        /// </summary>
        public static void Initialize()
        {
            var settings = TryGetLocalSettings();
            var saved = settings is not null
                && settings.Values.TryGetValue(SettingsKey, out var raw)
                && raw is string s
                ? s
                : string.Empty;

            // Validate against _namedLanguages; fall back to system default if unrecognized.
            var isKnown = saved.Length == 0;
            if (!isKnown)
                foreach (var lang in _namedLanguages)
                    if (lang.Tag == saved) { isKnown = true; break; }

            CurrentOverride = isKnown ? saved : string.Empty;
            ActiveOverride = CurrentOverride;
            ApplicationLanguages.PrimaryLanguageOverride = CurrentOverride;

            // GetForViewIndependentUse is safe to call before any window exists.
            _loader = ResourceLoader.GetForViewIndependentUse();

            var all = new LanguageOption[_namedLanguages.Length + 1];
            all[0] = new(string.Empty, GetString("LanguageSystemDefault"));
            for (var i = 0; i < _namedLanguages.Length; i++)
                all[i + 1] = _namedLanguages[i];
            SupportedLanguages = all;
        }

        /// <summary>Saves the selected language tag and updates the in-memory state.</summary>
        /// <param name="tag">BCP-47 tag ("en-US", "de-DE") or empty string for system default.</param>
        public static void SaveOverride(string tag)
        {
            CurrentOverride = tag;

            var settings = TryGetLocalSettings();
            if (settings is not null)
                settings.Values[SettingsKey] = tag;
        }

        /// <summary>Looks up a resource string by key (for use in C# code).</summary>
        public static string GetString(string key)
        {
            // ResourceLoader is initialized in Initialize(); return key as fallback if called early.
            if (_loader is null) return key;
            var value = _loader.GetString(key);
            return string.IsNullOrEmpty(value) ? key : value;
        }

        private static ApplicationDataContainer? TryGetLocalSettings()
        {
            try
            {
                return ApplicationData.Current.LocalSettings;
            }
            catch (COMException ex)
            {
                if (!_didLogUnpackagedSettingsWarning)
                {
                    _logger.LogWarning(ex, "ApplicationData.Current is unavailable. Localization settings will use in-memory fallback only");
                    _didLogUnpackagedSettingsWarning = true;
                }

                return null;
            }
        }
    }
}
