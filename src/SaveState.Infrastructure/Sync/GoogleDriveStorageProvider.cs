using Microsoft.Extensions.Logging;
using SaveState.Core.Common.Services;
using SaveState.Core.Sync;
using System.Net.Http.Headers;
using System.Text.Json;

namespace SaveState.Infrastructure.Sync;

/// <summary>
/// Google Drive implementation of ICloudStorageProvider using Google Drive API v3.
/// </summary>
public class GoogleDriveStorageProvider : ICloudStorageProvider
{
    private readonly HttpClient _httpClient;
    private readonly ICloudAuthenticationService _authService;
    private readonly ILogger<GoogleDriveStorageProvider> _logger;
    private readonly IUserPreferencesService _preferencesService;
    private OAuth2TokenResponse? _token;
    private readonly Dictionary<string, string> _pathToIdMap = new();

    public string ProviderName => "Google Drive";

    public bool IsAuthenticated => _token != null && _token.ExpiresAt > DateTime.UtcNow;

    public GoogleDriveStorageProvider(
        HttpClient httpClient,
        ICloudAuthenticationService authService,
        IUserPreferencesService preferencesService,
        ILogger<GoogleDriveStorageProvider> logger)
    {
        _httpClient = httpClient;
        _authService = authService;
        _preferencesService = preferencesService;
        _logger = logger;
    }

