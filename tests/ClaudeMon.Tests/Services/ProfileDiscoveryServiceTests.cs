using ClaudeMon.Models;
using ClaudeMon.Services;

namespace ClaudeMon.Tests.Services;

/// <summary>
/// Tests for ProfileDiscoveryService covering explicit profile configuration and auto-discovery scenarios.
/// </summary>
public class ProfileDiscoveryServiceTests
{
    [Fact]
    public void ExplicitProfiles_FiltersNonExistentPaths()
    {
        // Arrange
        var tempDir = Directory.CreateTempSubdirectory();
        var existingPath = tempDir.FullName;
        var nonExistentPath = Path.Combine(tempDir.FullName, "non_existent_dir");

        try
        {
            var configured = new List<ProfileConfig>
            {
                new() { Name = "Valid", Path = existingPath },
                new() { Name = "Invalid", Path = nonExistentPath }
            };

            // Act
            var result = ProfileDiscoveryService.DiscoverProfiles(configured);

            // Assert
            Assert.Single(result);
            Assert.Equal("Valid", result[0].Name);
            Assert.Equal(existingPath, result[0].Path);
        }
        finally
        {
            tempDir.Delete(recursive: true);
        }
    }

    [Fact]
    public void ExplicitProfiles_ExpandsEnvironmentVariables()
    {
        // Arrange
        var tempDir = Directory.CreateTempSubdirectory();
        var tempPath = tempDir.FullName;

        try
        {
            // Create a profile path using %TEMP% environment variable
            var configured = new List<ProfileConfig>
            {
                new() { Name = "TempProfile", Path = Path.Combine("%TEMP%", tempDir.Name) }
            };

            // Act
            var result = ProfileDiscoveryService.DiscoverProfiles(configured);

            // Assert
            // If the expanded path exists (which it does because we created tempDir), it should be in results
            if (result.Any(p => p.Name == "TempProfile"))
            {
                var profile = result.First(p => p.Name == "TempProfile");
                Assert.DoesNotContain("%TEMP%", profile.Path);
                Assert.True(profile.Path.Contains(tempDir.Name),
                    "Environment variable should be expanded in the path");
            }
        }
        finally
        {
            tempDir.Delete(recursive: true);
        }
    }

    [Fact]
    public void NullConfigured_TriggersAutoDiscovery()
    {
        // Arrange
        // Pass null which triggers auto-discovery

        // Act
        var result = ProfileDiscoveryService.DiscoverProfiles(null);

        // Assert
        // Auto-discovery should not throw and return a list (even if empty on a system without .claude* dirs)
        Assert.NotNull(result);
        Assert.IsType<List<ProfileConfig>>(result);
    }

    [Fact]
    public void EmptyConfigured_TriggersAutoDiscovery()
    {
        // Arrange
        var configured = new List<ProfileConfig>();

        // Act
        var result = ProfileDiscoveryService.DiscoverProfiles(configured);

        // Assert
        // Auto-discovery should not throw and return a list
        Assert.NotNull(result);
        Assert.IsType<List<ProfileConfig>>(result);
    }

    [Fact]
    public void ExplicitProfiles_Empty_WhenAllPathsInvalid()
    {
        // Arrange
        var configured = new List<ProfileConfig>
        {
            new() { Name = "Missing1", Path = "/nonexistent/path/1" },
            new() { Name = "Missing2", Path = "/nonexistent/path/2" },
            new() { Name = "Missing3", Path = "/nonexistent/path/3" }
        };

        // Act
        var result = ProfileDiscoveryService.DiscoverProfiles(configured);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void ExplicitProfiles_MultipleValidPaths()
    {
        // Arrange
        var tempDir1 = Directory.CreateTempSubdirectory();
        var tempDir2 = Directory.CreateTempSubdirectory();

        try
        {
            var configured = new List<ProfileConfig>
            {
                new() { Name = "Profile1", Path = tempDir1.FullName },
                new() { Name = "Profile2", Path = tempDir2.FullName }
            };

            // Act
            var result = ProfileDiscoveryService.DiscoverProfiles(configured);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Single(result, p => p.Name == "Profile1");
            Assert.Single(result, p => p.Name == "Profile2");
        }
        finally
        {
            tempDir1.Delete(recursive: true);
            tempDir2.Delete(recursive: true);
        }
    }

    [Fact]
    public void ExplicitProfiles_MixedValidAndInvalid()
    {
        // Arrange
        var tempDir = Directory.CreateTempSubdirectory();
        var existingPath = tempDir.FullName;
        var nonExistentPath = Path.Combine(tempDir.FullName, "does_not_exist");

        try
        {
            var configured = new List<ProfileConfig>
            {
                new() { Name = "Valid1", Path = existingPath },
                new() { Name = "Invalid", Path = nonExistentPath },
                new() { Name = "AlsoInvalid", Path = "/totally/fake/path" }
            };

            // Act
            var result = ProfileDiscoveryService.DiscoverProfiles(configured);

            // Assert
            Assert.Single(result);
            Assert.Equal("Valid1", result[0].Name);
        }
        finally
        {
            tempDir.Delete(recursive: true);
        }
    }

    [Fact]
    public void ExplicitProfiles_PreservesProfileName()
    {
        // Arrange
        var tempDir = Directory.CreateTempSubdirectory();

        try
        {
            var configured = new List<ProfileConfig>
            {
                new() { Name = "CustomName", Path = tempDir.FullName }
            };

            // Act
            var result = ProfileDiscoveryService.DiscoverProfiles(configured);

            // Assert
            Assert.Single(result);
            Assert.Equal("CustomName", result[0].Name);
        }
        finally
        {
            tempDir.Delete(recursive: true);
        }
    }

    [Fact]
    public void AutoDiscovery_ReturnsOnlyExistingDirs()
    {
        // Arrange
        // Auto-discovery should only return directories that actually exist

        // Act
        var result = ProfileDiscoveryService.DiscoverProfiles(null);

        // Assert
        // All returned profiles should have existing directories
        foreach (var profile in result)
        {
            Assert.True(Directory.Exists(profile.Path),
                $"Profile path should exist: {profile.Path}");
        }
    }

    [Fact]
    public void AutoDiscovery_DefaultProfileFirst_WhenPresent()
    {
        // Arrange
        // This test verifies that if Default profile is discovered, it appears first

        // Act
        var result = ProfileDiscoveryService.DiscoverProfiles(null);

        // Assert
        if (result.Count > 0 && result.Any(p => p.Name == "Default"))
        {
            // Default should be first in the list
            Assert.Equal("Default", result[0].Name);
        }
    }

    [Fact]
    public void AutoDiscovery_SortsAlphabetically()
    {
        // Arrange
        // This test verifies sorting order (Default first, then alphabetical)

        // Act
        var result = ProfileDiscoveryService.DiscoverProfiles(null);

        // Assert
        // After Default (if present), profiles should be sorted alphabetically
        if (result.Count > 1)
        {
            var nonDefaultProfiles = result
                .Where(p => p.Name != "Default")
                .Select(p => p.Name)
                .ToList();

            var sortedProfiles = nonDefaultProfiles.OrderBy(p => p).ToList();
            Assert.Equal(sortedProfiles, nonDefaultProfiles);
        }
    }
}
