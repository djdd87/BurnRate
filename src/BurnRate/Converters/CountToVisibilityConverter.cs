using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace BurnRate.Converters;

/// <summary>
/// Converts a count to Visibility. Shows element only when count > 1 (multiple profiles).
/// </summary>
public sealed class CountToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int count && count > 1)
            return Visibility.Visible;
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => DependencyProperty.UnsetValue;
}
