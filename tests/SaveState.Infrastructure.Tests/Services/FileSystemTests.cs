using FluentAssertions;
using SaveState.Core.Common.Interfaces;
using SaveState.Infrastructure.Services;
using Xunit;

namespace SaveState.Infrastructure.Tests.Services;

public class FileSystemTests : IDisposable
{
    private readonly IFileSystem _fileSystem;
    private readonly string _testDirectory;

    public FileSystemTests()
    {
        _fileSystem = new FileSystem();
        _testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);
    }

    [Fact]
    public async Task FileExistsAsync_WithExistingFile_ReturnsTrue()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "test.txt");
        await File.WriteAllTextAsync(filePath, "test content");

        // Act
        var result = await _fileSystem.FileExistsAsync(filePath);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task FileExistsAsync_WithNonExistingFile_ReturnsFalse()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "nonexistent.txt");

        // Act
        var result = await _fileSystem.FileExistsAsync(filePath);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task FileExistsAsync_WithNullPath_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _fileSystem.FileExistsAsync(null!));
    }

    [Fact]
    public async Task DirectoryExistsAsync_WithExistingDirectory_ReturnsTrue()
    {
        // Act
        var result = await _fileSystem.DirectoryExistsAsync(_testDirectory);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task DirectoryExistsAsync_WithNonExistingDirectory_ReturnsFalse()
    {
        // Arrange
        var nonExistentDir = Path.Combine(_testDirectory, "nonexistent");

        // Act
        var result = await _fileSystem.DirectoryExistsAsync(nonExistentDir);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetFileSizeAsync_WithExistingFile_ReturnsCorrectSize()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "test.txt");
        var testContent = "Hello, World! This is a test file.";
        await File.WriteAllTextAsync(filePath, testContent);
        var expectedSize = testContent.Length; // UTF-8 bytes

        // Act
        var result = await _fileSystem.GetFileSizeAsync(filePath);

        // Assert
        result.Should().Be(expectedSize);
    }

    [Fact]
    public async Task GetFileSizeAsync_WithNonExistingFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "nonexistent.txt");

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            _fileSystem.GetFileSizeAsync(filePath));
    }

    [Fact]
    public async Task GetFilesAsync_WithSearchPattern_ReturnsMatchingFiles()
    {
        // Arrange
        var txtFile1 = Path.Combine(_testDirectory, "test1.txt");
        var txtFile2 = Path.Combine(_testDirectory, "test2.txt");
        var binFile = Path.Combine(_testDirectory, "test.bin");

        await File.WriteAllTextAsync(txtFile1, "content1");
        await File.WriteAllTextAsync(txtFile2, "content2");
        await File.WriteAllTextAsync(binFile, "binary");

        // Act
        var result = await _fileSystem.GetFilesAsync(_testDirectory, "*.txt");

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(txtFile1);
        result.Should().Contain(txtFile2);
        result.Should().NotContain(binFile);
    }

    [Fact]
    public async Task GetFilesAsync_WithRecursiveSearch_ReturnsFilesFromSubdirectories()
    {
        // Arrange
        var subDir = Path.Combine(_testDirectory, "subdir");
        Directory.CreateDirectory(subDir);

        var rootFile = Path.Combine(_testDirectory, "root.txt");
        var subFile = Path.Combine(subDir, "sub.txt");

        await File.WriteAllTextAsync(rootFile, "root content");
        await File.WriteAllTextAsync(subFile, "sub content");

        // Act
        var result = await _fileSystem.GetFilesAsync(_testDirectory, "*.txt", SearchOption.AllDirectories);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(rootFile);
        result.Should().Contain(subFile);
    }

    [Fact]
    public async Task GetFilesAsync_WithInvalidDirectory_ThrowsDirectoryNotFoundException()
    {
        // Arrange
        var invalidDir = Path.Combine(_testDirectory, "nonexistent");

        // Act & Assert
        await Assert.ThrowsAsync<DirectoryNotFoundException>(() =>
            _fileSystem.GetFilesAsync(invalidDir, "*.txt"));
    }

    [Fact]
    public async Task ReadAllBytesAsync_WithExistingFile_ReturnsFileContent()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "test.bin");
        var testBytes = new byte[] { 1, 2, 3, 4, 5 };
        await File.WriteAllBytesAsync(filePath, testBytes);

        // Act
        var result = await _fileSystem.ReadAllBytesAsync(filePath);

        // Assert
        result.Should().BeEquivalentTo(testBytes);
    }

    [Fact]
    public async Task ReadAllBytesAsync_WithNonExistingFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "nonexistent.bin");

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            _fileSystem.ReadAllBytesAsync(filePath));
    }

    [Fact]
    public async Task ReadAllBytesAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        var filePath = Path.Combine(_testDirectory, "test.txt");
        await File.WriteAllTextAsync(filePath, "test content");

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _fileSystem.ReadAllBytesAsync(filePath, cts.Token));
    }

    [Fact]
    public async Task Operations_WithVeryLongPaths_HandlesCorrectly()
    {
        // Arrange
        var longFileName = new string('a', 200) + ".txt"; // Very long filename
        var longFilePath = Path.Combine(_testDirectory, longFileName);

        // Some filesystems have path length limits, so this tests the service behavior
        try
        {
            await File.WriteAllTextAsync(longFilePath, "test");

            // Act & Assert - If the file was created successfully, operations should work
            var exists = await _fileSystem.FileExistsAsync(longFilePath);
            exists.Should().BeTrue();
        }
        catch (PathTooLongException)
        {
            // If the filesystem doesn't support long paths, that's acceptable
            // The service should handle this gracefully
        }
    }

    [Fact]
    public async Task Operations_WithUnicodeCharactersInPath_HandlesCorrectly()
    {
        // Arrange
        var unicodeFileName = "测试文件_ñáéíóú.txt"; // Unicode characters
        var unicodeFilePath = Path.Combine(_testDirectory, unicodeFileName);

        await File.WriteAllTextAsync(unicodeFilePath, "unicode content");

        // Act & Assert
        var exists = await _fileSystem.FileExistsAsync(unicodeFilePath);
        exists.Should().BeTrue();

        var content = await _fileSystem.ReadAllBytesAsync(unicodeFilePath);
        content.Should().NotBeNull();
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_testDirectory, true);
        }
        catch
        {
            // Ignore cleanup errors in tests
        }
    }
}
