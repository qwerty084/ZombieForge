using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace ZombieForge.Converters
{
    /// <summary>Converts a bool to Visibility (true → Visible, false → Collapsed).</summary>
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
            => (bool)value ? Visibility.Visible : Visibility.Collapsed;

        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => (Visibility)value == Visibility.Visible;
    }
}
