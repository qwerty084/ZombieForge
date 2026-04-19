using System;
using System.ComponentModel;
using Microsoft.UI.Xaml.Controls;
using ZombieForge.Models;
using ZombieForge.ViewModels;

namespace ZombieForge.Views
{
    public sealed partial class HomePage : Page
    {
        public HomeViewModel ViewModel { get; }

        private readonly MainViewModel _mainVm;

        public HomePage()
        {
            InitializeComponent();

            _mainVm = App.MainWindow!.ViewModel;
            ViewModel = new HomeViewModel(DispatcherQueue, _mainVm.ActiveHandler, _mainVm.HistoryTracker);

            _mainVm.GameEventReceived   += OnGameEvent;
            _mainVm.PropertyChanged     += OnMainVmPropertyChanged;

            Unloaded += (_, _) =>
            {
                _mainVm.GameEventReceived  -= OnGameEvent;
                _mainVm.PropertyChanged    -= OnMainVmPropertyChanged;
                ViewModel.Dispose();
            };
        }

        private void OnGameEvent(object? sender, GameEventArgs e)
            => ViewModel.OnGameEvent(e);

        private void OnMainVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.ActiveHandler))
                ViewModel.SetHandler(_mainVm.ActiveHandler);
        }
    }
}
