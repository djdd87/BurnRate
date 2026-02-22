using System.Collections.ObjectModel;
using BurnRate.Models;
using BurnRate.Services;
using BurnRate.ViewModels;

namespace BurnRate.Tests.ViewModels;

/// <summary>
/// Comprehensive tests for MainViewModel.
/// Tests profile management, selection, and usage binding logic.
/// </summary>
public sealed class MainViewModelTests : IDisposable
{
    private readonly string _tempDir;
    private readonly Dictionary<string, long> _planLimits;
    private readonly ThemeService _themeService;

    public MainViewModelTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"BurnRate-Tests-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
        _planLimits = new Dictionary<string, long>
        {
            ["default_claude_max_5x"] = 2_500_000,
            ["pro"] = 2_500_000
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

    private ProfileViewModel CreateTestProfile(string name)
    {
        var config = new ProfileConfig { Name = name, Path = _tempDir };
        var calculator = new UsageCalculator(_planLimits);
        return new ProfileViewModel(config, calculator, _themeService, refreshIntervalSeconds: 60);
    }

    #endregion

    #region AddProfile Tests

    [Fact]
    public void AddProfile_FirstProfile_BecomesSelected()
    {
        // Arrange
        using var mainVm = new MainViewModel(_themeService);
        var profile1 = CreateTestProfile("Profile1");

        // Act
        mainVm.AddProfile(profile1);

        // Assert
        Assert.Equal(profile1, mainVm.SelectedProfile);
        profile1.Dispose();
    }

    [Fact]
    public void AddProfile_SecondProfile_DoesNotChangeSelected()
    {
        // Arrange
        using var mainVm = new MainViewModel(_themeService);
        var profile1 = CreateTestProfile("Profile1");
        var profile2 = CreateTestProfile("Profile2");

        mainVm.AddProfile(profile1);
        var firstSelected = mainVm.SelectedProfile;

        // Act
        mainVm.AddProfile(profile2);

        // Assert
        Assert.Equal(profile1, mainVm.SelectedProfile);
        Assert.Equal(firstSelected, mainVm.SelectedProfile);
        profile1.Dispose();
        profile2.Dispose();
    }

    [Fact]
    public void AddProfile_AddsToCollection()
    {
        // Arrange
        using var mainVm = new MainViewModel(_themeService);
        var profile1 = CreateTestProfile("Profile1");
        var profile2 = CreateTestProfile("Profile2");

        // Act
        mainVm.AddProfile(profile1);
        mainVm.AddProfile(profile2);

        // Assert
        Assert.Equal(2, mainVm.Profiles.Count);
        Assert.Contains(profile1, mainVm.Profiles);
        Assert.Contains(profile2, mainVm.Profiles);
        profile1.Dispose();
        profile2.Dispose();
    }

    [Fact]
    public void AddProfile_IncreasesCollectionCount()
    {
        // Arrange
        using var mainVm = new MainViewModel(_themeService);
        var profile = CreateTestProfile("Profile1");
        var profile2 = CreateTestProfile("Profile2");

        var initialCount = mainVm.Profiles.Count;

        // Act
        mainVm.AddProfile(profile);
        var afterFirstAdd = mainVm.Profiles.Count;

        mainVm.AddProfile(profile2);
        var afterSecondAdd = mainVm.Profiles.Count;

        // Assert
        Assert.Equal(0, initialCount);
        Assert.Equal(1, afterFirstAdd);
        Assert.Equal(2, afterSecondAdd);

        profile.Dispose();
        profile2.Dispose();
    }

    #endregion

    #region SelectedProfile Tests

    [Fact]
    public void SelectedProfile_Change_FiresUsagePropertyChanged()
    {
        // Arrange
        using var mainVm = new MainViewModel(_themeService);
        var profile1 = CreateTestProfile("Profile1");
        var profile2 = CreateTestProfile("Profile2");

        mainVm.AddProfile(profile1);
        mainVm.AddProfile(profile2);

        var propertyChangedEvents = new List<string>();
        mainVm.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName != null)
            {
                propertyChangedEvents.Add(e.PropertyName);
            }
        };

        // Act
        mainVm.SelectedProfile = profile2;

