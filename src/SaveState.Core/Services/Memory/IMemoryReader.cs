using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SaveState.Core.Services.Memory
{
    public interface IMemoryReader
    {
        bool Attach(int pid);
        void Detach();
        bool IsAttached { get; }
        
        // Generic Reads
        int ReadInt(long address);
        long ReadLong(long address);
        float ReadFloat(long address);
        string ReadString(long address, int length, Encoding? encoding = null);
        byte[] ReadBytes(long address, int length);
        
        /// <summary>
        /// Resolves a pointer chain and returns the final address.
        /// Example: ReadPointerChain(baseAddress, [0x10, 0x20, 0x30]) reads:
        /// address1 = read(baseAddress + 0x10)
        /// address2 = read(address1 + 0x20)
        /// finalAddress = address2 + 0x30
        /// </summary>
        long ReadPointerChain(long baseAddress, int[] offsets);
        
        // Scan
        Task<List<long>> ScanAobAsync(string aobPattern);
        
        // Modules
        long GetModuleBaseAddress(string moduleName);
    }

    public class WindowsMemoryReader : IMemoryReader
    {
        // --- Win32 Imports ---
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer, int dwSize, out IntPtr lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll")]
        private static extern int VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress, out MEMORY_BASIC_INFORMATION lpBuffer, uint dwLength);

        [StructLayout(LayoutKind.Sequential)]
        public struct MEMORY_BASIC_INFORMATION
        {
            public IntPtr BaseAddress;
            public IntPtr AllocationBase;
            public uint AllocationProtect;
            public IntPtr RegionSize;
            public uint State;
            public uint Protect;
            public uint Type;
        }

        // Access Rights
        private const int PROCESS_QUERY_INFORMATION = 0x0400;
        private const int PROCESS_VM_READ = 0x0010;
        private const int PROCESS_VM_WRITE = 0x0020;
        private const int PROCESS_VM_OPERATION = 0x0008;

        // Memory States
        private const uint MEM_COMMIT = 0x1000;
        private const uint PAGE_READWRITE = 0x04;
        private const uint PAGE_READONLY = 0x02;
        private const uint PAGE_EXECUTE_READ = 0x20;
        private const uint PAGE_EXECUTE_READWRITE = 0x40;

        private IntPtr _handle = IntPtr.Zero;
        private int _processId = 0;

        public bool IsAttached => _handle != IntPtr.Zero;

        public bool Attach(int pid)
        {
            if (IsAttached) Detach();

            _processId = pid;
            // Request permissions to Read and Query info (for scanning)
            _handle = OpenProcess(PROCESS_VM_READ | PROCESS_QUERY_INFORMATION | PROCESS_VM_OPERATION, false, pid);
            
            return _handle != IntPtr.Zero;
        }

        public void Detach()
        {
            if (_handle != IntPtr.Zero)
            {
                CloseHandle(_handle);
                _handle = IntPtr.Zero;
                _processId = 0;
            }
        }

        public int ReadInt(long address)
        {
            var buffer = ReadBytes(address, 4);
            if (buffer.Length == 0) return 0;
            return BitConverter.ToInt32(buffer, 0);
        }

        public long ReadLong(long address)
        {
            var buffer = ReadBytes(address, 8);
            if (buffer.Length == 0) return 0;
            return BitConverter.ToInt64(buffer, 0);
        }

        /// <summary>
        /// Resolves a pointer chain by following each offset.
        /// For a chain like [base, 0x10, 0x20, 0x30]:
        /// 1. Read pointer at (base + 0x10)
        /// 2. Read pointer at (result + 0x20)
        /// 3. Final address = result + 0x30
        /// </summary>
        public long ReadPointerChain(long baseAddress, int[] offsets)
        {
            if (!IsAttached || offsets == null || offsets.Length == 0)
                return baseAddress;

            long current = baseAddress;

            // For all offsets except the last, read the pointer value
            for (int i = 0; i < offsets.Length - 1; i++)
            {
                current = ReadLong(current + offsets[i]);
                if (current == 0) return 0; // Null pointer encountered
            }

            // For the last offset, just add it (don't dereference)
            current += offsets[offsets.Length - 1];
            
            return current;
        }

        public float ReadFloat(long address)
        {
            var buffer = ReadBytes(address, 4);
            if (buffer.Length == 0) return 0f;
            return BitConverter.ToSingle(buffer, 0);
        }

        public string ReadString(long address, int length, Encoding? encoding = null)
        {
            var buffer = ReadBytes(address, length);
            if (buffer.Length == 0) return string.Empty;
            
            var enc = encoding ?? Encoding.ASCII;
            var str = enc.GetString(buffer);
            // Trim null terminator if present
            var nullIndex = str.IndexOf('\0');
            return nullIndex >= 0 ? str.Substring(0, nullIndex) : str;
        }

        public byte[] ReadBytes(long address, int length)
        {
            if (!IsAttached || length <= 0) return Array.Empty<byte>();

            var buffer = new byte[length];
            IntPtr bytesRead;
            
            bool success = ReadProcessMemory(_handle, (IntPtr)address, buffer, length, out bytesRead);
            
            if (!success || bytesRead == IntPtr.Zero)
            {
                return Array.Empty<byte>();
            }

            return buffer;
        }

        /// <summary>
        /// Scans memory for an Array of Bytes pattern.
        /// Pattern format: "AA BB ?? DD"
        /// This is a basic implementation. For production, efficient Boyer-Moore or SIG scanning is needed.
        /// </summary>
        public async Task<List<long>> ScanAobAsync(string aobPattern)
        {
            if (!IsAttached) return new List<long>();

            return await Task.Run(() => 
            {
                var results = new List<long>();
                var (patternBytes, mask) = ParsePattern(aobPattern);
                
                // Iterate Memory Regions - Detect architecture for appropriate address space
                long maxAddress = Environment.Is64BitProcess ? 0x7FFFFFFFFFFFL : 0x7FFFFFFFL; // x64: ~8TB, x86: 2GB
                // On x64 we could go up to 0x7FFFFFFFFFFF (140TB) but scanning is slow.
                // We restrict to reasonable limits based on typical committed pages.

                long current = 0;
                while (current < maxAddress)
                {
                    MEMORY_BASIC_INFORMATION mbi;
                    var size = VirtualQueryEx(_handle, (IntPtr)current, out mbi, (uint)Marshal.SizeOf(typeof(MEMORY_BASIC_INFORMATION)));
                    
                    if (size == 0) break;

                    // Only scan committed, readable memory that isn't guarded
                    if (mbi.State == MEM_COMMIT && 
                        (mbi.Protect == PAGE_READWRITE || mbi.Protect == PAGE_READONLY || mbi.Protect == PAGE_EXECUTE_READ || mbi.Protect == PAGE_EXECUTE_READWRITE))
                    {
                        // Read the entire chunk
                        // Caution: Huge regions can OOM. Chunking recommended.
                        // For this basic impl, we check size.
                        int regionSize = (int)mbi.RegionSize;
                        if (regionSize > 0 && regionSize < 100 * 1024 * 1024) // Skip > 100MB chunks for safety
                        {
                            byte[] buffer = new byte[regionSize];
                            ReadProcessMemory(_handle, mbi.BaseAddress, buffer, regionSize, out IntPtr read);
                            
                            if (read != IntPtr.Zero)
                            {
                                int bufferLen = (int)read;
                                for (int i = 0; i < bufferLen - patternBytes.Length; i++)
                                {
                                    bool match = true;
                                    for (int j = 0; j < patternBytes.Length; j++)
                                    {
                                        if (mask[j] && buffer[i + j] != patternBytes[j])
                                        {
                                            match = false;
                                            break;
                                        }
                                    }

                                    if (match)
                                    {
                                        results.Add((long)mbi.BaseAddress + i);
                                        // Optional: return first match only? or all?
                                        if (results.Count >= 50) return results; // Cap results
                                    }
                                }
                            }
                        }
                    }

                    current = (long)mbi.BaseAddress + (long)mbi.RegionSize;
                }

                return results;
            });
        }

        public long GetModuleBaseAddress(string moduleName)
        {
             if (_processId == 0) return 0;
             try
             {
                 var process = Process.GetProcessById(_processId);
                 foreach (ProcessModule module in process.Modules)
                 {
                     if (module.ModuleName.Equals(moduleName, StringComparison.OrdinalIgnoreCase))
                     {
                         return (long)module.BaseAddress;
                     }
                 }
             }
             catch
             {
                 // Module enumeration can fail due to permissions or race conditions
             }
             return 0;
        }

        private (byte[] Bytes, bool[] Mask) ParsePattern(string signature)
        {
            var parts = signature.Split(' ');
            var bytes = new List<byte>();
            var mask = new List<bool>();

            foreach (var part in parts)
            {
                if (part == "?" || part == "??")
                {
                    bytes.Add(0);
                    mask.Add(false); // Wildcard
                }
                else
                {
                    bytes.Add(Convert.ToByte(part, 16));
                    mask.Add(true); // Exact match
                }
            }
            return (bytes.ToArray(), mask.ToArray());
        }
    }
}
