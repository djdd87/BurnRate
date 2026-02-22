using System.Reflection;
using BurnRate.Services;

namespace BurnRate.Tests.Services;

public class TrayIconServiceTests
{
    // Helper to invoke private static methods via reflection
    private static object? InvokeStaticMethod(string methodName, Type[] parameterTypes, params object[] parameters)
    {
        var method = typeof(TrayIconService).GetMethod(
            methodName,
            BindingFlags.NonPublic | BindingFlags.Static,
            null,
            parameterTypes,
            null);

        if (method == null)
            throw new InvalidOperationException($"Method {methodName} not found on TrayIconService");

        return method.Invoke(null, parameters);
    }

    // ────────────────────────────────────────────────────────
    // GetDisplayText Tests
    // ────────────────────────────────────────────────────────

    [Fact]
    public void GetDisplayText_NegativePercentage_ReturnsQuestionMark()
    {
        var result = (string)InvokeStaticMethod("GetDisplayText", new[] { typeof(double) }, -1.0)!;
        Assert.Equal("?", result);
    }

    [Fact]
    public void GetDisplayText_ZeroPercent_ReturnsZero()
    {
        var result = (string)InvokeStaticMethod("GetDisplayText", new[] { typeof(double) }, 0.0)!;
        Assert.Equal("0", result);
    }

    [Fact]
    public void GetDisplayText_FiftyPercent_ReturnsFifty()
    {
        var result = (string)InvokeStaticMethod("GetDisplayText", new[] { typeof(double) }, 50.0)!;
        Assert.Equal("50", result);
    }

    [Fact]
    public void GetDisplayText_NinetyNinePointFive_RoundsToHundred()
    {
        var result = (string)InvokeStaticMethod("GetDisplayText", new[] { typeof(double) }, 99.5)!;
        Assert.Equal("100", result);
    }

    [Fact]
    public void GetDisplayText_NinetyNinePointFour_ReturnsNinetyNine()
    {
        var result = (string)InvokeStaticMethod("GetDisplayText", new[] { typeof(double) }, 99.4)!;
        Assert.Equal("99", result);
    }

    [Fact]
    public void GetDisplayText_HundredPercent_ReturnsExclamation()
    {
        var result = (string)InvokeStaticMethod("GetDisplayText", new[] { typeof(double) }, 100.0)!;
        Assert.Equal("!", result);
    }

    [Fact]
    public void GetDisplayText_OverHundredPercent_ReturnsExclamation()
    {
        var result = (string)InvokeStaticMethod("GetDisplayText", new[] { typeof(double) }, 150.0)!;
        Assert.Equal("!", result);
    }

    // ────────────────────────────────────────────────────────
    // GetFontSize Tests
    // ────────────────────────────────────────────────────────

    [Fact]
    public void GetFontSize_SingleCharAt32px_ReturnsScaledSize()
    {
        var result = (float)InvokeStaticMethod("GetFontSize", new[] { typeof(string), typeof(int) }, "1", 32)!;
        var expected = 32f * 0.72f; // 23.04
        Assert.Equal(expected, result, 2);
    }

    [Fact]
    public void GetFontSize_TwoCharsAt32px_ReturnsScaledSize()
    {
        var result = (float)InvokeStaticMethod("GetFontSize", new[] { typeof(string), typeof(int) }, "99", 32)!;
        var expected = 32f * 0.65f; // 20.8
        Assert.Equal(expected, result, 2);
    }

    [Fact]
    public void GetFontSize_ThreeCharsAt32px_ReturnsScaledSize()
    {
        var result = (float)InvokeStaticMethod("GetFontSize", new[] { typeof(string), typeof(int) }, "100", 32)!;
        var expected = 32f * 0.55f; // 17.6
        Assert.Equal(expected, result, 2);
    }

    [Fact]
    public void GetFontSize_TinyIconSize_EnforcesMinimumOf8()
    {
        var result = (float)InvokeStaticMethod("GetFontSize", new[] { typeof(string), typeof(int) }, "abc", 10)!;
        Assert.Equal(8.0f, result);
    }

    [Fact]
    public void GetFontSize_LargeIcon_ScalesProportionally()
    {
        var result = (float)InvokeStaticMethod("GetFontSize", new[] { typeof(string), typeof(int) }, "1", 64)!;
        var expected = 64f * 0.72f; // 46.08
        Assert.Equal(expected, result, 2);
    }

    // ────────────────────────────────────────────────────────
    // GetStatusColor Tests
    // ────────────────────────────────────────────────────────

    [Fact]
    public void GetStatusColor_NegativePercentage_ReturnsGrey()
    {
        var result = (System.Drawing.Color)InvokeStaticMethod("GetStatusColor", new[] { typeof(double) }, -1.0)!;
        Assert.Equal(System.Drawing.Color.FromArgb(158, 158, 158), result);
    }

    [Fact]
    public void GetStatusColor_ZeroPercent_ReturnsGreen()
    {
        var result = (System.Drawing.Color)InvokeStaticMethod("GetStatusColor", new[] { typeof(double) }, 0.0)!;
        Assert.Equal(System.Drawing.Color.FromArgb(76, 175, 80), result);
    }

