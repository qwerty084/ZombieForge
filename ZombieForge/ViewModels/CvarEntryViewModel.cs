using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ZombieForge.Services.Games;

namespace ZombieForge.ViewModels
{
    public class CvarEntryViewModel : INotifyPropertyChanged
    {
        private string _value = string.Empty;

        public event PropertyChangedEventHandler? PropertyChanged;

        public string FriendlyName { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public string Category { get; init; } = string.Empty;
        public string CvarName { get; init; } = string.Empty;
        public CvarEditorType EditorType { get; init; }
        public double Min { get; init; }
        public double Max { get; init; }
        public double Step { get; init; } = 1;
        public IReadOnlyList<string>? AllowedValues { get; init; }

        public string Value
        {
            get => _value;
            set
            {
                if (_value == value) return;
                _value = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ValueAsDouble));
                ValueChanged?.Invoke(this, value);
            }
        }

        /// <summary>Numeric value for Slider/NumberBox two-way binding.</summary>
        public double ValueAsDouble
        {
            get => double.TryParse(_value, System.Globalization.NumberStyles.Any,
                       System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : 0;
            set
            {
                var str = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
                if (_value == str) return;
                _value = str;
                OnPropertyChanged(nameof(Value));
                OnPropertyChanged();
                ValueChanged?.Invoke(this, _value);
            }
        }

        /// <summary>Convenience property for Toggle-type CVars that map "1"/"0" to bool.</summary>
        public bool IsEnabled
        {
            get => _value == "1";
            set
            {
                var str = value ? "1" : "0";
                if (_value == str) return;
                _value = str;
                OnPropertyChanged(nameof(Value));
                OnPropertyChanged();
                ValueChanged?.Invoke(this, _value);
            }
        }

        /// <summary>Raised when the user changes the value. Arg is the new string value.</summary>
        public event System.EventHandler<string>? ValueChanged;

        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
