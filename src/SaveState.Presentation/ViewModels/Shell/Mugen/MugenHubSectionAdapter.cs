using System.Threading.Tasks;
using SaveState.Presentation.ViewModels.Shell;

namespace SaveState.Presentation.ViewModels.Shell.Mugen;

/// <summary>
/// Adapter to make MugenHubViewModel work as a MUGEN section.
/// </summary>
public class MugenHubSectionAdapter : MugenSectionViewModelBase
{
    private readonly MugenHubViewModel _hubViewModel;

    public MugenHubSectionAdapter(MugenHubViewModel hubViewModel)
    {
        _hubViewModel = hubViewModel;
    }

    public override Task InitializeAsync()
    {
        return _hubViewModel.RefreshCommand.ExecuteAsync(null);
    }

    /// <summary>
    /// Gets the underlying hub ViewModel for data binding.
    /// </summary>
    public MugenHubViewModel Hub => _hubViewModel;
}
