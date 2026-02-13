using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SaveState.Core.Common.Services;
using SaveState.Core.Sync;
using System.Net.Http.Headers;
using System.Text.Json;

namespace SaveState.Infrastructure.Sync;

/// <summary>
/// OneDrive implementation of ICloudStorageProvider using Microsoft Graph API.
/// </summary>
public class OneDriveStorageProvider : ICloudStorageProvider
{
    private readonly HttpClient _httpClient;
    private readonly ICloudAuthenticationService _authService;
    private readonly ILogger<OneDriveStorageProvider> _logger;
    private readonly IUserPreferencesService _preferencesService;
    private OAuth2TokenResponse? _token;

    public string ProviderName => "OneDrive";

    public bool IsAuthenticated => _token != null && _token.ExpiresAt > DateTime.UtcNow;

    public OneDriveStorageProvider(
        HttpClient httpClient,
        ICloudAuthenticationService authService,
        IUserPreferencesService preferencesService,
        ILogger<OneDriveStorageProvider> logger)
    {
        _httpClient = httpClient;
        _authService = authService;
        _preferencesService = preferencesService;
        _logger = logger;

        if (_httpClient.BaseAddress == null)
        {
            _httpClient.BaseAddress = new Uri("https://graph.microsoft.com/");
        }
    }

    public async Task<bool> AuthenticateAsync(CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Starting OneDrive authentication flow...");

            var savedClientId = await _preferencesService.GetCloudClientIdAsync("OneDrive", ct);
            var clientId = string.IsNullOrEmpty(savedClientId)
                ? "00000000-0000-0000-0000-000000000000" // Fallback placeholder
                : savedClientId;

            var scopes = new[] { "files.readwrite", "offline_access", "User.Read" };
            var authUrl = "https://login.microsoftonline.com/common/oauth2/v2.0/authorize";
            var tokenUrl = "https://login.microsoftonline.com/common/oauth2/v2.0/token";

            var result = await _authService.AuthenticateAsync("OneDrive", clientId, scopes, authUrl, tokenUrl, ct).ConfigureAwait(false);

            if (result.IsSuccess)
            {
                _token = result.Value;
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token.AccessToken);
                return true;
            }

