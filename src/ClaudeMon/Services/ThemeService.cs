using System.IO;
using System.Text.Json;
using Microsoft.Win32;
using ClaudeMon.Models;

namespace ClaudeMon.Services;

public enum AppThemeMode { Dark, Light, System, Custom }

public sealed class ThemeService : IDisposable
{
    private AppThemeMode _currentMode = AppThemeMode.Dark;
    private AppThemeMode _effectiveTheme = AppThemeMode.Dark;
    private CustomTheme? _activeCustomTheme;
    private bool _disposed;

    public event Action<AppThemeMode>? ThemeChanged;

    public AppThemeMode CurrentMode => _currentMode;
    public AppThemeMode EffectiveTheme => _effectiveTheme;
    public CustomTheme? ActiveCustomTheme => _activeCustomTheme;

    public void Initialize(IReadOnlyList<CustomTheme> available)
    {
        var (mode, customThemeId) = LoadSetting();
        _currentMode = mode;

        if (mode == AppThemeMode.Custom && customThemeId != null)
            _activeCustomTheme = available.FirstOrDefault(t => t.Id == customThemeId);

        ApplyTheme();
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
    }

    public void SetTheme(AppThemeMode mode)
    {
        if (mode != AppThemeMode.Custom)
            _activeCustomTheme = null;
        _currentMode = mode;
        SaveSetting();
        ApplyTheme();
    }

    public void SetCustomTheme(CustomTheme theme)
    {
        _activeCustomTheme = theme;
        _currentMode = AppThemeMode.Custom;
        SaveSetting();
        ApplyTheme();
    }

    private void ApplyTheme()
    {
        AppThemeMode resolved = _currentMode == AppThemeMode.System
            ? ResolveSystemTheme()
            : _currentMode;

        _effectiveTheme = resolved;

        Uri uri;
        if (resolved == AppThemeMode.Custom && _activeCustomTheme?.ColorsXamlPath != null)
            uri = new Uri(_activeCustomTheme.ColorsXamlPath, UriKind.Absolute);
        else if (resolved == AppThemeMode.Custom)
            uri = new Uri("pack://application:,,,/Themes/Colors_Dark.xaml");
        else
            uri = resolved switch
            {
                AppThemeMode.Light => new Uri("pack://application:,,,/Themes/Colors_Light.xaml"),
                _                  => new Uri("pack://application:,,,/Themes/Colors_Dark.xaml"),
            };

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

    private static (AppThemeMode mode, string? customThemeId) LoadSetting()
    {
        try
        {
            var path = SettingsPath;
            if (!File.Exists(path)) return (AppThemeMode.Dark, null);
            var json = File.ReadAllText(path);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("Theme", out var themeProp))
                return (AppThemeMode.Dark, null);

            var themeStr = themeProp.GetString();

            // Legacy migration: "Doom" was a built-in enum value, now it's a custom theme
            if (themeStr == "Doom")
                return (AppThemeMode.Custom, "Doom");

            if (!Enum.TryParse<AppThemeMode>(themeStr, out var mode))
                return (AppThemeMode.Dark, null);

            string? customId = null;
            if (mode == AppThemeMode.Custom && root.TryGetProperty("CustomTheme", out var ctProp))
                customId = ctProp.GetString();

            return (mode, customId);
        }
        catch
        {
            return (AppThemeMode.Dark, null);
        }
    }

    private void SaveSetting()
    {
        try
        {
            var path = SettingsPath;
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            object payload = _currentMode == AppThemeMode.Custom && _activeCustomTheme != null
                ? new { Theme = _currentMode.ToString(), CustomTheme = _activeCustomTheme.Id }
                : new { Theme = _currentMode.ToString() };

            var json = JsonSerializer.Serialize(payload);
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
