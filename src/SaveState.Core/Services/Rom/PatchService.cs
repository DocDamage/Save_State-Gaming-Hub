using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;

namespace SaveState.Core.Services.Rom
{
    public enum PatchFormat
    {
        IPS,        // International Patching System
        UPS,        // Universal Patching System
        BPS,        // Beat Patching System
        PPF,        // PlayStation Patch Format
        XDELTA,     // xdelta format
        RUP         // Rom Update Patch
    }

    public class PatchInfo
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public PatchFormat Format { get; set; }
        public string TargetRomHash { get; set; } = string.Empty; // CRC32 or MD5 of target ROM
        public string TargetRomName { get; set; } = string.Empty;
        public string Platform { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Version { get; set; } = "1.0";
        public DateTime AddedAt { get; set; }
        public bool AutoApply { get; set; }
        public List<string> Tags { get; set; } = new();
    }

    public class PatchResult
    {
        public bool Success { get; set; }
        public string? OutputPath { get; set; }
        public string? ErrorMessage { get; set; }
        public long OriginalSize { get; set; }
        public long PatchedSize { get; set; }
    }

    public class PatchService
    {
        private static PatchService? _instance;
        private readonly string _patchesPath;
        private readonly List<PatchInfo> _patches = new();

        public static PatchService Instance => _instance ??= new PatchService();

        private PatchService()
        {
            _patchesPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "SaveState2", "data", "patches");
            if (!Directory.Exists(_patchesPath)) Directory.CreateDirectory(_patchesPath);
            LoadPatches();
        }

        // Apply a patch to a ROM
        public async Task<PatchResult> ApplyPatchAsync(string romPath, string patchPath)
        {
            if (!File.Exists(romPath))
                return new PatchResult { Success = false, ErrorMessage = "ROM file not found" };
            if (!File.Exists(patchPath))
                return new PatchResult { Success = false, ErrorMessage = "Patch file not found" };

            var format = DetectPatchFormat(patchPath);
            var outputPath = GetOutputPath(romPath);

            return format switch
            {
                PatchFormat.IPS => await ApplyIpsPatchAsync(romPath, patchPath, outputPath),
                PatchFormat.UPS => await ApplyUpsPatchAsync(romPath, patchPath, outputPath),
                PatchFormat.BPS => await ApplyBpsPatchAsync(romPath, patchPath, outputPath),
                _ => new PatchResult { Success = false, ErrorMessage = $"Unsupported patch format: {format}" }
            };
        }

        // Apply IPS patch
        private async Task<PatchResult> ApplyIpsPatchAsync(string romPath, string patchPath, string outputPath)
        {
            try
            {
                var romData = await File.ReadAllBytesAsync(romPath);
                var patchData = await File.ReadAllBytesAsync(patchPath);

                // Verify IPS header "PATCH"
                if (patchData.Length < 5 || 
                    patchData[0] != 'P' || patchData[1] != 'A' || patchData[2] != 'T' || 
                    patchData[3] != 'C' || patchData[4] != 'H')
                {
                    return new PatchResult { Success = false, ErrorMessage = "Invalid IPS patch header" };
                }

                var output = new List<byte>(romData);
                int pos = 5;

                while (pos + 3 <= patchData.Length)
                {
                    // Check for EOF marker
                    if (patchData[pos] == 'E' && patchData[pos + 1] == 'O' && patchData[pos + 2] == 'F')
                        break;

                    // Read offset (3 bytes, big-endian)
                    int offset = (patchData[pos] << 16) | (patchData[pos + 1] << 8) | patchData[pos + 2];
                    pos += 3;

                    // Read size (2 bytes, big-endian)
                    int size = (patchData[pos] << 8) | patchData[pos + 1];
                    pos += 2;

                    if (size == 0)
                    {
                        // RLE encoding
                        int rleSize = (patchData[pos] << 8) | patchData[pos + 1];
                        pos += 2;
                        byte rleByte = patchData[pos++];

                        // Expand output if needed
                        while (output.Count < offset + rleSize)
                            output.Add(0);

                        for (int i = 0; i < rleSize; i++)
                            output[offset + i] = rleByte;
                    }
                    else
                    {
                        // Direct copy
                        while (output.Count < offset + size)
                            output.Add(0);

                        for (int i = 0; i < size && pos < patchData.Length; i++)
                            output[offset + i] = patchData[pos++];
                    }
                }

                await File.WriteAllBytesAsync(outputPath, output.ToArray());

                return new PatchResult
                {
                    Success = true,
                    OutputPath = outputPath,
                    OriginalSize = romData.Length,
                    PatchedSize = output.Count
                };
            }
            catch (Exception ex)
            {
                return new PatchResult { Success = false, ErrorMessage = ex.Message };
            }
        }

