using System.Text.Json.Serialization;

namespace BurnRate.Models;

/// <summary>
/// Maps to ~/.claude/.claude.json.
/// Captures a small subset of Claude Code configuration flags.
/// </summary>
public sealed class ClaudeConfig
{
    [JsonPropertyName("numStartups")]
    public int NumStartups { get; set; }

    [JsonPropertyName("installMethod")]
    public string? InstallMethod { get; set; }

    [JsonPropertyName("autoUpdates")]
    public bool AutoUpdates { get; set; }
}
