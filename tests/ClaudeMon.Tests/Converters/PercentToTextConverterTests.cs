using System.Globalization;
using System.Windows;
using ClaudeMon.Converters;

namespace ClaudeMon.Tests.Converters;

public class PercentToTextConverterTests
{
    private readonly PercentToTextConverter _converter = new();

    private string Convert(object value, object? parameter = null)
        => (string)_converter.Convert(value, typeof(string), parameter ?? string.Empty, CultureInfo.InvariantCulture);

    [Fact]
    public void NegativeValue_ReturnsQuestion()
    {
        Assert.Equal("?", Convert(-1.0));
    }

    [Fact]
    public void NonDouble_ReturnsQuestion()
    {
        Assert.Equal("?", Convert("not a number"));
    }

    [Fact]
    public void ZeroPercent_ReturnsZeroPercent()
    {
        Assert.Equal("0%", Convert(0.0));
    }

    [Fact]
    public void FiftyPercent_ReturnsFiftyPercent()
    {
        Assert.Equal("50%", Convert(50.0));
    }

    [Fact]
    public void NinetyNinePointFour_ReturnsNinetyNinePercent()
    {
        Assert.Equal("99%", Convert(99.4));
    }

    [Fact]
    public void NinetyNinePointFive_ReturnsLimit()
    {
        Assert.Equal("Limit", Convert(99.5));
    }

    [Fact]
    public void HundredPercent_ReturnsLimit()
    {
        Assert.Equal("Limit", Convert(100.0));
    }

    [Fact]
    public void WithPrefixParameter_PrependsPrefixToValue()
    {
        Assert.Equal("~50%", Convert(50.0, "~"));
    }

    [Fact]
    public void ConvertBack_ReturnsUnsetValue()
    {
        var result = _converter.ConvertBack("0%", typeof(double), null!, CultureInfo.InvariantCulture);
        Assert.Equal(DependencyProperty.UnsetValue, result);
    }
}
