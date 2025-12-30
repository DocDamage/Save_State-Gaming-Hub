using FluentAssertions;
using SaveState.Core.RomManagement.ValueObjects;

namespace SaveState.Core.Tests.RomManagement.ValueObjects;

public class FilePathTests
{
    [Fact]
    public void Constructor_WithValidAbsolutePath_CreatesFilePath()
    {
        // Arrange
        const string validPath = @"C:\Games\game.exe";

        // Act
        var filePath = new FilePath(validPath);

        // Assert
        filePath.Value.Should().Be(validPath);
    }

    [Fact]
    public void Constructor_WithNullValue_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new FilePath(null!));
    }

    [Fact]
    public void Constructor_WithEmptyValue_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentException>(() => new FilePath(string.Empty));
    }

    [Fact]
    public void Constructor_WithWhitespaceOnly_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentException>(() => new FilePath("   "));
    }

    [Fact]
    public void Constructor_WithRelativePath_ThrowsArgumentException()
    {
        // Arrange
        const string relativePath = @"Games\game.exe";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new FilePath(relativePath));
    }

    [Fact]
    public void Constructor_WithUnixStylePath_DoesNotThrowException()
    {
        // Arrange
        const string unixPath = "/games/game.exe";

        // Act & Assert
        // Path.IsPathRooted() may consider Unix-style paths as rooted on some systems
        // The important thing is that it doesn't throw an exception unexpectedly
        var action = () => new FilePath(unixPath);
        action.Should().NotThrow();
    }

    [Fact]
    public void GetDirectory_ReturnsDirectoryName()
    {
        // Arrange
        var filePath = new FilePath(@"C:\Games\Roms\game.exe");

        // Act
        var directory = filePath.GetDirectory();

        // Assert
        directory.Should().Be(@"C:\Games\Roms");
    }

    [Fact]
    public void GetFileName_ReturnsFileName()
    {
        // Arrange
        var filePath = new FilePath(@"C:\Games\Roms\game.exe");

        // Act
        var fileName = filePath.GetFileName();

        // Assert
        fileName.Should().Be("game.exe");
    }

    [Fact]
    public void GetExtension_ReturnsFileExtension()
    {
        // Arrange
        var filePath = new FilePath(@"C:\Games\Roms\game.exe");

        // Act
        var extension = filePath.GetExtension();

        // Assert
        extension.Should().Be(".exe");
    }

    [Fact]
    public void GetExtension_WithNoExtension_ReturnsEmptyString()
    {
        // Arrange
        var filePath = new FilePath(@"C:\Games\Roms\game");

        // Act
        var extension = filePath.GetExtension();

        // Assert
        extension.Should().BeEmpty();
    }

    [Fact]
    public void Exists_WithNonExistentFile_ReturnsFalse()
    {
        // Arrange
        var filePath = new FilePath(@"C:\NonExistent\file.exe");

        // Act
        var exists = filePath.Exists();

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public void ImplicitOperatorString_ReturnsValue()
    {
        // Arrange
        var filePath = new FilePath(@"C:\Games\game.exe");

        // Act
        string value = filePath;

        // Assert
        value.Should().Be(@"C:\Games\game.exe");
    }

    [Fact]
    public void ExplicitOperatorFilePath_CreatesFilePath()
    {
        // Arrange
        const string value = @"C:\Games\game.exe";

        // Act
        var filePath = (FilePath)value;

        // Assert
        filePath.Value.Should().Be(@"C:\Games\game.exe");
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        // Arrange
        var filePath = new FilePath(@"C:\Games\game.exe");

        // Act
        var result = filePath.ToString();

        // Assert
        result.Should().Be(@"C:\Games\game.exe");
    }

    [Fact]
    public void Equals_WithSameValues_ReturnsTrue()
    {
        // Arrange
        var path1 = new FilePath(@"C:\Games\game.exe");
        var path2 = new FilePath(@"C:\Games\game.exe");

        // Act & Assert
        path1.Should().Be(path2);
        path1.GetHashCode().Should().Be(path2.GetHashCode());
    }

    [Fact]
    public void Equals_WithDifferentCase_ReturnsTrue()
    {
        // Arrange
        var path1 = new FilePath(@"C:\Games\game.exe");
        var path2 = new FilePath(@"c:\games\game.exe");

        // Act & Assert
        path1.Should().Be(path2);
        path1.GetHashCode().Should().Be(path2.GetHashCode());
    }

    [Fact]
    public void Equals_WithDifferentValues_ReturnsFalse()
    {
        // Arrange
        var path1 = new FilePath(@"C:\Games\game.exe");
        var path2 = new FilePath(@"C:\Games\other.exe");

        // Act & Assert
        path1.Should().NotBe(path2);
    }

    [Fact]
    public void Equals_WithNull_ReturnsFalse()
    {
        // Arrange
        var path = new FilePath(@"C:\Games\game.exe");

        // Act & Assert
        path.Equals(null).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithDifferentType_ReturnsFalse()
    {
        // Arrange
        var path = new FilePath(@"C:\Games\game.exe");
        var other = @"C:\Games\game.exe";

        // Act & Assert
        path.Equals(other).Should().BeFalse();
    }
}
