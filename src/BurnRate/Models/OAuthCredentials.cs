using System.Text.Json.Serialization;

namespace BurnRate.Models;

/// <summary>
/// Full OAuth credentials from .credentials.json, including tokens needed for API calls.
/// These are read-only from disk - never persisted by this app.
/// </summary>
public sealed class OAuthCredentialsFile
{
    [JsonPropertyName("claudeAiOauth")]
    public OAuthTokenInfo? ClaudeAiOAuth { get; set; }
}

public sealed class OAuthTokenInfo
{
    [JsonPropertyName("accessToken")]
    public string? AccessToken { get; set; }

    [JsonPropertyName("refreshToken")]
    public string? RefreshToken { get; set; }

    [JsonPropertyName("expiresAt")]
    public long ExpiresAt { get; set; }

    [JsonPropertyName("scopes")]
    public List<string>? Scopes { get; set; }

    [JsonPropertyName("subscriptionType")]
    public string? SubscriptionType { get; set; }

    [JsonPropertyName("rateLimitTier")]
    public string? RateLimitTier { get; set; }
}
