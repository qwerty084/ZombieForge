using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ZombieForge.Models;
using ZombieForge.Services;

namespace ZombieForge.ViewModels
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        private LanguageOption _selectedLanguage;
        private bool _restartRequired;

        public event PropertyChangedEventHandler? PropertyChanged;

        public IReadOnlyList<LanguageOption> Languages => LocalizationService.SupportedLanguages;

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
