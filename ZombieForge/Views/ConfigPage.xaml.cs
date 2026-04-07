using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ZombieForge.ViewModels;

namespace ZombieForge.Views
{
    public sealed partial class ConfigPage : Page
    {
        public ConfigViewModel ViewModel { get; } = new();

        public ConfigPage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            ViewModel.AutoDetect();
        }

        private void AutoDetect_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.AutoDetect();
        }

        private async void Browse_Click(object sender, RoutedEventArgs e)
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.FileTypeFilter.Add(".cfg");
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;

            // Associate the picker with the window handle (required in WinUI 3)
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var file = await picker.PickSingleFileAsync();
            if (file is not null)
                ViewModel.LoadFile(file.Path);
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.Save();
        }

        private void Discard_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.Discard();
        }
    }
}
