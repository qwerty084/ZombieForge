using Microsoft.UI.Xaml.Controls;
using ZombieForge.ViewModels;

namespace ZombieForge.Views
{
    public sealed partial class SettingsPage : Page
    {
        public SettingsViewModel ViewModel { get; } = new();

        public SettingsPage()
        {
            InitializeComponent();
        }
    }
}
