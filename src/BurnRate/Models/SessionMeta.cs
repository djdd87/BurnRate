using System.Text.Json.Serialization;

namespace BurnRate.Models;

/// <summary>
/// Maps to individual JSON files under ~/.claude/usage-data/session-meta/*.json.
/// Each file represents metadata for a single Claude Code session.
/// </summary>
public sealed class SessionMeta
{
    [JsonPropertyName("session_id")]
    public string? SessionId { get; set; }

    [JsonPropertyName("project_path")]
    public string? ProjectPath { get; set; }

    [JsonPropertyName("start_time")]
    public string? StartTime { get; set; }

    [JsonPropertyName("duration_minutes")]
    public double DurationMinutes { get; set; }

    [JsonPropertyName("user_message_count")]
    public int UserMessageCount { get; set; }

    [JsonPropertyName("assistant_message_count")]
    public int AssistantMessageCount { get; set; }

    [JsonPropertyName("tool_counts")]
    public Dictionary<string, int> ToolCounts { get; set; } = new();

    [JsonPropertyName("languages")]
    public Dictionary<string, int> Languages { get; set; } = new();

    [JsonPropertyName("input_tokens")]
    public long InputTokens { get; set; }

    [JsonPropertyName("output_tokens")]
    public long OutputTokens { get; set; }

    [JsonPropertyName("summary")]
    public string? Summary { get; set; }

    [JsonPropertyName("lines_added")]
    public int LinesAdded { get; set; }

    [JsonPropertyName("lines_removed")]
    public int LinesRemoved { get; set; }

    [JsonPropertyName("files_modified")]
    public int FilesModified { get; set; }
}