            _logger.LogWarning("OneDrive authentication failed: {Error}", result.Error);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OneDrive authentication failed");
            return false;
        }
    }

    private async Task EnsureAuthenticatedAsync(CancellationToken ct)
    {
        if (_token != null && _token.ExpiresAt <= DateTime.UtcNow.AddMinutes(5) && _token.RefreshToken != null)
        {
            _logger.LogInformation("Refreshing OneDrive access token...");
            var savedClientId = await _preferencesService.GetCloudClientIdAsync("OneDrive", ct);
            var clientId = string.IsNullOrEmpty(savedClientId)
                ? "00000000-0000-0000-0000-000000000000"
                : savedClientId;
            var tokenUrl = "https://login.microsoftonline.com/common/oauth2/v2.0/token";

            var result = await _authService.RefreshTokenAsync(clientId, _token.RefreshToken, tokenUrl, ct).ConfigureAwait(false);
            if (result.IsSuccess)
            {
                _token = result.Value;
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token.AccessToken);
            }
            else
            {
                _logger.LogWarning("Token refresh failed: {Error}", result.Error);
            }
        }
    }

    public async Task<bool> UploadFileAsync(string localPath, string remotePath, CancellationToken ct = default)
    {
        if (!IsAuthenticated) return false;

        try
        {
            _logger.LogInformation("Uploading {LocalPath} to OneDrive:{RemotePath}", localPath, remotePath);

            var content = new StreamContent(File.OpenRead(localPath));
            // Microsoft Graph API path for simple upload: /me/drive/root:/path/to/file:/content
            var url = $"v1.0/me/drive/root:/{remotePath.TrimStart('/')}:/content";

            var response = await _httpClient.PutAsync(url, content, ct).ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload to OneDrive");
            return false;
        }
    }

    public async Task<bool> DownloadFileAsync(string remotePath, string localPath, CancellationToken ct = default)
    {
        if (!IsAuthenticated) return false;

        try
        {
            _logger.LogInformation("Downloading from OneDrive:{RemotePath} to {LocalPath}", remotePath, localPath);

            var url = $"v1.0/me/drive/root:/{remotePath.TrimStart('/')}:/content";
            var response = await _httpClient.GetAsync(url, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode) return false;

            var directory = Path.GetDirectoryName(localPath);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

            await using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            await using var fileStream = File.Create(localPath);
            await stream.CopyToAsync(fileStream, ct).ConfigureAwait(false);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download from OneDrive");
            return false;
        }
    }

    public async Task<bool> DeleteFileAsync(string remotePath, CancellationToken ct = default)
    {
        if (!IsAuthenticated) return false;

        try
        {
            _logger.LogInformation("Deleting from OneDrive:{RemotePath}", remotePath);
            var url = $"v1.0/me/drive/root:/{remotePath.TrimStart('/')}";
            var response = await _httpClient.DeleteAsync(url, ct).ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete from OneDrive");
            return false;
        }
    }

    public async Task<IReadOnlyList<CloudFileInfo>> ListFilesAsync(string remotePath, CancellationToken ct = default)
    {
        if (!IsAuthenticated) return new List<CloudFileInfo>();

        try
        {
            var url = remotePath == "/" || string.IsNullOrEmpty(remotePath)
                ? "v1.0/me/drive/root/children"
                : $"v1.0/me/drive/root:/{remotePath.TrimStart('/')}:/children";

            var response = await _httpClient.GetAsync(url, ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode) return new List<CloudFileInfo>();

            var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(json);

            var files = new List<CloudFileInfo>();
            if (doc.RootElement.TryGetProperty("value", out var value) && value.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in value.EnumerateArray())
                {
                    var name = item.GetProperty("name").GetString() ?? string.Empty;
                    var path = remotePath.EndsWith("/") ? remotePath + name : remotePath + "/" + name;
                    var size = item.TryGetProperty("size", out var s) ? s.GetInt64() : 0;
                    var modified = item.TryGetProperty("lastModifiedDateTime", out var m) ? m.GetDateTime() : DateTime.UtcNow;
                    var isDir = item.TryGetProperty("folder", out _);

                    files.Add(new CloudFileInfo(path, name, size, modified, IsDirectory: isDir));
                }
            }

            return files;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list OneDrive files");
            return new List<CloudFileInfo>();
        }
    }

    public async Task<Result<CloudFileInfo>> GetFileInfoAsync(string remotePath, CancellationToken ct = default)
    {
        if (!IsAuthenticated) return Result.Failure<CloudFileInfo>("Not authenticated", ErrorType.Unauthorized);

        try
        {
            var url = $"v1.0/me/drive/root:/{remotePath.TrimStart('/')}";
            var response = await _httpClient.GetAsync(url, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
                return Result.Failure<CloudFileInfo>($"Failed to get file info: {response.StatusCode}", ErrorType.External);

            var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var name = root.GetProperty("name").GetString() ?? string.Empty;
            var size = root.TryGetProperty("size", out var s) ? s.GetInt64() : 0;
            var modified = root.TryGetProperty("lastModifiedDateTime", out var m) ? m.GetDateTime() : DateTime.UtcNow;
            var isDir = root.TryGetProperty("folder", out _);

            return Result.Success(new CloudFileInfo(remotePath, name, size, modified, IsDirectory: isDir));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get OneDrive file info");
            return Result.Failure<CloudFileInfo>($"Failed to get file info: {ex.Message}", ErrorType.External);
        }
    }

    public async Task<bool> FileExistsAsync(string remotePath, CancellationToken ct = default)
    {
        var result = await GetFileInfoAsync(remotePath, ct).ConfigureAwait(false);
        return result.IsSuccess;
    }
}
