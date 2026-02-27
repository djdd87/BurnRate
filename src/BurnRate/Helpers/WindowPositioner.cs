using System.Windows;

namespace BurnRate.Helpers;

public static class WindowPositioner
{
    private const double Margin = 8.0;

    public static void PositionNearTray(Window window)
    {
        var workArea = SystemParameters.WorkArea;

        window.Left = workArea.Right - window.ActualWidth - Margin;
        window.Top = workArea.Bottom - window.ActualHeight - Margin;
    }
}
