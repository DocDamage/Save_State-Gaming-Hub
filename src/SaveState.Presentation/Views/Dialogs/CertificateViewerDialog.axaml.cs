using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SaveState.Presentation.Views.Dialogs;

/// <summary>
/// SSL/TLS certificate information viewer dialog.
/// </summary>
public partial class CertificateViewerDialog : Window
{
    public CertificateViewerDialog()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
