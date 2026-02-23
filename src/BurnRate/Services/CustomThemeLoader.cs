using System.Diagnostics;
using System.IO;
using System.Text.Json;
using BurnRate.Models;

namespace BurnRate.Services;

public static class CustomThemeLoader
{
    public static IReadOnlyList<CustomTheme> LoadAll()
    {
        var results = new List<CustomTheme>();
        var baseDir = Path.Combine(AppContext.BaseDirectory, "CustomThemes");

        if (!Directory.Exists(baseDir))
            return results;

        foreach (var themeDir in Directory.EnumerateDirectories(baseDir))
        {
            try
            {
                var manifestPath = Path.Combine(themeDir, "theme.json");
                if (!File.Exists(manifestPath))
                    continue;

                var json = File.ReadAllText(manifestPath);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var displayName = root.TryGetProperty("displayName", out var dn)
                    ? dn.GetString() ?? Path.GetFileName(themeDir)
                    : Path.GetFileName(themeDir);

                // Resolve the canonical theme directory path once for validation below.
                var themeDirFull = Path.GetFullPath(themeDir) + Path.DirectorySeparatorChar;

                string? colorsPath = null;
                if (root.TryGetProperty("colorsDictionary", out var cd) && cd.GetString() is string cdName)
                {
                    var candidate = Path.GetFullPath(Path.Combine(themeDir, cdName));
                    // Reject paths that escape the theme directory (path traversal guard).
                    if (candidate.StartsWith(themeDirFull, StringComparison.OrdinalIgnoreCase) && File.Exists(candidate))
                        colorsPath = candidate;
                }

                var faces = new List<FaceImageEntry>();
                if (root.TryGetProperty("faceImages", out var faceArr) && faceArr.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in faceArr.EnumerateArray())
                    {
                        if (!item.TryGetProperty("upToPercent", out var pct)) continue;
                        if (!item.TryGetProperty("image", out var img)) continue;

                        var imagePath = Path.GetFullPath(Path.Combine(themeDir, img.GetString() ?? ""));
                        // Reject paths that escape the theme directory (path traversal guard).
                        if (!imagePath.StartsWith(themeDirFull, StringComparison.OrdinalIgnoreCase)) continue;
                        if (!File.Exists(imagePath)) continue;

                        faces.Add(new FaceImageEntry(pct.GetDouble(), imagePath));
                    }
                }

                results.Add(new CustomTheme
                {
                    Id = Path.GetFileName(themeDir),
                    DisplayName = displayName,
                    ColorsXamlPath = colorsPath,
                    FaceImages = faces
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CustomThemeLoader] Failed to load theme from '{themeDir}': {ex.Message}");
            }
        }

        return results;
    }
}
