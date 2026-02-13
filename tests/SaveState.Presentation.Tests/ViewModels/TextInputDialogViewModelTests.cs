using FluentAssertions;
using SaveState.Presentation.ViewModels.Dialogs;

namespace SaveState.Presentation.Tests.ViewModels;

public class TextInputDialogViewModelTests
{
    [Fact]
    public void PasswordChar_WhenNotSensitive_IsNull()
    {
        // Arrange
        var viewModel = new TextInputDialogViewModel
        {
            IsSensitive = false
        };

        // Act
        var passwordChar = viewModel.PasswordChar;

        // Assert
        passwordChar.Should().BeNull();
    }

    [Fact]
    public void PasswordChar_WhenSensitiveAndHidden_IsMasked()
    {
        // Arrange
        var viewModel = new TextInputDialogViewModel
        {
            IsSensitive = true,
            ShowSensitiveText = false
        };

        // Act
        var passwordChar = viewModel.PasswordChar;

        // Assert
        passwordChar.Should().Be('*');
    }

    [Fact]
    public void PasswordChar_WhenSensitiveAndVisible_IsNull()
    {
        // Arrange
        var viewModel = new TextInputDialogViewModel
        {
            IsSensitive = true,
            ShowSensitiveText = true
        };

        // Act
        var passwordChar = viewModel.PasswordChar;

        // Assert
        passwordChar.Should().BeNull();
    }

    [Fact]
    public void IsSensitive_SetToFalse_ClearsShowSensitiveText()
    {
        // Arrange
        var viewModel = new TextInputDialogViewModel
        {
            IsSensitive = true,
            ShowSensitiveText = true
        };

        // Act
        viewModel.IsSensitive = false;

        // Assert
        viewModel.ShowSensitiveText.Should().BeFalse();
        viewModel.PasswordChar.Should().BeNull();
    }
}
