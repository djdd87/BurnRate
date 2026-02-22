using System.Text.Json.Serialization;

namespace BurnRate.Models;

/// <summary>
/// Maps to ~/.claude/stats-cache.json (version 2).
/// Contains aggregated usage statistics computed by Claude Code.
/// </summary>
public sealed class StatsCache
{
    [JsonPropertyName("version")]
    public int Version { get; set; }

    [JsonPropertyName("lastComputedDate")]
    public string? LastComputedDate { get; set; }

    [JsonPropertyName("dailyActivity")]
    public List<DailyActivityEntry> DailyActivity { get; set; } = [];

    [JsonPropertyName("dailyModelTokens")]
    public List<DailyModelTokensEntry> DailyModelTokens { get; set; } = [];

    [JsonPropertyName("modelUsage")]
    public Dictionary<string, ModelUsageEntry> ModelUsage { get; set; } = new();

    [JsonPropertyName("totalSessions")]
    public int TotalSessions { get; set; }

    [JsonPropertyName("totalMessages")]
    public int TotalMessages { get; set; }

    [JsonPropertyName("longestSession")]
    public LongestSessionEntry? LongestSession { get; set; }

    [JsonPropertyName("firstSessionDate")]
    public string? FirstSessionDate { get; set; }

    [JsonPropertyName("hourCounts")]
    public Dictionary<string, int> HourCounts { get; set; } = new();

    [JsonPropertyName("totalSpeculationTimeSavedMs")]
    public long TotalSpeculationTimeSavedMs { get; set; }
}

public sealed class DailyActivityEntry
{
    [JsonPropertyName("date")]
    public string? Date { get; set; }

    [JsonPropertyName("messageCount")]
    public int MessageCount { get; set; }

    [JsonPropertyName("sessionCount")]
    public int SessionCount { get; set; }

    [JsonPropertyName("toolCallCount")]
    public int ToolCallCount { get; set; }
}

public sealed class DailyModelTokensEntry
{
    [JsonPropertyName("date")]
    public string? Date { get; set; }

    [JsonPropertyName("tokensByModel")]
    public Dictionary<string, long> TokensByModel { get; set; } = new();
}

public sealed class ModelUsageEntry
{
    [JsonPropertyName("inputTokens")]
    public long InputTokens { get; set; }

    [JsonPropertyName("outputTokens")]
    public long OutputTokens { get; set; }

    [JsonPropertyName("cacheReadInputTokens")]
    public long CacheReadInputTokens { get; set; }

    [JsonPropertyName("cacheCreationInputTokens")]
    public long CacheCreationInputTokens { get; set; }

    [JsonPropertyName("webSearchRequests")]
    public int WebSearchRequests { get; set; }

    [JsonPropertyName("costUSD")]
    public double CostUSD { get; set; }

    [JsonPropertyName("contextWindow")]
    public long ContextWindow { get; set; }

    [JsonPropertyName("maxOutputTokens")]
    public long MaxOutputTokens { get; set; }
}

public sealed class LongestSessionEntry
{
    [JsonPropertyName("sessionId")]
    public string? SessionId { get; set; }

    [JsonPropertyName("duration")]
    public long Duration { get; set; }

    [JsonPropertyName("messageCount")]
    public int MessageCount { get; set; }

    [JsonPropertyName("timestamp")]
    public string? Timestamp { get; set; }
}
