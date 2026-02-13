using FluentAssertions;
using SaveState.Presentation.Services;
using SaveState.Presentation.ViewModels.Dialogs;

namespace SaveState.Presentation.Tests.ViewModels;

public class CloudProviderConfigDialogViewModelTests
{
    [Fact]
    public void Constructor_WithCurrentSettings_InitializesFields()
    {
        // Arrange
        var currentSettings = new CloudProviderConfigResult(
            ProviderName: "OneDrive",
            ApiKey: "client-id",
            BucketName: "bucket",
            EnableAutoSync: false,
            EnableBackgroundFailureAlerts: false,
            EnableBackgroundConflictAlerts: true,
            AlertCooldownSeconds: 120);

        // Act
        var viewModel = new CloudProviderConfigDialogViewModel(currentSettings);

        // Assert
        viewModel.SelectedProvider.Should().Be("OneDrive");
        viewModel.ApiKey.Should().Be("client-id");
        viewModel.BucketName.Should().Be("bucket");
        viewModel.EnableAutoSync.Should().BeFalse();
        viewModel.EnableBackgroundFailureAlerts.Should().BeFalse();
        viewModel.EnableBackgroundConflictAlerts.Should().BeTrue();
        viewModel.AlertCooldownSeconds.Should().Be(120);
    }

    [Fact]
    public void SaveCommand_WhenCooldownIsOutOfRange_ClampsResult()
    {
        // Arrange
        var viewModel = new CloudProviderConfigDialogViewModel
        {
            SelectedProvider = "GoogleDrive",
            ApiKey = "key",
            EnableAutoSync = true,
            EnableBackgroundFailureAlerts = true,
            EnableBackgroundConflictAlerts = true,
            AlertCooldownSeconds = 5
        };

        // Act
        viewModel.SaveCommand.Execute(null);

        // Assert
        viewModel.Result.Should().NotBeNull();
        viewModel.Result!.AlertCooldownSeconds.Should().Be(15);
    }
}
