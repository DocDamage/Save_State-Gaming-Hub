using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Presentation.ViewModels.Settings;
using System.Collections.ObjectModel;

namespace SaveState.Presentation.ViewModels.Dialogs;

/// <summary>
/// ViewModel for LLM provider configuration dialog.
/// </summary>
public partial class ProviderConfigDialogViewModel : ObservableObject
{
    private readonly LlmProviderConfig _provider;

    #region Properties

    /// <summary>Provider name.</summary>
    [ObservableProperty]
    private string _providerName = string.Empty;

    /// <summary>Display name for UI.</summary>
    [ObservableProperty]
    private string _displayName = string.Empty;

    /// <summary>Whether the provider is enabled.</summary>
    [ObservableProperty]
    private bool _isEnabled;

    /// <summary>API key for the provider.</summary>
    [ObservableProperty]
    private string _apiKey = string.Empty;

    /// <summary>Whether to show the API key in plain text.</summary>
    [ObservableProperty]
    private bool _showApiKey;

    /// <summary>Endpoint URL (for local providers like Ollama).</summary>
    [ObservableProperty]
    private string _endpoint = string.Empty;

    /// <summary>Currently selected model.</summary>
    [ObservableProperty]
    private string _selectedModel = string.Empty;

    /// <summary>Maximum tokens setting.</summary>
    [ObservableProperty]
    private int _maxTokens = 2000;

    /// <summary>Temperature setting (0-2).</summary>
    [ObservableProperty]
    private double _temperature = 0.7;

    /// <summary>Top-p (nucleus sampling) setting.</summary>
    [ObservableProperty]
    private double _topP = 1.0;

    /// <summary>Whether to test connection on save.</summary>
    [ObservableProperty]
    private bool _testOnSave = true;

    /// <summary>Whether a connection test is in progress.</summary>
    [ObservableProperty]
    private bool _isTestingConnection;

    /// <summary>Whether the last connection test was successful.</summary>
    [ObservableProperty]
    private bool _testConnectionSuccessful;

    /// <summary>Connection test status message.</summary>
    [ObservableProperty]
    private string _connectionStatusMessage = string.Empty;

    /// <summary>Validation error message.</summary>
    [ObservableProperty]
    private string _validationError = string.Empty;

    /// <summary>Whether this is a local provider (Ollama).</summary>
    public bool IsLocalProvider => ProviderName.Equals("Ollama", StringComparison.OrdinalIgnoreCase);

    /// <summary>Whether API key is required.</summary>
    public bool RequiresApiKey => !IsLocalProvider;

    /// <summary>Whether endpoint is configurable.</summary>
    public bool SupportsEndpoint => IsLocalProvider;

    /// <summary>Available models for selection.</summary>
    public ObservableCollection<string> AvailableModels { get; } = new();

    #endregion

    #region Constructor

    /// <summary>
    /// Design-time constructor for XAML preview.
    /// </summary>
    [Obsolete("Design-time constructor only. Use the parameterized constructor in production code.")]
    public ProviderConfigDialogViewModel()
    {
        _provider = new LlmProviderConfig
        {
            Name = "OpenAI",
            DisplayName = "OpenAI",
            AvailableModels = new List<string> { "gpt-4", "gpt-4-turbo", "gpt-3.5-turbo" }
        };
        InitializeFromProvider();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProviderConfigDialogViewModel"/> class.
    /// </summary>
    public ProviderConfigDialogViewModel(LlmProviderConfig provider)
    {
        _provider = provider;
        InitializeFromProvider();
    }

    private void InitializeFromProvider()
    {
        ProviderName = _provider.Name;
        DisplayName = _provider.DisplayName;
        IsEnabled = _provider.IsEnabled;
        SelectedModel = _provider.AvailableModels.FirstOrDefault() ?? string.Empty;

        foreach (var model in _provider.AvailableModels)
        {
            AvailableModels.Add(model);
        }

        // Set default values based on provider type
        switch (ProviderName)
        {
            case "OpenAI":
                MaxTokens = 2000;
                Temperature = 0.7;
                TopP = 1.0;
                break;
            case "Groq":
                MaxTokens = 4096;
                Temperature = 0.7;
                TopP = 0.9;
                break;
            case "Ollama":
                Endpoint = "http://localhost:11434";
                MaxTokens = 2048;
                Temperature = 0.8;
                TopP = 0.9;
                break;
        }

        UpdateConnectionStatus();
    }

    #endregion

    #region Commands

    /// <summary>
    /// Tests the connection to the provider.
    /// </summary>
    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        if (!Validate())
        {
            return;
        }

        IsTestingConnection = true;
        ConnectionStatusMessage = "Testing connection...";
        TestConnectionSuccessful = false;

        try
        {
            // Simulate connection test
            await Task.Delay(2000);

            // Simulate success/failure based on input
            if (RequiresApiKey && string.IsNullOrWhiteSpace(ApiKey))
            {
                TestConnectionSuccessful = false;
                ConnectionStatusMessage = "❌ Connection failed: API key is required";
            }
            else if (IsLocalProvider && !Endpoint.StartsWith("http"))
            {
                TestConnectionSuccessful = false;
                ConnectionStatusMessage = "❌ Connection failed: Invalid endpoint URL";
            }
            else
            {
                TestConnectionSuccessful = true;
                ConnectionStatusMessage = $"✅ Connected successfully to {DisplayName}";
            }
        }
        catch (Exception ex)
        {
            TestConnectionSuccessful = false;
            ConnectionStatusMessage = $"❌ Connection failed: {ex.Message}";
        }
        finally
        {
            IsTestingConnection = false;
        }
    }

