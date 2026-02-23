using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using SaveState.Presentation.Models.Security;
using SaveState.Presentation.ViewModels.Settings;

namespace SaveState.Presentation.Views.Settings;

/// <summary>
/// View for user management interface.
/// </summary>
public partial class UserManagementView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserManagementView"/> class.
    /// </summary>
    public UserManagementView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <summary>
    /// Handles pointer pressed on a user card to select the user.
    /// </summary>
    private void OnUserCardPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Border border && border.Tag is UserAccount user)
        {
            if (DataContext is UserManagementViewModel viewModel)
            {
                viewModel.SelectedUser = user;
            }
        }
    }
}
