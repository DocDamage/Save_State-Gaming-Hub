using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SaveState.Presentation.Views.Dialogs;

public partial class RomDetailsDialog : Window
{
    public RomDetailsDialog()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}

public partial class EmulatorConfigDialog : Window
{
    public EmulatorConfigDialog()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}

public partial class RomScanProgressDialog : Window
{
    public RomScanProgressDialog()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}

public partial class EmulatorSetupWizard : Window
{
    public EmulatorSetupWizard()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}

public partial class PriceHistoryView : UserControl
{
    public PriceHistoryView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
