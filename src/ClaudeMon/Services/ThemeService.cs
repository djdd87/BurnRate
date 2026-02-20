using System.IO;
using System.Text.Json;
using Microsoft.Win32;

namespace ClaudeMon.Services;

public enum AppThemeMode { Dark, Light, System }

public sealed class ThemeService : IDisposable
{
    private AppThemeMode _currentMode = AppThemeMode.Dark;
    private AppThemeMode _effectiveTheme = AppThemeMode.Dark;
    private bool _disposed;

    public event Action<AppThemeMode>? ThemeChanged;

    public AppThemeMode CurrentMode => _currentMode;
    public AppThemeMode EffectiveTheme => _effectiveTheme;

    public void Initialize()
    {
        _currentMode = LoadSetting();
        ApplyTheme();
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
    }

    public void SetTheme(AppThemeMode mode)
    {
        _currentMode = mode;
        SaveSetting(mode);
        ApplyTheme();
    }

    private void ApplyTheme()
    {
        var resolved = _currentMode == AppThemeMode.System ? ResolveSystemTheme() : _currentMode;
        _effectiveTheme = resolved;

        var uri = resolved == AppThemeMode.Light
            ? new Uri("pack://application:,,,/Themes/Colors_Light.xaml")
            : new Uri("pack://application:,,,/Themes/Colors_Dark.xaml");

        var dict = new System.Windows.ResourceDictionary { Source = uri };
        System.Windows.Application.Current.Resources.MergedDictionaries[0] = dict;

        ThemeChanged?.Invoke(resolved);
    }

    private static AppThemeMode ResolveSystemTheme()
    {
        try
        {
            var value = Registry.GetValue(
                @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                "AppsUseLightTheme",
                1);
            return value is int v && v == 0 ? AppThemeMode.Dark : AppThemeMode.Light;
        }
        catch
        {
            return AppThemeMode.Dark;
        }
    }

    private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (_currentMode != AppThemeMode.System) return;
        System.Windows.Application.Current.Dispatcher.Invoke(ApplyTheme);
    }

    private static string SettingsPath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ClaudeMon",
            "settings.json");

    private static AppThemeMode LoadSetting()
    {
        try
        {
            var path = SettingsPath;
            if (!File.Exists(path)) return AppThemeMode.Dark;
            var json = File.ReadAllText(path);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("Theme", out var prop)
                && Enum.TryParse<AppThemeMode>(prop.GetString(), out var mode))
                return mode;
        }
        catch { }
        return AppThemeMode.Dark;
    }

    private static void SaveSetting(AppThemeMode mode)
    {
        try
        {
            var path = SettingsPath;
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var json = JsonSerializer.Serialize(new { Theme = mode.ToString() });
            File.WriteAllText(path, json);
        }
        catch { }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
    }
}
