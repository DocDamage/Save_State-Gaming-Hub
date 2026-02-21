using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Presentation.Models.Security;
using System.Collections.ObjectModel;

namespace SaveState.Presentation.ViewModels.Settings;

/// <summary>
/// ViewModel for API key management.
/// Provides functionality for generating, viewing, and revoking API keys.
/// </summary>
public partial class ApiKeyManagerViewModel : ObservableObject
{
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

    /// <summary>The newly generated API key value (shown once).</summary>
    [ObservableProperty]
    private string? _newlyGeneratedKey;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiKeyManagerViewModel"/> class.
    /// </summary>
    public ApiKeyManagerViewModel()
    {
        InitializeSampleData();
    }

    private void InitializeSampleData()
    {
        ApiKeys = new ObservableCollection<ApiKey>
        {
            new() { Id = "key_1", Name = "Plugin Dev", MaskedKey = "ssk_••••••••xxxx", CreatedAt = DateTime.Now.AddMonths(-1), LastUsed = DateTime.Now.AddDays(-1), Permissions = new() { "read:library", "write:games" } },
            new() { Id = "key_2", Name = "External App", MaskedKey = "ssk_••••••••yyyy", CreatedAt = DateTime.Now.AddDays(-20), Permissions = new() { "read:library" } }
        };
    }

    /// <summary>
    /// Shows the generate key dialog.
    /// </summary>
    [RelayCommand]
    private void ShowGenerateKey()
    {
        IsGeneratingKey = true;
        NewlyGeneratedKey = null;
    }

    /// <summary>
    /// Cancels the key generation process.
    /// </summary>
    [RelayCommand]
    private void CancelGenerateKey()
    {
        IsGeneratingKey = false;
        NewKeyName = string.Empty;
        NewlyGeneratedKey = null;
    }

    /// <summary>
    /// Generates a new API key.
    /// </summary>
    [RelayCommand]
    private async Task GenerateKeyAsync()
    {
        if (string.IsNullOrWhiteSpace(NewKeyName)) return;

        // Simulate key generation
        var newKey = $"ssk_{Guid.NewGuid().ToString("N")[..16]}";
        NewlyGeneratedKey = newKey;

        ApiKeys.Add(new ApiKey
        {
            Id = Guid.NewGuid().ToString(),
            Name = NewKeyName,
            MaskedKey = $"ssk_••••••••{newKey[^4..]}",
            CreatedAt = DateTime.Now,
            Permissions = new() { "read:library" }
        });

        await Task.CompletedTask;
    }

    /// <summary>
    /// Copies the newly generated key to clipboard.
    /// </summary>
    [RelayCommand]
    private void CopyKeyToClipboard()
    {
        if (NewlyGeneratedKey is null) return;
        // TODO: Copy to clipboard through service
    }

    /// <summary>
    /// Revokes an API key.
    /// </summary>
    /// <param name="key">The key to revoke.</param>
    [RelayCommand]
    private async Task RevokeKeyAsync(ApiKey? key)
    {
        if (key is null) return;
        ApiKeys.Remove(key);
        await Task.CompletedTask;
    }
}
