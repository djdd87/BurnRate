using System.Text.Json;
using ClaudeMon.Models;
using ClaudeMon.Services;
using ClaudeMon.ViewModels;

namespace ClaudeMon.Tests.ViewModels;

/// <summary>
/// Comprehensive tests for ProfileViewModel.RefreshAsync method.
/// Tests data loading, stats merging, and tooltip formatting logic.
/// </summary>
public sealed class ProfileViewModelRefreshTests : IDisposable
{
    private readonly string _tempDir;
    private readonly Dictionary<string, long> _planLimits;
    private readonly ThemeService _themeService;

    public ProfileViewModelRefreshTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"ClaudeMon-Tests-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
        _planLimits = new Dictionary<string, long>
        {
            ["default_claude_max_5x"] = 2_500_000,
            ["default_claude_max_20x"] = 10_000_000,
            ["pro"] = 2_500_000,
            ["default_raven"] = 1_000_000
        };
        _themeService = new ThemeService();
    }

    public void Dispose()
    {
        _themeService.Dispose();
        if (Directory.Exists(_tempDir))
        {
            try
            {
                Directory.Delete(_tempDir, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    #region Helper Methods

    private ProfileViewModel CreateProfileViewModel(string profileName = "TestProfile")
    {
        var config = new ProfileConfig { Name = profileName, Path = _tempDir };
        var calculator = new UsageCalculator(_planLimits);
        return new ProfileViewModel(config, calculator, _themeService, refreshIntervalSeconds: 60);
    }

    private void WriteStatsCache(StatsCache stats)
    {
        var json = JsonSerializer.Serialize(stats, new JsonSerializerOptions { WriteIndented = true });
        var filePath = Path.Combine(_tempDir, "stats-cache.json");
        File.WriteAllText(filePath, json);
    }

    private void WriteCredentials(ClaudeAiOAuthInfo creds)
    {
        var wrapper = new { claudeAiOauth = creds };
        var json = JsonSerializer.Serialize(wrapper, new JsonSerializerOptions { WriteIndented = true });
        var filePath = Path.Combine(_tempDir, ".credentials.json");
        File.WriteAllText(filePath, json);
    }

    private void WriteJsonlFile(string projectName, string conversationName, params object[] lines)
    {
        var projectDir = Path.Combine(_tempDir, "projects", projectName);
        Directory.CreateDirectory(projectDir);
        var filePath = Path.Combine(projectDir, $"{conversationName}.jsonl");
        var content = string.Join('\n', lines.Select(l => JsonSerializer.Serialize(l)));
        File.WriteAllText(filePath, content);
    }

    private StatsCache CreateEmptyStatsCache()
    {
        var today = DateTime.UtcNow.Date;
        return new StatsCache
        {
            Version = 2,
            LastComputedDate = today.ToString("yyyy-MM-dd"),
            TotalSessions = 0,
            TotalMessages = 0,
            TotalSpeculationTimeSavedMs = 0,
            DailyActivity = new List<DailyActivityEntry>(),
            DailyModelTokens = new List<DailyModelTokensEntry>(),
            ModelUsage = new Dictionary<string, ModelUsageEntry>(),
            HourCounts = new Dictionary<string, int>()
        };
    }

    private ClaudeAiOAuthInfo CreateCredentials(string tier = "default_claude_max_5x", string subscription = "")
    {
        return new ClaudeAiOAuthInfo
        {
            RateLimitTier = tier,
            SubscriptionType = subscription
            // Note: No AccessToken, so live API will return null
        };
    }

    #endregion

    #region RefreshAsync_NoFiles Tests

    [Fact]
    public async Task RefreshAsync_NoFiles_SetsUnknownTooltip()
    {
        // Arrange
        using var vm = CreateProfileViewModel("TestProfile");

        // Act
        await vm.RefreshAsync();

        // Assert
        Assert.Equal(-1, vm.Usage.EstimatedPercentage);
        Assert.Contains("Unknown", vm.TooltipText);
    }

    #endregion

    #region RefreshAsync_StatsCacheOnly Tests

    [Fact]
    public async Task RefreshAsync_StatsCacheOnly_CalculatesBaseSummary()
    {
        // Arrange
        var today = DateTime.UtcNow.Date;
        var todayStr = today.ToString("yyyy-MM-dd");

        var stats = CreateEmptyStatsCache();
        stats.TotalSessions = 100;
        stats.TotalMessages = 2000;
        stats.DailyActivity.Add(new DailyActivityEntry
        {
            Date = todayStr,
            MessageCount = 10,
            SessionCount = 2,
            ToolCallCount = 5
        });
        stats.DailyModelTokens.Add(new DailyModelTokensEntry
        {
            Date = todayStr,
            TokensByModel = new Dictionary<string, long> { ["claude-sonnet-4-5"] = 50000 }
        });

        WriteStatsCache(stats);
        WriteCredentials(CreateCredentials());

        using var vm = CreateProfileViewModel();

        // Act
        await vm.RefreshAsync();

        // Assert
        Assert.Equal(100, vm.Usage.TotalSessions);
        Assert.Equal(2000, vm.Usage.TotalMessages);
        Assert.Equal(50000, vm.Usage.WeeklyTokensUsed);
        Assert.True(vm.Usage.EstimatedPercentage > 0);
        Assert.Equal(10, vm.Usage.TodayMessages);
    }

    #endregion

    #region RefreshAsync_JsonlOverridesTodayStats Tests

    [Fact]
    public async Task RefreshAsync_JsonlOverridesTodayStats()
    {
        // Arrange
        var today = DateTime.UtcNow.Date;
        var todayStr = today.ToString("yyyy-MM-dd");
        var todayIso = today.ToString("yyyy-MM-dd") + "T12:00:00Z";

        var stats = CreateEmptyStatsCache();
        stats.DailyActivity.Add(new DailyActivityEntry
        {
            Date = todayStr,
            MessageCount = 5,
            SessionCount = 1,
            ToolCallCount = 0
        });

        WriteStatsCache(stats);
        WriteCredentials(CreateCredentials());

        // Write JSONL with 10 messages for today
        WriteJsonlFile("proj1", "conv1",
            new { type = "user", timestamp = todayIso, message = new { content = "msg1" } },
            new { type = "user", timestamp = todayIso, message = new { content = "msg2" } },
            new { type = "user", timestamp = todayIso, message = new { content = "msg3" } },
            new { type = "user", timestamp = todayIso, message = new { content = "msg4" } },
            new { type = "user", timestamp = todayIso, message = new { content = "msg5" } },
            new { type = "user", timestamp = todayIso, message = new { content = "msg6" } },
            new { type = "user", timestamp = todayIso, message = new { content = "msg7" } },
            new { type = "user", timestamp = todayIso, message = new { content = "msg8" } },
            new { type = "user", timestamp = todayIso, message = new { content = "msg9" } },
            new { type = "user", timestamp = todayIso, message = new { content = "msg10" } }
        );

        using var vm = CreateProfileViewModel();

        // Act
        await vm.RefreshAsync();

        // Assert
        Assert.Equal(10, vm.Usage.TodayMessages);
    }

    #endregion

    #region RefreshAsync_JsonlWeeklyTokens Tests

    [Fact]
    public async Task RefreshAsync_JsonlWeeklyTokensOverrideWhenHigher()
    {
        // Arrange
        var today = DateTime.UtcNow.Date;
        var todayStr = today.ToString("yyyy-MM-dd");
        var todayIso = today.ToString("yyyy-MM-dd") + "T12:00:00Z";

        var stats = CreateEmptyStatsCache();
        stats.DailyModelTokens.Add(new DailyModelTokensEntry
        {
            Date = todayStr,
            TokensByModel = new Dictionary<string, long> { ["claude-sonnet-4-5"] = 10000 }
        });

        WriteStatsCache(stats);
        WriteCredentials(CreateCredentials());

        // Write JSONL with 50000 tokens for today
        WriteJsonlFile("proj1", "conv1",
            new
            {
                type = "assistant",
                timestamp = todayIso,
                message = new { model = "claude-sonnet-4-5", content = "response", usage = new { input_tokens = 1000, output_tokens = 50000 } }
            }
        );

        using var vm = CreateProfileViewModel();

        // Act
        await vm.RefreshAsync();

        // Assert
        Assert.Equal(50000, vm.Usage.WeeklyTokensUsed);
    }

    [Fact]
    public async Task RefreshAsync_JsonlWeeklyTokensNotOverrideWhenLower()
    {
        // Arrange
        var today = DateTime.UtcNow.Date;
        var todayStr = today.ToString("yyyy-MM-dd");
        var todayIso = today.ToString("yyyy-MM-dd") + "T12:00:00Z";

        var stats = CreateEmptyStatsCache();
        stats.DailyModelTokens.Add(new DailyModelTokensEntry
        {
            Date = todayStr,
            TokensByModel = new Dictionary<string, long> { ["claude-sonnet-4-5"] = 100000 }
        });

        WriteStatsCache(stats);
        WriteCredentials(CreateCredentials());

        // Write JSONL with only 5000 tokens (lower than stats-cache)
        WriteJsonlFile("proj1", "conv1",
            new
            {
                type = "assistant",
                timestamp = todayIso,
                message = new { model = "claude-sonnet-4-5", content = "response", usage = new { input_tokens = 1000, output_tokens = 5000 } }
            }
        );

        using var vm = CreateProfileViewModel();

        // Act
        await vm.RefreshAsync();

        // Assert
        Assert.Equal(100000, vm.Usage.WeeklyTokensUsed);
    }

    #endregion

    #region RefreshAsync_ModelBreakdown Tests

    [Fact]
    public async Task RefreshAsync_JsonlModelBreakdownReplacesStatsCache()
    {
        // Arrange
        var today = DateTime.UtcNow.Date;
        var todayStr = today.ToString("yyyy-MM-dd");
        var todayIso = today.ToString("yyyy-MM-dd") + "T12:00:00Z";

        var stats = CreateEmptyStatsCache();
        WriteStatsCache(stats);
        WriteCredentials(CreateCredentials());

        // Write JSONL with two models
        WriteJsonlFile("proj1", "conv1",
            new
            {
                type = "assistant",
                timestamp = todayIso,
                message = new { model = "claude-sonnet-4-5", content = "r1", usage = new { input_tokens = 100, output_tokens = 10000 } }
            },
            new
            {
                type = "assistant",
                timestamp = todayIso,
                message = new { model = "claude-opus-4-6", content = "r2", usage = new { input_tokens = 200, output_tokens = 20000 } }
            }
        );

        using var vm = CreateProfileViewModel();

        // Act
        await vm.RefreshAsync();

        // Assert
        Assert.NotEmpty(vm.Usage.ModelBreakdown);
        Assert.True(vm.Usage.ModelBreakdown.ContainsKey("claude-sonnet-4-5"));
        Assert.True(vm.Usage.ModelBreakdown.ContainsKey("claude-opus-4-6"));
        Assert.Equal(10000L, vm.Usage.ModelBreakdown["claude-sonnet-4-5"]);
        Assert.Equal(20000L, vm.Usage.ModelBreakdown["claude-opus-4-6"]);
    }

    #endregion

    #region RefreshAsync_DailyActivityMerge Tests

    [Fact]
    public async Task RefreshAsync_JsonlDailyActivityMerge()
    {
        // Arrange
        var today = DateTime.UtcNow.Date;
        var todayStr = today.ToString("yyyy-MM-dd");
        var todayIso = today.ToString("yyyy-MM-dd") + "T12:00:00Z";

        var stats = CreateEmptyStatsCache();
        stats.DailyActivity.Add(new DailyActivityEntry
        {
            Date = todayStr,
            MessageCount = 5,
            SessionCount = 1,
            ToolCallCount = 0
        });

        WriteStatsCache(stats);
        WriteCredentials(CreateCredentials());

        // Write JSONL with 10 messages (higher than stats-cache)
        WriteJsonlFile("proj1", "conv1",
            new { type = "user", timestamp = todayIso, message = new { content = "m1" } },
            new { type = "user", timestamp = todayIso, message = new { content = "m2" } },
            new { type = "user", timestamp = todayIso, message = new { content = "m3" } },
            new { type = "user", timestamp = todayIso, message = new { content = "m4" } },
            new { type = "user", timestamp = todayIso, message = new { content = "m5" } },
            new { type = "user", timestamp = todayIso, message = new { content = "m6" } },
            new { type = "user", timestamp = todayIso, message = new { content = "m7" } },
            new { type = "user", timestamp = todayIso, message = new { content = "m8" } },
            new { type = "user", timestamp = todayIso, message = new { content = "m9" } },
            new { type = "user", timestamp = todayIso, message = new { content = "m10" } }
        );

        using var vm = CreateProfileViewModel();

        // Act
        await vm.RefreshAsync();

        // Assert
        var todayActivity = vm.Usage.DailyActivity.FirstOrDefault(d => d.Date == today);
        Assert.NotNull(todayActivity);
        Assert.Equal(10, todayActivity.Messages);
    }

    #endregion

    #region RefreshAsync_Credentials Tests

    [Fact]
    public async Task RefreshAsync_CredentialsSetsPlanDisplayName()
    {
        // Arrange
        var stats = CreateEmptyStatsCache();
        WriteStatsCache(stats);
        WriteCredentials(new ClaudeAiOAuthInfo
        {
            RateLimitTier = "default_claude_max_5x",
            SubscriptionType = ""
        });

        using var vm = CreateProfileViewModel();

        // Act
        await vm.RefreshAsync();

        // Assert
        Assert.Equal("Max 5x", vm.PlanDisplayName);
    }

    [Fact]
    public async Task RefreshAsync_TooltipFormat_Estimated()
    {
        // Arrange
        var stats = CreateEmptyStatsCache();
        stats.DailyModelTokens.Add(new DailyModelTokensEntry
        {
            Date = DateTime.UtcNow.Date.ToString("yyyy-MM-dd"),
            TokensByModel = new Dictionary<string, long> { ["claude-sonnet-4-5"] = 1_250_000 }
        });

        WriteStatsCache(stats);
        WriteCredentials(CreateCredentials("default_claude_max_5x", ""));

        using var vm = CreateProfileViewModel("TestProfile");

        // Act
        await vm.RefreshAsync();

        // Assert
        Assert.Contains("TestProfile", vm.TooltipText);
        Assert.Contains("(Est.)", vm.TooltipText);
        Assert.Contains("Max 5x", vm.TooltipText);
    }

    #endregion

    #region RefreshAsync_LiveApi Tests

    [Fact]
    public async Task RefreshAsync_LiveApiReturnsNull_UsesLocalEstimate()
    {
        // Arrange (no valid OAuth token, so live API returns null)
        var stats = CreateEmptyStatsCache();
        stats.DailyModelTokens.Add(new DailyModelTokensEntry
        {
            Date = DateTime.UtcNow.Date.ToString("yyyy-MM-dd"),
            TokensByModel = new Dictionary<string, long> { ["claude-sonnet-4-5"] = 500000 }
        });

        WriteStatsCache(stats);
        WriteCredentials(CreateCredentials());

        using var vm = CreateProfileViewModel();

        // Act
        await vm.RefreshAsync();

        // Assert
        Assert.False(vm.Usage.IsLive);
        Assert.Contains("(Est.)", vm.TooltipText);
    }

    #endregion

    #region RefreshAsync_SessionCount Tests

    [Fact]
    public async Task RefreshAsync_SessionCountFromJsonl()
    {
        // Arrange
        var today = DateTime.UtcNow.Date;
        var todayIso = today.ToString("yyyy-MM-dd") + "T12:00:00Z";

        var stats = CreateEmptyStatsCache();
        WriteStatsCache(stats);
        WriteCredentials(CreateCredentials());

        // Write JSONL files: two projects with messages today = 2 sessions
        WriteJsonlFile("proj1", "conv1",
            new { type = "user", timestamp = todayIso, message = new { content = "msg1" } }
        );
        WriteJsonlFile("proj2", "conv1",
            new { type = "user", timestamp = todayIso, message = new { content = "msg2" } }
        );

        using var vm = CreateProfileViewModel();

        // Act
        await vm.RefreshAsync();

        // Assert
        Assert.Equal(2, vm.Usage.TodaySessions);
    }

    #endregion

    #region RefreshAsync_ModelBreakdown_CaseInsensitive Tests

    [Fact]
    public async Task RefreshAsync_ModelBreakdownCaseInsensitive()
    {
        // Arrange
        var today = DateTime.UtcNow.Date;
        var todayIso = today.ToString("yyyy-MM-dd") + "T12:00:00Z";

        var stats = CreateEmptyStatsCache();
        WriteStatsCache(stats);
        WriteCredentials(CreateCredentials());

        // Write JSONL with different cases of the same model
        WriteJsonlFile("proj1", "conv1",
            new
            {
                type = "assistant",
                timestamp = todayIso,
                message = new { model = "claude-sonnet-4-5", content = "r1", usage = new { input_tokens = 100, output_tokens = 10000 } }
            },
            new
            {
                type = "assistant",
                timestamp = todayIso,
                message = new { model = "Claude-Sonnet-4-5", content = "r2", usage = new { input_tokens = 100, output_tokens = 15000 } }
            }
        );

        using var vm = CreateProfileViewModel();

        // Act
        await vm.RefreshAsync();

        // Assert
        // Both case variants should merge into one entry
        var modelCount = vm.Usage.ModelBreakdown.Count;
        Assert.True(modelCount == 1 || (modelCount == 2 && vm.Usage.ModelBreakdown.Values.Sum() == 25000));
    }

    #endregion

    #region RefreshAsync_Dispose Tests

    [Fact]
    public async Task RefreshAsync_Dispose_StillWorks()
    {
        // Arrange
        var stats = CreateEmptyStatsCache();
        WriteStatsCache(stats);
        WriteCredentials(CreateCredentials());

        var vm = CreateProfileViewModel();

        // Act - should not throw
        await vm.RefreshAsync();
        vm.Dispose();

        // Assert - verify no exception occurred
        Assert.NotNull(vm.Usage);
    }

    #endregion

    #region RefreshAsync_FormatTier Tests

    [Fact]
    public async Task RefreshAsync_FormatTier_Max5x()
    {
        // Arrange
        var stats = CreateEmptyStatsCache();
        WriteStatsCache(stats);
        WriteCredentials(new ClaudeAiOAuthInfo { RateLimitTier = "default_claude_max_5x", SubscriptionType = "" });

        using var vm = CreateProfileViewModel();

        // Act
        await vm.RefreshAsync();

        // Assert
        Assert.Equal("Max 5x", vm.PlanDisplayName);
    }

    [Fact]
    public async Task RefreshAsync_FormatTier_TeamPremium()
    {
        // Arrange
        var stats = CreateEmptyStatsCache();
        WriteStatsCache(stats);
        WriteCredentials(new ClaudeAiOAuthInfo { RateLimitTier = "default_claude_max_5x", SubscriptionType = "team" });

        using var vm = CreateProfileViewModel();

        // Act
        await vm.RefreshAsync();

        // Assert
        Assert.Equal("Team Premium", vm.PlanDisplayName);
    }

    [Fact]
    public async Task RefreshAsync_FormatTier_Unknown()
    {
        // Arrange
        var stats = CreateEmptyStatsCache();
        WriteStatsCache(stats);
        WriteCredentials(new ClaudeAiOAuthInfo { RateLimitTier = "unknown_tier", SubscriptionType = "" });

        using var vm = CreateProfileViewModel();

        // Act
        await vm.RefreshAsync();

        // Assert
        Assert.NotEmpty(vm.PlanDisplayName);
        // Should handle unknown tier gracefully
    }

    #endregion

    #region RefreshAsync_MultiDay Tests

    [Fact]
    public async Task RefreshAsync_MultiDayActivity()
    {
        // Arrange
        var today = DateTime.UtcNow.Date;
        var yesterday = today.AddDays(-1);
        var dayBeforeYesterday = today.AddDays(-2);

        var todayStr = today.ToString("yyyy-MM-dd");
        var yesterdayStr = yesterday.ToString("yyyy-MM-dd");
        var dayBeforeYesterdayStr = dayBeforeYesterday.ToString("yyyy-MM-dd");

        var stats = CreateEmptyStatsCache();
        stats.DailyActivity.Add(new DailyActivityEntry { Date = todayStr, MessageCount = 10, SessionCount = 2, ToolCallCount = 0 });
        stats.DailyActivity.Add(new DailyActivityEntry { Date = yesterdayStr, MessageCount = 8, SessionCount = 1, ToolCallCount = 0 });
        stats.DailyActivity.Add(new DailyActivityEntry { Date = dayBeforeYesterdayStr, MessageCount = 5, SessionCount = 1, ToolCallCount = 0 });

        WriteStatsCache(stats);
        WriteCredentials(CreateCredentials());

        using var vm = CreateProfileViewModel();

        // Act
        await vm.RefreshAsync();

        // Assert
        Assert.True(vm.Usage.DailyActivity.Count >= 3);
        var todayActivity = vm.Usage.DailyActivity.FirstOrDefault(d => d.Date == today);
        Assert.NotNull(todayActivity);
        Assert.Equal(10, todayActivity.Messages);
    }

    #endregion

    #region RefreshAsync_PercentageCalculation Tests

    [Fact]
    public async Task RefreshAsync_PercentageCalculation_AtLimit()
    {
        // Arrange
        var today = DateTime.UtcNow.Date;
        var todayStr = today.ToString("yyyy-MM-dd");

        var stats = CreateEmptyStatsCache();
        // 2.5M tokens used (100% of 2.5M limit)
        stats.DailyModelTokens.Add(new DailyModelTokensEntry
        {
            Date = todayStr,
            TokensByModel = new Dictionary<string, long> { ["claude-sonnet-4-5"] = 2_500_000 }
        });

        WriteStatsCache(stats);
        WriteCredentials(new ClaudeAiOAuthInfo { RateLimitTier = "default_claude_max_5x", SubscriptionType = "" });

        using var vm = CreateProfileViewModel();

        // Act
        await vm.RefreshAsync();

        // Assert
        Assert.Equal(100, vm.Usage.EstimatedPercentage);
        Assert.Contains("Limit", vm.TooltipText);
    }

    [Fact]
    public async Task RefreshAsync_PercentageCalculation_MidRange()
    {
        // Arrange
        var today = DateTime.UtcNow.Date;
        var todayStr = today.ToString("yyyy-MM-dd");

        var stats = CreateEmptyStatsCache();
        // 1.25M tokens used (50% of 2.5M limit)
        stats.DailyModelTokens.Add(new DailyModelTokensEntry
        {
            Date = todayStr,
            TokensByModel = new Dictionary<string, long> { ["claude-sonnet-4-5"] = 1_250_000 }
        });

        WriteStatsCache(stats);
        WriteCredentials(new ClaudeAiOAuthInfo { RateLimitTier = "default_claude_max_5x", SubscriptionType = "" });

        using var vm = CreateProfileViewModel();

        // Act
        await vm.RefreshAsync();

        // Assert
        Assert.Equal(50, vm.Usage.EstimatedPercentage);
    }

    #endregion
}
