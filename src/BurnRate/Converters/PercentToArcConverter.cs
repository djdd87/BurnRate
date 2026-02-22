using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace BurnRate.Converters;

public class PercentToArcConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2
            || values[0] is not double percentage
            || values[1] is not double radius)
        {
            return Geometry.Empty;
        }

        // Clamp percentage to 0-100
        percentage = Math.Clamp(percentage, 0.0, 100.0);

        if (percentage is 0.0)
            return Geometry.Empty;

        double sweepAngle = percentage / 100.0 * 360.0;

        // Start at the top of the circle (12 o'clock = -90 degrees in standard math coords)
        double startAngleRad = -Math.PI / 2.0;
        double endAngleRad = startAngleRad + (sweepAngle * Math.PI / 180.0);

        double centreX = radius;
        double centreY = radius;

        var startPoint = new Point(
            centreX + radius * Math.Cos(startAngleRad),
            centreY + radius * Math.Sin(startAngleRad));

        var endPoint = new Point(
            centreX + radius * Math.Cos(endAngleRad),
            centreY + radius * Math.Sin(endAngleRad));

        bool isLargeArc = sweepAngle > 180.0;

        var geometry = new StreamGeometry();

        using (var context = geometry.Open())
        {
            context.BeginFigure(startPoint, false, false);
            context.ArcTo(
                endPoint,
                new Size(radius, radius),
                0,
                isLargeArc,
                SweepDirection.Clockwise,
                true,
                false);
        }

        geometry.Freeze();
        return geometry;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        return [DependencyProperty.UnsetValue, DependencyProperty.UnsetValue];
    }
}
