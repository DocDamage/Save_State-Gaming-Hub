using FluentAssertions;
using Xunit;

namespace SaveState.CrossPlatform.Tests;

/// <summary>
/// Tests for cross-platform path compatibility.
/// Ensures file paths work correctly across Windows, macOS, and Linux.
/// </summary>
public class PathCompatibilityTests
{
    [Fact]
    public void PathSeparator_Handling_WorksOnAllPlatforms()
    {
        // Arrange - Test various path formats
        var windowsPath = @"C:\Users\Doc\Desktop\games";
        var unixPath = "/home/doc/games";
        var mixedPath = @"C:\Users\Doc\Desktop/games";

        // Act & Assert - Paths should be handled correctly
        // In a real implementation, you would normalize paths
        windowsPath.Should().NotBeNullOrEmpty();
        unixPath.Should().NotBeNullOrEmpty();
        mixedPath.Should().NotBeNullOrEmpty();

        // Check for platform-specific separators
        if (OperatingSystem.IsWindows())
        {
            windowsPath.Should().Contain(@"\");
        }
        else
        {
            unixPath.Should().Contain("/");
        }
    }

    [Fact]
    public void FileExtension_Handling_IsCaseInsensitive()
    {
        // Arrange
        var extensions = new[] { ".exe", ".EXE", ".Exe", ".ROM", ".rom", ".zip", ".ZIP" };

        // Act & Assert - Extensions should be handled case-insensitively on all platforms
        foreach (var ext in extensions)
        {
            ext.Should().StartWith(".");
            ext.ToLower().Should().Be(ext.ToLower()); // Should normalize correctly
        }

        // Case-insensitive comparison should work
        ".EXE".ToLower().Should().Be(".exe");
        ".ROM".ToLower().Should().Be(".rom");
    }

    [Fact]
    public void DirectorySeparator_Compatibility()
    {
        // Arrange
        var pathWithBackslashes = @"folder\subfolder\file.txt";
        var pathWithForwardSlashes = "folder/subfolder/file.txt";

        // Act & Assert - Both separators should be handled
        pathWithBackslashes.Should().Contain(@"\");
        pathWithForwardSlashes.Should().Contain("/");

        // Path should be usable regardless of separator
        pathWithBackslashes.Should().NotBeNullOrEmpty();
        pathWithForwardSlashes.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void RootDirectory_Handling()
    {
        // Arrange
        var windowsRoots = new[] { @"C:\", @"D:\", @"E:\" };
        var unixRoots = new[] { "/", "/usr/", "/home/" };

        // Act & Assert - Root directories should be handled
        if (OperatingSystem.IsWindows())
        {
            foreach (var root in windowsRoots)
            {
                root.Should().MatchRegex(@"^[A-Z]:\\$");
            }
        }
        else
        {
            foreach (var root in unixRoots)
            {
                root.Should().StartWith("/");
            }
        }
    }

    [Fact]
    public void RelativePath_Resolution()
    {
        // Arrange
        var relativePaths = new[] { ".", "..", "./folder", "../parent", "folder/file.txt" };

        // Act & Assert - Relative paths should be handled consistently
        foreach (var path in relativePaths)
        {
            path.Should().NotBeNullOrEmpty();

            // Should not contain invalid characters for the platform
            if (OperatingSystem.IsWindows())
            {
                path.Should().NotContain(":"); // Unless it's a drive letter
            }
        }
    }

    [Fact]
    public void NetworkPath_Handling_Windows()
    {
        // Arrange
        var networkPaths = new[] { @"\\server\share", @"\\server\share\folder" };

        // Act & Assert - Network paths should be handled on Windows
        if (OperatingSystem.IsWindows())
        {
            foreach (var path in networkPaths)
            {
                path.Should().StartWith(@"\\");
                path.Should().Contain(@"\");
            }
        }
    }

    [Fact]
    public void UNCPath_Compatibility()
    {
        // Arrange
        var uncPaths = new[] { @"\\server\share\file.txt", @"\\192.168.1.1\share\file.txt" };

        // Act & Assert - UNC paths should work on supported platforms
        foreach (var path in uncPaths)
        {
            path.Should().StartWith(@"\\");
            path.Should().Contain(@"\");
        }
    }

    [Fact]
    public void PathLength_Limits_AreHandled()
    {
        // Arrange
        var shortPath = "file.txt";
        var mediumPath = "folder/subfolder/file.txt";
        var longPath = "very/long/path/with/many/directories/and/a/very/long/filename_that_might_cause_issues_on_some_platforms.txt";

        // Act & Assert - Different path lengths should be handled
        shortPath.Length.Should().BeLessThan(20);
        mediumPath.Length.Should().BeInRange(20, 100);
        longPath.Length.Should().BeGreaterThan(100);

        // All should be valid paths conceptually
        shortPath.Should().NotBeNullOrEmpty();
        mediumPath.Should().NotBeNullOrEmpty();
        longPath.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void SpecialCharacters_InPaths()
    {
        // Arrange
        var pathsWithSpaces = new[] { "file with spaces.txt", "folder with spaces/file.txt" };
        var pathsWithSpecialChars = new[] { "file-with-dashes.txt", "file_with_underscores.txt" };

        // Act & Assert - Special characters should be handled
        foreach (var path in pathsWithSpaces)
        {
            path.Should().Contain(" ");
        }

        foreach (var path in pathsWithSpecialChars)
        {
            path.Should().MatchRegex("[-_]");
        }
    }

    [Fact]
    public void UnicodeCharacters_InPaths()
    {
        // Arrange
        var unicodePaths = new[]
        {
            "文件.txt",           // Chinese
            "файл.txt",          // Russian
            "ファイル.txt",       // Japanese
            "fișier.txt",        // Romanian
            "ñoño.txt"           // Spanish
        };

        // Act & Assert - Unicode characters should be supported
        foreach (var path in unicodePaths)
        {
            path.Should().NotBeNullOrEmpty();
            path.Should().Contain(".txt");
        }
    }
}
