using Microsoft.UI.Xaml;
using ZombieForge.ViewModels;

namespace ZombieForge
{
    public sealed partial class MainWindow : Window
    {
        public MainViewModel ViewModel { get; }

        public MainWindow()
        {
            InitializeComponent();
            ViewModel = new MainViewModel(DispatcherQueue);
        }
    }
}
