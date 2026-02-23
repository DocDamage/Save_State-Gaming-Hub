using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SaveState.Presentation.ViewModels.RgbSync;

namespace SaveState.Presentation.Views.RgbSync;

public partial class RgbControlPanelView : UserControl
{
    public RgbControlPanelView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is RgbControlPanelViewModel vm)
        {
            // Set up child view content controls
            var deviceEditorContent = this.FindControl<ContentControl>("DeviceEditorContent");
            var profileManagerContent = this.FindControl<ContentControl>("ProfileManagerContent");
            var syncGroupContent = this.FindControl<ContentControl>("SyncGroupEditorContent");
            var gameStateContent = this.FindControl<ContentControl>("GameStateConfigContent");

            // Child viewmodels would be created and assigned here
            // In a real implementation, these would be injected or created via a factory
        }
    }
}
