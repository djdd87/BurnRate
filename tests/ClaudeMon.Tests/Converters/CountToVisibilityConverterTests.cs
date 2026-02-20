using System.Globalization;
using System.Windows;
using ClaudeMon.Converters;

namespace ClaudeMon.Tests.Converters;

public class CountToVisibilityConverterTests
{
    private readonly CountToVisibilityConverter _converter = new();

    private Visibility Convert(object value)
        => (Visibility)_converter.Convert(value, typeof(Visibility), null!, CultureInfo.InvariantCulture);

    [Fact]
    public void CountZero_ReturnsCollapsed()
    {
        Assert.Equal(Visibility.Collapsed, Convert(0));
    }

    [Fact]
    public void CountOne_ReturnsCollapsed()
    {
        Assert.Equal(Visibility.Collapsed, Convert(1));
    }

    [Fact]
    public void CountTwo_ReturnsVisible()
    {
        Assert.Equal(Visibility.Visible, Convert(2));
    }

    [Fact]
    public void CountTen_ReturnsVisible()
    {
        Assert.Equal(Visibility.Visible, Convert(10));
    }

    [Fact]
    public void NonInt_ReturnsCollapsed()
    {
        Assert.Equal(Visibility.Collapsed, Convert("not a number"));
    }

    [Fact]
    public void ConvertBack_ReturnsUnsetValue()
    {
        var result = _converter.ConvertBack(Visibility.Visible, typeof(int), null!, CultureInfo.InvariantCulture);
        Assert.Equal(DependencyProperty.UnsetValue, result);
    }
}
