using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
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

            var result = await _authService.AuthenticateAsync("Google Drive", clientId, scopes, authUrl, tokenUrl, ct).ConfigureAwait(false);

            if (result.IsSuccess)
            {
                _token = result.Value;
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token.AccessToken);
                return true;
            }

            _logger.LogWarning("Google Drive authentication failed: {Error}", result.Error);
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

    public async Task<Result<bool>> UploadFileAsync(string localPath, string remotePath, CancellationToken ct = default)
    {
        if (!IsAuthenticated)
        {
            return Result.Failure<bool>("Not authenticated", ErrorType.Unauthorized);
        }

        if (string.IsNullOrWhiteSpace(localPath) || !File.Exists(localPath))
        {
            return Result.Failure<bool>("Local file not found", ErrorType.NotFound);
        }

        if (string.IsNullOrWhiteSpace(remotePath))
        {
            return Result.Failure<bool>("Remote path is required", ErrorType.Validation);
        }

        try
        {
            await EnsureAuthenticatedAsync(ct).ConfigureAwait(false);

            _logger.LogInformation("Uploading {LocalPath} to Google Drive:{RemotePath}", localPath, remotePath);

            // Extract parent folder and filename from remote path
            var fileName = Path.GetFileName(remotePath);
            var parentPath = Path.GetDirectoryName(remotePath)?.Replace("\\", "/") ?? "/";

            // Resolve parent folder ID (or use root if it's the root folder)
            string? parentId = null;
            if (parentPath != "/" && !string.IsNullOrEmpty(parentPath))
            {
                parentId = await ResolvePathToIdAsync(parentPath, ct).ConfigureAwait(false);
                if (parentId == null)
                {
                    // Try to create the parent folder path
                    parentId = await CreateFolderPathAsync(parentPath, ct).ConfigureAwait(false);
                }
            }

            // Create file metadata
            var metadata = new Dictionary<string, object>
            {
                ["name"] = fileName
            };
            if (parentId != null)
            {
                metadata["parents"] = new[] { parentId };
            }

            // Use multipart upload for files with metadata
            var boundary = "----SaveStateBoundary" + Guid.NewGuid().ToString("N");
            using var fileContent = new MultipartFormDataContent(boundary);

            var metadataJson = JsonSerializer.Serialize(metadata);
            var metadataContent = new StringContent(metadataJson, System.Text.Encoding.UTF8, "application/json");
            fileContent.Add(metadataContent, "metadata");

            var fileBytes = await File.ReadAllBytesAsync(localPath, ct).ConfigureAwait(false);
            var streamContent = new ByteArrayContent(fileBytes);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            fileContent.Add(streamContent, "file");

            var url = "https://www.googleapis.com/upload/drive/v3/files?uploadType=multipart";
            var response = await _httpClient.PostAsync(url, fileContent, ct).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                // Parse response to cache the new file ID
                var responseJson = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                using var doc = JsonDocument.Parse(responseJson);
                if (doc.RootElement.TryGetProperty("id", out var idProp))
                {
                    _pathToIdMap[remotePath] = idProp.GetString() ?? string.Empty;
                }
                return Result.Success(true);
            }

            _logger.LogWarning("Google Drive upload failed with status {Status}", response.StatusCode);
            return Result.Failure<bool>($"Google Drive upload failed: {response.StatusCode}", ErrorType.External);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload to Google Drive");
            return Result.Failure<bool>($"Failed to upload to Google Drive: {ex.Message}", ErrorType.External);
        }
    }

    public async Task<Result<bool>> DownloadFileAsync(string remotePath, string localPath, CancellationToken ct = default)
    {
        if (!IsAuthenticated)
        {
            return Result.Failure<bool>("Not authenticated", ErrorType.Unauthorized);
        }

        if (string.IsNullOrWhiteSpace(remotePath))
        {
            return Result.Failure<bool>("Remote path is required", ErrorType.Validation);
        }

        if (string.IsNullOrWhiteSpace(localPath))
        {
            return Result.Failure<bool>("Local path is required", ErrorType.Validation);
        }

        try
        {
            await EnsureAuthenticatedAsync(ct).ConfigureAwait(false);

            _logger.LogInformation("Downloading from Google Drive:{RemotePath} to {LocalPath}", remotePath, localPath);

            // Resolve the remote path to a Google Drive file ID
            var fileId = await ResolvePathToIdAsync(remotePath, ct).ConfigureAwait(false);
            if (string.IsNullOrEmpty(fileId))
            {
                _logger.LogWarning("Could not resolve path '{Path}' to Google Drive file ID", remotePath);
                return Result.Failure<bool>("Remote file not found", ErrorType.NotFound);
            }

            var url = $"https://www.googleapis.com/drive/v3/files/{fileId}?alt=media";
            var response = await _httpClient.GetAsync(url, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Google Drive download failed with status {Status}", response.StatusCode);
                var errorType = response.StatusCode == System.Net.HttpStatusCode.NotFound
                    ? ErrorType.NotFound
                    : ErrorType.External;
                return Result.Failure<bool>($"Google Drive download failed: {response.StatusCode}", errorType);
            }

            var directory = Path.GetDirectoryName(localPath);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

            await using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            await using var fileStream = File.Create(localPath);
            await stream.CopyToAsync(fileStream, ct).ConfigureAwait(false);

            return Result.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download from Google Drive");
            return Result.Failure<bool>($"Failed to download from Google Drive: {ex.Message}", ErrorType.External);
        }
    }

    public async Task<bool> DeleteFileAsync(string remotePath, CancellationToken ct = default)
    {
        if (!IsAuthenticated) return false;

        try
        {
            await EnsureAuthenticatedAsync(ct).ConfigureAwait(false);

            _logger.LogInformation("Deleting from Google Drive:{RemotePath}", remotePath);

            // Resolve the remote path to a Google Drive file ID
            var fileId = await ResolvePathToIdAsync(remotePath, ct).ConfigureAwait(false);
            if (string.IsNullOrEmpty(fileId))
            {
                _logger.LogWarning("Could not resolve path '{Path}' to Google Drive file ID", remotePath);
                return false;
            }

            var url = $"https://www.googleapis.com/drive/v3/files/{fileId}";
            var response = await _httpClient.DeleteAsync(url, ct).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                // Remove from cache
                _pathToIdMap.Remove(remotePath);
                return true;
            }

            _logger.LogWarning("Google Drive delete failed with status {Status}", response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete from Google Drive");
            return false;
        }
    }

    /// <summary>
    /// Resolves a file path to a Google Drive file ID.
    /// Uses the cached _pathToIdMap, falling back to searching the parent directory.
    /// </summary>
    private async Task<string?> ResolvePathToIdAsync(string remotePath, CancellationToken ct)
    {
        // Normalize path
        remotePath = remotePath.Replace("\\", "/").TrimEnd('/');
        if (!remotePath.StartsWith("/")) remotePath = "/" + remotePath;

        // Check cache first
        if (_pathToIdMap.TryGetValue(remotePath, out var cachedId))
        {
            return cachedId;
        }

        // Search for the file by name in its parent directory
        var fileName = Path.GetFileName(remotePath);
        var parentPath = Path.GetDirectoryName(remotePath)?.Replace("\\", "/") ?? "/";

        // Get parent folder ID
        string? parentId = null;
        if (parentPath != "/" && !string.IsNullOrEmpty(parentPath) && parentPath != "\\")
        {
            if (_pathToIdMap.TryGetValue(parentPath, out var cachedParentId))
            {
                parentId = cachedParentId;
            }
            else
            {
                // Recursively resolve parent
                parentId = await ResolvePathToIdAsync(parentPath, ct).ConfigureAwait(false);
            }
        }

        // Search for file by name in parent
        var query = $"name = '{fileName.Replace("'", "\\'")}' and trashed = false";
        if (parentId != null)
        {
            query += $" and '{parentId}' in parents";
        }
        else if (parentPath == "/" || string.IsNullOrEmpty(parentPath))
        {
            query += " and 'root' in parents";
        }

        var searchUrl = $"https://www.googleapis.com/drive/v3/files?q={Uri.EscapeDataString(query)}&fields=files(id,name)";
        var response = await _httpClient.GetAsync(searchUrl, ct).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        using var doc = JsonDocument.Parse(json);

        if (doc.RootElement.TryGetProperty("files", out var files) && files.ValueKind == JsonValueKind.Array)
        {
            foreach (var file in files.EnumerateArray())
            {
                var id = file.GetProperty("id").GetString();
                if (!string.IsNullOrEmpty(id))
                {
                    // Cache and return
                    _pathToIdMap[remotePath] = id;
                    return id;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Creates a folder path in Google Drive, creating intermediate folders as needed.
    /// </summary>
    private async Task<string?> CreateFolderPathAsync(string folderPath, CancellationToken ct)
    {
        var parts = folderPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        string? parentId = null; // null means root
        var currentPath = "";

        foreach (var part in parts)
        {
            currentPath += "/" + part;

            // Check if this folder already exists
            var existingId = await ResolvePathToIdAsync(currentPath, ct).ConfigureAwait(false);
            if (existingId != null)
            {
                parentId = existingId;
                continue;
            }

            // Create the folder
            var metadata = new Dictionary<string, object>
            {
                ["name"] = part,
                ["mimeType"] = "application/vnd.google-apps.folder"
            };
            if (parentId != null)
            {
                metadata["parents"] = new[] { parentId };
            }

            var metadataJson = JsonSerializer.Serialize(metadata);
            var content = new StringContent(metadataJson, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("https://www.googleapis.com/drive/v3/files", content, ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to create folder '{Folder}' in Google Drive", part);
                return null;
            }

            var responseJson = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(responseJson);
            if (doc.RootElement.TryGetProperty("id", out var idProp))
            {
                parentId = idProp.GetString();
                if (parentId != null)
                {
                    _pathToIdMap[currentPath] = parentId;
                }
            }
        }

        return parentId;
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

    public async Task<Result<CloudFileInfo>> GetFileInfoAsync(string remotePath, CancellationToken ct = default)
    {
        if (!IsAuthenticated) return Result.Failure<CloudFileInfo>("Not authenticated", ErrorType.Unauthorized);

        try
        {
            if (!_pathToIdMap.TryGetValue(remotePath, out var fileId))
            {
                // If not in map, we might need to search by name/parent, but for now we'll try to find it in root
                await ListFilesAsync("/", ct).ConfigureAwait(false);
                if (!_pathToIdMap.TryGetValue(remotePath, out fileId))
                    return Result.Failure<CloudFileInfo>("File not found", ErrorType.NotFound);
            }

            var url = $"https://www.googleapis.com/drive/v3/files/{fileId}?fields=id,name,size,modifiedTime,mimeType";
            var response = await _httpClient.GetAsync(url, ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                return Result.Failure<CloudFileInfo>($"Failed to get file info: {response.StatusCode}", ErrorType.External);

            var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var name = root.GetProperty("name").GetString() ?? string.Empty;
            var size = root.TryGetProperty("size", out var s) ? long.Parse(s.GetString() ?? "0") : 0;
            var modified = root.TryGetProperty("modifiedTime", out var m) ? m.GetDateTime() : DateTime.UtcNow;
            var mimeType = root.TryGetProperty("mimeType", out var mt) ? mt.GetString() : string.Empty;
            var isDir = mimeType == "application/vnd.google-apps.folder";

            return Result.Success(new CloudFileInfo(remotePath, name, size, modified, IsDirectory: isDir));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Google Drive file info");
            return Result.Failure<CloudFileInfo>($"Failed to get file info: {ex.Message}", ErrorType.External);
        }
    }

    public async Task<bool> FileExistsAsync(string remotePath, CancellationToken ct = default)
    {
        var result = await GetFileInfoAsync(remotePath, ct).ConfigureAwait(false);
        return result.IsSuccess;
    }
}
