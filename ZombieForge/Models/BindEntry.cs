using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ZombieForge.Models
{
    /// <summary>
    /// Represents a single key-to-command binding entry from the BO1 config.
    /// </summary>
    public class BindEntry : INotifyPropertyChanged
    {
        private string _key;
        private string _command;

        /// <summary>
        /// Occurs when a bind field changes.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Initializes a new bind entry.
        /// </summary>
        /// <param name="key">The bound key name.</param>
        /// <param name="command">The command executed by the key.</param>
        public BindEntry(string key, string command)
        {
            _key = key;
            _command = command;
        }

        /// <summary>
        /// Gets or sets the bound key name.
        /// </summary>
        public string Key
        {
            get => _key;
            set { if (_key != value) { _key = value; OnPropertyChanged(); } }
        }

        /// <summary>
        /// Gets or sets the command executed by the key.
        /// </summary>
        public string Command
        {
            get => _command;
            set { if (_command != value) { _command = value; OnPropertyChanged(); } }
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
