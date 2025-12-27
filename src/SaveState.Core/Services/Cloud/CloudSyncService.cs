using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SaveState.Core.Services.Cloud
{
    public enum SyncStatus
    {
        Idle,
        Syncing,
        Uploading,
        Downloading,
        Conflict,
        Error,
        Complete
    }

    public class SyncItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string LocalPath { get; set; } = string.Empty;
        public string RemotePath { get; set; } = string.Empty;
        public string Hash { get; set; } = string.Empty;
        public DateTime LastModified { get; set; }
        public DateTime? LastSynced { get; set; }
        public long SizeBytes { get; set; }
        public bool IsDirectory { get; set; }
    }

    public class SyncConflict
    {
        public SyncItem LocalVersion { get; set; } = new();
        public SyncItem RemoteVersion { get; set; } = new();
        public string Resolution { get; set; } = string.Empty; // "local", "remote", "merge", "skip"
    }

    public class SyncManifest
    {
        public string DeviceId { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
        public DateTime LastSync { get; set; }
        public List<SyncItem> Items { get; set; } = new();
        public int Version { get; set; } = 1;
    }

    public class CloudSyncService
    {
        private static CloudSyncService? _instance;
        private readonly HttpClient _httpClient;
        private readonly string _localSyncPath;
        private readonly string _manifestPath;
        private SyncStatus _status = SyncStatus.Idle;
        private SyncManifest _manifest;
        private string _cloudEndpoint = "";
        private string _authToken = "";

        public event EventHandler<SyncStatus>? StatusChanged;
        public event EventHandler<SyncConflict>? ConflictDetected;
        public event EventHandler<(string file, int percent)>? ProgressChanged;

        public static CloudSyncService Instance => _instance ??= new CloudSyncService();
        public SyncStatus Status => _status;
        public bool IsConfigured => !string.IsNullOrEmpty(_cloudEndpoint);

        private CloudSyncService()
        {
            _httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
            _localSyncPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "SaveState2", "data");
            _manifestPath = Path.Combine(_localSyncPath, ".sync_manifest.json");
            
            _manifest = LoadManifest();
        }

        public void Configure(string endpoint, string authToken)
        {
            _cloudEndpoint = endpoint.TrimEnd('/');
            _authToken = authToken;
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {authToken}");
        }

        public async Task<bool> SyncAllAsync()
        {
            if (!IsConfigured)
            {
                Console.WriteLine("Cloud sync not configured");
                return false;
            }

            SetStatus(SyncStatus.Syncing);

            try
            {
                // 1. Get remote manifest
                var remoteManifest = await GetRemoteManifestAsync();

                // 2. Compare and identify changes
                var toUpload = new List<SyncItem>();
                var toDownload = new List<SyncItem>();
                var conflicts = new List<SyncConflict>();

                CompareManifests(_manifest, remoteManifest, toUpload, toDownload, conflicts);

                // 3. Handle conflicts
                foreach (var conflict in conflicts)
                {
                    ConflictDetected?.Invoke(this, conflict);
                    // Default: prefer newer
                    if (conflict.LocalVersion.LastModified > conflict.RemoteVersion.LastModified)
                    {
                        toUpload.Add(conflict.LocalVersion);
                    }
                    else
                    {
                        toDownload.Add(conflict.RemoteVersion);
                    }
                }

                // 4. Upload local changes
                SetStatus(SyncStatus.Uploading);
                foreach (var item in toUpload)
                {
                    await UploadFileAsync(item);
                    ProgressChanged?.Invoke(this, (item.LocalPath, 100));
                }

                // 5. Download remote changes
                SetStatus(SyncStatus.Downloading);
                foreach (var item in toDownload)
                {
                    await DownloadFileAsync(item);
                    ProgressChanged?.Invoke(this, (item.RemotePath, 100));
                }

                // 6. Update manifest
                _manifest.LastSync = DateTime.UtcNow;
                SaveManifest();
                await UploadManifestAsync();

                SetStatus(SyncStatus.Complete);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Sync error: {ex.Message}");
                SetStatus(SyncStatus.Error);
                return false;
            }
            finally
            {
                await Task.Delay(2000);
                SetStatus(SyncStatus.Idle);
            }
        }

        public async Task<bool> UploadFileAsync(SyncItem item)
        {
            try
            {
                if (!File.Exists(item.LocalPath)) return false;

                var content = new ByteArrayContent(await File.ReadAllBytesAsync(item.LocalPath));
                var response = await _httpClient.PutAsync(
                    $"{_cloudEndpoint}/files/{Uri.EscapeDataString(item.RemotePath)}", 
                    content);

                if (response.IsSuccessStatusCode)
                {
                    item.LastSynced = DateTime.UtcNow;
                    UpdateManifestItem(item);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Upload error: {ex.Message}");
            }
            return false;
        }

        public async Task<bool> DownloadFileAsync(SyncItem item)
        {
            try
            {
                var response = await _httpClient.GetAsync(
                    $"{_cloudEndpoint}/files/{Uri.EscapeDataString(item.RemotePath)}");

                if (response.IsSuccessStatusCode)
                {
                    var data = await response.Content.ReadAsByteArrayAsync();
                    var localPath = Path.Combine(_localSyncPath, item.RemotePath);
                    
                    var dir = Path.GetDirectoryName(localPath);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                        Directory.CreateDirectory(dir);

                    await File.WriteAllBytesAsync(localPath, data);
                    
                    item.LocalPath = localPath;
                    item.LastSynced = DateTime.UtcNow;
                    UpdateManifestItem(item);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Download error: {ex.Message}");
            }
            return false;
        }

        private async Task<SyncManifest?> GetRemoteManifestAsync()
        {
            try
            {
                var response = await _httpClient.GetStringAsync($"{_cloudEndpoint}/manifest");
                return JsonSerializer.Deserialize<SyncManifest>(response);
            }
            catch
            {
                return new SyncManifest();
            }
        }

        private async Task UploadManifestAsync()
        {
            var json = JsonSerializer.Serialize(_manifest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _httpClient.PutAsync($"{_cloudEndpoint}/manifest", content);
        }

        private void CompareManifests(SyncManifest local, SyncManifest? remote,
            List<SyncItem> toUpload, List<SyncItem> toDownload, List<SyncConflict> conflicts)
        {
            var remoteItems = remote?.Items.ToDictionary(i => i.RemotePath) ?? new();

            foreach (var localItem in local.Items)
            {
                if (remoteItems.TryGetValue(localItem.RemotePath, out var remoteItem))
                {
                    if (localItem.Hash != remoteItem.Hash)
                    {
                        // Changed on both sides - conflict
                        if (localItem.LastModified != remoteItem.LastModified)
                        {
                            conflicts.Add(new SyncConflict
                            {
                                LocalVersion = localItem,
                                RemoteVersion = remoteItem
                            });
                        }
                    }
                    remoteItems.Remove(localItem.RemotePath);
                }
                else
                {
                    // New local file
                    toUpload.Add(localItem);
                }
            }

            // Remaining remote items are new
            foreach (var item in remoteItems.Values)
            {
                toDownload.Add(item);
            }
        }

        private void UpdateManifestItem(SyncItem item)
        {
            var existing = _manifest.Items.FirstOrDefault(i => i.LocalPath == item.LocalPath);
            if (existing != null)
            {
                existing.Hash = item.Hash;
                existing.LastModified = item.LastModified;
                existing.LastSynced = item.LastSynced;
            }
            else
            {
                _manifest.Items.Add(item);
            }
            SaveManifest();
        }

        public void AddToSync(string localPath)
        {
            if (!File.Exists(localPath) && !Directory.Exists(localPath)) return;

            var isDir = Directory.Exists(localPath);
            var relativePath = Path.GetRelativePath(_localSyncPath, localPath);
            
            var item = new SyncItem
            {
                LocalPath = localPath,
                RemotePath = relativePath.Replace('\\', '/'),
                LastModified = isDir ? DateTime.Now : File.GetLastWriteTimeUtc(localPath),
                SizeBytes = isDir ? 0 : new FileInfo(localPath).Length,
                IsDirectory = isDir,
                Hash = isDir ? "" : ComputeHash(localPath)
            };

            _manifest.Items.Add(item);
            SaveManifest();
        }

        private string ComputeHash(string filePath)
        {
            try
            {
                using var md5 = System.Security.Cryptography.MD5.Create();
                using var stream = File.OpenRead(filePath);
                var hash = md5.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
            catch
            {
                return "";
            }
        }

        private SyncManifest LoadManifest()
        {
            if (File.Exists(_manifestPath))
            {
                try
                {
                    var json = File.ReadAllText(_manifestPath);
                    return JsonSerializer.Deserialize<SyncManifest>(json) ?? new SyncManifest();
                }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Operation failed: {ex.Message}"); }
            }

            return new SyncManifest
            {
                DeviceId = Guid.NewGuid().ToString(),
                DeviceName = Environment.MachineName
            };
        }

        private void SaveManifest()
        {
            var json = JsonSerializer.Serialize(_manifest, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_manifestPath, json);
        }

        private void SetStatus(SyncStatus status)
        {
            if (_status != status)
            {
                _status = status;
                StatusChanged?.Invoke(this, status);
            }
        }

        public SyncManifest GetManifest() => _manifest;
        public string GetSyncPath() => _localSyncPath;
        public DateTime? GetLastSync() => _manifest.LastSync == default ? null : _manifest.LastSync;
    }
}
