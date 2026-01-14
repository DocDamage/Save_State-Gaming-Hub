using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Diagnostics;

namespace SaveState.Presentation.Views.Dialogs;

public partial class EmulatorEditorDialog : Window
{
    public EmulatorEditorDialog()
    {
        InitializeComponent();
#if DEBUG
        this.AttachDevTools();
#endif
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