    [Fact]
    public void GetStatusColor_FiftyPercent_ReturnsGreen()
    {
        var result = (System.Drawing.Color)InvokeStaticMethod("GetStatusColor", new[] { typeof(double) }, 50.0)!;
        Assert.Equal(System.Drawing.Color.FromArgb(76, 175, 80), result);
    }

    [Fact]
    public void GetStatusColor_FiftyOnePercent_ReturnsAmber()
    {
        var result = (System.Drawing.Color)InvokeStaticMethod("GetStatusColor", new[] { typeof(double) }, 51.0)!;
        Assert.Equal(System.Drawing.Color.FromArgb(255, 152, 0), result);
    }

    [Fact]
    public void GetStatusColor_EightyPercent_ReturnsAmber()
    {
        var result = (System.Drawing.Color)InvokeStaticMethod("GetStatusColor", new[] { typeof(double) }, 80.0)!;
        Assert.Equal(System.Drawing.Color.FromArgb(255, 152, 0), result);
    }

    [Fact]
    public void GetStatusColor_EightyOnePercent_ReturnsRed()
    {
        var result = (System.Drawing.Color)InvokeStaticMethod("GetStatusColor", new[] { typeof(double) }, 81.0)!;
        Assert.Equal(System.Drawing.Color.FromArgb(244, 67, 54), result);
    }

    [Fact]
    public void GetStatusColor_HundredPercent_ReturnsRed()
    {
        var result = (System.Drawing.Color)InvokeStaticMethod("GetStatusColor", new[] { typeof(double) }, 100.0)!;
        Assert.Equal(System.Drawing.Color.FromArgb(244, 67, 54), result);
    }

    // ────────────────────────────────────────────────────────
    // Color boundary conditions
    // ────────────────────────────────────────────────────────

    [Fact]
    public void GetStatusColor_JustBelowFifty_IsGreen()
    {
        var result = (System.Drawing.Color)InvokeStaticMethod("GetStatusColor", new[] { typeof(double) }, 49.9)!;
        Assert.Equal(System.Drawing.Color.FromArgb(76, 175, 80), result);
    }

    [Fact]
    public void GetStatusColor_JustBelowEighty_IsAmber()
    {
        var result = (System.Drawing.Color)InvokeStaticMethod("GetStatusColor", new[] { typeof(double) }, 79.9)!;
        Assert.Equal(System.Drawing.Color.FromArgb(255, 152, 0), result);
    }

    // ────────────────────────────────────────────────────────
    // Public Method Tests
    // ────────────────────────────────────────────────────────

    private static void RunOnStaThread(Action action)
    {
        Exception? exception = null;
        var thread = new Thread(() =>
        {
            try { action(); }
            catch (Exception ex) { exception = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (exception != null) throw exception;
    }

    [Fact]
    public void CreateNotifyIcon_ReturnsValidIcon()
    {
        using var service = new TrayIconService();
        var icon = service.CreateNotifyIcon(50.0);

        Assert.NotNull(icon);
        Assert.True(icon.Width > 0);
        Assert.True(icon.Height > 0);
    }

    [Fact]
    public void CreateNotifyIcon_NegativePercentage_ReturnsIcon()
    {
        using var service = new TrayIconService();
        var icon = service.CreateNotifyIcon(-1.0);

        Assert.NotNull(icon);
        Assert.True(icon.Width > 0);
        Assert.True(icon.Height > 0);
    }

    [Fact]
    public void CreateNotifyIcon_100Percent_ReturnsIcon()
    {
        using var service = new TrayIconService();
        var icon = service.CreateNotifyIcon(100.0);

        Assert.NotNull(icon);
        Assert.True(icon.Width > 0);
        Assert.True(icon.Height > 0);
    }

    [Fact]
    public void CreateNotifyIcon_MultipleCallsCleansPrevious()
    {
        using var service = new TrayIconService();
        var icon1 = service.CreateNotifyIcon(25.0);
        Assert.NotNull(icon1);

        var icon2 = service.CreateNotifyIcon(75.0);
        Assert.NotNull(icon2);

        // Second call should have cleaned up the first handle without throwing
        // If cleanup fails, Icon disposal would throw
        icon1.Dispose();
        icon2.Dispose();
    }

    [Fact]
    public void Dispose_CleansUpResources()
    {
        var service = new TrayIconService();
        var icon = service.CreateNotifyIcon(50.0);
        Assert.NotNull(icon);

        // Should dispose without throwing
        service.Dispose();
        icon.Dispose();
    }

    [Fact]
    public void Dispose_TwiceDoesNotThrow()
    {
        var service = new TrayIconService();
        service.CreateNotifyIcon(50.0);

        // First dispose
        service.Dispose();

        // Second dispose should not throw
        service.Dispose();
    }

    [Fact]
    public void CreateIcon_ReturnsValidImageSource()
    {
        // CreateIcon uses WPF's Imaging.CreateBitmapSourceFromHBitmap,
        // which requires STA thread. Run the test on an STA thread.
        RunOnStaThread(() =>
        {
            using var service = new TrayIconService();
            var imageSource = service.CreateIcon(50.0);

            Assert.NotNull(imageSource);
            Assert.True(imageSource.Width > 0);
            Assert.True(imageSource.Height > 0);
        });
    }
}
