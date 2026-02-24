using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Presentation.Models.Security;
using SaveState.Presentation.Services;
using System.Collections.ObjectModel;

namespace SaveState.Presentation.ViewModels.Settings;

/// <summary>
/// Service for API key management operations.
/// </summary>
public interface IApiKeyService
{
    /// <summary>
    /// Gets all API keys for the current user.
    /// </summary>
    Task<Result<IReadOnlyList<ApiKey>>> GetApiKeysAsync(CancellationToken ct = default);

    /// <summary>
    /// Generates a new API key.
    /// </summary>
    Task<Result<(ApiKey key, string fullKey)>> GenerateApiKeyAsync(string name, string description, List<string> scopes, DateTime? expiresAt, CancellationToken ct = default);

    /// <summary>
    /// Revokes an API key.
    /// </summary>
    Task<Result> RevokeApiKeyAsync(string keyId, CancellationToken ct = default);

    /// <summary>
    /// Deletes an API key permanently.
    /// </summary>
    Task<Result> DeleteApiKeyAsync(string keyId, CancellationToken ct = default);

    /// <summary>
    /// Gets API key usage statistics.
    /// </summary>
    Task<Result<ApiKeyUsageStats>> GetUsageStatsAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets detailed usage for a specific API key.
    /// </summary>
    Task<Result<object>> GetKeyUsageDetailsAsync(string keyId, CancellationToken ct = default);
}

/// <summary>
/// ViewModel for API key management.
/// Provides functionality for generating, viewing, and revoking API keys.
/// </summary>
public partial class ApiKeyManagerViewModel : ObservableObject
{
    private readonly IApiKeyService? _apiKeyService;
    private readonly IClipboardService? _clipboardService;
    private readonly IDialogService? _dialogService;
    private readonly INotificationService? _notificationService;
    private readonly ITimeProvider _timeProvider;

    /// <summary>Collection of API keys.</summary>
    [ObservableProperty]
    private ObservableCollection<ApiKey> _apiKeys = new();

    /// <summary>Currently selected API key.</summary>
    [ObservableProperty]
    private ApiKey? _selectedKey;

    /// <summary>Whether the generate key dialog is visible.</summary>
    [ObservableProperty]
    private bool _isGeneratingKey;

    /// <summary>Name for the new API key being generated.</summary>
    [ObservableProperty]
    private string _newKeyName = string.Empty;

    /// <summary>Description for the new API key.</summary>
    [ObservableProperty]
    private string _newKeyDescription = string.Empty;

    /// <summary>Expiration date for the new API key.</summary>
    [ObservableProperty]
    private DateTime? _newKeyExpiresAt;

    /// <summary>Scopes for the new API key.</summary>
    [ObservableProperty]
    private ObservableCollection<string> _newKeyScopes = new();

    /// <summary>The newly generated API key value (shown once).</summary>
    [ObservableProperty]
    private string? _newlyGeneratedKey;

    /// <summary>Whether to show the newly generated key.</summary>
    [ObservableProperty]
    private bool _showNewKey;

    /// <summary>API key usage statistics.</summary>
    [ObservableProperty]
    private ApiKeyUsageStats _usageStats = new();

    /// <summary>Whether the usage details panel is visible.</summary>
    [ObservableProperty]
    private bool _showUsageDetails;

    /// <summary>Available scopes for API keys.</summary>
    public List<string> AvailableScopes { get; } = new()
    {
        "read:library",
        "write:games",
        "read:savestates",
        "write:savestates",
        "read:achievements",
        "write:achievements",
        "read:collections",
        "write:collections",
        "read:user",
        "write:user",
        "admin:users",
        "admin:settings",
        "plugin:install",
        "plugin:manage",
        "api:full"
    };

