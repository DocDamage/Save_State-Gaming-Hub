using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Core.Helpers;
using SaveState.Core.Services.Ai;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace SaveState.UI.ViewModels;

public partial class AiSettingsViewModel : ViewModelBase
{
    private readonly LlmService _llmService;

    [ObservableProperty]
    private string _systemStatus = "Checking system...";

    [ObservableProperty]
    private string _systemWarnings = string.Empty;

    [ObservableProperty]
    private string _ollamaStatus = "Unknown";

    [ObservableProperty]
    private string _sdStatus = "Not Connected";

    [ObservableProperty]
    private ObservableCollection<ModelDisplayInfo> _installedModels = new();

    [ObservableProperty]
    private ObservableCollection<ModelDisplayInfo> _availableModels = new();

    [ObservableProperty]
    private ObservableCollection<string> _activeProviders = new();

    [ObservableProperty]
    private string _selectedProvider = "Ollama";

    [ObservableProperty]
    private bool _isDownloading;

    [ObservableProperty]
    private string _downloadProgress = string.Empty;

    [ObservableProperty]
    private int _maxModels;

    [ObservableProperty]
    private string _ramInfo = string.Empty;

    [ObservableProperty]
    private string _gpuInfo = string.Empty;

    public IAsyncRelayCommand RefreshCommand { get; }
    public IAsyncRelayCommand<string> DownloadModelCommand { get; }
    public IAsyncRelayCommand<string> DeleteModelCommand { get; }
    public IAsyncRelayCommand StartOllamaCommand { get; }
    public IAsyncRelayCommand CheckSdCommand { get; }

    /// <summary>
    /// Constructor for dependency injection.
    /// </summary>
    /// <param name="llmService">The LLM service to use.</param>
    public AiSettingsViewModel(LlmService llmService)
    {
        _llmService = llmService ?? throw new ArgumentNullException(nameof(llmService));

        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        DownloadModelCommand = new AsyncRelayCommand<string>(DownloadModelAsync);
        DeleteModelCommand = new AsyncRelayCommand<string>(DeleteModelAsync);
        StartOllamaCommand = new AsyncRelayCommand(StartOllamaAsync);
        CheckSdCommand = new AsyncRelayCommand(CheckStableDiffusionAsync);

        _ = InitializeAsync();
    }
    
    /// <summary>
    /// Design-time/fallback constructor.
    /// </summary>
    public AiSettingsViewModel() : this(new LlmService())
    {
    }

    private async Task InitializeAsync()
    {
        await _llmService.InitializeAsync();
        await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        // System info
        var sysInfo = SystemCapabilities.GetSystemInfo();
        RamInfo = $"{SystemCapabilities.FormatBytes(sysInfo.TotalRamBytes)} RAM ({SystemCapabilities.FormatBytes(sysInfo.AvailableRamBytes)} available)";
        
        if (sysInfo.Gpu != null)
        {
            GpuInfo = $"{sysInfo.Gpu.Name} ({SystemCapabilities.FormatBytes(sysInfo.Gpu.VramBytes)} VRAM)";
        }
        else
        {
            GpuInfo = "No dedicated GPU detected";
        }

        SystemWarnings = SystemCapabilities.GetSystemWarnings();
        MaxModels = SystemCapabilities.GetSafeModelCount();

        // Ollama status
        OllamaStatus = OllamaManager.Instance.Status.ToString();

        // SD status
        if (await StableDiffusionService.Instance.CheckConnectionAsync())
        {
            SdStatus = "Connected";
        }
        else
        {
            SdStatus = "Not Running";
        }

        // Providers
        ActiveProviders.Clear();
        foreach (var provider in _llmService.GetAvailableProviders())
        {
            ActiveProviders.Add(provider.ToString());
        }
        SelectedProvider = _llmService.CurrentProvider.ToString();

        // Installed models
        InstalledModels.Clear();
        var installed = await ModelManager.Instance.GetInstalledModelsAsync();
        foreach (var model in installed)
        {
            InstalledModels.Add(new ModelDisplayInfo
            {
                Name = model.Name,
                DisplayName = model.DisplayName,
                Description = model.Description,
                Size = SystemCapabilities.FormatBytes(model.SizeBytes),
                IsInstalled = true,
                CanDelete = true
            });
        }

        // Available models (not installed)
        AvailableModels.Clear();
        foreach (var model in ModelManager.AvailableModels)
        {
            if (!installed.Any(i => i.Name == model.Name))
            {
                AvailableModels.Add(new ModelDisplayInfo
                {
                    Name = model.Name,
                    DisplayName = model.DisplayName,
                    Description = model.Description,
                    Size = SystemCapabilities.FormatBytes(model.SizeBytes),
                    IsInstalled = false,
                    CanDownload = InstalledModels.Count < MaxModels
                });
            }
        }

        SystemStatus = $"AI Ready • {InstalledModels.Count}/{MaxModels} models loaded";
    }

    private async Task DownloadModelAsync(string? modelName)
    {
        if (string.IsNullOrEmpty(modelName) || IsDownloading) return;

        if (InstalledModels.Count >= MaxModels)
        {
            SystemStatus = $"⚠️ Max models reached ({MaxModels}). Delete one first.";
            return;
        }

        IsDownloading = true;
        DownloadProgress = $"Downloading {modelName}...";

        var progress = new Progress<DownloadProgress>(p =>
        {
            DownloadProgress = $"Downloading {modelName}: {p.PercentComplete:F0}%";
        });

        var success = await ModelManager.Instance.DownloadModelAsync(modelName, progress);

        IsDownloading = false;
        DownloadProgress = string.Empty;

        if (success)
        {
            SystemStatus = $"✅ {modelName} installed successfully!";
            await RefreshAsync();
        }
        else
        {
            SystemStatus = $"❌ Failed to download {modelName}";
        }
    }

    private async Task DeleteModelAsync(string? modelName)
    {
        if (string.IsNullOrEmpty(modelName)) return;

        var success = await ModelManager.Instance.DeleteModelAsync(modelName);
        if (success)
        {
            SystemStatus = $"🗑️ {modelName} removed";
            await RefreshAsync();
        }
    }

    private async Task StartOllamaAsync()
    {
        SystemStatus = "Starting Ollama...";
        var started = await OllamaManager.Instance.CheckAndStartAsync();
        OllamaStatus = OllamaManager.Instance.Status.ToString();
        
        if (started)
        {
            SystemStatus = "✅ Ollama started successfully!";
            await RefreshAsync();
        }
        else
        {
            SystemStatus = "❌ Failed to start Ollama. Is it installed?";
        }
    }

    private async Task CheckStableDiffusionAsync()
    {
        SystemStatus = "Checking Stable Diffusion...";
        var connected = await StableDiffusionService.Instance.CheckConnectionAsync();
        SdStatus = connected ? "Connected" : "Not Running";
        SystemStatus = connected 
            ? "✅ Stable Diffusion connected!" 
            : "❌ SD not detected. Start Automatic1111 WebUI on port 7860.";
    }
}

public class ModelDisplayInfo
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;
    public bool IsInstalled { get; set; }
    public bool CanDownload { get; set; }
    public bool CanDelete { get; set; }
}
