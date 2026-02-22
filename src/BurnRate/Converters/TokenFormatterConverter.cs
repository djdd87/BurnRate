using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace BurnRate.Converters;

public class TokenFormatterConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        long tokens = value switch
        {
            long l => l,
            int i => i,
            double d => (long)d,
            _ => 0
        };

        return tokens switch
        {
            < 1_000 => tokens.ToString(),
            < 1_000_000 => $"{tokens / 1_000.0:0.#}K",
            _ => $"{tokens / 1_000_000.0:0.#}M"
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return DependencyProperty.UnsetValue;
    }
}
