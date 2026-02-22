using System.Globalization;
using BurnRate.Converters;

namespace BurnRate.Tests.Converters;

public class EqualityConverterTests
{
    private readonly EqualityConverter _converter = new();

    private bool Convert(object[] values)
        => (bool)_converter.Convert(values, typeof(bool), null!, CultureInfo.InvariantCulture);

    [Fact]
    public void SameReference_ReturnsTrue()
    {
        var obj = new object();
        var values = new[] { obj, obj };
        Assert.True(Convert(values));
    }

    [Fact]
    public void DifferentReferences_ReturnsFalse()
    {
        var values = new object[] { new object(), new object() };
        Assert.False(Convert(values));
    }

    [Fact]
    public void ArrayOfOneItem_ReturnsFalse()
    {
        var values = new object[] { new object() };
        Assert.False(Convert(values));
    }

    [Fact]
    public void ArrayOfThreeItems_ReturnsFalse()
    {
        var values = new object[] { new object(), new object(), new object() };
        Assert.False(Convert(values));
    }

    [Fact]
    public void ConvertBack_ThrowsNotSupportedException()
    {
        Assert.Throws<NotSupportedException>(() =>
            _converter.ConvertBack(true, new[] { typeof(object), typeof(object) }, null!, CultureInfo.InvariantCulture)
        );
    }
}
