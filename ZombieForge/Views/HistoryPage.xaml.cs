using System;
using Microsoft.UI.Xaml.Controls;
using ZombieForge.Services;
using ZombieForge.ViewModels;

namespace ZombieForge.Views
{
    /// <summary>
    /// Hosts the game-history page.
    /// </summary>
    public sealed partial class HistoryPage : Page
    {
        public HistoryViewModel ViewModel { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="HistoryPage"/> class.
        /// </summary>
        public HistoryPage()
        {
            InitializeComponent();

            var mainVm = App.MainWindow!.ViewModel;
            ViewModel = new HistoryViewModel(mainVm.HistoryTracker);

            Unloaded += (_, _) => ViewModel.Dispose();
        }

        /// <summary>Formats the "All" filter option display text. Used from XAML x:Bind.</summary>
        public static string FormatMapFilterOption(string value)
            => string.IsNullOrEmpty(value)
                ? LocalizationService.GetString("HistoryFilterAll")
                : value;

        /// <summary>Formats a UTC timestamp as a local date/time string. Used from XAML x:Bind.</summary>
        public static string FormatSessionDate(DateTime utcDate)
            => utcDate.ToLocalTime().ToString("g");

        /// <summary>Formats a round number with a label prefix. Used from XAML x:Bind.</summary>
        public static string FormatRound(int round)
            => $"{LocalizationService.GetString("HistoryRoundLabel")} {round}";
    }
}
