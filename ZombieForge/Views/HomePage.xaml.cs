using Microsoft.UI.Xaml.Controls;
using ZombieForge.ViewModels;

namespace ZombieForge.Views
{
    public sealed partial class HomePage : Page
    {
        private readonly HomeViewModel _viewModel = new();

        public HomePage()
        {
            InitializeComponent();
        }
    }
}