    /// <summary>
    /// Design-time constructor for XAML preview.
    /// </summary>
    [Obsolete("Design-time constructor only. Use the parameterized constructor in production code.")]
    public ApiKeyManagerViewModel()
    {
        _timeProvider = SystemTimeProvider.Instance;
        InitializeSampleData();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiKeyManagerViewModel"/> class.
    /// </summary>
    public ApiKeyManagerViewModel(
        IClipboardService clipboardService,
        INotificationService notificationService,
        IApiKeyService? apiKeyService = null,
        IDialogService? dialogService = null,
        ITimeProvider? timeProvider = null)
    {
        _clipboardService = clipboardService;
        _notificationService = notificationService;
        _apiKeyService = apiKeyService;
        _dialogService = dialogService;
        _timeProvider = timeProvider ?? SystemTimeProvider.Instance;
        InitializeSampleData();
    }

    private void InitializeSampleData()
    {
        ApiKeys = new ObservableCollection<ApiKey>
        {
            new()
            {
                Id = "key_1",
                Name = "Plugin Dev",
                Description = "Development key for plugin testing",
                MaskedKey = "ssk_••••••••xxxx",
                CreatedAt = _timeProvider.UtcNow.AddMonths(-1),
                LastUsed = _timeProvider.UtcNow.AddDays(-1),
                ExpiresAt = null,
                Scopes = new() { "read:library", "write:games", "plugin:install" },
                Permissions = new() { "read:library", "write:games", "plugin:install" },
                IsActive = true,
                CreatedBy = "admin"
            },
            new()
            {
                Id = "key_2",
                Name = "External App",
                Description = "Integration with external dashboard",
                MaskedKey = "ssk_••••••••yyyy",
                CreatedAt = _timeProvider.UtcNow.AddDays(-20),
                LastUsed = _timeProvider.UtcNow.AddHours(-2),
                ExpiresAt = _timeProvider.UtcNow.AddDays(30),
                Scopes = new() { "read:library", "read:savestates", "read:achievements" },
                Permissions = new() { "read:library", "read:savestates", "read:achievements" },
                IsActive = true,
                CreatedBy = "admin"
            },
            new()
            {
                Id = "key_3",
                Name = "Automation",
                Description = "CI/CD automation scripts",
                MaskedKey = "ssk_••••••••zzzz",
                CreatedAt = _timeProvider.UtcNow.AddDays(-60),
                LastUsed = _timeProvider.UtcNow.AddDays(-45),
                ExpiresAt = _timeProvider.UtcNow.AddDays(-5),
                Scopes = new() { "write:games", "api:full" },
                Permissions = new() { "write:games", "api:full" },
                IsActive = false,
                CreatedBy = "dev_user"
            },
            new()
            {
                Id = "key_4",
                Name = "Mobile Sync",
                Description = "Mobile app synchronization",
                MaskedKey = "ssk_••••••••wwww",
                CreatedAt = _timeProvider.UtcNow.AddDays(-5),
                LastUsed = _timeProvider.UtcNow,
                ExpiresAt = _timeProvider.UtcNow.AddYears(1),
                Scopes = new() { "read:library", "read:savestates", "write:savestates", "read:user" },
                Permissions = new() { "read:library", "read:savestates", "write:savestates", "read:user" },
                IsActive = true,
                CreatedBy = "admin"
            }
        };

        UsageStats = new ApiKeyUsageStats
        {
            TotalCallsToday = 1234,
            MostActiveKeyName = "Plugin Dev",
            MostActiveKeyCalls = 892,
            CallsLastHour = 45,
            AverageResponseTimeMs = 125.5
        };
    }

    /// <summary>
    /// Loads API keys from the service.
    /// </summary>
    [RelayCommand]
    private async Task LoadApiKeysAsync()
    {
        try
        {
            if (_apiKeyService is not null)
            {
                var result = await _apiKeyService.GetApiKeysAsync();
                if (result.IsSuccess && result.Value is not null)
                {
                    ApiKeys = new ObservableCollection<ApiKey>(result.Value);
                    _notificationService?.ShowSuccess($"Loaded {ApiKeys.Count} API keys", "API Keys Loaded");
                }
                else
                {
                    _notificationService?.ShowError($"Failed to load API keys: {result.Error}");
                }

                var statsResult = await _apiKeyService.GetUsageStatsAsync();
                if (statsResult.IsSuccess && statsResult.Value is not null)
                {
                    UsageStats = statsResult.Value;
                }
            }
            else
            {
                _notificationService?.ShowNotificationAsync("API keys refreshed (sample data)", "Refresh");
            }
        }
        catch (Exception ex)
        {
            _notificationService?.ShowError($"Error loading API keys: {ex.Message}");
        }
    }

    /// <summary>
    /// Shows the generate key dialog.
    /// </summary>
    [RelayCommand]
    private void ShowGenerateDialog()
    {
        IsGeneratingKey = true;
        ShowNewKey = false;
        NewlyGeneratedKey = null;
        NewKeyName = string.Empty;
        NewKeyDescription = string.Empty;
        NewKeyExpiresAt = null;
        NewKeyScopes.Clear();
        // Default to read:library
        NewKeyScopes.Add("read:library");
    }

    /// <summary>
    /// Cancels the key generation process.
    /// </summary>
    [RelayCommand]
    private void CancelGenerateKey()
    {
        IsGeneratingKey = false;
        NewKeyName = string.Empty;
        NewKeyDescription = string.Empty;
        NewKeyExpiresAt = null;
        NewKeyScopes.Clear();
        if (!ShowNewKey)
        {
            NewlyGeneratedKey = null;
        }
    }

    /// <summary>
    /// Hides the new key display.
    /// </summary>
    [RelayCommand]
    private void HideNewKeyDisplay()
    {
        ShowNewKey = false;
        NewlyGeneratedKey = null;
        IsGeneratingKey = false;
    }

    /// <summary>
    /// Generates a new API key.
    /// </summary>
    [RelayCommand]
    private async Task GenerateKeyAsync()
    {
        if (string.IsNullOrWhiteSpace(NewKeyName))
        {
            _notificationService?.ShowNotificationAsync("Key name is required", "Validation Error");
            return;
        }

        if (NewKeyScopes.Count == 0)
        {
            _notificationService?.ShowNotificationAsync("At least one scope must be selected", "Validation Error");
            return;
        }

        try
        {
            if (_apiKeyService is not null)
            {
                var result = await _apiKeyService.GenerateApiKeyAsync(NewKeyName, NewKeyDescription, NewKeyScopes.ToList(), NewKeyExpiresAt);
                if (result.IsSuccess)
                {
                    var (key, fullKey) = result.Value;
                    ApiKeys.Add(key);
                    NewlyGeneratedKey = fullKey;
                    ShowNewKey = true;
                    _notificationService?.ShowSuccess("API key generated successfully", "Key Generated");
                }
                else
                {
                    _notificationService?.ShowError($"Failed to generate key: {result.Error}");
                }
            }
            else
            {
                // Simulate key generation
                var fullKey = $"ssk_live_{Guid.NewGuid().ToString("N")}";
                var maskedKey = $"ssk_••••••••{fullKey[^4..]}";

                var newKey = new ApiKey
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = NewKeyName,
                    Description = NewKeyDescription,
                    MaskedKey = maskedKey,
                    CreatedAt = _timeProvider.UtcNow,
                    ExpiresAt = NewKeyExpiresAt,
                    Scopes = new List<string>(NewKeyScopes),
                    Permissions = new List<string>(NewKeyScopes),
                    IsActive = true,
                    CreatedBy = "current_user"
                };

                ApiKeys.Add(newKey);
                NewlyGeneratedKey = fullKey;
                ShowNewKey = true;
                _notificationService?.ShowSuccess("API key generated (sample mode)", "Key Generated");
            }
        }
        catch (Exception ex)
        {
            _notificationService?.ShowError($"Error generating key: {ex.Message}");
        }
    }

