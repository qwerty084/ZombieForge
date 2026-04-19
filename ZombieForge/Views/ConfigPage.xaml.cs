using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ZombieForge.Models;
using ZombieForge.ViewModels;

namespace ZombieForge.Views
{
    /// <summary>
    /// Displays config editing tools for BO1 dvars and keybinds.
    /// </summary>
    public sealed partial class ConfigPage : Page
    {
        /// <summary>
        /// Gets the view model that backs this page.
        /// </summary>
        public ConfigViewModel ViewModel { get; } = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigPage"/> class.
        /// </summary>
        public ConfigPage()
        {
            InitializeComponent();
        }

        private void RemoveBindButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { Tag: BindEntry entry })
                ViewModel.RemoveBindCommand.Execute(entry);
        }

        private void BindPreset_Click(object sender, RoutedEventArgs e)
        {
            if (PresetsList.SelectedItem is not PresetCommand preset)
            {
                ShowInfoDialog("No preset selected", "Please select a preset from the list first.");
                return;
            }

            if (PresetKeyCombo.SelectedItem is not string key)
            {
                ShowInfoDialog("No key selected", "Please select a key from the dropdown first.");
                return;
            }

            ViewModel.AddOrUpdateBind(key, preset.Command);
        }

        private void AddCustomBind_Click(object sender, RoutedEventArgs e)
        {
            var keyBox     = new ComboBox { PlaceholderText = "Key", ItemsSource = ViewModel.BindableKeys, Width = 140 };
            var commandBox = new TextBox  { PlaceholderText = "Command (e.g. setviewpos 0 0 0)", Width = 320 };

            var panel = new StackPanel { Spacing = 12 };
            panel.Children.Add(new TextBlock { Text = "Key" });
            panel.Children.Add(keyBox);
            panel.Children.Add(new TextBlock { Text = "Command", Margin = new Thickness(0, 4, 0, 0) });
            panel.Children.Add(commandBox);

            var dialog = new ContentDialog
            {
                Title             = "Add Custom Bind",
                Content           = panel,
                PrimaryButtonText = "Add",
                CloseButtonText   = "Cancel",
                XamlRoot          = XamlRoot,
                DefaultButton     = ContentDialogButton.Primary,
            };

            dialog.PrimaryButtonClick += (_, _) =>
            {
                var key     = keyBox.SelectedItem as string ?? string.Empty;
                var command = commandBox.Text.Trim();

                if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(command))
                    ViewModel.AddOrUpdateBind(key, command);
            };

            _ = dialog.ShowAsync();
        }

        private void EditBindButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { Tag: BindEntry entry }) return;

            var commandBox = new TextBox
            {
                Text              = entry.Command,
                PlaceholderText   = "Command",
                Width             = 360,
                AcceptsReturn     = false,
            };

            var panel = new StackPanel { Spacing = 8 };
            panel.Children.Add(new TextBlock
            {
                Text  = $"Key: {entry.Key}",
                Style = (Style)Application.Current.Resources["BodyStrongTextBlockStyle"],
            });
            panel.Children.Add(new TextBlock { Text = "Command" });
            panel.Children.Add(commandBox);

            var dialog = new ContentDialog
            {
                Title             = "Edit Bind",
                Content           = panel,
                PrimaryButtonText = "Save",
                CloseButtonText   = "Cancel",
                XamlRoot          = XamlRoot,
                DefaultButton     = ContentDialogButton.Primary,
            };

            dialog.PrimaryButtonClick += (_, _) =>
            {
                var command = commandBox.Text.Trim();
                if (!string.IsNullOrWhiteSpace(command))
                    ViewModel.AddOrUpdateBind(entry.Key, command);
            };

            _ = dialog.ShowAsync();
        }

        private void ShowInfoDialog(string title, string message)
        {
            var dialog = new ContentDialog
            {
                Title           = title,
                Content         = message,
                CloseButtonText = "OK",
                XamlRoot        = XamlRoot,
            };
            _ = dialog.ShowAsync();
        }
    }
}
