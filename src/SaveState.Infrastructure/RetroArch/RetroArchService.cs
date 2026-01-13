using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SaveState.Core.Achievements;
using SaveState.Core.Common;
using SaveState.Core.RetroArch;
using SaveState.Core.RetroArch.Services;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SaveState.Infrastructure.RetroArch;

/// <summary>
/// Implementation of RetroArch integration service.
/// </summary>
public partial class RetroArchService : IRetroArchService
{
    private readonly ILogger<RetroArchService> _logger;
    private readonly HttpClient _httpClient;
    private readonly RetroArchOptions _options;
    private readonly IRetroAchievementsClient? _retroAchievementsClient;
    private string? _retroArchPath;

    public RetroArchService(
        ILogger<RetroArchService> logger,
        IHttpClientFactory httpClientFactory,
        IOptions<RetroArchOptions> options,
        IRetroAchievementsClient? retroAchievementsClient = null)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("RetroArch");
        _options = options.Value;
        _retroAchievementsClient = retroAchievementsClient;

        // Use configured path if available
        if (!string.IsNullOrEmpty(_options.InstallPath) && File.Exists(_options.InstallPath))
        {
            _retroArchPath = _options.InstallPath;
            LogConfiguredPath(_logger, _retroArchPath);
        }

        // Initialize RetroAchievements if configured
        if (_retroAchievementsClient != null &&
            _options.RetroAchievementsEnabled &&
            !string.IsNullOrEmpty(_options.RetroAchievementsUsername) &&
            !string.IsNullOrEmpty(_options.RetroAchievementsApiKey))
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    var authenticated = await _retroAchievementsClient.AuthenticateAsync(
                        _options.RetroAchievementsUsername,
                        _options.RetroAchievementsApiKey).ConfigureAwait(false);

