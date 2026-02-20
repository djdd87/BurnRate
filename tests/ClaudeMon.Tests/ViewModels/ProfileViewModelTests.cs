using System.Reflection;
using ClaudeMon.ViewModels;

namespace ClaudeMon.Tests.ViewModels;

public class ProfileViewModelTests
{
    // Helper to invoke private static methods via reflection
    private static object? InvokeStaticMethod(string methodName, Type[] parameterTypes, params object[] parameters)
    {
        var method = typeof(ProfileViewModel).GetMethod(
            methodName,
            BindingFlags.NonPublic | BindingFlags.Static,
            null,
            parameterTypes,
            null);

        if (method == null)
            throw new InvalidOperationException($"Method {methodName} not found on ProfileViewModel");

        return method.Invoke(null, parameters);
    }

    // ────────────────────────────────────────────────────────
    // FormatPct Tests
    // ────────────────────────────────────────────────────────

    [Fact]
    public void FormatPct_ZeroPercent_ReturnsZeroPercent()
    {
        var result = (string)InvokeStaticMethod("FormatPct", new[] { typeof(double) }, 0.0)!;
        Assert.Equal("0%", result);
    }

    [Fact]
    public void FormatPct_FiftyPercent_ReturnsFiftyPercent()
    {
        var result = (string)InvokeStaticMethod("FormatPct", new[] { typeof(double) }, 50.0)!;
        Assert.Equal("50%", result);
    }

    [Fact]
    public void FormatPct_NinetyNinePointFour_ReturnsNinetyNinePercent()
    {
        var result = (string)InvokeStaticMethod("FormatPct", new[] { typeof(double) }, 99.4)!;
        Assert.Equal("99%", result);
    }

    [Fact]
    public void FormatPct_NinetyNinePointFive_ReturnsLimit()
    {
        var result = (string)InvokeStaticMethod("FormatPct", new[] { typeof(double) }, 99.5)!;
        Assert.Equal("Limit", result);
    }

    [Fact]
    public void FormatPct_HundredPercent_ReturnsLimit()
    {
        var result = (string)InvokeStaticMethod("FormatPct", new[] { typeof(double) }, 100.0)!;
        Assert.Equal("Limit", result);
    }

    // ────────────────────────────────────────────────────────
    // FormatPct edge cases
    // ────────────────────────────────────────────────────────

    [Fact]
    public void FormatPct_OnePercent_ReturnsOnePercent()
    {
        var result = (string)InvokeStaticMethod("FormatPct", new[] { typeof(double) }, 1.0)!;
        Assert.Equal("1%", result);
    }

    [Fact]
    public void FormatPct_NinetyNinePointFour9_ReturnsNinetyNinePercent()
    {
        var result = (string)InvokeStaticMethod("FormatPct", new[] { typeof(double) }, 99.49)!;
        Assert.Equal("99%", result);
    }

    [Fact]
    public void FormatPct_JustAtThreshold_ReturnsLimit()
    {
        var result = (string)InvokeStaticMethod("FormatPct", new[] { typeof(double) }, 99.50)!;
        Assert.Equal("Limit", result);
    }

    [Fact]
    public void FormatPct_OverHundred_ReturnsLimit()
    {
        var result = (string)InvokeStaticMethod("FormatPct", new[] { typeof(double) }, 150.0)!;
        Assert.Equal("Limit", result);
    }
}
