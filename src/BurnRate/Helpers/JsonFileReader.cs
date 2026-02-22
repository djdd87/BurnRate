using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace BurnRate.Helpers;

public static class JsonFileReader
{
    private const int MaxRetries = 3;
    private const int RetryDelayMs = 200;

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static async Task<T?> ReadAsync<T>(string filePath)
    {
        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                await using var stream = new FileStream(
                    filePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete);

                return await JsonSerializer.DeserializeAsync<T>(stream, Options);
            }
            catch (IOException ex) when (attempt < MaxRetries)
            {
                Debug.WriteLine(
                    $"JsonFileReader: IOException reading '{filePath}' (attempt {attempt}/{MaxRetries}): {ex.Message}");
                await Task.Delay(RetryDelayMs);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    $"JsonFileReader: Failed to read '{filePath}': {ex.Message}");
                return default;
            }
        }

        return default;
    }
}