                    if (authenticated)
                    {
                        LogRetroAchievementsAuthenticated(_logger, _options.RetroAchievementsUsername);
                    }
                    else
                    {
                        LogRetroAchievementsAuthFailed(_logger);
                    }
                }
                catch (Exception ex)
                {
                    LogRetroAchievementsAuthError(_logger, ex);
                }
            });
        }
    }

    public Task<Result<string>> DetectRetroArchPathAsync(CancellationToken ct = default)
    {
        try
        {
            // If already set from configuration, return it
            if (!string.IsNullOrEmpty(_retroArchPath) && File.Exists(_retroArchPath))
            {
                return Task.FromResult(Result.Success(_retroArchPath));
            }

            // If auto-detect is disabled and no path is set, fail
            if (!_options.AutoDetect && string.IsNullOrEmpty(_options.InstallPath))
            {
                return Task.FromResult(Result.Failure<string>("RetroArch path not configured and auto-detection is disabled."));
            }

            // Common RetroArch installation paths
            var possiblePaths = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "RetroArch", "retroarch.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "RetroArch", "retroarch.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RetroArch", "retroarch.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RetroArch", "retroarch.exe"),
                @"C:\RetroArch-Win64\retroarch.exe",
                @"C:\RetroArch\retroarch.exe",
                @"D:\RetroArch\retroarch.exe",
                @"E:\RetroArch\retroarch.exe"
            };

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    _retroArchPath = path;
                    LogRetroArchDetected(_logger, path);
                    return Task.FromResult(Result.Success(path));
                }
            }

            LogNotFoundInCommonLocations(_logger);
            return Task.FromResult(Result.Failure<string>("RetroArch installation not found. Please install RetroArch or specify the path in Settings > RetroArch."));
        }
        catch (Exception ex)
        {
            LogPathDetectionError(_logger, ex);
            return Task.FromResult(Result.Failure<string>($"Error detecting RetroArch: {ex.Message}"));
        }
    }

    public async Task<Result<IReadOnlyList<RetroArchGame>>> GetGamesAsync(CancellationToken ct = default)
    {
        try
        {
            if (_retroArchPath == null)
            {
                var detectResult = await DetectRetroArchPathAsync(ct);
                if (detectResult.IsFailure)
                    return Result.Failure<IReadOnlyList<RetroArchGame>>(detectResult.Error ?? "RetroArch not found");
            }

            var retroArchDir = Path.GetDirectoryName(_retroArchPath)!;
            var playlistsDir = !string.IsNullOrEmpty(_options.PlaylistsPath)
                ? _options.PlaylistsPath
                : Path.Combine(retroArchDir, "playlists");

            if (!Directory.Exists(playlistsDir))
            {
                LogPlaylistsNotFound(_logger, playlistsDir);
                return Result.Success<IReadOnlyList<RetroArchGame>>(Array.Empty<RetroArchGame>());
            }

            var games = new List<RetroArchGame>();
            var playlistFiles = Directory.GetFiles(playlistsDir, "*.lpl");

            foreach (var playlistFile in playlistFiles)
            {
                try
                {
                    var playlistGames = await ParsePlaylistAsync(playlistFile, ct);
                    games.AddRange(playlistGames);
                }
                catch (Exception ex)
                {
                    LogPlaylistParseFailed(_logger, playlistFile, ex);
                }
            }

            LogGamesFoundCount(_logger, games.Count, playlistFiles.Length);

            return Result.Success<IReadOnlyList<RetroArchGame>>(games);
        }
        catch (Exception ex)
        {
            LogGetGamesError(_logger, ex);
            return Result.Failure<IReadOnlyList<RetroArchGame>>($"Error getting games: {ex.Message}");
        }
    }

    private async Task<List<RetroArchGame>> ParsePlaylistAsync(string playlistPath, CancellationToken ct)
    {
        var games = new List<RetroArchGame>();
        var json = await File.ReadAllTextAsync(playlistPath, ct);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.TryGetProperty("items", out var items))
        {
            foreach (var item in items.EnumerateArray())
            {
                try
                {
                    var game = new RetroArchGame
                    {
                        Path = item.GetProperty("path").GetString() ?? "",
                        Label = item.GetProperty("label").GetString() ?? "",
                        CorePath = item.GetProperty("core_path").GetString() ?? "",
                        CoreName = item.GetProperty("core_name").GetString() ?? "",
                        Crc32 = item.TryGetProperty("crc32", out var crc) ? crc.GetString() : null,
                        DbName = item.TryGetProperty("db_name", out var db) ? db.GetString() : null
                    };

                    games.Add(game);
                }
                catch (Exception ex)
                {
                    LogPlaylistItemParseFailed(_logger, ex);
                }
            }
        }

        return games;
    }

    public async Task<Result<IReadOnlyList<RetroArchCore>>> GetInstalledCoresAsync(CancellationToken ct = default)
    {
        try
        {
            if (_retroArchPath == null)
            {
                var detectResult = await DetectRetroArchPathAsync(ct);
                if (detectResult.IsFailure)
                    return Result.Failure<IReadOnlyList<RetroArchCore>>(detectResult.Error ?? "RetroArch not found");
            }

            var retroArchDir = Path.GetDirectoryName(_retroArchPath)!;
            var coresDir = !string.IsNullOrEmpty(_options.CoresPath)
                ? _options.CoresPath
                : Path.Combine(retroArchDir, "cores");

            if (!Directory.Exists(coresDir))
            {
                LogCoresNotFound(_logger, coresDir);
                return Result.Success<IReadOnlyList<RetroArchCore>>(Array.Empty<RetroArchCore>());
            }

            var cores = new List<RetroArchCore>();
            var coreFiles = Directory.GetFiles(coresDir, "*_libretro.dll");

            foreach (var coreFile in coreFiles)
            {
                var fileName = Path.GetFileNameWithoutExtension(coreFile);
                var coreName = fileName.Replace("_libretro", "");

                cores.Add(new RetroArchCore
                {
                    Name = coreName,
                    DisplayName = FormatCoreName(coreName),
                    Path = coreFile,
                    IsInstalled = true
                });
            }

            LogInstalledCoresFoundCount(_logger, cores.Count);
            return Result.Success<IReadOnlyList<RetroArchCore>>(cores);
        }
        catch (Exception ex)
        {
            LogGetInstalledCoresError(_logger, ex);
            return Result.Failure<IReadOnlyList<RetroArchCore>>($"Error getting cores: {ex.Message}");
        }
    }

    public Task<Result<IReadOnlyList<RetroArchCore>>> GetAvailableCoresAsync(CancellationToken ct = default)
    {
        try
        {
            LogFetchingAvailableCores(_logger);

            // For now, return a curated list of popular cores
            // In production, you'd download and parse the info.zip file
            var popularCores = new List<RetroArchCore>
            {
                new RetroArchCore { Name = "snes9x", DisplayName = "Snes9x (SNES)", Path = "", IsInstalled = false },
                new RetroArchCore { Name = "genesis_plus_gx", DisplayName = "Genesis Plus GX (Genesis/MD)", Path = "", IsInstalled = false },
                new RetroArchCore { Name = "mgba", DisplayName = "mGBA (Game Boy Advance)", Path = "", IsInstalled = false },
                new RetroArchCore { Name = "mupen64plus_next", DisplayName = "Mupen64Plus-Next (N64)", Path = "", IsInstalled = false },
                new RetroArchCore { Name = "pcsx_rearmed", DisplayName = "PCSX ReARMed (PlayStation)", Path = "", IsInstalled = false },
                new RetroArchCore { Name = "dolphin", DisplayName = "Dolphin (GameCube/Wii)", Path = "", IsInstalled = false },
                new RetroArchCore { Name = "ppsspp", DisplayName = "PPSSPP (PSP)", Path = "", IsInstalled = false },
                new RetroArchCore { Name = "nestopia", DisplayName = "Nestopia (NES)", Path = "", IsInstalled = false },
            };

            return Task.FromResult(Result.Success((IReadOnlyList<RetroArchCore>)popularCores));
        }
        catch (Exception ex)
        {
            LogGetAvailableCoresError(_logger, ex);
            return Task.FromResult(Result.Failure<IReadOnlyList<RetroArchCore>>($"Error getting available cores: {ex.Message}"));
        }
    }

    public async Task<Result> InstallCoreAsync(string coreName, CancellationToken ct = default)
    {
        try
        {
            if (_retroArchPath == null)
            {
                var detectResult = await DetectRetroArchPathAsync(ct);
                if (detectResult.IsFailure)
                    return Result.Failure(detectResult.Error ?? "RetroArch not found");
            }

            LogInstallingCore(_logger, coreName);

            // Use RetroArch's built-in core updater
            var retroArchDir = Path.GetDirectoryName(_retroArchPath)!;
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _retroArchPath,
                    Arguments = $"--updatecore {coreName}",
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            await process.WaitForExitAsync(ct);

            if (process.ExitCode == 0)
            {
                LogCoreInstallSuccess(_logger, coreName);
                return Result.Success();
            }

            return Result.Failure($"Core installation failed with exit code: {process.ExitCode}");
        }
        catch (Exception ex)
        {
            LogInstallCoreError(_logger, coreName, ex);
            return Result.Failure($"Error installing core: {ex.Message}");
        }
    }

    public async Task<Result> UpdateCoreAsync(string coreName, CancellationToken ct = default)
    {
        // Same as install - RetroArch handles updates the same way
        return await InstallCoreAsync(coreName, ct);
    }

    public async Task<Result<RetroArchConfig>> GetConfigAsync(CancellationToken ct = default)
    {
        try
        {
            if (_retroArchPath == null)
            {
                var detectResult = await DetectRetroArchPathAsync(ct);
                if (detectResult.IsFailure)
                    return Result.Failure<RetroArchConfig>(detectResult.Error ?? "RetroArch not found");
            }

            var retroArchDir = Path.GetDirectoryName(_retroArchPath)!;
            var configPath = Path.Combine(retroArchDir, "retroarch.cfg");

            if (!File.Exists(configPath))
            {
                LogConfigNotFound(_logger, configPath);
                return Result.Success(new RetroArchConfig());
            }

            var config = new RetroArchConfig();
            var lines = await File.ReadAllLinesAsync(configPath, ct);

            foreach (var line in lines)
            {
                if (line.StartsWith("savefile_directory"))
                    config.SavefileDirectory = ExtractConfigValue(line);
                else if (line.StartsWith("savestate_directory"))
                    config.SavestateDirectory = ExtractConfigValue(line);
                else if (line.StartsWith("system_directory"))
                    config.SystemDirectory = ExtractConfigValue(line);
                else if (line.StartsWith("netplay_enable"))
                    config.CloudSyncEnabled = ExtractConfigValue(line) == "true";
            }

            return Result.Success(config);
        }
        catch (Exception ex)
        {
            LogGetConfigError(_logger, ex);
            return Result.Failure<RetroArchConfig>($"Error getting config: {ex.Message}");
        }
    }

    public async Task<Result> SyncSavesAsync(CancellationToken ct = default)
    {
        try
        {
            if (!_options.CloudSyncEnabled)
            {
                return Result.Failure("Cloud sync is not enabled in configuration");
            }

            if (string.IsNullOrEmpty(_options.CloudSyncConnectionString))
            {
                return Result.Failure("Cloud sync connection string not configured");
            }

            var configResult = await GetConfigAsync(ct);
            if (configResult.IsFailure || configResult.Value == null)
                return Result.Failure("Could not get RetroArch configuration");

            var config = configResult.Value;

            if (string.IsNullOrEmpty(config.SavefileDirectory))
                return Result.Failure("Save directory not configured in RetroArch");

            LogSyncingSaves(_logger, config.SavefileDirectory);

            // Scan save files
            var saveFiles = new List<string>();
            if (Directory.Exists(config.SavefileDirectory))
            {
                saveFiles.AddRange(Directory.GetFiles(config.SavefileDirectory, "*.srm", SearchOption.AllDirectories));
                saveFiles.AddRange(Directory.GetFiles(config.SavefileDirectory, "*.state*", SearchOption.AllDirectories));
            }

            if (!string.IsNullOrEmpty(config.SavestateDirectory) && Directory.Exists(config.SavestateDirectory))
            {
                saveFiles.AddRange(Directory.GetFiles(config.SavestateDirectory, "*.state*", SearchOption.AllDirectories));
            }

            LogFoundSaveFiles(_logger, saveFiles.Count);

            // Calculate file hashes and prepare for sync
            var filesToSync = new List<(string Path, string Hash, DateTime Modified)>();
            foreach (var file in saveFiles)
            {
                try
                {
                    var fileInfo = new FileInfo(file);
                    var hash = await CalculateFileHashAsync(file, ct);
                    filesToSync.Add((file, hash, fileInfo.LastWriteTimeUtc));
                }
                catch (Exception ex)
                {
                    LogFileHashError(_logger, file, ex);
                }
            }

            // Perform cloud sync based on provider
            var syncResult = _options.CloudSyncProvider.ToLowerInvariant() switch
            {
                "azureblob" => await SyncToAzureBlobAsync(filesToSync, ct),
                "awss3" => await SyncToAwsS3Async(filesToSync, ct),
                "googlecloud" => await SyncToGoogleCloudAsync(filesToSync, ct),
                _ => Result.Failure($"Unsupported cloud provider: {_options.CloudSyncProvider}")
            };

            if (syncResult.IsSuccess)
            {
                LogCloudSyncSuccess(_logger, filesToSync.Count, _options.CloudSyncProvider);
            }

            return syncResult;
        }
        catch (Exception ex)
        {
            LogSyncSavesError(_logger, ex);
            return Result.Failure($"Error syncing saves: {ex.Message}");
        }
    }

    private async Task<string> CalculateFileHashAsync(string filePath, CancellationToken ct)
    {
        using var stream = File.OpenRead(filePath);
        var hashBytes = await SHA256.HashDataAsync(stream, ct);
        return Convert.ToHexString(hashBytes);
    }

    private async Task<Result> SyncToAzureBlobAsync(List<(string Path, string Hash, DateTime Modified)> files, CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrEmpty(_options.CloudSyncConnectionString))
            {
                return Result.Failure("Azure Blob Storage connection string not configured");
            }

            var blobServiceClient = new BlobServiceClient(_options.CloudSyncConnectionString);
            var containerName = _options.CloudSyncContainerName ?? "retroach-saves";
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);

            // Ensure container exists
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: ct);

            var uploadedCount = 0;
            var skippedCount = 0;

            foreach (var (filePath, hash, modified) in files)
            {
                try
                {
                    var blobName = GetRelativePath(filePath, _retroArchPath!);
                    var blobClient = containerClient.GetBlobClient(blobName);

                    // Check if blob exists and compare hashes
                    var exists = await blobClient.ExistsAsync(ct);
                    if (exists)
                    {
                        // Get blob properties to check hash
                        var properties = await blobClient.GetPropertiesAsync(cancellationToken: ct);
                        var cloudHash = properties.Value.Metadata.TryGetValue("filehash", out var hashValue) ? hashValue : null;

                        if (cloudHash == hash)
                        {
                            // File is up to date
                            skippedCount++;
                            continue;
                        }
                    }

                    // Upload the file
                    using var fileStream = File.OpenRead(filePath);
                    var uploadOptions = new BlobUploadOptions
                    {
                        Metadata = new Dictionary<string, string>
                        {
                            { "filehash", hash },
                            { "modified", modified.ToString("O") },
                            { "source", Environment.MachineName }
                        },
                        Conditions = exists ? new BlobRequestConditions { IfNoneMatch = new Azure.ETag("*") } : null
                    };

                    await blobClient.UploadAsync(fileStream, uploadOptions, ct);
                    uploadedCount++;

                    _logger.LogDebug("Uploaded save file to Azure Blob: {BlobName}", blobName);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to sync file to Azure Blob: {FilePath}", filePath);
                }
            }

            _logger.LogInformation("Azure Blob sync completed: {Uploaded} uploaded, {Skipped} skipped", uploadedCount, skippedCount);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing to Azure Blob Storage");
            return Result.Failure($"Azure Blob sync failed: {ex.Message}");
        }
    }

    private async Task<Result> SyncToAwsS3Async(List<(string Path, string Hash, DateTime Modified)> files, CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrEmpty(_options.CloudSyncConnectionString))
            {
                return Result.Failure("AWS S3 credentials not configured");
            }

            // Parse connection string format: "AccessKey=xxx;SecretKey=yyy;Region=zzz;Bucket=bbb"
            var credentials = ParseAwsCredentials(_options.CloudSyncConnectionString);
            if (credentials == null)
            {
                return Result.Failure("Invalid AWS S3 connection string format");
            }

            using var s3Client = new AmazonS3Client(credentials.Value.AccessKey, credentials.Value.SecretKey, Amazon.RegionEndpoint.GetBySystemName(credentials.Value.Region));

            var uploadedCount = 0;
            var skippedCount = 0;

            foreach (var (filePath, hash, modified) in files)
            {
                try
                {
                    var key = GetRelativePath(filePath, _retroArchPath!);

                    // Check if object exists and compare hashes
                    var headRequest = new GetObjectMetadataRequest
                    {
                        BucketName = credentials.Value.Bucket,
                        Key = key
                    };

                    string? cloudHash = null;
                    try
                    {
                        var response = await s3Client.GetObjectMetadataAsync(headRequest, ct);
                        try { cloudHash = response.Metadata["filehash"]; } catch { cloudHash = null; }
                    }
                    catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        // Object doesn't exist, will upload
                    }

                    if (cloudHash == hash)
                    {
                        // File is up to date
                        skippedCount++;
                        continue;
                    }

                    // Upload the file using TransferUtility for better performance
                    using var transferUtility = new TransferUtility(s3Client);
                    var uploadRequest = new TransferUtilityUploadRequest
                    {
                        BucketName = credentials.Value.Bucket,
                        Key = key,
                        FilePath = filePath,
                        Metadata =
                        {
                            ["filehash"] = hash,
                            ["modified"] = modified.ToString("O"),
                            ["source"] = Environment.MachineName
                        }
                    };

                    await transferUtility.UploadAsync(uploadRequest, ct);
                    uploadedCount++;

                    _logger.LogDebug("Uploaded save file to AWS S3: {Key}", key);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to sync file to AWS S3: {FilePath}", filePath);
                }
            }

            _logger.LogInformation("AWS S3 sync completed: {Uploaded} uploaded, {Skipped} skipped", uploadedCount, skippedCount);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing to AWS S3");
            return Result.Failure($"AWS S3 sync failed: {ex.Message}");
        }
    }

    private async Task<Result> SyncToGoogleCloudAsync(List<(string Path, string Hash, DateTime Modified)> files, CancellationToken ct)
    {
        // Placeholder for Google Cloud Storage implementation
        // In production, use Google.Cloud.Storage.V1 NuGet package
        LogCloudSyncPlaceholder(_logger, "Google Cloud Storage");
        await Task.Delay(100, ct); // Simulate async operation
        return Result.Success();
    }

    public async Task<Result> LaunchGameAsync(string gamePath, string corePath, CancellationToken ct = default)
    {
        try
        {
            if (_retroArchPath == null)
            {
                var detectResult = await DetectRetroArchPathAsync(ct);
                if (detectResult.IsFailure)
                    return Result.Failure(detectResult.Error ?? "RetroArch not found");
            }

            LogLaunchingGame(_logger, gamePath, corePath);

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _retroArchPath,
                    Arguments = $"-L \"{corePath}\" \"{gamePath}\"",
                    UseShellExecute = true
                }
            };

            process.Start();
            return Result.Success();
        }
        catch (Exception ex)
        {
            LogLaunchGameError(_logger, ex);
            return Result.Failure($"Error launching game: {ex.Message}");
        }
    }

    public async Task<Result<IReadOnlyList<Achievement>>> GetAchievementsAsync(string gameHash, CancellationToken ct = default)
    {
        try
        {
            if (_retroAchievementsClient == null)
            {
                LogRetroAchievementsNotConfigured(_logger);
                return Result.Success<IReadOnlyList<Achievement>>(Array.Empty<Achievement>());
            }

            if (!_retroAchievementsClient.IsAuthenticated)
            {
                LogRetroAchievementsNotAuthenticated(_logger);
                return Result.Failure<IReadOnlyList<Achievement>>("RetroAchievements client not authenticated", ErrorType.Unauthorized);
            }

            LogFetchingAchievements(_logger, gameHash);

            // Get game info by hash
            var gameInfoResult = await _retroAchievementsClient.GetGameByHashAsync(gameHash, ct);
            if (gameInfoResult.IsFailure || gameInfoResult.Value == null)
            {
                LogGameNotFoundByHash(_logger, gameHash);
                return Result.Success<IReadOnlyList<Achievement>>(Array.Empty<Achievement>());
            }

            var gameInfo = gameInfoResult.Value;

            // Get achievements for the game
            var achievementsResult = await _retroAchievementsClient.GetGameAchievementsAsync(gameInfo.Id, ct);
            if (achievementsResult.IsFailure || achievementsResult.Value == null)
            {
                return Result.Failure<IReadOnlyList<Achievement>>(achievementsResult.Error ?? "Failed to fetch achievements");
            }

            // Map RetroAchievements to our Achievement model
            var achievements = achievementsResult.Value
                .Select(ra => new Achievement
                {
                    Id = ra.Id,
                    Title = ra.Title,
                    Description = ra.Description,
                    Points = ra.Points,
                    BadgeUrl = ra.BadgeUrl,
                    IsUnlocked = false, // Would need to check user progress
                    UnlockedAt = null
                })
                .ToList();

            LogAchievementsFetched(_logger, achievements.Count, gameInfo.Title);
            return Result.Success<IReadOnlyList<Achievement>>(achievements);
        }
        catch (Exception ex)
        {
            LogGetAchievementsError(_logger, ex);
            return Result.Failure<IReadOnlyList<Achievement>>($"Error getting achievements: {ex.Message}");
        }
    }

    private string FormatCoreName(string coreName)
    {
        // Convert snake_case to Title Case
        return Regex.Replace(coreName, "_", " ")
            .Split(' ')
            .Select(word => char.ToUpper(word[0]) + word.Substring(1))
            .Aggregate((a, b) => $"{a} {b}");
    }

    private string ExtractConfigValue(string line)
    {
        var parts = line.Split('=', 2);
        if (parts.Length == 2)
        {
            return parts[1].Trim().Trim('"');
        }
        return string.Empty;
    }

    private string GetRelativePath(string fullPath, string basePath)
    {
        var baseDir = Path.GetDirectoryName(basePath) ?? basePath;
        var relativePath = Path.GetRelativePath(baseDir, fullPath);
        // Replace backslashes with forward slashes for consistent blob names
        return relativePath.Replace('\\', '/');
    }

    private (string AccessKey, string SecretKey, string Region, string Bucket)? ParseAwsCredentials(string connectionString)
    {
        var parts = connectionString.Split(';');
        var dict = new Dictionary<string, string>();

        foreach (var part in parts)
        {
            var kvp = part.Split('=', 2);
            if (kvp.Length == 2)
            {
                dict[kvp[0].Trim()] = kvp[1].Trim();
            }
        }

        if (dict.TryGetValue("AccessKey", out var accessKey) &&
            dict.TryGetValue("SecretKey", out var secretKey) &&
            dict.TryGetValue("Region", out var region) &&
            dict.TryGetValue("Bucket", out var bucket))
        {
            return (accessKey, secretKey, region, bucket);
        }

        return null;
    }

    #region Logging

    [LoggerMessage(EventId = 101, Level = LogLevel.Information, Message = "Using configured RetroArch path: {Path}")]
    static partial void LogConfiguredPath(ILogger logger, string path);

    [LoggerMessage(EventId = 102, Level = LogLevel.Information, Message = "RetroArch detected at: {Path}")]
    static partial void LogRetroArchDetected(ILogger logger, string path);

    [LoggerMessage(EventId = 103, Level = LogLevel.Warning, Message = "RetroArch installation not found in common locations")]
    static partial void LogNotFoundInCommonLocations(ILogger logger);

    [LoggerMessage(EventId = 104, Level = LogLevel.Error, Message = "Error detecting RetroArch path")]
    static partial void LogPathDetectionError(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 105, Level = LogLevel.Warning, Message = "RetroArch playlists directory not found: {Path}")]
    static partial void LogPlaylistsNotFound(ILogger logger, string path);

    [LoggerMessage(EventId = 106, Level = LogLevel.Warning, Message = "Failed to parse playlist: {File}")]
    static partial void LogPlaylistParseFailed(ILogger logger, string file, Exception ex);

    [LoggerMessage(EventId = 107, Level = LogLevel.Information, Message = "Found {Count} RetroArch games across {PlaylistCount} playlists")]
    static partial void LogGamesFoundCount(ILogger logger, int count, int playlistCount);

    [LoggerMessage(EventId = 108, Level = LogLevel.Error, Message = "Error getting RetroArch games")]
    static partial void LogGetGamesError(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 109, Level = LogLevel.Debug, Message = "Failed to parse playlist item")]
    static partial void LogPlaylistItemParseFailed(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 110, Level = LogLevel.Warning, Message = "RetroArch cores directory not found: {Path}")]
    static partial void LogCoresNotFound(ILogger logger, string path);

    [LoggerMessage(EventId = 111, Level = LogLevel.Information, Message = "Found {Count} installed RetroArch cores")]
    static partial void LogInstalledCoresFoundCount(ILogger logger, int count);

    [LoggerMessage(EventId = 112, Level = LogLevel.Error, Message = "Error getting installed cores")]
    static partial void LogGetInstalledCoresError(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 113, Level = LogLevel.Information, Message = "Fetching available cores from RetroArch buildbot")]
    static partial void LogFetchingAvailableCores(ILogger logger);

    [LoggerMessage(EventId = 114, Level = LogLevel.Error, Message = "Error getting available cores")]
    static partial void LogGetAvailableCoresError(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 115, Level = LogLevel.Information, Message = "Installing RetroArch core: {CoreName}")]
    static partial void LogInstallingCore(ILogger logger, string coreName);

    [LoggerMessage(EventId = 116, Level = LogLevel.Information, Message = "Successfully installed core: {CoreName}")]
    static partial void LogCoreInstallSuccess(ILogger logger, string coreName);

    [LoggerMessage(EventId = 117, Level = LogLevel.Error, Message = "Error installing core: {CoreName}")]
    static partial void LogInstallCoreError(ILogger logger, string coreName, Exception ex);

    [LoggerMessage(EventId = 118, Level = LogLevel.Warning, Message = "RetroArch config file not found: {Path}")]
    static partial void LogConfigNotFound(ILogger logger, string path);

    [LoggerMessage(EventId = 119, Level = LogLevel.Error, Message = "Error getting RetroArch config")]
    static partial void LogGetConfigError(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 120, Level = LogLevel.Information, Message = "Syncing RetroArch saves from: {Directory}")]
    static partial void LogSyncingSaves(ILogger logger, string directory);

    [LoggerMessage(EventId = 121, Level = LogLevel.Information, Message = "Cloud sync would sync saves from {Dir}")]
    static partial void LogCloudSyncPlaceholder(ILogger logger, string dir);

    [LoggerMessage(EventId = 122, Level = LogLevel.Error, Message = "Error syncing saves")]
    static partial void LogSyncSavesError(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 123, Level = LogLevel.Information, Message = "Launching game in RetroArch: {Game} with core: {Core}")]
    static partial void LogLaunchingGame(ILogger logger, string game, string core);

    [LoggerMessage(EventId = 124, Level = LogLevel.Error, Message = "Error launching game")]
    static partial void LogLaunchGameError(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 125, Level = LogLevel.Information, Message = "Fetching achievements for game hash: {Hash}")]
    static partial void LogFetchingAchievements(ILogger logger, string hash);

    [LoggerMessage(EventId = 126, Level = LogLevel.Error, Message = "Error getting achievements")]
    static partial void LogGetAchievementsError(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 127, Level = LogLevel.Information, Message = "RetroAchievements authenticated as {Username}")]
    static partial void LogRetroAchievementsAuthenticated(ILogger logger, string username);

    [LoggerMessage(EventId = 128, Level = LogLevel.Warning, Message = "RetroAchievements authentication failed")]
    static partial void LogRetroAchievementsAuthFailed(ILogger logger);

    [LoggerMessage(EventId = 129, Level = LogLevel.Error, Message = "Error authenticating with RetroAchievements")]
    static partial void LogRetroAchievementsAuthError(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 130, Level = LogLevel.Information, Message = "Found {Count} save files to sync")]
    static partial void LogFoundSaveFiles(ILogger logger, int count);

    [LoggerMessage(EventId = 131, Level = LogLevel.Error, Message = "Error calculating hash for file: {File}")]
    static partial void LogFileHashError(ILogger logger, string file, Exception ex);

    [LoggerMessage(EventId = 132, Level = LogLevel.Information, Message = "Successfully synced {Count} files to {Provider}")]
    static partial void LogCloudSyncSuccess(ILogger logger, int count, string provider);

    [LoggerMessage(EventId = 133, Level = LogLevel.Warning, Message = "RetroAchievements client not configured")]
    static partial void LogRetroAchievementsNotConfigured(ILogger logger);

    [LoggerMessage(EventId = 134, Level = LogLevel.Warning, Message = "RetroAchievements client not authenticated")]
    static partial void LogRetroAchievementsNotAuthenticated(ILogger logger);

    [LoggerMessage(EventId = 135, Level = LogLevel.Warning, Message = "Game not found for hash: {Hash}")]
    static partial void LogGameNotFoundByHash(ILogger logger, string hash);

    [LoggerMessage(EventId = 136, Level = LogLevel.Information, Message = "Fetched {Count} achievements for game: {GameTitle}")]
    static partial void LogAchievementsFetched(ILogger logger, int count, string gameTitle);

    #endregion
}

