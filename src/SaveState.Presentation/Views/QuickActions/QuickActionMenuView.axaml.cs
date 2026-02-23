using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using SaveState.Presentation.ViewModels.QuickActions;

namespace SaveState.Presentation.Views.QuickActions;

/// <summary>
/// View for the quick action menu.
/// </summary>
public partial class QuickActionMenuView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the QuickActionMenuView class.
    /// </summary>
    public QuickActionMenuView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (DataContext is QuickActionMenuViewModel viewModel)
        {
            if (viewModel.HandleKey(e.Key))
            {
                e.Handled = true;
            }
        }
    }

    /// <inheritdoc />
    protected override void OnAttachedToVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        // Focus the search box when the menu opens
        if (DataContext is QuickActionMenuViewModel viewModel)
        {
            viewModel.RequestClose += OnRequestClose;
        }

        // Find and focus the search box
        var searchBox = this.FindControl<TextBox>("SearchBox");
        searchBox?.Focus();
    }

    /// <inheritdoc />
    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        if (DataContext is QuickActionMenuViewModel viewModel)
        {
            viewModel.RequestClose -= OnRequestClose;
        }
    }

    private void OnRequestClose(object? sender, EventArgs e)
    {
        // Find the parent window and close it
        var window = this.GetVisualParent() as Window
            ?? TopLevel.GetTopLevel(this) as Window;
        window?.Close();
    }
}
