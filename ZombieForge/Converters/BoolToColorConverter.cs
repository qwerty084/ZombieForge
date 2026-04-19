using System;
using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace ZombieForge.Converters
{
    /// <summary>
    /// Converts Boolean values into status colors for UI indicators.
    /// </summary>
    public class BoolToColorConverter : IValueConverter
    {
        /// <summary>
        /// Converts a Boolean status to a themed brush.
        /// </summary>
        /// <param name="value">The source value to convert.</param>
        /// <param name="targetType">The target binding type.</param>
        /// <param name="parameter">An optional converter parameter.</param>
        /// <param name="language">The current culture name.</param>
        /// <returns>A brush that represents the status value.</returns>
        public object Convert(object value, Type targetType, object parameter, string language)
            => value is bool b && b
                ? new SolidColorBrush(Colors.LimeGreen)
                : new SolidColorBrush(Colors.Gray);

        /// <summary>
        /// Converts a brush value back to the source value.
        /// </summary>
        /// <param name="value">The value produced by the target.</param>
        /// <param name="targetType">The target source type.</param>
        /// <param name="parameter">An optional converter parameter.</param>
        /// <param name="language">The current culture name.</param>
        /// <returns>This method never returns.</returns>
        /// <exception cref="NotImplementedException">The reverse conversion is not supported.</exception>
        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotImplementedException();
    }
}
