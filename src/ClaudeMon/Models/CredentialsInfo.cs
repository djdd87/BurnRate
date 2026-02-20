using System.Text.Json.Serialization;

namespace ClaudeMon.Models;

/// <summary>
/// Maps to ~/.claude/.credentials.json.
/// Only captures subscription/tier metadata - NEVER stores access or refresh tokens.
/// </summary>
public sealed class CredentialsInfo
{
    [JsonPropertyName("claudeAiOauth")]
    public ClaudeAiOAuthInfo? ClaudeAiOAuth { get; set; }
}

public sealed class ClaudeAiOAuthInfo
{
    [JsonPropertyName("subscriptionType")]
    public string? SubscriptionType { get; set; }

    [JsonPropertyName("rateLimitTier")]
    public string? RateLimitTier { get; set; }
}
