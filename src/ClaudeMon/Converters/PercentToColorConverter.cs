using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace ClaudeMon.Converters;

public class PercentToColorConverter : IValueConverter
{
    private static readonly SolidColorBrush GreenBrush = new(Color.FromRgb(0x4C, 0xAF, 0x50));
    private static readonly SolidColorBrush AmberBrush = new(Color.FromRgb(0xFF, 0x98, 0x00));
    private static readonly SolidColorBrush RedBrush = new(Color.FromRgb(0xF4, 0x43, 0x36));
    private static readonly SolidColorBrush GreyBrush = new(Color.FromRgb(0x9E, 0x9E, 0x9E));

    static PercentToColorConverter()
    {
        GreenBrush.Freeze();
        AmberBrush.Freeze();
        RedBrush.Freeze();
        GreyBrush.Freeze();
    }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not double percentage)
            return GreyBrush;

        return percentage switch
        {
            >= 0 and <= 50 => GreenBrush,
            > 50 and <= 80 => AmberBrush,
            > 80 and <= 100 => RedBrush,
            _ => GreyBrush
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return DependencyProperty.UnsetValue;
    }
}
