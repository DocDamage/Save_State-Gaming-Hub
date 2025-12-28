using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Serilog;

namespace SaveState.Core.Services.Cloud
{
    public class BackupInfo
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public long SizeBytes { get; set; }
        public string Description { get; set; } = string.Empty;
        public List<string> IncludedItems { get; set; } = new();
        public bool IsAutoBackup { get; set; }
    }

    public class BackupService
    {
        private readonly ILogger _logger = Log.ForContext<BackupService>();
        private readonly string _backupPath;
        private readonly string _dataPath;
        private readonly List<BackupInfo> _backups = new();
        private int _maxAutoBackups = 5;

        public static BackupService Instance { get; private set; }

        public BackupService()
        {
            Instance = this;
            _dataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "SaveState2", "data");
            _backupPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "SaveState2", "backups");

            if (!Directory.Exists(_backupPath)) Directory.CreateDirectory(_backupPath);
            LoadBackupList();
        }

        public async Task<BackupInfo?> CreateBackupAsync(string description = "", bool isAuto = false)
        {
            try
            {
                var timestamp = DateTime.Now;
                var fileName = $"savestate_backup_{timestamp:yyyyMMdd_HHmmss}.zip";
                var filePath = Path.Combine(_backupPath, fileName);

                var backup = new BackupInfo
                {
                    FileName = fileName,
                    FilePath = filePath,
                    CreatedAt = timestamp,
                    Description = description,
                    IsAutoBackup = isAuto
                };

                await Task.Run(() =>
                {
                    // Create ZIP backup
                    if (File.Exists(filePath)) File.Delete(filePath);

                    ZipFile.CreateFromDirectory(_dataPath, filePath, CompressionLevel.Optimal, false);

                    backup.SizeBytes = new FileInfo(filePath).Length;
                    backup.IncludedItems = Directory.GetFiles(_dataPath, "*", SearchOption.AllDirectories)
                        .Select(f => Path.GetRelativePath(_dataPath, f))
                        .ToList();
                });

                _backups.Add(backup);
                SaveBackupList();

                // Cleanup old auto backups
                if (isAuto) CleanupOldAutoBackups();

                _logger.Information("Backup created: {FileName}", fileName);
                return backup;
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Backup creation failed");
                return null;
            }
        }

        public async Task<bool> RestoreBackupAsync(string backupId)
        {
            var backup = _backups.FirstOrDefault(b => b.Id == backupId);
            if (backup == null || !File.Exists(backup.FilePath))
            {
                _logger.Warning("Backup not found: {BackupId}", backupId);
                return false;
            }

            try
            {
                // Create a safety backup first
                await CreateBackupAsync("Pre-restore safety backup", true);

                await Task.Run(() =>
                {
                    // Extract to temp location first
                    var tempPath = Path.Combine(_backupPath, "temp_restore");
                    if (Directory.Exists(tempPath)) Directory.Delete(tempPath, true);

                    ZipFile.ExtractToDirectory(backup.FilePath, tempPath);

                    // Copy files to data directory
                    foreach (var file in Directory.GetFiles(tempPath, "*", SearchOption.AllDirectories))
                    {
                        var relativePath = Path.GetRelativePath(tempPath, file);
                        var destPath = Path.Combine(_dataPath, relativePath);
                        var destDir = Path.GetDirectoryName(destPath);

                        if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                            Directory.CreateDirectory(destDir);

                        File.Copy(file, destPath, true);
                    }

                    // Cleanup temp
                    Directory.Delete(tempPath, true);
                });

                _logger.Information("Restored from backup: {FileName}", backup.FileName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Backup restore failed");
                return false;
            }
        }

        public async Task<string?> ExportBackupAsync(string backupId, string destinationPath)
        {
            var backup = _backups.FirstOrDefault(b => b.Id == backupId);
            if (backup == null || !File.Exists(backup.FilePath)) return null;

            try
            {
                var destFile = Path.Combine(destinationPath, backup.FileName);
                await Task.Run(() => File.Copy(backup.FilePath, destFile, true));
                return destFile;
            }
            catch
            {
                return null;
            }
        }

        public async Task<BackupInfo?> ImportBackupAsync(string filePath)
        {
            if (!File.Exists(filePath) || !filePath.EndsWith(".zip")) return null;

            try
            {
                var fileName = Path.GetFileName(filePath);
                var destPath = Path.Combine(_backupPath, fileName);

                await Task.Run(() => File.Copy(filePath, destPath, true));

                var backup = new BackupInfo
                {
                    FileName = fileName,
                    FilePath = destPath,
                    CreatedAt = DateTime.Now,
                    SizeBytes = new FileInfo(destPath).Length,
                    Description = "Imported backup"
                };

                _backups.Add(backup);
                SaveBackupList();
                return backup;
            }
            catch
            {
                return null;
            }
        }

        public bool DeleteBackup(string backupId)
        {
            var backup = _backups.FirstOrDefault(b => b.Id == backupId);
            if (backup == null) return false;

            try
            {
                if (File.Exists(backup.FilePath))
                    File.Delete(backup.FilePath);

                _backups.Remove(backup);
                SaveBackupList();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public List<BackupInfo> GetAllBackups() => _backups.OrderByDescending(b => b.CreatedAt).ToList();

        public List<BackupInfo> GetAutoBackups() => _backups.Where(b => b.IsAutoBackup).OrderByDescending(b => b.CreatedAt).ToList();

        public BackupInfo? GetLatestBackup() => _backups.OrderByDescending(b => b.CreatedAt).FirstOrDefault();

        public long GetTotalBackupSize() => _backups.Sum(b => b.SizeBytes);

        public void SetMaxAutoBackups(int count)
        {
            _maxAutoBackups = Math.Max(1, Math.Min(20, count));
            CleanupOldAutoBackups();
        }

        private void CleanupOldAutoBackups()
        {
            var autoBackups = _backups.Where(b => b.IsAutoBackup).OrderByDescending(b => b.CreatedAt).ToList();

            while (autoBackups.Count > _maxAutoBackups)
            {
                var oldest = autoBackups.Last();
                DeleteBackup(oldest.Id);
                autoBackups.Remove(oldest);
            }
        }

        private void LoadBackupList()
        {
            var listPath = Path.Combine(_backupPath, "backup_list.json");
            if (File.Exists(listPath))
            {
                try
                {
                    var json = File.ReadAllText(listPath);
                    var list = JsonSerializer.Deserialize<List<BackupInfo>>(json);
                    if (list != null)
                    {
                        _backups.Clear();
                        _backups.AddRange(list.Where(b => File.Exists(b.FilePath)));
                    }
                }
                catch (Exception ex) { _logger.Warning(ex, "Failed to load backup list"); }
            }
        }

        private void SaveBackupList()
        {
            var listPath = Path.Combine(_backupPath, "backup_list.json");
            var json = JsonSerializer.Serialize(_backups, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(listPath, json);
        }

        public string GetBackupPath() => _backupPath;

        // Schedule auto backup (call from app startup)
        public async Task RunAutoBackupIfNeededAsync()
        {
            var latest = GetLatestBackup();
            if (latest == null || (DateTime.Now - latest.CreatedAt).TotalDays >= 1)
            {
                await CreateBackupAsync("Daily auto-backup", true);
            }
        }
    }
}
