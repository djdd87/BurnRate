# CLAUDE.md - BurnRate

## Project Overview

BurnRate is a Windows system tray application that monitors Claude Code usage across multiple profiles. Built with WPF (.NET 10), it shows real-time usage percentages, token counts, session stats, and cost estimates via tray icons and a popup dashboard.

## Build & Run

```bash
dotnet build src/BurnRate/BurnRate.csproj
dotnet run --project src/BurnRate/BurnRate.csproj
```

No tests exist yet. The app targets `net10.0-windows` and requires Windows.

## Project Structure

```
src/BurnRate/
  Models/          # Data models (ProfileConfig, UsageSummary, StatsCache, etc.)
  Services/        # Data access, file watching, live API, icon rendering
  ViewModels/      # MainViewModel, ProfileViewModel (MVVM with CommunityToolkit.Mvvm)
  Views/           # MainWindow, UsageGaugeControl, StatCard, ActivityChart, ModelUsageBar
  Themes/          # Colors.xaml, DarkTheme.xaml, Controls.xaml
  Helpers/         # JsonFileReader, SingleInstanceGuard, WindowPositioner
  Converters/      # TokenFormatter, PercentToColor, CountToVisibility, Equality
  appsettings.json # Configuration (refresh interval, plan limits, profiles)
```

## Data Sources (Priority Order)

### 1. JSONL Conversation Files (primary for current/recent data)
- Path: `~/.claude*/projects/**/*.jsonl` (excludes `subagents/` subdirectories)
- Each file = one conversation. Each line = one turn (JSON object).
- Used for: today's messages/tokens/sessions, 7-day model breakdown, activity chart, weekly token total.
- **Always prefer JSONL over stats-cache for anything recent** — stats-cache is only recomputed periodically by Claude Code and is often stale for the current day.

Message counting rules:
- Only count `type: "user"` entries where `message.content` is a non-`<`-prefixed string or an array with a `"text"` block.
- Exclude tool results (`tool_result` arrays) and system injections (content starting with `<`).
- Session count: each JSONL file with at least one human message on a date = 1 session.

Token counting:
- Only count `message.usage.output_tokens` from `type: "assistant"` entries.
- Never include cache creation/read tokens (they inflate counts ~700x).

### 2. stats-cache.json (secondary - aggregated history)
- Path: `~/.claude*/stats-cache.json`
- Used for: lifetime `totalSessions`, `totalMessages`, `modelUsage[].costUSD`, `totalSpeculationTimeSavedMs`.
- **Not reliable for current-day data** — may have no entry for today at all.

### 3. Anthropic Live API (authoritative percentages)
- Endpoint: `GET https://api.anthropic.com/api/oauth/usage`
- Auth: Bearer token read passively from `.credentials.json` (never refreshed by this app).
- Headers: `anthropic-beta: oauth-2025-04-20`, `User-Agent: BurnRate/1.0`
- Returns 5-hour session window and 7-day weekly window utilization percentages + reset times.
- Falls back gracefully to local estimates on any failure.

### 4. .credentials.json (metadata only)
- Path: `~/.claude*/.credentials.json`
- Used for: `rateLimitTier`, `subscriptionType`, and `accessToken` (for live API).

## Key Architecture Decisions

- **JSONL over stats-cache**: stats-cache.json lags behind reality. Every metric that needs to be current is sourced from JSONL scanning. stats-cache is only used for lifetime aggregates.
- **Passive token reading**: The app never writes or refreshes OAuth tokens. It reads what Claude Code has stored.
- **`UsageSummary.UpdateFrom()`**: Mutates an existing instance rather than replacing it, so WPF bindings stay connected.
- **FileSystemWatcher + poll timer**: Watcher for near-instant updates; poll timer as fallback (watcher can fail on some systems).
- **500ms debounce**: Collapses rapid file changes into a single refresh.
- **Reverse initialization order**: Tray icons are initialized in reverse profile order so "Default" is created last and appears closest to the clock.
- **`FileShare.ReadWrite`**: All file reads use this flag so we don't block Claude Code's open handles.
- **JsonFileReader retries**: Up to 3 retries with 200ms delay on IOException.

## Plan Name Mapping

Known `(rateLimitTier, subscriptionType)` combinations are mapped to friendly names in `ProfileViewModel.KnownPlans`. When adding new plans, add entries there. The fallback formatter strips `default_claude_`/`default_` prefixes and capitalizes.

## Configuration (appsettings.json)

- `RefreshIntervalSeconds`: Poll interval (default 60).
- `Profiles`: Empty = auto-discover `~/.claude*` directories with `.credentials.json`. Can be set explicitly.
- `PlanLimits`: Maps `rateLimitTier` to weekly token limits. Unknown tiers default to 2,500,000.

## Icon Color Thresholds

Used consistently across tray icons, gauge, and percentage text:
- Grey: no data (percentage < 0)
- Green: 0-50%
- Amber: 51-80%
- Red: 81-100%

## Dependencies

- `Hardcodet.NotifyIcon.Wpf` — system tray icons
- `CommunityToolkit.Mvvm` — MVVM source generators (`[ObservableProperty]`, `[RelayCommand]`)
- `MaterialDesignThemes` — imported but used lightly
- `Microsoft.Extensions.Configuration.*` — appsettings.json loading
