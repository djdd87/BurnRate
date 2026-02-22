using BurnRate.Models;

namespace BurnRate.Tests.Models;

/// <summary>
/// Tests for UsageSummary model, including UpdateFrom method and default values.
/// </summary>
public class UsageSummaryTests
{
    [Fact]
    public void Constructor_InitializesDefaultValues()
    {
        // Arrange & Act
        var summary = new UsageSummary();

        // Assert
        Assert.Equal(0d, summary.EstimatedPercentage);
        Assert.Equal(0L, summary.WeeklyTokensUsed);
        Assert.Equal(0L, summary.WeeklyTokenLimit);
        Assert.Equal(string.Empty, summary.RateLimitTier);
        Assert.Equal(string.Empty, summary.SubscriptionType);
        Assert.Equal(0, summary.TodayMessages);
        Assert.Equal(0L, summary.TodayTokens);
        Assert.Equal(0, summary.TodaySessions);
        Assert.Equal(0, summary.TotalSessions);
        Assert.Equal(0, summary.TotalMessages);
        Assert.NotNull(summary.ModelBreakdown);
        Assert.Empty(summary.ModelBreakdown);
        Assert.NotNull(summary.DailyActivity);
        Assert.Empty(summary.DailyActivity);
        Assert.Null(summary.LastDataDate);
        Assert.Equal(default(DateTime), summary.LastRefreshTime);
        Assert.Equal(0d, summary.EstimatedCostUsd);
        Assert.Equal("—", summary.TimeSavedFormatted);
        Assert.False(summary.IsLive);
        Assert.Equal(0d, summary.SessionPercentage);
        Assert.Null(summary.SessionResetsAt);
        Assert.Equal(0d, summary.WeeklyPercentage);
        Assert.Null(summary.WeeklyResetsAt);
    }

    [Fact]
    public void UpdateFrom_CopiesAllProperties()
    {
        // Arrange
        var source = new UsageSummary
        {
            EstimatedPercentage = 45.5,
            WeeklyTokensUsed = 1250000,
            WeeklyTokenLimit = 2500000,
            RateLimitTier = "default_claude_max_5x",
            SubscriptionType = "pro",
            TodayMessages = 42,
            TodayTokens = 567890,
            TodaySessions = 5,
            TotalSessions = 128,
            TotalMessages = 3456,
            ModelBreakdown = new Dictionary<string, long> { ["claude-3-5-sonnet"] = 450000, ["claude-3-opus"] = 217890 },
            DailyActivity = new List<DailyActivitySummary>
            {
                new(DateTime.Parse("2026-02-20"), 10, 150000),
                new(DateTime.Parse("2026-02-19"), 8, 120000)
            },
            LastDataDate = DateTime.Parse("2026-02-20"),
            LastRefreshTime = DateTime.Parse("2026-02-20T16:30:00Z"),
            EstimatedCostUsd = 12.45,
            TimeSavedFormatted = "2.5h",
            IsLive = true,
            SessionPercentage = 35.0,
            SessionResetsAt = DateTime.Parse("2026-02-20T20:00:00Z"),
            WeeklyPercentage = 50.0,
            WeeklyResetsAt = DateTime.Parse("2026-02-27T00:00:00Z")
        };

        var target = new UsageSummary();

        // Act
        target.UpdateFrom(source);

        // Assert
        Assert.Equal(source.EstimatedPercentage, target.EstimatedPercentage);
        Assert.Equal(source.WeeklyTokensUsed, target.WeeklyTokensUsed);
        Assert.Equal(source.WeeklyTokenLimit, target.WeeklyTokenLimit);
        Assert.Equal(source.RateLimitTier, target.RateLimitTier);
        Assert.Equal(source.SubscriptionType, target.SubscriptionType);
        Assert.Equal(source.TodayMessages, target.TodayMessages);
        Assert.Equal(source.TodayTokens, target.TodayTokens);
        Assert.Equal(source.TodaySessions, target.TodaySessions);
        Assert.Equal(source.TotalSessions, target.TotalSessions);
        Assert.Equal(source.TotalMessages, target.TotalMessages);
        Assert.Equal(source.ModelBreakdown, target.ModelBreakdown);
        Assert.Equal(source.DailyActivity, target.DailyActivity);
        Assert.Equal(source.LastDataDate, target.LastDataDate);
        Assert.Equal(source.LastRefreshTime, target.LastRefreshTime);
        Assert.Equal(source.EstimatedCostUsd, target.EstimatedCostUsd);
        Assert.Equal(source.TimeSavedFormatted, target.TimeSavedFormatted);
        Assert.Equal(source.IsLive, target.IsLive);
        Assert.Equal(source.SessionPercentage, target.SessionPercentage);
        Assert.Equal(source.SessionResetsAt, target.SessionResetsAt);
        Assert.Equal(source.WeeklyPercentage, target.WeeklyPercentage);
        Assert.Equal(source.WeeklyResetsAt, target.WeeklyResetsAt);
    }

