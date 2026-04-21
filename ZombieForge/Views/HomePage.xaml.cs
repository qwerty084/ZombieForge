using System;
using System.ComponentModel;
using Microsoft.UI.Xaml.Controls;
using ZombieForge.Models;
using ZombieForge.ViewModels;

namespace ZombieForge.Views
{
    /// <summary>
    /// Displays live zombie run stats and event activity.
    /// </summary>
    public sealed partial class HomePage : Page
    {
        /// <summary>
        /// Gets the view model that backs this page.
        /// </summary>
        public HomeViewModel ViewModel { get; }

        private readonly MainViewModel _mainVm;

        /// <summary>
        /// Initializes a new instance of the <see cref="HomePage"/> class.
        /// </summary>
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
