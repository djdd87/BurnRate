using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using ClaudeMon.Models;

namespace ClaudeMon.Views.Controls;

/// <summary>
/// A 7-day bar chart showing daily message activity.
/// Each column shows a vertical bar (token height proportional to max) with a day label below.
/// </summary>
public partial class ActivityChart : UserControl
{
    /// <summary>
    /// Maximum bar area height in pixels (total chart height minus space for labels).
    /// </summary>
    private const double MaxBarHeight = 94.0;

    /// <summary>
    /// Minimum bar height so that zero-value days still show a visual baseline.
    /// </summary>
    private const double MinBarHeight = 2.0;

    /// <summary>
    /// Accent color for the bars, matching the AccentPrimary theme color.
    /// </summary>
    private static readonly SolidColorBrush BarBrush;

    /// <summary>
    /// Slightly transparent version of the accent for hover/visual variety.
    /// </summary>
    private static readonly SolidColorBrush BarBrushToday;

    static ActivityChart()
    {
        BarBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D97706"));
        BarBrush.Freeze();

        BarBrushToday = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B"));
        BarBrushToday.Freeze();
    }

    public static readonly DependencyProperty DailyDataProperty =
        DependencyProperty.Register(
            nameof(DailyData),
            typeof(List<DailyActivitySummary>),
            typeof(ActivityChart),
            new PropertyMetadata(null, OnDailyDataChanged));

    /// <summary>
    /// List of daily activity records. Setting this triggers chart redraw.
    /// </summary>
    public List<DailyActivitySummary> DailyData
    {
        get => (List<DailyActivitySummary>)GetValue(DailyDataProperty);
        set => SetValue(DailyDataProperty, value);
    }

    public ActivityChart()
    {
        InitializeComponent();
    }

    private static void OnDailyDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ActivityChart control)
        {
            control.UpdateChart();
        }
    }

    /// <summary>
    /// Rebuilds the chart bars from the current DailyData.
    /// Creates ChartBarItem records, computes relative heights, and populates the UniformGrid.
    /// </summary>
    private void UpdateChart()
    {
        BarGrid.Children.Clear();

        if (DailyData is null || DailyData.Count == 0)
        {
            EmptyState.Visibility = Visibility.Visible;
            return;
        }

        EmptyState.Visibility = Visibility.Collapsed;

        // Determine the maximum token value for proportional scaling.
        long maxTokens = DailyData.Max(d => d.Tokens);
        if (maxTokens == 0) maxTokens = 1; // Avoid division by zero.

        DateTime today = DateTime.Today;

        foreach (var day in DailyData)
        {
            var item = new ChartBarItem
            {
                DayLabel = day.Date.ToString("ddd", CultureInfo.InvariantCulture),
                BarHeight = (double)day.Tokens / maxTokens,
                Tooltip = $"{day.Date:ddd MMM d}: {day.Messages:N0} msgs, {FormatTokenCount(day.Tokens)}"
            };

            bool isToday = day.Date.Date == today;

            // Build the visual column: a DockPanel with label at bottom and bar above.
            var column = new DockPanel
            {
                Margin = new Thickness(2, 0, 2, 0)
            };

            // Day label at the bottom.
            var label = new TextBlock
            {
                Text = item.DayLabel,
                FontSize = 10,
                Foreground = isToday
                    ? (Brush)FindResource("TextPrimaryBrush")
                    : (Brush)FindResource("TextMutedBrush"),
                FontWeight = isToday ? FontWeights.SemiBold : FontWeights.Normal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 4, 0, 0)
            };
            DockPanel.SetDock(label, Dock.Bottom);
            column.Children.Add(label);

            // Bar area: a grid to align the bar at the bottom.
            var barContainer = new Grid
            {
                VerticalAlignment = VerticalAlignment.Bottom
            };

            double computedHeight = Math.Max(item.BarHeight * MaxBarHeight, MinBarHeight);

            var bar = new Border
            {
                Height = computedHeight,
                Background = isToday ? BarBrushToday : BarBrush,
                CornerRadius = new CornerRadius(2, 2, 0, 0),
                VerticalAlignment = VerticalAlignment.Bottom,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(4, 0, 4, 0),
                ToolTip = item.Tooltip
            };

            barContainer.Children.Add(bar);
            column.Children.Add(barContainer);

            BarGrid.Children.Add(column);
        }

        // Update the UniformGrid column count to match the actual data count.
        BarGrid.Columns = DailyData.Count;
    }

    /// <summary>
    /// Formats a token count into a human-readable string.
    /// </summary>
    private static string FormatTokenCount(long tokens)
    {
        return tokens switch
        {
            >= 1_000_000 => $"{tokens / 1_000_000.0:0.#}M tokens",
            >= 1_000 => $"{tokens / 1_000.0:0.#}K tokens",
            _ => $"{tokens:N0} tokens"
        };
    }
}

/// <summary>
/// Represents a single day column in the activity chart.
/// </summary>
public class ChartBarItem
{
    /// <summary>
    /// Day abbreviation (e.g., "Mon", "Tue").
    /// </summary>
    public string DayLabel { get; set; } = string.Empty;

    /// <summary>
    /// Bar height as a fraction of the maximum (0.0 to 1.0).
    /// </summary>
    public double BarHeight { get; set; }

    /// <summary>
    /// Descriptive tooltip (e.g., "Mon Feb 17: 1,410 msgs, 85.4K tokens").
    /// </summary>
    public string Tooltip { get; set; } = string.Empty;
}
