using Microsoft.Extensions.Logging;
using SaveState.Core.Common.Services;
using SaveState.Presentation.ViewModels.Dialogs;
using SaveState.Presentation.ViewModels.Settings;
using SaveState.Presentation.Views.Dialogs;

namespace SaveState.Presentation.Services;

/// <summary>
/// Performance-related dialog methods for the DialogService.
/// </summary>
public partial class DialogService
{
    /// <inheritdoc />
    public async Task ShowMessageAsync(string title, string message)
    {
        await ShowInformationAsync(title, message);
    }
}
