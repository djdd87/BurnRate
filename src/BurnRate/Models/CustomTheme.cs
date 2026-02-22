namespace BurnRate.Models;

public sealed class CustomTheme
{
    public required string Id { get; init; }          // folder name, e.g. "Doom"
    public required string DisplayName { get; init; } // from theme.json
    public string? ColorsXamlPath { get; init; }      // absolute path or null
    public IReadOnlyList<FaceImageEntry> FaceImages { get; init; } = [];

    /// <summary>
    /// Returns the absolute path of the face image appropriate for the given percentage,
    /// or null if this theme has no face images.
    /// </summary>
    public string? ResolveFacePath(double percentage)
    {
        foreach (var entry in FaceImages)
            if (percentage <= entry.UpToPercent)
                return entry.AbsoluteImagePath;
        return FaceImages.Count > 0 ? FaceImages[^1].AbsoluteImagePath : null;
    }
}

public sealed record FaceImageEntry(double UpToPercent, string AbsoluteImagePath);