    /// <summary>
    /// Copies a key to clipboard.
    /// </summary>
    [RelayCommand]
    private async Task CopyKeyAsync(string? key)
    {
        if (string.IsNullOrEmpty(key) || _clipboardService is null) return;

        try
        {
            await _clipboardService.SetTextAsync(key);
            _notificationService?.ShowSuccess(
                "API key copied to clipboard. Store it securely - it won't be shown again.",
                "Key Copied");
        }
        catch (Exception ex)
        {
            _notificationService?.ShowError(
                $"Failed to copy to clipboard: {ex.Message}",
                "Copy Failed");
        }
    }

    /// <summary>
    /// Revokes an API key.
    /// </summary>
    [RelayCommand]
    private async Task RevokeKeyAsync(ApiKey? key)
    {
        if (key is null) return;

        try
        {
            var confirmed = await (_dialogService?.ShowConfirmationAsync(
                "Revoke API Key",
                $"Are you sure you want to revoke the API key '{key.Name}'?\n\nThis will immediately disable the key and any applications using it will stop working.",
                "Revoke",
                "Cancel") ?? Task.FromResult(false));

            if (!confirmed) return;

            if (_apiKeyService is not null)
            {
                var result = await _apiKeyService.RevokeApiKeyAsync(key.Id);
                if (result.IsSuccess)
                {
                    key.IsActive = false;
                    // Update in collection
                    var index = ApiKeys.IndexOf(key);
                    if (index >= 0) ApiKeys[index] = key;
                    _notificationService?.ShowSuccess($"API key '{key.Name}' revoked", "Key Revoked");
                }
                else
                {
                    _notificationService?.ShowError($"Failed to revoke key: {result.Error}");
                }
            }
            else
            {
                key.IsActive = false;
                var index = ApiKeys.IndexOf(key);
                if (index >= 0) ApiKeys[index] = key;
                _notificationService?.ShowSuccess($"API key '{key.Name}' revoked (sample mode)", "Key Revoked");
            }
        }
        catch (Exception ex)
        {
            _notificationService?.ShowError($"Error revoking key: {ex.Message}");
        }
    }

