using System.IO;
using ClaudeMon.Helpers;
using ClaudeMon.Models;

namespace ClaudeMon.Services;

/// <summary>
/// Reads and deserializes all Claude Code data files from the local ~/.claude/ directory.
/// </summary>
public class ClaudeDataService
{
    private readonly string _claudePath;

    public ClaudeDataService(string claudePath)
    {
        _claudePath = Environment.ExpandEnvironmentVariables(claudePath);
    }

    /// <summary>
    /// Reads the aggregated usage statistics from stats-cache.json.
    /// </summary>
    public async Task<StatsCache?> GetStatsCacheAsync()
        => await JsonFileReader.ReadAsync<StatsCache>(Path.Combine(_claudePath, "stats-cache.json"));

    /// <summary>
    /// Reads subscription/tier metadata from .credentials.json.
    /// Returns the nested OAuth info (never stores tokens).
    /// </summary>
    public async Task<ClaudeAiOAuthInfo?> GetCredentialsAsync()
    {
        var file = await JsonFileReader.ReadAsync<CredentialsInfo>(
            Path.Combine(_claudePath, ".credentials.json"));
        return file?.ClaudeAiOAuth;
    }

    /// <summary>
    /// Reads Claude Code configuration flags from .claude.json.
    /// </summary>
    public async Task<ClaudeConfig?> GetConfigAsync()
        => await JsonFileReader.ReadAsync<ClaudeConfig>(Path.Combine(_claudePath, ".claude.json"));

    /// <summary>
    /// Reads all session-meta JSON files from the last N days.
    /// Files live under ~/.claude/usage-data/session-meta/*.json.
    /// </summary>
    public async Task<List<SessionMeta>> GetRecentSessionsAsync(int days = 7)
    {
        var dir = Path.Combine(_claudePath, "usage-data", "session-meta");
        var result = new List<SessionMeta>();

        if (!Directory.Exists(dir))
            return result;

        var cutoff = DateTime.UtcNow.AddDays(-days);

        foreach (var file in Directory.EnumerateFiles(dir, "*.json"))
        {
            var session = await JsonFileReader.ReadAsync<SessionMeta>(file);
            if (session is null)
                continue;

            // StartTime is stored as an ISO-8601 string; parse to compare with cutoff.
            if (DateTime.TryParse(session.StartTime, null,
                    System.Globalization.DateTimeStyles.RoundtripKind, out var startTime)
                && startTime >= cutoff)
            {
                result.Add(session);
            }
        }

        return result;
    }
}
