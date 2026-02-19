using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SaveState.Core.Sync;
using SaveState.Infrastructure.Sync;
using Xunit;

namespace SaveState.Infrastructure.Tests.Sync;

public class LocalFileStorageProviderTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly string _sourceDirectory;
    private readonly LocalFileStorageProvider _sut;

    public LocalFileStorageProviderTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"cloud-{Guid.NewGuid()}");
        _sourceDirectory = Path.Combine(Path.GetTempPath(), $"source-{Guid.NewGuid()}");
        Directory.CreateDirectory(_sourceDirectory);
        _sut = new LocalFileStorageProvider(_tempDirectory, NullLogger<LocalFileStorageProvider>.Instance);
    }

    [Fact]
    public async Task UploadFileAsync_WithValidFile_CreatesFile()
    {
        // Arrange
        var testPath = "test/file.txt";
        var localFile = Path.Combine(_sourceDirectory, "upload.txt");
        await File.WriteAllTextAsync(localFile, "Hello, World!");

        // Act
        var result = await _sut.UploadFileAsync(localFile, testPath);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        var fullPath = Path.Combine(_tempDirectory, testPath);
        File.Exists(fullPath).Should().BeTrue();
        var fileContent = await File.ReadAllTextAsync(fullPath);
        fileContent.Should().Be("Hello, World!");
    }

    [Fact]
    public async Task DownloadFileAsync_WithExistingFile_DownloadsFile()
    {
        // Arrange
        var remotePath = "existing/file.txt";
        var content = "Test content";
        var fullPath = Path.Combine(_tempDirectory, remotePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await File.WriteAllTextAsync(fullPath, content);
        var localFile = Path.Combine(_sourceDirectory, "download.txt");

        // Act
        var result = await _sut.DownloadFileAsync(remotePath, localFile);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        File.Exists(localFile).Should().BeTrue();
        var downloadedContent = await File.ReadAllTextAsync(localFile);
        downloadedContent.Should().Be(content);
    }

    [Fact]
    public async Task DownloadFileAsync_WithNonExistingFile_ReturnsFalse()
    {
        // Act
        var localFile = Path.Combine(_sourceDirectory, "nonexistent.txt");
        var result = await _sut.DownloadFileAsync("nonexistent/file.txt", localFile);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteFileAsync_WithExistingFile_RemovesFile()
    {
        // Arrange
        var testPath = "delete/file.txt";
        var fullPath = Path.Combine(_tempDirectory, testPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await File.WriteAllTextAsync(fullPath, "content");

        // Act
        var result = await _sut.DeleteFileAsync(testPath);

        // Assert
        result.Should().BeTrue();
        File.Exists(fullPath).Should().BeFalse();
    }

    [Fact]
    public async Task ListFilesAsync_WithExistingDirectory_ReturnsItems()
    {
        // Arrange
        var testDir = "list";
        var file1Path = Path.Combine(_tempDirectory, testDir, "file1.txt");
        var file2Path = Path.Combine(_tempDirectory, testDir, "file2.txt");
        Directory.CreateDirectory(Path.Combine(_tempDirectory, testDir));
        await File.WriteAllTextAsync(file1Path, "content1");
        await File.WriteAllTextAsync(file2Path, "content2");

        // Act
        var result = await _sut.ListFilesAsync(testDir);

        // Assert
        result.Should().HaveCount(2);
        result.Select(i => i.Name).Should().Contain(new[] { "file1.txt", "file2.txt" });
    }

    [Fact]
    public async Task FileExistsAsync_WithExistingFile_ReturnsTrue()
    {
        // Arrange
        var testPath = "exists/file.txt";
        var fullPath = Path.Combine(_tempDirectory, testPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await File.WriteAllTextAsync(fullPath, "content");

        // Act
        var result = await _sut.FileExistsAsync(testPath);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task FileExistsAsync_WithNonExistingFile_ReturnsFalse()
    {
        // Act
        var result = await _sut.FileExistsAsync("nonexistent/file.txt");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ProviderName_ReturnsCorrectName()
    {
        // Act & Assert
        _sut.ProviderName.Should().Be("Local Storage");
    }

    [Fact]
    public void IsAuthenticated_ReturnsTrue()
    {
        // Act & Assert
        _sut.IsAuthenticated.Should().BeTrue();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
        if (Directory.Exists(_sourceDirectory))
        {
            Directory.Delete(_sourceDirectory, true);
        }
    }
}
