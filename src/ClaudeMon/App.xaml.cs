using System.Windows;
using Microsoft.Extensions.Configuration;
using ClaudeMon.Helpers;
using ClaudeMon.Models;
using ClaudeMon.Services;
using ClaudeMon.ViewModels;

namespace ClaudeMon;

public partial class App : Application
{
    private SingleInstanceGuard? _guard;
    private MainViewModel? _mainViewModel;
    private ThemeService? _themeService;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _guard = new SingleInstanceGuard();
        if (!_guard.TryAcquire())
        {
            MessageBox.Show("ClaudMon is already running.", "ClaudMon",
                MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        _themeService = new ThemeService();
        _themeService.Initialize();

        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .Build();

        var section = config.GetSection("ClaudeMon");
        var refreshInterval = int.TryParse(section["RefreshIntervalSeconds"], out var r) ? r : 60;

        // Plan limits
        var planLimits = new Dictionary<string, long>();
        foreach (var child in section.GetSection("PlanLimits").GetChildren())
        {
            if (long.TryParse(child.Value, out var limit))
                planLimits[child.Key] = limit;
        }

        // Profile discovery
        var configuredProfiles = section.GetSection("Profiles")
            .GetChildren()
            .Select(p => new ProfileConfig
            {
                Name = p["Name"] ?? "Unknown",
                Path = p["Path"] ?? ""
            })
            .Where(p => !string.IsNullOrEmpty(p.Path))
            .ToList();

        var profiles = ProfileDiscoveryService.DiscoverProfiles(
            configuredProfiles.Count > 0 ? configuredProfiles : null);

        if (profiles.Count == 0)
        {
            MessageBox.Show(
                "No Claude profiles found.\n\n" +
                "ClaudMon looks for directories matching ~/.claude* that contain .credentials.json.\n" +
                "You can also configure profiles explicitly in appsettings.json.",
                "ClaudMon - No Profiles",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            Shutdown();
            return;
        }

        var calculator = new UsageCalculator(planLimits);
        _mainViewModel = new MainViewModel(_themeService);

        foreach (var profileConfig in profiles)
        {
            var profileVm = new ProfileViewModel(profileConfig, calculator, refreshInterval);
            _mainViewModel.AddProfile(profileVm);
        }

        _mainViewModel.InitializeAll();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _mainViewModel?.Dispose();
        _themeService?.Dispose();
        _guard?.Dispose();
        base.OnExit(e);
    }
}
