using System.IO;
using BurnRate.Models;

namespace BurnRate.Services;

public static class ProfileDiscoveryService
{
    /// <summary>
    /// Discovers Claude profiles. If explicit profiles are configured, uses those.
    /// Otherwise, auto-scans the user's home directory for directories matching
    /// .claude* that contain a .credentials.json file.
    /// </summary>
    public static List<ProfileConfig> DiscoverProfiles(List<ProfileConfig>? configured)
    {
        // If explicit profiles configured and non-empty, expand env vars and return those
        if (configured is { Count: > 0 })
        {
            return configured
                .Select(p => p with { Path = Environment.ExpandEnvironmentVariables(p.Path) })
                .Where(p => Directory.Exists(p.Path))
                .ToList();
        }

        // Auto-discover: scan user home dir for .claude* directories with .credentials.json
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var profiles = new List<ProfileConfig>();

        foreach (var dir in Directory.EnumerateDirectories(home, ".claude*"))
        {
            var credFile = Path.Combine(dir, ".credentials.json");
            if (!File.Exists(credFile)) continue;

            // Derive a friendly name from the directory name
            var dirName = Path.GetFileName(dir); // e.g., ".claude", ".claude-personal"
            var name = dirName switch
            {
                ".claude" => "Default",
                _ => dirName.Replace(".claude-", "").Replace(".claude", "")
                            // Capitalize first letter
                            is { Length: > 0 } s ? char.ToUpper(s[0]) + s[1..] : dirName
            };

            profiles.Add(new ProfileConfig { Name = name, Path = dir });
        }

        // Sort: "Default" first, then alphabetical
        return profiles
            .OrderBy(p => p.Name != "Default")
            .ThenBy(p => p.Name)
            .ToList();
    }
}
