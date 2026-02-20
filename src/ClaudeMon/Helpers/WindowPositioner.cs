using System.Windows;

namespace ClaudeMon.Helpers;

public static class WindowPositioner
{
    private const double Margin = 8.0;

    public static void PositionNearTray(Window window)
    {
        var workArea = SystemParameters.WorkArea;

        window.Left = workArea.Right - window.Width - Margin;
        window.Top = workArea.Bottom - window.Height - Margin;
    }
}
