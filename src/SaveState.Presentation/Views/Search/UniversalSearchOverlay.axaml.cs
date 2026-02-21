using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using SaveState.Presentation.ViewModels.Search;

namespace SaveState.Presentation.Views.Search;

/// <summary>
/// Universal search overlay view - command palette style search interface.
/// </summary>
public partial class UniversalSearchOverlay : UserControl
{
    public UniversalSearchOverlay()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (DataContext is not UniversalSearchViewModel vm)
        {
            return;
        }

        switch (e.Key)
        {
            case Key.Escape:
                // Close the search overlay
                vm.CloseCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.Enter:
                // Execute selected result
                vm.ExecuteSelectedResultCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.Down:
                // Navigate to next result
                vm.SelectNextResultCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.Up:
                // Navigate to previous result
                vm.SelectPreviousResultCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.Tab:
                // Cycle through scopes
                if (e.KeyModifiers == KeyModifiers.Shift)
                {
                    CycleScope(vm, -1);
                }
                else
                {
                    CycleScope(vm, 1);
                }
                e.Handled = true;
                break;
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        // Focus the search text box when the overlay is shown
        var textBox = this.FindControl<TextBox>("SearchTextBox");
        textBox?.Focus();
    }

    /// <summary>
    /// Cycles through search scopes.
    /// </summary>
    private static void CycleScope(UniversalSearchViewModel vm, int direction)
    {
        var scopes = vm.AvailableScopes.ToList();
        var currentIndex = scopes.IndexOf(vm.CurrentScope);
        var newIndex = (currentIndex + direction + scopes.Count) % scopes.Count;
        vm.ChangeScopeCommand.Execute(scopes[newIndex]);
    }
}
