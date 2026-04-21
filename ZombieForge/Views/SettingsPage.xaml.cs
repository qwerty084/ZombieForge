using Microsoft.UI.Xaml.Controls;
using ZombieForge.ViewModels;

namespace ZombieForge.Views
{
    /// <summary>
    /// Displays application settings controls.
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        /// <summary>
        /// Gets the view model that backs this page.
        /// </summary>
        public SettingsViewModel ViewModel { get; } = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsPage"/> class.
        /// </summary>
        public SettingsPage()
        {
            InitializeComponent();
        }
    }
}