    /// <summary>
    /// Permanently deletes an API key.
    /// </summary>
    [RelayCommand]
    private async Task DeleteKeyAsync(ApiKey? key)
    {
        if (key is null) return;

        try
        {
            var confirmed = await (_dialogService?.ShowConfirmationAsync(
                "Delete API Key",
                $"Are you sure you want to permanently delete the API key '{key.Name}'?\n\nThis action cannot be undone.",
                "Delete",
                "Cancel") ?? Task.FromResult(false));

            if (!confirmed) return;

            if (_apiKeyService is not null)
            {
                var result = await _apiKeyService.DeleteApiKeyAsync(key.Id);
                if (result.IsSuccess)
                {
                    ApiKeys.Remove(key);
                    if (SelectedKey == key) SelectedKey = null;
                    _notificationService?.ShowSuccess($"API key '{key.Name}' deleted", "Key Deleted");
                }
                else
                {
                    _notificationService?.ShowError($"Failed to delete key: {result.Error}");
                }
            }
            else
            {
                ApiKeys.Remove(key);
                if (SelectedKey == key) SelectedKey = null;
                _notificationService?.ShowSuccess($"API key '{key.Name}' deleted (sample mode)", "Key Deleted");
            }
        }
        catch (Exception ex)
        {
            _notificationService?.ShowError($"Error deleting key: {ex.Message}");
        }
    }

    /// <summary>
    /// Views usage statistics for an API key.
    /// </summary>
    [RelayCommand]
    private async Task ViewKeyUsageAsync(ApiKey? key)
    {
        if (key is null) return;

        try
        {
            if (_apiKeyService is not null)
            {
                var result = await _apiKeyService.GetKeyUsageDetailsAsync(key.Id);
                if (result.IsSuccess)
                {
                    // Show usage details dialog
                    await (_dialogService?.ShowInformationAsync(
                        $"API Key Usage - {key.Name}",
                        $"Usage details for this key would be shown here.") ?? Task.CompletedTask);
                }
                else
                {
                    _notificationService?.ShowError($"Failed to load usage: {result.Error}");
                }
            }
            else
            {
                // Sample usage data
                var usageInfo =
                    $"Total Calls: 1,234\n" +
                    $"Calls Today: 56\n" +
                    $"Calls This Week: 423\n" +
                    $"Calls This Month: 1,234\n" +
                    $"\n" +
                    $"Average Response Time: 125ms\n" +
                    $"Error Rate: 0.3%\n" +
                    $"\n" +
                    $"Top Endpoints:\n" +
                    $"  - GET /api/games: 523 calls\n" +
                    $"  - GET /api/savestates: 312 calls\n" +
                    $"  - POST /api/savestates: 189 calls\n" +
                    $"  - GET /api/achievements: 210 calls\n" +
                    $"\n" +
                    $"Last Used: {key.LastUsed:g}\n" +
                    $"Created: {key.CreatedAt:g}";

                await (_dialogService?.ShowInformationAsync(
                    $"API Key Usage - {key.Name} (Sample)",
                    usageInfo) ?? Task.CompletedTask);
            }
        }
        catch (Exception ex)
        {
            _notificationService?.ShowError($"Error loading usage: {ex.Message}");
        }
    }

    /// <summary>
    /// Toggles a scope selection for the new key.
    /// </summary>
    [RelayCommand]
    private void ToggleScope(string scope)
    {
        if (NewKeyScopes.Contains(scope))
        {
            // Prevent removing the last scope
            if (NewKeyScopes.Count > 1)
            {
                NewKeyScopes.Remove(scope);
            }
            else
            {
                _notificationService?.ShowNotificationAsync("At least one scope must be selected", "Scope Required");
            }
        }
        else
        {
            NewKeyScopes.Add(scope);
        }
    }

    /// <summary>
    /// Sets the expiration date for the new key.
    /// </summary>
    [RelayCommand]
    private void SetExpiration(string? duration)
    {
        NewKeyExpiresAt = duration switch
        {
            "7days" => _timeProvider.UtcNow.AddDays(7),
            "30days" => _timeProvider.UtcNow.AddDays(30),
            "90days" => _timeProvider.UtcNow.AddDays(90),
            "1year" => _timeProvider.UtcNow.AddYears(1),
            _ => null
        };
    }
}
