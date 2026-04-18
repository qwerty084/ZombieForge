using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ZombieForge.Models
{
    public class BindEntry : INotifyPropertyChanged
    {
        private string _key;
        private string _command;

        public event PropertyChangedEventHandler? PropertyChanged;

        public BindEntry(string key, string command)
        {
            _key = key;
            _command = command;
        }

        public string Key
        {
            get => _key;
            set { if (_key != value) { _key = value; OnPropertyChanged(); } }
        }

        public string Command
        {
            get => _command;
            set { if (_command != value) { _command = value; OnPropertyChanged(); } }
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
