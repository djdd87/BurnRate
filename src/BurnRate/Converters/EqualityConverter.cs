using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace BurnRate.Converters;

/// <summary>
/// IMultiValueConverter that returns true if two bound values are the same reference.
/// Used for highlighting the selected profile tab.
/// </summary>
public sealed class EqualityConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length == 2)
            return ReferenceEquals(values[0], values[1]);
        return false;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
