using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;

namespace SaveState.Core.Helpers
{
    public class GpuInfo
    {
        public string Name { get; set; } = string.Empty;
        public long VramBytes { get; set; }
        public bool IsNvidia { get; set; }
        public bool IsAmd { get; set; }
        public bool IsIntel { get; set; }
        public bool SupportsCuda { get; set; }
        public string DriverVersion { get; set; } = string.Empty;
    }

    public class SystemInfo
    {
        public long TotalRamBytes { get; set; }
        public long AvailableRamBytes { get; set; }
        public long TotalDiskBytes { get; set; }
        public long AvailableDiskBytes { get; set; }
        public GpuInfo? Gpu { get; set; }
        public int CpuCores { get; set; }
        public string OsVersion { get; set; } = string.Empty;
    }

    public class ModelRecommendation
    {
        public string ModelName { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public long RequiredRam { get; set; }
        public long RequiredVram { get; set; }
        public bool CanRunOnCpu { get; set; }
    }

    public static class SystemCapabilities
    {
        private static SystemInfo? _cachedInfo;

        public static SystemInfo GetSystemInfo(bool refresh = false)
        {
            if (_cachedInfo != null && !refresh) return _cachedInfo;

            _cachedInfo = new SystemInfo
            {
                TotalRamBytes = GetTotalRam(),
                AvailableRamBytes = GetAvailableRam(),
                TotalDiskBytes = GetTotalDisk(),
                AvailableDiskBytes = GetAvailableDisk(),
                CpuCores = Environment.ProcessorCount,
                OsVersion = Environment.OSVersion.ToString(),
                Gpu = GetGpuInfo()
            };

            return _cachedInfo;
        }

