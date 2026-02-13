using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SaveState.Presentation.Views.Mugen.Sections;

public partial class DownloadsSection : UserControl
{
    public DownloadsSection()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnSearchKeyDown(object sender, Avalonia.Input.KeyEventArgs e)
    {
        if (e.Key == Avalonia.Input.Key.Enter)
        {
            if (DataContext is SaveState.Presentation.ViewModels.Shell.Mugen.MugenDownloadsViewModel vm)
            {
                if (vm.SearchCommand.CanExecute(null))
                    vm.SearchCommand.Execute(null);
            }
        }
    }
}
