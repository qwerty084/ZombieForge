using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ZombieForge.ViewModels;
using ZombieForge.Views;

namespace ZombieForge
{
    public sealed partial class MainWindow : Window
    {
        public MainViewModel ViewModel { get; }

        public MainWindow()
        {
            InitializeComponent();
            ViewModel = new MainViewModel(DispatcherQueue);
            NavView.SelectedItem = NavView.MenuItems[0];
            ContentFrame.Navigate(typeof(HomePage));
        }

        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected)
            {
                ContentFrame.Navigate(typeof(SettingsPage));
                return;
            }

            if (args.SelectedItem is NavigationViewItem item)
            {
                Type page = item.Tag switch
                {
                    "home"    => typeof(HomePage),
                    "config"  => typeof(ConfigPage),
                    "history" => typeof(HistoryPage),
                    _         => typeof(HomePage)
                };
                ContentFrame.Navigate(page);
            }
        }
    }
}