        public static long GetTotalRam()
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return GetWindowsTotalRam();
                }
                // Linux/Mac fallback
                return GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
            }
            catch
            {
                return 8L * 1024 * 1024 * 1024; // Default 8GB
            }
        }

        private static long GetWindowsTotalRam()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem");
                foreach (var obj in searcher.Get())
                {
                    return Convert.ToInt64(obj["TotalPhysicalMemory"]);
                }
            }
            catch { }
            return GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
        }

        public static long GetAvailableRam()
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    using var searcher = new ManagementObjectSearcher("SELECT FreePhysicalMemory FROM Win32_OperatingSystem");
                    foreach (var obj in searcher.Get())
                    {
                        return Convert.ToInt64(obj["FreePhysicalMemory"]) * 1024; // KB to bytes
                    }
                }
            }
            catch { }
            return GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / 2;
        }

        public static long GetTotalDisk(string? path = null)
        {
            path ??= Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            try
            {
                var drive = new DriveInfo(Path.GetPathRoot(path) ?? "C:\\");
                return drive.TotalSize;
            }
            catch
            {
                return 100L * 1024 * 1024 * 1024; // Default 100GB
            }
        }

        public static long GetAvailableDisk(string? path = null)
        {
            path ??= Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            try
            {
                var drive = new DriveInfo(Path.GetPathRoot(path) ?? "C:\\");
                return drive.AvailableFreeSpace;
            }
            catch
            {
                return 10L * 1024 * 1024 * 1024; // Default 10GB
            }
        }

        public static GpuInfo? GetGpuInfo()
        {
            try
            {
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return null;

                using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");
                foreach (var obj in searcher.Get())
                {
                    var name = obj["Name"]?.ToString() ?? "";
                    var vram = Convert.ToInt64(obj["AdapterRAM"] ?? 0);
                    var driver = obj["DriverVersion"]?.ToString() ?? "";

                    return new GpuInfo
                    {
                        Name = name,
                        VramBytes = vram,
                        IsNvidia = name.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase),
                        IsAmd = name.Contains("AMD", StringComparison.OrdinalIgnoreCase) || name.Contains("Radeon", StringComparison.OrdinalIgnoreCase),
                        IsIntel = name.Contains("Intel", StringComparison.OrdinalIgnoreCase),
                        SupportsCuda = name.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase),
                        DriverVersion = driver
                    };
                }
            }
            catch { }
            return null;
        }

        public static bool HasNvidiaGpu()
        {
            var gpu = GetGpuInfo();
            return gpu?.IsNvidia == true;
        }

        public static long GetGpuVram()
        {
            return GetGpuInfo()?.VramBytes ?? 0;
        }

        public static string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int i = 0;
            double size = bytes;
            while (size >= 1024 && i < suffixes.Length - 1)
            {
                size /= 1024;
                i++;
            }
            return $"{size:F1} {suffixes[i]}";
        }

        public static ModelRecommendation[] GetRecommendedModels()
        {
            var info = GetSystemInfo();
            var ramGb = info.TotalRamBytes / (1024.0 * 1024 * 1024);
            var vramGb = (info.Gpu?.VramBytes ?? 0) / (1024.0 * 1024 * 1024);
            var hasGpu = info.Gpu?.SupportsCuda == true;

            var recommendations = new List<ModelRecommendation>();

            // Always recommend smallest model
            recommendations.Add(new ModelRecommendation
            {
                ModelName = "phi",
                Reason = "Smallest & fastest (2.7B params)",
                RequiredRam = 4L * 1024 * 1024 * 1024,
                RequiredVram = 2L * 1024 * 1024 * 1024,
                CanRunOnCpu = true
            });

            if (ramGb >= 8)
            {
                recommendations.Add(new ModelRecommendation
                {
                    ModelName = "llama3.2",
                    Reason = "Best quality for size (3B params)",
                    RequiredRam = 6L * 1024 * 1024 * 1024,
                    RequiredVram = 3L * 1024 * 1024 * 1024,
                    CanRunOnCpu = true
                });
            }

            if (ramGb >= 12 || vramGb >= 6)
            {
                recommendations.Add(new ModelRecommendation
                {
                    ModelName = "mistral",
                    Reason = "Fast & creative (7B params)",
                    RequiredRam = 8L * 1024 * 1024 * 1024,
                    RequiredVram = 6L * 1024 * 1024 * 1024,
                    CanRunOnCpu = ramGb >= 16
                });

                recommendations.Add(new ModelRecommendation
                {
                    ModelName = "llama2",
                    Reason = "Well-rounded (7B params)",
                    RequiredRam = 8L * 1024 * 1024 * 1024,
                    RequiredVram = 6L * 1024 * 1024 * 1024,
                    CanRunOnCpu = ramGb >= 16
                });
            }

            if (vramGb >= 8 && hasGpu)
            {
                recommendations.Add(new ModelRecommendation
                {
                    ModelName = "codellama",
                    Reason = "Code generation specialist",
                    RequiredRam = 10L * 1024 * 1024 * 1024,
                    RequiredVram = 8L * 1024 * 1024 * 1024,
                    CanRunOnCpu = false
                });
            }

            return recommendations.ToArray();
        }

        public static string GetSystemWarnings()
        {
            var info = GetSystemInfo();
            var warnings = new List<string>();
            var ramGb = info.TotalRamBytes / (1024.0 * 1024 * 1024);
            var availableGb = info.AvailableDiskBytes / (1024.0 * 1024 * 1024);

            if (ramGb < 8)
                warnings.Add($"⚠️ Low RAM ({ramGb:F1}GB). Recommend 8GB+ for smooth AI operation.");

            if (info.Gpu == null)
                warnings.Add("⚠️ No dedicated GPU detected. AI will run on CPU (slower).");
            else if (!info.Gpu.SupportsCuda)
                warnings.Add($"⚠️ {info.Gpu.Name} does not support CUDA. AI will run on CPU.");
            else if (info.Gpu.VramBytes < 4L * 1024 * 1024 * 1024)
                warnings.Add($"⚠️ Low VRAM ({FormatBytes(info.Gpu.VramBytes)}). Larger models may not fit.");

            if (availableGb < 10)
                warnings.Add($"⚠️ Low disk space ({availableGb:F1}GB free). AI models need 2-8GB each.");

            return warnings.Count > 0 ? string.Join("\n", warnings) : "✅ System looks good for AI!";
        }

        public static int GetSafeModelCount()
        {
            var info = GetSystemInfo();
            var ramGb = info.TotalRamBytes / (1024.0 * 1024 * 1024);
            
            // Reserve ~4GB for OS and app, each model needs ~4-8GB
            if (ramGb >= 64) return 6;
            if (ramGb >= 32) return 4;
            if (ramGb >= 16) return 2;
            return 1;
        }
    }
}
