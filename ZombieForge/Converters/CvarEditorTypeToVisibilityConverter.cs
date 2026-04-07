using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using ZombieForge.Services.Games;

namespace ZombieForge.Converters
{
    /// <summary>
    /// Converts a <see cref="CvarEditorType"/> to <see cref="Visibility"/>.
    /// Pass the target type name as the converter parameter (e.g. "Slider").
    /// Returns Visible when the value matches, Collapsed otherwise.
    /// </summary>
    public class CvarEditorTypeToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is CvarEditorType editorType && parameter is string typeName &&
                Enum.TryParse<CvarEditorType>(typeName, out var target))
            {
                return editorType == target ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotImplementedException();
    }
}
