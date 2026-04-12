using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ZombieForge.Services;
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
            Title = LocalizationService.GetString("AppTitle");
            ViewModel = new MainViewModel(DispatcherQueue);

            Closed += (_, _) => ViewModel.Dispose();

            // Defer navigation so App.MainWindow is assigned before child pages access it.
            ContentFrame.Loaded += (_, _) =>
            {
                NavView.SelectedItem = NavView.MenuItems[0];
                ContentFrame.Navigate(typeof(HomePage));
            };
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