        // Apply UPS patch (placeholder - full implementation is complex)
        private async Task<PatchResult> ApplyUpsPatchAsync(string romPath, string patchPath, string outputPath)
        {
            // UPS uses XOR-based patching
            // This is a placeholder - full implementation requires:
            // 1. Read variable-length integers
            // 2. XOR difference blocks
            // 3. Verify CRC32 checksums
            await Task.Yield();
            return new PatchResult { Success = false, ErrorMessage = "UPS patching not yet fully implemented" };
        }

        // Apply BPS patch (placeholder - full implementation is complex)
        private async Task<PatchResult> ApplyBpsPatchAsync(string romPath, string patchPath, string outputPath)
        {
            await Task.Yield();
            return new PatchResult { Success = false, ErrorMessage = "BPS patching not yet fully implemented" };
        }

        // Detect patch format from file
        private PatchFormat DetectPatchFormat(string patchPath)
        {
            var ext = Path.GetExtension(patchPath).ToLower();
            
            if (ext == ".ips") return PatchFormat.IPS;
            if (ext == ".ups") return PatchFormat.UPS;
            if (ext == ".bps") return PatchFormat.BPS;
            if (ext == ".ppf") return PatchFormat.PPF;
            if (ext == ".xdelta" || ext == ".vcdiff") return PatchFormat.XDELTA;

            // Check magic bytes
            try
            {
                using var fs = File.OpenRead(patchPath);
                var header = new byte[5];
                fs.Read(header, 0, 5);

                if (header[0] == 'P' && header[1] == 'A' && header[2] == 'T' && header[3] == 'C' && header[4] == 'H')
                    return PatchFormat.IPS;
                if (header[0] == 'U' && header[1] == 'P' && header[2] == 'S' && header[3] == '1')
                    return PatchFormat.UPS;
                if (header[0] == 'B' && header[1] == 'P' && header[2] == 'S' && header[3] == '1')
                    return PatchFormat.BPS;
            }
            catch { }

            return PatchFormat.IPS; // Default
        }

        private string GetOutputPath(string romPath)
        {
            var dir = Path.GetDirectoryName(romPath) ?? "";
            var name = Path.GetFileNameWithoutExtension(romPath);
            var ext = Path.GetExtension(romPath);
            return Path.Combine(dir, $"{name}_patched{ext}");
        }

        // Register a patch for a specific ROM
        public PatchInfo RegisterPatch(string name, string patchPath, string targetRomHash, 
            string platform, string? description = null, bool autoApply = false)
        {
            var patch = new PatchInfo
            {
                Name = name,
                FilePath = patchPath,
                Format = DetectPatchFormat(patchPath),
                TargetRomHash = targetRomHash,
                Platform = platform,
                Description = description ?? "",
                AutoApply = autoApply,
                AddedAt = DateTime.UtcNow
            };

            _patches.Add(patch);
            SavePatches();
            return patch;
        }

        // Get patches for a ROM by hash
        public List<PatchInfo> GetPatchesForRom(string romHash)
        {
            return _patches.Where(p => p.TargetRomHash == romHash).ToList();
        }

        // Get auto-apply patches for a ROM
        public List<PatchInfo> GetAutoApplyPatches(string romHash)
        {
            return _patches.Where(p => p.TargetRomHash == romHash && p.AutoApply).ToList();
        }

        // Calculate ROM hash
        public string CalculateRomHash(string romPath, string algorithm = "MD5")
        {
            using var fs = File.OpenRead(romPath);
            using var hash = algorithm.ToUpper() switch
            {
                "MD5" => (HashAlgorithm)MD5.Create(),
                "SHA1" => SHA1.Create(),
                "SHA256" => SHA256.Create(),
                _ => MD5.Create()
            };

            var hashBytes = hash.ComputeHash(fs);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }

        // Calculate CRC32 (commonly used for ROM verification)
        public uint CalculateCrc32(string romPath)
        {
            var data = File.ReadAllBytes(romPath);
            uint crc = 0xFFFFFFFF;
            
            foreach (byte b in data)
            {
                crc ^= b;
                for (int i = 0; i < 8; i++)
                {
                    crc = (crc >> 1) ^ ((crc & 1) != 0 ? 0xEDB88320 : 0);
                }
            }

            return ~crc;
        }

        public List<PatchInfo> GetAllPatches() => _patches.ToList();

        public bool DeletePatch(string patchId)
        {
            var patch = _patches.FirstOrDefault(p => p.Id == patchId);
            if (patch == null) return false;

            _patches.Remove(patch);
            SavePatches();
            return true;
        }

        private void LoadPatches()
        {
            var dbPath = Path.Combine(_patchesPath, "patches.json");
            if (File.Exists(dbPath))
            {
                try
                {
                    var json = File.ReadAllText(dbPath);
                    var patches = JsonSerializer.Deserialize<List<PatchInfo>>(json);
                    if (patches != null) _patches.AddRange(patches);
                }
                catch { }
            }
        }

        private void SavePatches()
        {
            var dbPath = Path.Combine(_patchesPath, "patches.json");
            var json = JsonSerializer.Serialize(_patches, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(dbPath, json);
        }
    }
}
