using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace ZombieForge.Converters
{
    /// <summary>
    /// Converts a bool to Visibility, inverted: false → Visible, true → Collapsed.
    /// Used to show controls when a condition is false.
    /// </summary>
    public class InverseBoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
            => (bool)value ? Visibility.Collapsed : Visibility.Visible;

        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => (Visibility)value == Visibility.Collapsed;
    }
}
