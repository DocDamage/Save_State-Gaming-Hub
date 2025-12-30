using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SaveState.Core.Common;
using SaveState.Core.Sync;
using SaveState.Infrastructure.Sync;
using Xunit;

namespace SaveState.Infrastructure.Tests.Sync;

public class LocalFileStorageProviderTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly LocalFileStorageProvider _sut;

    public LocalFileStorageProviderTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _sut = new LocalFileStorageProvider(_tempDirectory, NullLogger<LocalFileStorageProvider>.Instance);
    }

    [Fact]
    public async Task UploadAsync_WithValidStream_CreatesFile()
    {
        // Arrange
        var testPath = "test/file.txt";
        var content = "Hello, World!"u8.ToArray();
        using var stream = new MemoryStream(content);

        // Act
        var result = await _sut.UploadAsync(testPath, stream);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var fullPath = Path.Combine(_tempDirectory, testPath);
        File.Exists(fullPath).Should().BeTrue();
        var fileContent = await File.ReadAllTextAsync(fullPath);
        fileContent.Should().Be("Hello, World!");
    }

    [Fact]
    public async Task DownloadAsync_WithExistingFile_ReturnsStream()
    {
        // Arrange
        var testPath = "existing/file.txt";
        var content = "Test content";
        var fullPath = Path.Combine(_tempDirectory, testPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await File.WriteAllTextAsync(fullPath, content);

        // Act
        var result = await _sut.DownloadAsync(testPath);

        // Assert
        result.IsSuccess.Should().BeTrue();
        using var reader = new StreamReader(result.Value!);
        var downloadedContent = await reader.ReadToEndAsync();
        downloadedContent.Should().Be(content);
    }

    [Fact]
    public async Task DownloadAsync_WithNonExistingFile_ReturnsFailure()
    {
        // Act
        var result = await _sut.DownloadAsync("nonexistent/file.txt");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("File not found");
    }

    [Fact]
    public async Task DeleteAsync_WithExistingFile_RemovesFile()
    {
        // Arrange
        var testPath = "delete/file.txt";
        var fullPath = Path.Combine(_tempDirectory, testPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await File.WriteAllTextAsync(fullPath, "content");

        // Act
        var result = await _sut.DeleteAsync(testPath);

        // Assert
        result.IsSuccess.Should().BeTrue();
        File.Exists(fullPath).Should().BeFalse();
    }

    [Fact]
    public async Task ListAsync_WithExistingDirectory_ReturnsItems()
    {
        // Arrange
        var testDir = "list";
        var file1Path = Path.Combine(_tempDirectory, testDir, "file1.txt");
        var file2Path = Path.Combine(_tempDirectory, testDir, "file2.txt");
        Directory.CreateDirectory(Path.Combine(_tempDirectory, testDir));
        await File.WriteAllTextAsync(file1Path, "content1");
        await File.WriteAllTextAsync(file2Path, "content2");

        // Act
        var result = await _sut.ListAsync(testDir);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value!.Select(i => i.Path).Should().Contain(new[] { "file1.txt", "file2.txt" });
    }

    [Fact]
    public async Task ExistsAsync_WithExistingFile_ReturnsTrue()
    {
        // Arrange
        var testPath = "exists/file.txt";
        var fullPath = Path.Combine(_tempDirectory, testPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await File.WriteAllTextAsync(fullPath, "content");

        // Act
        var result = await _sut.ExistsAsync(testPath);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WithNonExistingFile_ReturnsFalse()
    {
        // Act
        var result = await _sut.ExistsAsync("nonexistent/file.txt");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    [Fact]
    public void ProviderName_ReturnsCorrectName()
    {
        // Act & Assert
        _sut.ProviderName.Should().Be("LocalFile");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }
}
