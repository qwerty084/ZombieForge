using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace ZombieForge.Converters
{
    /// <summary>
    /// Converts Boolean values to <see cref="Visibility"/> values.
    /// </summary>
    public class BoolToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Converts a Boolean value to <see cref="Visibility.Visible"/> or <see cref="Visibility.Collapsed"/>.
        /// </summary>
        /// <param name="value">The source value to convert.</param>
        /// <param name="targetType">The target binding type.</param>
        /// <param name="parameter">An optional converter parameter.</param>
        /// <param name="language">The current culture name.</param>
        /// <returns>A visibility value mapped from <paramref name="value"/>.</returns>
        public object Convert(object value, Type targetType, object parameter, string language)
            => value is bool b && b ? Visibility.Visible : Visibility.Collapsed;

        /// <summary>
        /// Converts a visibility value back to a Boolean value.
        /// </summary>
        /// <param name="value">The value produced by the target.</param>
        /// <param name="targetType">The target source type.</param>
        /// <param name="parameter">An optional converter parameter.</param>
        /// <param name="language">The current culture name.</param>
        /// <returns><see langword="true" /> if <paramref name="value"/> is <see cref="Visibility.Visible"/>; otherwise, <see langword="false" />.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => value is Visibility v && v == Visibility.Visible;
    }
}
