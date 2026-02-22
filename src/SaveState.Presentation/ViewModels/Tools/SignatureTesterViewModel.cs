using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Core.GameLibrary.Entities;

namespace SaveState.Presentation.ViewModels.Tools;

/// <summary>
/// View model for the signature tester tool.
/// </summary>
public partial class SignatureTesterViewModel : ObservableObject
{
    // Simplified for design-time compatibility
    [ObservableProperty]
    private string _testResults = string.Empty;
}

/// <summary>
/// View model for a selectable signature.
/// </summary>
public partial class SelectableSignatureViewModel : ObservableObject
{
    [ObservableProperty]
    private GameMemorySignature _signature = new();

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private string _healthScore = "-";

    [ObservableProperty]
    private string _lastVerified = "Never";
}

/// <summary>
/// View model for a signature test result.
/// </summary>
public partial class SignatureTestResultViewModel : ObservableObject
{
    [ObservableProperty]
    private string _signatureName = string.Empty;

    [ObservableProperty]
    private bool _isSuccess;

    [ObservableProperty]
    private string _message = string.Empty;
}
