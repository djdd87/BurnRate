namespace ClaudeMon.Models;

/// <summary>
/// Configuration for a single Claude profile (data directory).
/// </summary>
public sealed record ProfileConfig
{
    /// <summary>
    /// Display name for this profile (e.g., "Work", "Personal").
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Absolute path to the .claude* directory.
    /// </summary>
    public required string Path { get; init; }
}
