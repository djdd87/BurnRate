using System.Text.Json;
using ClaudeMon.Models;

namespace ClaudeMon.Tests.Models;

/// <summary>
/// Tests for JSON deserialization of model classes using System.Text.Json.
/// Validates that models correctly deserialize from their respective JSON file formats.
/// </summary>
public class JsonSerializationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public void StatsCache_DeserializesMinimalJson()
    {
        // Arrange
        var json = """
        {
            "version": 2,
            "lastComputedDate": "2026-02-20",
            "totalSessions": 128,
            "totalMessages": 3456,
            "dailyActivity": [
                {
                    "date": "2026-02-20",
                    "messageCount": 10,
                    "sessionCount": 2,
                    "toolCallCount": 5
                }
            ],
            "dailyModelTokens": [
                {
                    "date": "2026-02-20",
                    "tokensByModel": {
                        "claude-3-5-sonnet": 150000
                    }
                }
            ],
            "modelUsage": {
                "claude-3-5-sonnet": {
                    "inputTokens": 100000,
                    "outputTokens": 50000,
                    "cacheReadInputTokens": 0,
                    "cacheCreationInputTokens": 0,
                    "webSearchRequests": 0,
                    "costUSD": 0.75,
                    "contextWindow": 200000,
                    "maxOutputTokens": 8192
                }
            },
            "totalSpeculationTimeSavedMs": 12500
        }
        """;

        // Act
        var cache = JsonSerializer.Deserialize<StatsCache>(json, JsonOptions);

        // Assert
        Assert.NotNull(cache);
        Assert.Equal(2, cache.Version);
        Assert.Equal("2026-02-20", cache.LastComputedDate);
        Assert.Equal(128, cache.TotalSessions);
        Assert.Equal(3456, cache.TotalMessages);
        Assert.Single(cache.DailyActivity);
        Assert.Equal(10, cache.DailyActivity[0].MessageCount);
        Assert.Single(cache.DailyModelTokens);
        Assert.True(cache.ModelUsage.ContainsKey("claude-3-5-sonnet"));
        Assert.Equal(12500L, cache.TotalSpeculationTimeSavedMs);
    }

    [Fact]
    public void StatsCache_DeserializesComplexJson()
    {
        // Arrange
        var json = """
        {
            "version": 2,
            "lastComputedDate": "2026-02-20",
            "totalSessions": 250,
            "totalMessages": 7890,
            "dailyActivity": [
                {
                    "date": "2026-02-20",
                    "messageCount": 15,
                    "sessionCount": 3,
                    "toolCallCount": 8
                },
                {
                    "date": "2026-02-19",
                    "messageCount": 12,
                    "sessionCount": 2,
                    "toolCallCount": 5
                }
            ],
            "dailyModelTokens": [
                {
                    "date": "2026-02-20",
                    "tokensByModel": {
                        "claude-3-5-sonnet": 200000,
                        "claude-3-opus": 150000
                    }
                }
            ],
            "modelUsage": {
                "claude-3-5-sonnet": {
                    "inputTokens": 500000,
                    "outputTokens": 250000,
                    "cacheReadInputTokens": 0,
                    "cacheCreationInputTokens": 0,
                    "webSearchRequests": 3,
                    "costUSD": 3.75,
                    "contextWindow": 200000,
                    "maxOutputTokens": 8192
                },
                "claude-3-opus": {
                    "inputTokens": 400000,
                    "outputTokens": 200000,
                    "cacheReadInputTokens": 100000,
                    "cacheCreationInputTokens": 50000,
                    "webSearchRequests": 2,
                    "costUSD": 6.50,
                    "contextWindow": 200000,
                    "maxOutputTokens": 4096
                }
            },
            "longestSession": {
                "sessionId": "sess-123",
                "duration": 3600000,
                "messageCount": 50,
                "timestamp": "2026-02-15T14:30:00Z"
            },
            "firstSessionDate": "2024-01-01",
            "hourCounts": {
                "14": 10,
                "15": 8
            },
            "totalSpeculationTimeSavedMs": 45000
        }
        """;

        // Act
        var cache = JsonSerializer.Deserialize<StatsCache>(json, JsonOptions);

        // Assert
        Assert.NotNull(cache);
        Assert.Equal(250, cache.TotalSessions);
        Assert.Equal(2, cache.DailyActivity.Count);
        Assert.Equal(2, cache.DailyModelTokens[0].TokensByModel.Count);
        Assert.Equal(2, cache.ModelUsage.Count);
        Assert.NotNull(cache.LongestSession);
        Assert.Equal("sess-123", cache.LongestSession.SessionId);
        Assert.Equal("2024-01-01", cache.FirstSessionDate);
        Assert.Equal(2, cache.HourCounts.Count);
        Assert.Equal(45000L, cache.TotalSpeculationTimeSavedMs);
    }

    [Fact]
    public void CredentialsInfo_DeserializesWithClaudeAiOAuth()
    {
        // Arrange
        var json = """
        {
            "claudeAiOauth": {
                "subscriptionType": "pro",
                "rateLimitTier": "default_claude_max_5x"
            }
        }
        """;

        // Act
        var credentials = JsonSerializer.Deserialize<CredentialsInfo>(json, JsonOptions);

        // Assert
        Assert.NotNull(credentials);
        Assert.NotNull(credentials.ClaudeAiOAuth);
        Assert.Equal("pro", credentials.ClaudeAiOAuth.SubscriptionType);
        Assert.Equal("default_claude_max_5x", credentials.ClaudeAiOAuth.RateLimitTier);
    }

    [Fact]
    public void CredentialsInfo_DeserializesWithNullOAuth()
    {
        // Arrange
        var json = """
        {
            "claudeAiOauth": null
        }
        """;

        // Act
        var credentials = JsonSerializer.Deserialize<CredentialsInfo>(json, JsonOptions);

        // Assert
        Assert.NotNull(credentials);
        Assert.Null(credentials.ClaudeAiOAuth);
    }

    [Fact]
    public void CredentialsInfo_DeserializesEmptyObject()
    {
        // Arrange
        var json = "{}";

        // Act
        var credentials = JsonSerializer.Deserialize<CredentialsInfo>(json, JsonOptions);

        // Assert
        Assert.NotNull(credentials);
        Assert.Null(credentials.ClaudeAiOAuth);
    }

    [Fact]
    public void LiveUsageResponse_DeserializesFiveHourWindow()
    {
        // Arrange
        var json = """
        {
            "five_hour": {
                "utilization": 35.5,
                "resets_at": "2026-02-20T20:00:00Z"
            }
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize<LiveUsageResponse>(json, JsonOptions);

        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.FiveHour);
        Assert.Equal(35.5, response.FiveHour.Utilization);
        Assert.Equal("2026-02-20T20:00:00Z", response.FiveHour.ResetsAt);
    }

    [Fact]
    public void LiveUsageResponse_DeserializesSevenDayWindow()
    {
        // Arrange
        var json = """
        {
            "seven_day": {
                "utilization": 50.0,
                "resets_at": "2026-02-27T00:00:00Z"
            }
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize<LiveUsageResponse>(json, JsonOptions);

        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.SevenDay);
        Assert.Equal(50.0, response.SevenDay.Utilization);
        Assert.Equal("2026-02-27T00:00:00Z", response.SevenDay.ResetsAt);
    }

    [Fact]
    public void LiveUsageResponse_DeserializesMultipleWindows()
    {
        // Arrange
        var json = """
        {
            "five_hour": {
                "utilization": 25.0,
                "resets_at": "2026-02-20T19:30:00Z"
            },
            "seven_day": {
                "utilization": 45.5,
                "resets_at": "2026-02-27T00:00:00Z"
            },
            "seven_day_oauth_apps": {
                "utilization": 15.0,
                "resets_at": "2026-02-27T00:00:00Z"
            },
            "seven_day_opus": {
                "utilization": 60.0,
                "resets_at": "2026-02-27T00:00:00Z"
            },
            "seven_day_sonnet": {
                "utilization": 40.0,
                "resets_at": "2026-02-27T00:00:00Z"
            }
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize<LiveUsageResponse>(json, JsonOptions);

        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.FiveHour);
        Assert.Equal(25.0, response.FiveHour.Utilization);
        Assert.NotNull(response.SevenDay);
        Assert.Equal(45.5, response.SevenDay.Utilization);
        Assert.NotNull(response.SevenDayOAuthApps);
        Assert.Equal(15.0, response.SevenDayOAuthApps.Utilization);
        Assert.NotNull(response.SevenDayOpus);
        Assert.Equal(60.0, response.SevenDayOpus.Utilization);
        Assert.NotNull(response.SevenDaySonnet);
        Assert.Equal(40.0, response.SevenDaySonnet.Utilization);
    }

    [Fact]
    public void LiveUsageResponse_DeserializesExtraUsage()
    {
        // Arrange
        var json = """
        {
            "extra_usage": {
                "is_enabled": true,
                "monthly_limit": 500.00,
                "used_credits": 125.50,
                "utilization": 25.1,
                "currency": "USD"
            }
        }
        """;

        // Act
        var response = JsonSerializer.Deserialize<LiveUsageResponse>(json, JsonOptions);

        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.ExtraUsage);
        Assert.True(response.ExtraUsage.IsEnabled);
        Assert.Equal(500.00, response.ExtraUsage.MonthlyLimit);
        Assert.Equal(125.50, response.ExtraUsage.UsedCredits);
        Assert.Equal(25.1, response.ExtraUsage.Utilization);
        Assert.Equal("USD", response.ExtraUsage.Currency);
    }

    [Fact]
    public void SessionMeta_DeserializesBasicSession()
    {
        // Arrange
        var json = """
        {
            "session_id": "session-abc123",
            "project_path": "/path/to/project",
            "start_time": "2026-02-20T14:30:00Z",
            "duration_minutes": 45.5,
            "user_message_count": 12,
            "assistant_message_count": 15,
            "input_tokens": 50000,
            "output_tokens": 75000
        }
        """;

        // Act
        var meta = JsonSerializer.Deserialize<SessionMeta>(json, JsonOptions);

        // Assert
        Assert.NotNull(meta);
        Assert.Equal("session-abc123", meta.SessionId);
        Assert.Equal("/path/to/project", meta.ProjectPath);
        Assert.Equal("2026-02-20T14:30:00Z", meta.StartTime);
        Assert.Equal(45.5, meta.DurationMinutes);
        Assert.Equal(12, meta.UserMessageCount);
        Assert.Equal(15, meta.AssistantMessageCount);
        Assert.Equal(50000L, meta.InputTokens);
        Assert.Equal(75000L, meta.OutputTokens);
    }

    [Fact]
    public void SessionMeta_DeserializesCompleteSession()
    {
        // Arrange
        var json = """
        {
            "session_id": "session-xyz789",
            "project_path": "/workspace/myapp",
            "start_time": "2026-02-20T10:00:00Z",
            "duration_minutes": 120.75,
            "user_message_count": 25,
            "assistant_message_count": 30,
            "tool_counts": {
                "read_file": 8,
                "write_file": 5,
                "run_command": 12
            },
            "languages": {
                "csharp": 150,
                "xaml": 50,
                "json": 25
            },
            "input_tokens": 200000,
            "output_tokens": 150000,
            "summary": "Implemented feature X and fixed bugs",
            "lines_added": 425,
            "lines_removed": 120,
            "files_modified": 8
        }
        """;

        // Act
        var meta = JsonSerializer.Deserialize<SessionMeta>(json, JsonOptions);

        // Assert
        Assert.NotNull(meta);
        Assert.Equal("session-xyz789", meta.SessionId);
        Assert.Equal(120.75, meta.DurationMinutes);
        Assert.Equal(3, meta.ToolCounts.Count);
        Assert.Equal(8, meta.ToolCounts["read_file"]);
        Assert.Equal(3, meta.Languages.Count);
        Assert.Equal(150, meta.Languages["csharp"]);
        Assert.Equal("Implemented feature X and fixed bugs", meta.Summary);
        Assert.Equal(425, meta.LinesAdded);
        Assert.Equal(120, meta.LinesRemoved);
        Assert.Equal(8, meta.FilesModified);
    }

    [Fact]
    public void SessionMeta_DeserializesWithEmptyCollections()
    {
        // Arrange
        var json = """
        {
            "session_id": "session-empty",
            "tool_counts": {},
            "languages": {}
        }
        """;

        // Act
        var meta = JsonSerializer.Deserialize<SessionMeta>(json, JsonOptions);

        // Assert
        Assert.NotNull(meta);
        Assert.Equal("session-empty", meta.SessionId);
        Assert.Empty(meta.ToolCounts);
        Assert.Empty(meta.Languages);
    }

    [Fact]
    public void ClaudeConfig_DeserializesBasicConfig()
    {
        // Arrange
        var json = """
        {
            "numStartups": 42,
            "installMethod": "vsce-install",
            "autoUpdates": true
        }
        """;

        // Act
        var config = JsonSerializer.Deserialize<ClaudeConfig>(json, JsonOptions);

        // Assert
        Assert.NotNull(config);
        Assert.Equal(42, config.NumStartups);
        Assert.Equal("vsce-install", config.InstallMethod);
        Assert.True(config.AutoUpdates);
    }

    [Fact]
    public void ClaudeConfig_DeserializesDisabledAutoUpdates()
    {
        // Arrange
        var json = """
        {
            "numStartups": 1,
            "installMethod": "manual",
            "autoUpdates": false
        }
        """;

        // Act
        var config = JsonSerializer.Deserialize<ClaudeConfig>(json, JsonOptions);

        // Assert
        Assert.NotNull(config);
        Assert.False(config.AutoUpdates);
    }

    [Fact]
    public void ClaudeConfig_DeserializesEmptyObject()
    {
        // Arrange
        var json = "{}";

        // Act
        var config = JsonSerializer.Deserialize<ClaudeConfig>(json, JsonOptions);

        // Assert
        Assert.NotNull(config);
        Assert.Equal(0, config.NumStartups);
        Assert.Null(config.InstallMethod);
        Assert.False(config.AutoUpdates);
    }

    [Fact]
    public void OAuthCredentialsFile_DeserializesAccessToken()
    {
        // Arrange
        var json = """
        {
            "claudeAiOauth": {
                "accessToken": "sk_live_abc123xyz",
                "refreshToken": "refresh_token_here",
                "expiresAt": 1708444800000,
                "scopes": ["read", "write"],
                "subscriptionType": "pro",
                "rateLimitTier": "default_claude_max_5x"
            }
        }
        """;

        // Act
        var credentials = JsonSerializer.Deserialize<OAuthCredentialsFile>(json, JsonOptions);

        // Assert
        Assert.NotNull(credentials);
        Assert.NotNull(credentials.ClaudeAiOAuth);
        Assert.Equal("sk_live_abc123xyz", credentials.ClaudeAiOAuth.AccessToken);
        Assert.Equal("refresh_token_here", credentials.ClaudeAiOAuth.RefreshToken);
        Assert.Equal(1708444800000L, credentials.ClaudeAiOAuth.ExpiresAt);
        Assert.Equal(2, credentials.ClaudeAiOAuth.Scopes?.Count);
        Assert.Equal("pro", credentials.ClaudeAiOAuth.SubscriptionType);
        Assert.Equal("default_claude_max_5x", credentials.ClaudeAiOAuth.RateLimitTier);
    }

    [Fact]
    public void OAuthCredentialsFile_DeserializesMultipleScopes()
    {
        // Arrange
        var json = """
        {
            "claudeAiOauth": {
                "accessToken": "token_abc",
                "expiresAt": 1708444800000,
                "scopes": ["read:user", "read:projects", "write:projects", "usage:read"],
                "subscriptionType": "max",
                "rateLimitTier": "default_claude_max_10x"
            }
        }
        """;

        // Act
        var credentials = JsonSerializer.Deserialize<OAuthCredentialsFile>(json, JsonOptions);

        // Assert
        Assert.NotNull(credentials?.ClaudeAiOAuth?.Scopes);
        Assert.Equal(4, credentials.ClaudeAiOAuth.Scopes.Count);
        Assert.Contains("usage:read", credentials.ClaudeAiOAuth.Scopes);
    }

    [Fact]
    public void OAuthCredentialsFile_DeserializesWithNullScopes()
    {
        // Arrange
        var json = """
        {
            "claudeAiOauth": {
                "accessToken": "token",
                "expiresAt": 1708444800000,
                "scopes": null,
                "subscriptionType": "free"
            }
        }
        """;

        // Act
        var credentials = JsonSerializer.Deserialize<OAuthCredentialsFile>(json, JsonOptions);

        // Assert
        Assert.NotNull(credentials);
        Assert.NotNull(credentials.ClaudeAiOAuth);
        Assert.Null(credentials.ClaudeAiOAuth.Scopes);
    }

    [Fact]
    public void OAuthTokenInfo_PreservesLargeTimestamp()
    {
        // Arrange - Unix timestamp in milliseconds far in future
        var json = """
        {
            "claudeAiOauth": {
                "expiresAt": 9999999999999
            }
        }
        """;

        // Act
        var credentials = JsonSerializer.Deserialize<OAuthCredentialsFile>(json, JsonOptions);

        // Assert
        Assert.NotNull(credentials?.ClaudeAiOAuth);
        Assert.Equal(9999999999999L, credentials.ClaudeAiOAuth.ExpiresAt);
    }

    [Fact]
    public void MultipleModels_SnakeCasePropertyMapping()
    {
        // Arrange - test that snake_case properties are correctly mapped
        var statsJson = """
        {
            "version": 2,
            "totalSessions": 10,
            "totalMessages": 50,
            "totalSpeculationTimeSavedMs": 5000
        }
        """;

        var sessionJson = """
        {
            "session_id": "test",
            "start_time": "2026-02-20T00:00:00Z",
            "duration_minutes": 30.0,
            "user_message_count": 5,
            "output_tokens": 1000
        }
        """;

        // Act
        var stats = JsonSerializer.Deserialize<StatsCache>(statsJson, JsonOptions);
        var session = JsonSerializer.Deserialize<SessionMeta>(sessionJson, JsonOptions);

        // Assert
        Assert.NotNull(stats);
        Assert.Equal(10, stats.TotalSessions);
        Assert.Equal(5000L, stats.TotalSpeculationTimeSavedMs);
        Assert.NotNull(session);
        Assert.Equal("test", session.SessionId);
        Assert.Equal(5, session.UserMessageCount);
        Assert.Equal(1000L, session.OutputTokens);
    }
}
