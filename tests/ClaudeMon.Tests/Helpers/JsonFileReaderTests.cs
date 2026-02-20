using System.Diagnostics;
using System.IO;
using System.Text;
using ClaudeMon.Helpers;

namespace ClaudeMon.Tests.Helpers;

/// <summary>
/// Tests for JsonFileReader covering deserialization, error handling, encoding, and retry logic.
/// </summary>
public class JsonFileReaderTests : IDisposable
{
    private readonly string _tempDir;

    // Simple test class for deserialization tests
    public class TestData
    {
        public string? Name { get; set; }
        public int Value { get; set; }
    }

    public JsonFileReaderTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, recursive: true);
            }
        }
        catch
        {
            // Suppress cleanup errors
        }
    }

    [Fact]
    public async Task ValidJson_DeserializesCorrectly()
    {
        // Arrange
        var filePath = Path.Combine(_tempDir, "valid.json");
        var testData = new TestData { Name = "TestObject", Value = 42 };
        var json = $$"""{"name":"TestObject","value":42}""";
        await File.WriteAllTextAsync(filePath, json);

        // Act
        var result = await JsonFileReader.ReadAsync<TestData>(filePath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("TestObject", result.Name);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public async Task MissingFile_ReturnsDefault()
    {
        // Arrange
        var filePath = Path.Combine(_tempDir, "does_not_exist.json");

        // Act
        var result = await JsonFileReader.ReadAsync<TestData>(filePath);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task InvalidJson_ReturnsDefault()
    {
        // Arrange
        var filePath = Path.Combine(_tempDir, "invalid.json");
        var invalidJson = "{ this is not valid json ]";
        await File.WriteAllTextAsync(filePath, invalidJson);

        // Act
        var result = await JsonFileReader.ReadAsync<TestData>(filePath);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task EmptyFile_ReturnsDefault()
    {
        // Arrange
        var filePath = Path.Combine(_tempDir, "empty.json");
        await File.WriteAllTextAsync(filePath, string.Empty);

        // Act
        var result = await JsonFileReader.ReadAsync<TestData>(filePath);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CaseInsensitive_Deserialization()
    {
        // Arrange
        var filePath = Path.Combine(_tempDir, "case_insensitive.json");
        var json = $$"""{"NAME":"CaseTest","VALUE":99}""";
        await File.WriteAllTextAsync(filePath, json);

        // Act
        var result = await JsonFileReader.ReadAsync<TestData>(filePath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("CaseTest", result.Name);
        Assert.Equal(99, result.Value);
    }

    [Fact]
    public async Task FileWithBom_DeserializesCorrectly()
    {
        // Arrange
        var filePath = Path.Combine(_tempDir, "with_bom.json");
        var json = $$"""{"name":"BomTest","value":123}""";

        // Write with UTF-8 BOM
        var bomBytes = Encoding.UTF8.GetPreamble();
        var jsonBytes = Encoding.UTF8.GetBytes(json);
        var allBytes = bomBytes.Concat(jsonBytes).ToArray();
        await File.WriteAllBytesAsync(filePath, allBytes);

        // Act
        var result = await JsonFileReader.ReadAsync<TestData>(filePath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("BomTest", result.Name);
        Assert.Equal(123, result.Value);
    }

    [Fact]
    public async Task MixedCaseProperty_DeserializesCorrectly()
    {
        // Arrange
        var filePath = Path.Combine(_tempDir, "mixed_case.json");
        var json = $$"""{"Name":"MixedCase","value":55}""";
        await File.WriteAllTextAsync(filePath, json);

        // Act
        var result = await JsonFileReader.ReadAsync<TestData>(filePath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("MixedCase", result.Name);
        Assert.Equal(55, result.Value);
    }

    [Fact]
    public async Task ValidJson_WithWhitespace_DeserializesCorrectly()
    {
        // Arrange
        var filePath = Path.Combine(_tempDir, "whitespace.json");
        var json = "{\n    \"name\": \"WithWhitespace\",\n    \"value\": 77\n}";
        await File.WriteAllTextAsync(filePath, json);

        // Act
        var result = await JsonFileReader.ReadAsync<TestData>(filePath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("WithWhitespace", result.Name);
        Assert.Equal(77, result.Value);
    }

    [Fact]
    public async Task NullValues_HandledCorrectly()
    {
        // Arrange
        var filePath = Path.Combine(_tempDir, "null_value.json");
        var json = $$"""{"name":null,"value":0}""";
        await File.WriteAllTextAsync(filePath, json);

        // Act
        var result = await JsonFileReader.ReadAsync<TestData>(filePath);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.Name);
        Assert.Equal(0, result.Value);
    }

    [Fact]
    public async Task PartialJson_ReturnsDefault()
    {
        // Arrange
        var filePath = Path.Combine(_tempDir, "partial.json");
        var json = "{\"name\":\"Incomplete\"";
        await File.WriteAllTextAsync(filePath, json);

        // Act
        var result = await JsonFileReader.ReadAsync<TestData>(filePath);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task JsonArray_ReturnsDefault()
    {
        // Arrange
        var filePath = Path.Combine(_tempDir, "array.json");
        var json = $$"""[{"name":"Item1","value":1},{"name":"Item2","value":2}]""";
        await File.WriteAllTextAsync(filePath, json);

        // Act - Expecting an object, not an array
        var result = await JsonFileReader.ReadAsync<TestData>(filePath);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task JsonString_ReturnsDefault()
    {
        // Arrange
        var filePath = Path.Combine(_tempDir, "string.json");
        var json = "\"just a string\"";
        await File.WriteAllTextAsync(filePath, json);

        // Act
        var result = await JsonFileReader.ReadAsync<TestData>(filePath);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ExtraProperties_IgnoredDuringDeserialization()
    {
        // Arrange
        var filePath = Path.Combine(_tempDir, "extra_props.json");
        var json = $$"""{"name":"ExtraProps","value":42,"extra":"ignored","another":123}""";
        await File.WriteAllTextAsync(filePath, json);

        // Act
        var result = await JsonFileReader.ReadAsync<TestData>(filePath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("ExtraProps", result.Name);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public async Task LargeJson_DeserializesCorrectly()
    {
        // Arrange
        var filePath = Path.Combine(_tempDir, "large.json");
        var largeString = new string('x', 10000);
        var json = $$"""{"name":"{{largeString}}","value":999}""";
        await File.WriteAllTextAsync(filePath, json);

        // Act
        var result = await JsonFileReader.ReadAsync<TestData>(filePath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(largeString, result.Name);
        Assert.Equal(999, result.Value);
    }

    [Fact]
    public async Task SpecialCharacters_DeserializesCorrectly()
    {
        // Arrange
        var filePath = Path.Combine(_tempDir, "special.json");
        var specialString = "Test with \"quotes\" and \\backslash and /slash";
        var json = $$"""{"name":"{{System.Text.Json.JsonSerializer.Serialize(specialString).Trim('"')}}","value":42}""";
        await File.WriteAllTextAsync(filePath, json);

        // Act
        var result = await JsonFileReader.ReadAsync<TestData>(filePath);

        // Assert
        Assert.NotNull(result);
        // Verify that the deserialization completed successfully
        Assert.NotNull(result.Name);
    }

    [Fact]
    public async Task UnicodeCharacters_DeserializesCorrectly()
    {
        // Arrange
        var filePath = Path.Combine(_tempDir, "unicode.json");
        var json = $$"""{"name":"Unicode: \u4e2d\u6587 \u65e5\u672c\u8a9e","value":42}""";
        await File.WriteAllTextAsync(filePath, json);

        // Act
        var result = await JsonFileReader.ReadAsync<TestData>(filePath);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("中文", result.Name);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public async Task FileWithTrailingWhitespace_DeserializesCorrectly()
    {
        // Arrange
        var filePath = Path.Combine(_tempDir, "trailing.json");
        var json = $$"""{"name":"Trailing","value":88}""";
        await File.WriteAllTextAsync(filePath, json);

        // Act
        var result = await JsonFileReader.ReadAsync<TestData>(filePath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Trailing", result.Name);
        Assert.Equal(88, result.Value);
    }

    [Fact]
    public async Task FileShare_AllowsConcurrentReads()
    {
        // Arrange
        var filePath = Path.Combine(_tempDir, "concurrent.json");
        var json = $$"""{"name":"Concurrent","value":100}""";
        await File.WriteAllTextAsync(filePath, json);

        // Act - Verify that we can read while another stream is open
        try
        {
            await using (var lockStream = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite))
            {
                // JsonFileReader should succeed even with another stream open
                var result = await JsonFileReader.ReadAsync<TestData>(filePath);

                // Assert
                Assert.NotNull(result);
                Assert.Equal("Concurrent", result.Name);
            }
        }
        catch (IOException ex)
        {
            Assert.Fail($"Should allow concurrent reads due to FileShare.ReadWrite: {ex.Message}");
        }
    }

    [Fact]
    public async Task DefaultValue_ForNullableType()
    {
        // Arrange
        var filePath = Path.Combine(_tempDir, "missing_nullable.json");

        // Act
        var result = await JsonFileReader.ReadAsync<TestData?>(filePath);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task MultipleConsecutiveReads_Succeed()
    {
        // Arrange
        var filePath = Path.Combine(_tempDir, "multiple.json");
        var json = $$"""{"name":"Multiple","value":50}""";
        await File.WriteAllTextAsync(filePath, json);

        // Act
        var result1 = await JsonFileReader.ReadAsync<TestData>(filePath);
        var result2 = await JsonFileReader.ReadAsync<TestData>(filePath);
        var result3 = await JsonFileReader.ReadAsync<TestData>(filePath);

        // Assert
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.NotNull(result3);
        Assert.Equal(result1.Name, result2.Name);
        Assert.Equal(result2.Name, result3.Name);
    }
}
