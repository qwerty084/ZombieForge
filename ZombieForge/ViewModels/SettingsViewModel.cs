using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ZombieForge.Models;
using ZombieForge.Services;

namespace ZombieForge.ViewModels
{
    /// <summary>
    /// Manages language selection and restart-notice state for the Settings page.
    /// </summary>
    public class SettingsViewModel : INotifyPropertyChanged
    {
        private LanguageOption _selectedLanguage;
        private bool _restartRequired;

        /// <summary>
        /// Occurs when a bindable property value changes.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Gets the available language options.
        /// </summary>
        public IReadOnlyList<LanguageOption> Languages => LocalizationService.SupportedLanguages;

        /// <summary>
        /// Gets or sets the currently selected language option.
        /// </summary>
        public LanguageOption SelectedLanguage
        {
            get => _selectedLanguage;
            set
            {
                if (_selectedLanguage == value) return;
                _selectedLanguage = value;
                OnPropertyChanged();
                ApplySelection(value);
            }
        }

        /// <summary>
        /// Gets a value that indicates whether an app restart is required for the selected language to take effect.
        /// </summary>
        public bool RestartRequired
        {
            get => _restartRequired;
            private set
            {
                if (_restartRequired == value) return;
                _restartRequired = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsViewModel"/> class.
        /// </summary>
        public SettingsViewModel()
        {
            // Pre-select the option that matches the current saved override.
            var current = LocalizationService.CurrentOverride;
            _selectedLanguage = FindByTag(current) ?? LocalizationService.SupportedLanguages[0];

            // Persist restart banner state: if the user already changed the language
            // in a previous visit to Settings, the restart notice should still show.
            _restartRequired = current != LocalizationService.ActiveOverride;
        }

        private void ApplySelection(LanguageOption option)
        {
            LocalizationService.SaveOverride(option.Tag);

            // Show the restart notice only when the selection differs from what was
            // actually applied at startup — that's the language currently visible in the UI.
            RestartRequired = option.Tag != LocalizationService.ActiveOverride;
        }

        private static LanguageOption? FindByTag(string tag)
        {
            foreach (var lang in LocalizationService.SupportedLanguages)
                if (lang.Tag == tag) return lang;
            return null;
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
