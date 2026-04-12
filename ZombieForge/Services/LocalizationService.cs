using System.Collections.Generic;
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

        private static ResourceLoader? _loader;

        public static IReadOnlyList<LanguageOption> SupportedLanguages { get; } =
        [
            LanguageOption.SystemDefault,
            LanguageOption.English,
            LanguageOption.German,
        ];

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
            var settings = ApplicationData.Current.LocalSettings;
            CurrentOverride = settings.Values.TryGetValue(SettingsKey, out var saved)
                ? (string)saved
                : string.Empty;

            ActiveOverride = CurrentOverride;
            ApplicationLanguages.PrimaryLanguageOverride = CurrentOverride;

            _loader = new ResourceLoader();
        }

        /// <summary>Saves the selected language tag and updates the in-memory state.</summary>
        /// <param name="tag">BCP-47 tag ("en-US", "de-DE") or empty string for system default.</param>
        public static void SaveOverride(string tag)
        {
            CurrentOverride = tag;
            ApplicationData.Current.LocalSettings.Values[SettingsKey] = tag;
        }

        /// <summary>Looks up a resource string by key (for use in C# code).</summary>
        public static string GetString(string key)
        {
            // ResourceLoader is initialized in Initialize(); return key as fallback if called early.
            if (_loader is null) return key;
            var value = _loader.GetString(key);
            return string.IsNullOrEmpty(value) ? key : value;
        }
    }
}
