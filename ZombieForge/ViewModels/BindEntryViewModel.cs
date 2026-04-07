using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ZombieForge.Services.Games;

namespace ZombieForge.ViewModels
{
    public class BindEntryViewModel : INotifyPropertyChanged
    {
        private string _selectedKey = string.Empty;

        public event PropertyChangedEventHandler? PropertyChanged;

        public string FriendlyName { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public string Category { get; init; } = string.Empty;
        public string Command { get; init; } = string.Empty;

        /// <summary>All valid key names shown in the picker, plus an "(Unbound)" option at index 0.</summary>
        public IReadOnlyList<string> AvailableKeys { get; } = BuildKeyList();

        public string SelectedKey
        {
            get => _selectedKey;
            set
            {
                if (_selectedKey == value) return;
                _selectedKey = value;
                OnPropertyChanged();
                KeyChanged?.Invoke(this, value);
            }
        }

        /// <summary>Raised when the user picks a different key. Arg is the new key name, or empty string = unbound.</summary>
        public event System.EventHandler<string>? KeyChanged;

        private static List<string> BuildKeyList()
        {
            var list = new List<string> { "(Unbound)" };
            list.AddRange(BO1ConfigCatalog.KeyNames);
            return list;
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
