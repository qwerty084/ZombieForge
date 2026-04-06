using System;
using Microsoft.UI.Xaml.Controls;
using ZombieForge.Models;
using ZombieForge.ViewModels;

namespace ZombieForge.Views
{
    public sealed partial class HomePage : Page
    {
        public HomeViewModel ViewModel { get; }

        public HomePage()
        {
            InitializeComponent();

            ViewModel = new HomeViewModel(DispatcherQueue);

            var mainVm = App.MainWindow!.ViewModel;
            mainVm.GameEventReceived += OnGameEvent;

            Unloaded += (_, _) =>
            {
                mainVm.GameEventReceived -= OnGameEvent;
                ViewModel.Dispose();
            };
        }

        private void OnGameEvent(object? sender, GameEventArgs e)
        {
            ViewModel.OnGameEvent(e);
        }
    }
}
