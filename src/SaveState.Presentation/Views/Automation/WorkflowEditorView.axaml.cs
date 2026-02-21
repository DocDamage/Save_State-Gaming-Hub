using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SaveState.Presentation.Views.Automation;

/// <summary>
/// View for the Automation Studio workflow editor.
/// </summary>
public partial class WorkflowEditorView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the WorkflowEditorView.
    /// </summary>
    public WorkflowEditorView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
