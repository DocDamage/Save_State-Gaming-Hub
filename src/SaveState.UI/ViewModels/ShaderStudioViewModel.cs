using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Core.Services.EmulatorEnhancements;
using System.Collections.ObjectModel;

namespace SaveState.UI.ViewModels;

public partial class ShaderStudioViewModel : ViewModelBase
{
    private readonly ShaderStudioService _shaderService;

    [ObservableProperty]
    private ObservableCollection<ShaderPreset> _presets = new();

    [ObservableProperty]
    private ShaderPreset? _activePreset;

    [ObservableProperty]
    private string _newShaderName = string.Empty;

    [ObservableProperty]
    private string _newShaderDescription = string.Empty;

    [ObservableProperty]
    private string _newShaderCode = "// Custom GLSL shader\nvoid main() {\n    // Your code here\n}";

    [ObservableProperty]
    private string _statusMessage = "No shader active";

    public IRelayCommand<string> ApplyPresetCommand { get; }
    public IRelayCommand DisableShaderCommand { get; }
    public IRelayCommand CreateCustomShaderCommand { get; }
    public IRelayCommand<string> DeleteShaderCommand { get; }

    public ShaderStudioViewModel()
    {
        _shaderService = new ShaderStudioService();

        ApplyPresetCommand = new RelayCommand<string>(ApplyPreset);
        DisableShaderCommand = new RelayCommand(DisableShader);
        CreateCustomShaderCommand = new RelayCommand(CreateCustomShader, CanCreateCustomShader);
        DeleteShaderCommand = new RelayCommand<string>(DeleteShader);

        LoadPresets();
    }

    private bool CanCreateCustomShader() => !string.IsNullOrWhiteSpace(NewShaderName);

    private void LoadPresets()
    {
        Presets.Clear();
        foreach (var preset in _shaderService.GetPresets())
        {
            Presets.Add(preset);
        }
        ActivePreset = _shaderService.GetActivePreset();
    }

    private void ApplyPreset(string? presetId)
    {
        if (string.IsNullOrEmpty(presetId)) return;
        _shaderService.ApplyPreset(presetId);
        ActivePreset = _shaderService.GetActivePreset();
        StatusMessage = $"Applied shader: {ActivePreset?.Name}";
    }

    private void DisableShader()
    {
        _shaderService.DisableShader();
        ActivePreset = null;
        StatusMessage = "Shader disabled";
    }

    private void CreateCustomShader()
    {
        if (string.IsNullOrWhiteSpace(NewShaderName)) return;

        var preset = _shaderService.CreateCustomShader(
            NewShaderName,
            NewShaderDescription,
            NewShaderCode
        );

        Presets.Add(preset);
        NewShaderName = string.Empty;
        NewShaderDescription = string.Empty;
        NewShaderCode = "// Custom GLSL shader\nvoid main() {\n    // Your code here\n}";
        StatusMessage = $"Created custom shader: {preset.Name}";
    }

    private void DeleteShader(string? presetId)
    {
        if (string.IsNullOrEmpty(presetId)) return;
        _shaderService.DeleteCustomShader(presetId);
        LoadPresets();
        StatusMessage = "Shader deleted";
    }

    public void UpdateParameter(string parameterId, float value)
    {
        _shaderService.UpdateParameter(parameterId, value);
    }

    partial void OnNewShaderNameChanged(string value) => CreateCustomShaderCommand.NotifyCanExecuteChanged();
}