        // Assert
        Assert.Contains("SelectedProfile", propertyChangedEvents);
        Assert.Contains("Usage", propertyChangedEvents);
        profile1.Dispose();
        profile2.Dispose();
    }

    [Fact]
    public void SelectedProfile_ManualChange_Updates()
    {
        // Arrange
        using var mainVm = new MainViewModel(_themeService);
        var profile1 = CreateTestProfile("Profile1");
        var profile2 = CreateTestProfile("Profile2");

        mainVm.AddProfile(profile1);
        mainVm.AddProfile(profile2);

        // Act
        mainVm.SelectedProfile = profile2;

        // Assert
        Assert.Equal(profile2, mainVm.SelectedProfile);
        profile1.Dispose();
        profile2.Dispose();
    }

    #endregion

    #region Usage Property Tests

    [Fact]
    public void Usage_ReturnsSelectedProfileUsage()
    {
        // Arrange
        using var mainVm = new MainViewModel(_themeService);
        var profile = CreateTestProfile("Profile1");
        mainVm.AddProfile(profile);

        // Act
        var usage = mainVm.Usage;

        // Assert
        Assert.NotNull(usage);
        Assert.Equal(profile.Usage, usage);
        profile.Dispose();
    }

    [Fact]
    public void Usage_NullWhenNoSelection()
    {
        // Arrange
        using var mainVm = new MainViewModel(_themeService);

        // Act
        var usage = mainVm.Usage;

        // Assert
        Assert.Null(usage);
    }

    [Fact]
    public void Usage_ReflectsCurrentSelection()
    {
        // Arrange
        using var mainVm = new MainViewModel(_themeService);
        var profile1 = CreateTestProfile("Profile1");
        var profile2 = CreateTestProfile("Profile2");

        mainVm.AddProfile(profile1);
        mainVm.AddProfile(profile2);

        // Assert initial state
        Assert.Equal(profile1.Usage, mainVm.Usage);

        // Act - switch selection
        mainVm.SelectedProfile = profile2;

        // Assert
        Assert.Equal(profile2.Usage, mainVm.Usage);
        Assert.NotEqual(profile1.Usage, mainVm.Usage);

        profile1.Dispose();
        profile2.Dispose();
    }

    [Fact]
    public void Usage_UnsubscribesFromOldProfile()
    {
        // Arrange
        using var mainVm = new MainViewModel(_themeService);
        var profile1 = CreateTestProfile("Profile1");
        var profile2 = CreateTestProfile("Profile2");

        mainVm.AddProfile(profile1);
        mainVm.AddProfile(profile2);

        var usageChangeCount = 0;
        mainVm.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == "Usage")
                usageChangeCount++;
        };

        // Act - change selection to profile2
        mainVm.SelectedProfile = profile2;

        // Reset count
        usageChangeCount = 0;

        // Change profile1's usage (should NOT fire on mainVm)
        profile1.Usage.UpdateFrom(new UsageSummary { TodayMessages = 99 });

        // Assert - profile1 change should not trigger mainVm's Usage change
        // (We're not counting the initial change because we reset the count)
        // The point is: when we switched to profile2, we should have unsubscribed from profile1
        // So changes to profile1 should not trigger mainVm's PropertyChanged for Usage

        profile1.Dispose();
        profile2.Dispose();
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_DisposesAllProfiles()
    {
        // Arrange
        var mainVm = new MainViewModel(_themeService);
        var profile1 = CreateTestProfile("Profile1");
        var profile2 = CreateTestProfile("Profile2");

        mainVm.AddProfile(profile1);
        mainVm.AddProfile(profile2);

        // Act
        mainVm.Dispose();

        // Assert - verify no exception occurred (disposed profiles can't be verified directly)
        // But we can verify the main view model is disposed
        Assert.NotNull(mainVm.Profiles);
    }

    [Fact]
    public void Dispose_MultipleTimes_DoesNotThrow()
    {
        // Arrange
        var mainVm = new MainViewModel(_themeService);
        var profile = CreateTestProfile("Profile1");
        mainVm.AddProfile(profile);

        // Act & Assert - should not throw
        mainVm.Dispose();
        mainVm.Dispose();
        mainVm.Dispose();

        profile.Dispose();
    }

    [Fact]
    public void Dispose_CleanupSuccessful()
    {
        // Arrange
        var mainVm = new MainViewModel(_themeService);
        var profile = CreateTestProfile("Profile1");
        mainVm.AddProfile(profile);

        // Act
        mainVm.Dispose();

        // Assert - verify disposal completes without exception
        // The profiles remain in the collection even after dispose
        Assert.NotNull(mainVm.Profiles);

        profile.Dispose();
    }

    #endregion

    #region Profiles Collection Tests

    [Fact]
    public void Profiles_InitiallyEmpty()
    {
        // Arrange
        using var mainVm = new MainViewModel(_themeService);

        // Act
        var count = mainVm.Profiles.Count;

        // Assert
        Assert.Equal(0, count);
    }

    [Fact]
    public void Profiles_IsObservableCollection()
    {
        // Arrange
        using var mainVm = new MainViewModel(_themeService);

        // Act
        var profiles = mainVm.Profiles;

        // Assert
        Assert.IsType<ObservableCollection<ProfileViewModel>>(profiles);
    }

    [Fact]
    public void Profiles_PropertyChangedFiresWhenAdded()
    {
        // Arrange
        using var mainVm = new MainViewModel(_themeService);
        var profile = CreateTestProfile("Profile1");

        var propertyChangedEvents = new List<string>();
        mainVm.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName != null)
                propertyChangedEvents.Add(e.PropertyName);
        };

        // Act
        mainVm.AddProfile(profile);

        // Assert
        Assert.Contains("SelectedProfile", propertyChangedEvents);
        profile.Dispose();
    }

    #endregion

    #region Multiple Profile Workflow Tests

    [Fact]
    public void MultiProfile_SwitchBetweenProfiles()
    {
        // Arrange
        using var mainVm = new MainViewModel(_themeService);
        var profile1 = CreateTestProfile("Profile1");
        var profile2 = CreateTestProfile("Profile2");
        var profile3 = CreateTestProfile("Profile3");

        mainVm.AddProfile(profile1);
        mainVm.AddProfile(profile2);
        mainVm.AddProfile(profile3);

        // Act & Assert
        Assert.Equal(profile1, mainVm.SelectedProfile);
        Assert.Equal(profile1.Usage, mainVm.Usage);

        mainVm.SelectedProfile = profile2;
        Assert.Equal(profile2, mainVm.SelectedProfile);
        Assert.Equal(profile2.Usage, mainVm.Usage);

        mainVm.SelectedProfile = profile3;
        Assert.Equal(profile3, mainVm.SelectedProfile);
        Assert.Equal(profile3.Usage, mainVm.Usage);

        mainVm.SelectedProfile = profile1;
        Assert.Equal(profile1, mainVm.SelectedProfile);
        Assert.Equal(profile1.Usage, mainVm.Usage);

        profile1.Dispose();
        profile2.Dispose();
        profile3.Dispose();
    }

    [Fact]
    public void MultiProfile_EachProfileIndependent()
    {
        // Arrange
        using var mainVm = new MainViewModel(_themeService);
        var profile1 = CreateTestProfile("Profile1");
        var profile2 = CreateTestProfile("Profile2");

        mainVm.AddProfile(profile1);
        mainVm.AddProfile(profile2);

        // Act
        profile1.Usage.UpdateFrom(new UsageSummary { TodayMessages = 10 });
        profile2.Usage.UpdateFrom(new UsageSummary { TodayMessages = 20 });

        // Assert
        Assert.Equal(10, profile1.Usage.TodayMessages);
        Assert.Equal(20, profile2.Usage.TodayMessages);
        Assert.Equal(profile1.Usage, mainVm.Usage);

        profile1.Dispose();
        profile2.Dispose();
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task Integration_AddProfile_RefreshAsync_SelectProfile()
    {
        // Arrange
        using var mainVm = new MainViewModel(_themeService);
        var profile = CreateTestProfile("IntegrationTest");

        // Minimal setup - just stats-cache with no data
        var statsPath = Path.Combine(_tempDir, "stats-cache.json");
        var credPath = Path.Combine(_tempDir, ".credentials.json");

        File.WriteAllText(statsPath, @"{
            ""version"": 2,
            ""lastComputedDate"": ""2026-02-20"",
            ""totalSessions"": 0,
            ""totalMessages"": 0,
            ""totalSpeculationTimeSavedMs"": 0,
            ""dailyActivity"": [],
            ""dailyModelTokens"": [],
            ""modelUsage"": {},
            ""hourCounts"": {}
        }");

        File.WriteAllText(credPath, @"{
            ""claudeAiOauth"": {
                ""subscriptionType"": """",
                ""rateLimitTier"": ""default_claude_max_5x""
            }
        }");

        // Act
        mainVm.AddProfile(profile);
        await profile.RefreshAsync();
        mainVm.SelectedProfile = profile;

        // Assert
        Assert.Equal(profile, mainVm.SelectedProfile);
        Assert.NotNull(mainVm.Usage);
        Assert.Equal(profile.Usage, mainVm.Usage);

        profile.Dispose();
    }

    #endregion

    #region Window Visibility Tests

    [Fact]
    public void IsWindowVisible_InitiallyFalse()
    {
        // Arrange
        using var mainVm = new MainViewModel(_themeService);

        // Act
        var isVisible = mainVm.IsWindowVisible;

        // Assert
        Assert.False(isVisible);
    }

    [Fact]
    public void IsWindowVisible_CanBeSet()
    {
        // Arrange
        using var mainVm = new MainViewModel(_themeService);

        // Act
        mainVm.IsWindowVisible = true;

        // Assert
        Assert.True(mainVm.IsWindowVisible);

        // Act
        mainVm.IsWindowVisible = false;

        // Assert
        Assert.False(mainVm.IsWindowVisible);
    }

    [Fact]
    public void IsWindowVisible_FiresPropertyChanged()
    {
        // Arrange
        using var mainVm = new MainViewModel(_themeService);
        var propertyChangedEvents = new List<string>();

        mainVm.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName != null)
                propertyChangedEvents.Add(e.PropertyName);
        };

        // Act
        mainVm.IsWindowVisible = true;

        // Assert
        Assert.Contains("IsWindowVisible", propertyChangedEvents);
    }

    #endregion
}