    [Fact]
    public void UpdateFrom_FiresPropertyChangedEvents()
    {
        // Arrange
        var source = new UsageSummary
        {
            EstimatedPercentage = 75.0,
            TodayMessages = 15,
            RateLimitTier = "pro_max"
        };

        var target = new UsageSummary();
        var propertyChangedEvents = new List<string>();

        target.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName != null)
            {
                propertyChangedEvents.Add(e.PropertyName);
            }
        };

        // Act
        target.UpdateFrom(source);

        // Assert
        Assert.NotEmpty(propertyChangedEvents);
        // Should fire events for all modified properties (those with non-default values in source)
        Assert.Contains("EstimatedPercentage", propertyChangedEvents);
        Assert.Contains("TodayMessages", propertyChangedEvents);
        Assert.Contains("RateLimitTier", propertyChangedEvents);
    }

    [Fact]
    public void UpdateFrom_WithNullDates()
    {
        // Arrange
        var source = new UsageSummary
        {
            LastDataDate = null,
            SessionResetsAt = null,
            WeeklyResetsAt = null
        };

        var target = new UsageSummary
        {
            LastDataDate = DateTime.Parse("2026-02-20"),
            SessionResetsAt = DateTime.Parse("2026-02-20T20:00:00Z"),
            WeeklyResetsAt = DateTime.Parse("2026-02-27T00:00:00Z")
        };

        // Act
        target.UpdateFrom(source);

        // Assert
        Assert.Null(target.LastDataDate);
        Assert.Null(target.SessionResetsAt);
        Assert.Null(target.WeeklyResetsAt);
    }

    [Fact]
    public void UpdateFrom_PreservesWpfBinding()
    {
        // Arrange
        var source = new UsageSummary { TodayMessages = 25 };
        var target = new UsageSummary { TodayMessages = 10 };

        var propertyChangeCount = 0;
        target.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == "TodayMessages")
            {
                propertyChangeCount++;
            }
        };

        // Act
        target.UpdateFrom(source);

        // Assert - WPF binding should be notified once
        Assert.Equal(1, propertyChangeCount);
        Assert.Equal(25, target.TodayMessages);
    }

    [Fact]
    public void DailyActivitySummary_RecordEquality()
    {
        // Arrange
        var date = DateTime.Parse("2026-02-20");
        var activity1 = new DailyActivitySummary(date, 10, 150000);
        var activity2 = new DailyActivitySummary(date, 10, 150000);
        var activity3 = new DailyActivitySummary(date, 11, 150000);

        // Act & Assert
        Assert.Equal(activity1, activity2);
        Assert.NotEqual(activity1, activity3);
    }

    [Fact]
    public void DailyActivitySummary_PropertiesAccessible()
    {
        // Arrange
        var date = DateTime.Parse("2026-02-20");
        var activity = new DailyActivitySummary(date, 15, 250000);

        // Act & Assert
        Assert.Equal(date, activity.Date);
        Assert.Equal(15, activity.Messages);
        Assert.Equal(250000L, activity.Tokens);
    }

    [Fact]
    public void UpdateFrom_WithEmptyCollections()
    {
        // Arrange
        var source = new UsageSummary
        {
            ModelBreakdown = new Dictionary<string, long>(),
            DailyActivity = new List<DailyActivitySummary>()
        };

        var target = new UsageSummary
        {
            ModelBreakdown = new Dictionary<string, long> { ["claude-3-5-sonnet"] = 100000 },
            DailyActivity = new List<DailyActivitySummary> { new(DateTime.Now, 5, 50000) }
        };

        // Act
        target.UpdateFrom(source);

        // Assert
        Assert.Empty(target.ModelBreakdown);
        Assert.Empty(target.DailyActivity);
    }

    [Fact]
    public void UpdateFrom_WithLargeNumbers()
    {
        // Arrange
        var source = new UsageSummary
        {
            WeeklyTokensUsed = long.MaxValue / 2,
            TodayTokens = long.MaxValue / 4,
            EstimatedCostUsd = 999999.99
        };

        var target = new UsageSummary();

        // Act
        target.UpdateFrom(source);

        // Assert
        Assert.Equal(long.MaxValue / 2, target.WeeklyTokensUsed);
        Assert.Equal(long.MaxValue / 4, target.TodayTokens);
        Assert.Equal(999999.99, target.EstimatedCostUsd);
    }

    [Fact]
    public void UpdateFrom_PercentagesRange()
    {
        // Arrange
        var source = new UsageSummary
        {
            EstimatedPercentage = 100.0,
            SessionPercentage = 0.0,
            WeeklyPercentage = 50.5
        };

        var target = new UsageSummary();

        // Act
        target.UpdateFrom(source);

        // Assert
        Assert.Equal(100.0, target.EstimatedPercentage);
        Assert.Equal(0.0, target.SessionPercentage);
        Assert.Equal(50.5, target.WeeklyPercentage);
    }
}