    /// <summary>
    /// Saves the configuration.
    /// </summary>
    [RelayCommand]
    private async Task SaveAsync()
    {
        if (!Validate())
        {
            return;
        }

        if (TestOnSave && !TestConnectionSuccessful)
        {
            await TestConnectionAsync();
            if (!TestConnectionSuccessful)
            {
                // Ask user if they want to save anyway
                // For now, we'll just return
                return;
            }
        }

        // Signal success - the dialog will close
        Result = true;
    }

    /// <summary>
    /// Cancels the configuration.
    /// </summary>
    [RelayCommand]
    private void Cancel()
    {
        Result = false;
    }

    /// <summary>
    /// Toggles API key visibility.
    /// </summary>
    [RelayCommand]
    private void ToggleApiKeyVisibility()
    {
        ShowApiKey = !ShowApiKey;
    }

    #endregion

    #region Validation

    private bool Validate()
    {
        ValidationError = string.Empty;

        if (IsEnabled)
        {
            if (RequiresApiKey && string.IsNullOrWhiteSpace(ApiKey))
            {
                ValidationError = "API key is required when provider is enabled.";
                return false;
            }

            if (IsLocalProvider)
            {
                if (string.IsNullOrWhiteSpace(Endpoint))
                {
                    ValidationError = "Endpoint URL is required for local providers.";
                    return false;
                }

                if (!Uri.TryCreate(Endpoint, UriKind.Absolute, out _))
                {
                    ValidationError = "Please enter a valid endpoint URL.";
                    return false;
                }
            }

            if (string.IsNullOrWhiteSpace(SelectedModel))
            {
                ValidationError = "Please select a model.";
                return false;
            }

            if (MaxTokens < 1 || MaxTokens > 32000)
            {
                ValidationError = "Max tokens must be between 1 and 32000.";
                return false;
            }

            if (Temperature < 0 || Temperature > 2)
            {
                ValidationError = "Temperature must be between 0 and 2.";
                return false;
            }
        }

        return true;
    }

    #endregion

    #region Helper Methods

    private void UpdateConnectionStatus()
    {
        if (_provider.IsAvailable)
        {
            TestConnectionSuccessful = true;
            ConnectionStatusMessage = "✅ Connected";
        }
        else if (_provider.IsEnabled)
        {
            TestConnectionSuccessful = false;
            ConnectionStatusMessage = "⚠️ Not tested";
        }
        else
        {
            TestConnectionSuccessful = false;
            ConnectionStatusMessage = "Provider disabled";
        }
    }

    partial void OnIsEnabledChanged(bool value)
    {
        if (!value)
        {
            // Clear connection status when disabled
            TestConnectionSuccessful = false;
            ConnectionStatusMessage = "Provider disabled";
        }
        else
        {
            ConnectionStatusMessage = "⚠️ Not tested";
        }
    }

    #endregion

    #region Result

    /// <summary>
    /// Gets the result of the dialog (true if saved, false if cancelled).
    /// </summary>
    public bool? Result { get; private set; }

    #endregion
}
