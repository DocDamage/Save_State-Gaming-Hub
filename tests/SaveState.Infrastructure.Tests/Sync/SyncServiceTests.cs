using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using FluentAssertions;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Sync;
using SaveState.Infrastructure.Sync;

namespace SaveState.Infrastructure.Tests.Sync;

public class SyncServiceTests
{
    [Fact]
    public async Task PullAsync_WhenPreferredProviderUsesAlias_SelectsMatchingProvider()
    {
        // Arrange
        var preferencesMock = new Mock<IUserPreferencesService>();
        preferencesMock
            .Setup(service => service.GetPreferredCloudProviderAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("GoogleDrive");

        var oneDriveProvider = new TestCloudStorageProvider("OneDrive");
        var googleDriveProvider = new TestCloudStorageProvider("Google Drive");

        var sut = new SyncService(
            new ICloudStorageProvider[] { oneDriveProvider, googleDriveProvider },
            preferencesMock.Object,
            NullLogger<SyncService>.Instance,
            SystemTimeProvider.Instance);

        // Act
        var result = await sut.PullAsync();

        // Assert
        result.Success.Should().BeTrue();
        sut.ActiveProviderName.Should().Be("Google Drive");
        googleDriveProvider.AuthenticateCallCount.Should().Be(1);
        oneDriveProvider.AuthenticateCallCount.Should().Be(0);
    }

    [Fact]
    public async Task PullAsync_WhenPreferredProviderNotFound_FallsBackToFirstNonLocalProvider()
    {
        // Arrange
        var preferencesMock = new Mock<IUserPreferencesService>();
        preferencesMock
            .Setup(service => service.GetPreferredCloudProviderAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("UnknownProvider");

        var localProvider = new TestCloudStorageProvider("Local Storage");
        var oneDriveProvider = new TestCloudStorageProvider("OneDrive");

        var sut = new SyncService(
            new ICloudStorageProvider[] { localProvider, oneDriveProvider },
            preferencesMock.Object,
            NullLogger<SyncService>.Instance,
            SystemTimeProvider.Instance);

        // Act
        var result = await sut.PullAsync();

        // Assert
        result.Success.Should().BeTrue();
        sut.ActiveProviderName.Should().Be("OneDrive");
        oneDriveProvider.AuthenticateCallCount.Should().Be(1);
        localProvider.AuthenticateCallCount.Should().Be(0);
    }

    private sealed class TestCloudStorageProvider : ICloudStorageProvider
    {
        private bool _isAuthenticated;

        public TestCloudStorageProvider(string providerName, bool isAuthenticated = false)
        {
            ProviderName = providerName;
            _isAuthenticated = isAuthenticated;
        }

        public string ProviderName { get; }

        public bool IsAuthenticated => _isAuthenticated;

        public int AuthenticateCallCount { get; private set; }

        public Task<bool> AuthenticateAsync(CancellationToken ct = default)
        {
            AuthenticateCallCount++;
            _isAuthenticated = true;
            return Task.FromResult(true);
        }

        public Task<Result<bool>> UploadFileAsync(string localPath, string remotePath, CancellationToken ct = default)
            => Task.FromResult(Result.Success(true));

        public Task<Result<bool>> DownloadFileAsync(string remotePath, string localPath, CancellationToken ct = default)
            => Task.FromResult(Result.Success(true));

        public Task<bool> DeleteFileAsync(string remotePath, CancellationToken ct = default)
            => Task.FromResult(true);

        public Task<IReadOnlyList<CloudFileInfo>> ListFilesAsync(string remotePath, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<CloudFileInfo>>(Array.Empty<CloudFileInfo>());

        public Task<Result<CloudFileInfo>> GetFileInfoAsync(string remotePath, CancellationToken ct = default)
            => Task.FromResult(Result.Failure<CloudFileInfo>("Not found", ErrorType.NotFound));

        public Task<bool> FileExistsAsync(string remotePath, CancellationToken ct = default)
            => Task.FromResult(false);
    }
}