    public async Task<bool> AuthenticateAsync(CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Starting Google Drive authentication flow...");

            var savedClientId = await _preferencesService.GetCloudClientIdAsync("Google Drive", ct);
            var clientId = string.IsNullOrEmpty(savedClientId)
                ? "your-google-client-id.apps.googleusercontent.com" // Placeholder
                : savedClientId;
            var scopes = new[] { "https://www.googleapis.com/auth/drive.file", "https://www.googleapis.com/auth/drive.metadata.readonly" };
            var authUrl = "https://accounts.google.com/o/oauth2/v2/auth";
            var tokenUrl = "https://oauth2.googleapis.com/token";

            _token = await _authService.AuthenticateAsync("Google Drive", clientId, scopes, authUrl, tokenUrl, ct).ConfigureAwait(false);

            if (_token != null)
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token.AccessToken);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Google Drive authentication failed");
            return false;
        }
    }

    private async Task EnsureAuthenticatedAsync(CancellationToken ct)
    {
        if (_token != null && _token.ExpiresAt <= DateTime.UtcNow.AddMinutes(5) && _token.RefreshToken != null)
        {
            _logger.LogInformation("Refreshing Google Drive access token...");
            var savedClientId = await _preferencesService.GetCloudClientIdAsync("Google Drive", ct);
            var clientId = string.IsNullOrEmpty(savedClientId)
                ? "your-google-client-id.apps.googleusercontent.com"
                : savedClientId;
            var tokenUrl = "https://oauth2.googleapis.com/token";

            var newToken = await _authService.RefreshTokenAsync(clientId, _token.RefreshToken, tokenUrl, ct).ConfigureAwait(false);
            if (newToken != null)
            {
                _token = newToken;
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token.AccessToken);
            }
        }
    }

    public async Task<bool> UploadFileAsync(string localPath, string remotePath, CancellationToken ct = default)
    {
        if (!IsAuthenticated) return false;

        try
        {
            _logger.LogInformation("Uploading {LocalPath} to Google Drive:{RemotePath}", localPath, remotePath);

            // Google Drive uses a different approach: multipart/related or simple upload.
            // For simplicity, we use the simple upload for small files.
            var content = new StreamContent(File.OpenRead(localPath));
            var url = "https://www.googleapis.com/upload/drive/v3/files?uploadType=media";

            var response = await _httpClient.PostAsync(url, content, ct).ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload to Google Drive");
            return false;
        }
    }

    public async Task<bool> DownloadFileAsync(string remotePath, string localPath, CancellationToken ct = default)
    {
        if (!IsAuthenticated) return false;

        try
        {
            _logger.LogInformation("Downloading from Google Drive:{RemotePath} to {LocalPath}", remotePath, localPath);

            // In Google Drive, you need the file ID. remotePath would need to be mapped to ID.
            // Placeholder URL
            var fileId = "placeholder_id";
            var url = $"https://www.googleapis.com/drive/v3/files/{fileId}?alt=media";

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
            _logger.LogError(ex, "Failed to download from Google Drive");
            return false;
        }
    }

    public async Task<bool> DeleteFileAsync(string remotePath, CancellationToken ct = default)
    {
        if (!IsAuthenticated) return false;

        try
        {
            _logger.LogInformation("Deleting from Google Drive:{RemotePath}", remotePath);
            var fileId = "placeholder_id";
            var url = $"https://www.googleapis.com/drive/v3/files/{fileId}";
            var response = await _httpClient.DeleteAsync(url, ct).ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete from Google Drive");
            return false;
        }
    }

    public async Task<IReadOnlyList<CloudFileInfo>> ListFilesAsync(string remotePath, CancellationToken ct = default)
    {
        if (!IsAuthenticated) return new List<CloudFileInfo>();

        try
        {
            var url = "https://www.googleapis.com/drive/v3/files?fields=files(id,name,size,modifiedTime,mimeType,parents)";
            var response = await _httpClient.GetAsync(url, ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode) return new List<CloudFileInfo>();

            var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(json);

            var files = new List<CloudFileInfo>();
            if (doc.RootElement.TryGetProperty("files", out var filesProp) && filesProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in filesProp.EnumerateArray())
                {
                    var id = item.GetProperty("id").GetString() ?? string.Empty;
                    var name = item.GetProperty("name").GetString() ?? string.Empty;
                    var path = remotePath.EndsWith("/") ? remotePath + name : remotePath + "/" + name;
                    var size = item.TryGetProperty("size", out var s) ? long.Parse(s.GetString() ?? "0") : 0;
                    var modified = item.TryGetProperty("modifiedTime", out var m) ? m.GetDateTime() : DateTime.UtcNow;
                    var mimeType = item.TryGetProperty("mimeType", out var mt) ? mt.GetString() : string.Empty;
                    var isDir = mimeType == "application/vnd.google-apps.folder";

                    _pathToIdMap[path] = id;
                    files.Add(new CloudFileInfo(path, name, size, modified, IsDirectory: isDir));
                }
            }

            return files;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list Google Drive files");
            return new List<CloudFileInfo>();
        }
    }

    public async Task<CloudFileInfo?> GetFileInfoAsync(string remotePath, CancellationToken ct = default)
    {
        if (!IsAuthenticated) return null;

        try
        {
            if (!_pathToIdMap.TryGetValue(remotePath, out var fileId))
            {
                // If not in map, we might need to search by name/parent, but for now we'll try to find it in root
                await ListFilesAsync("/", ct).ConfigureAwait(false);
                if (!_pathToIdMap.TryGetValue(remotePath, out fileId)) return null;
            }

            var url = $"https://www.googleapis.com/drive/v3/files/{fileId}?fields=id,name,size,modifiedTime,mimeType";
            var response = await _httpClient.GetAsync(url, ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var name = root.GetProperty("name").GetString() ?? string.Empty;
            var size = root.TryGetProperty("size", out var s) ? long.Parse(s.GetString() ?? "0") : 0;
            var modified = root.TryGetProperty("modifiedTime", out var m) ? m.GetDateTime() : DateTime.UtcNow;
            var mimeType = root.TryGetProperty("mimeType", out var mt) ? mt.GetString() : string.Empty;
            var isDir = mimeType == "application/vnd.google-apps.folder";

            return new CloudFileInfo(remotePath, name, size, modified, IsDirectory: isDir);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Google Drive file info");
            return null;
        }
    }

    public async Task<bool> FileExistsAsync(string remotePath, CancellationToken ct = default)
    {
        return (await GetFileInfoAsync(remotePath, ct).ConfigureAwait(false)) != null;
    }
}
