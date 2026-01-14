using CommunityToolkit.Mvvm.ComponentModel;
using System.Threading.Tasks;

namespace SaveState.Presentation.ViewModels.Shell;

public partial class RomManagementViewModel : ObservableObject
{
    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }
}
