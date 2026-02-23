using System.Runtime.InteropServices;

namespace SaveState.Infrastructure.GameLibrary.Services;

public sealed partial class MacOSMemoryReader
{
    // Mach API Constants
    private const int KERN_SUCCESS = 0;
    private const int TASK_DYLD_INFO = 17;

    // vm_prot_t values
    private const int VM_PROT_NONE = 0x00;
    private const int VM_PROT_READ = 0x01;
    private const int VM_PROT_WRITE = 0x02;
    private const int VM_PROT_COPY = 0x08;
    private const int VM_PROT_EXECUTE = 0x04;

    // Mach API P/Invoke Declarations
    [DllImport("/usr/lib/libSystem.dylib")]
    private static extern int task_for_pid(IntPtr target_tport, int pid, out IntPtr t);

    [DllImport("/usr/lib/libSystem.dylib")]
    private static extern IntPtr mach_task_self();

    [DllImport("/usr/lib/libSystem.dylib")]
    private static extern int mach_port_deallocate(IntPtr task, IntPtr name);

    [DllImport("/usr/lib/libSystem.dylib")]
    private static extern int vm_read(
        IntPtr target_task,
        ulong address,
        ulong size,
        out IntPtr data,
        out int dataCnt);

    [DllImport("/usr/lib/libSystem.dylib")]
    private static extern int vm_write(
        IntPtr target_task,
        ulong address,
        IntPtr data,
        int dataCnt);

    [DllImport("/usr/lib/libSystem.dylib")]
    private static extern int vm_protect(
        IntPtr target_task,
        ulong address,
        ulong size,
        bool set_maximum,
        int new_protection);

    [DllImport("/usr/lib/libSystem.dylib")]
    private static extern int vm_deallocate(IntPtr target_task, IntPtr address, uint size);

    [DllImport("/usr/lib/libSystem.dylib")]
    private static extern int vm_region_recurse_64(
        IntPtr target_task,
        ref ulong address,
        ref ulong size,
        ref uint depth,
        ref vm_region_submap_info_64 info,
        ref uint infoCount);

    private static string GetMachErrorString(int errorCode)
    {
        return errorCode switch
        {
            1 => "KERN_INVALID_ADDRESS",
            2 => "KERN_PROTECTION_FAILURE",
            3 => "KERN_NO_SPACE",
            4 => "KERN_INVALID_ARGUMENT",
            5 => "KERN_FAILURE",
            6 => "KERN_RESOURCE_SHORTAGE",
            7 => "KERN_NOT_RECEIVER",
            8 => "KERN_NO_ACCESS",
            9 => "KERN_MEMORY_FAILURE",
            10 => "KERN_MEMORY_ERROR",
            11 => "KERN_ALREADY_IN_SET",
            12 => "KERN_NOT_IN_SET",
            13 => "KERN_NAME_EXISTS",
            14 => "KERN_ABORTED",
            15 => "KERN_INVALID_NAME",
            16 => "KERN_INVALID_TASK",
            17 => "KERN_INVALID_RIGHT",
            18 => "KERN_INVALID_VALUE",
            19 => "KERN_UREFS_OVERFLOW",
            20 => "KERN_INVALID_CAPABILITY",
            21 => "KERN_RIGHT_EXISTS",
            22 => "KERN_INVALID_HOST",
            23 => "KERN_MEMORY_PRESENT",
            24 => "KERN_MEMORY_DATA_MOVED",
            25 => "KERN_MEMORY_RESTART_COPY",
            26 => "KERN_INVALID_PROCESSOR_SET",
            27 => "KERN_POLICY_LIMIT",
            28 => "KERN_INVALID_POLICY",
            29 => "KERN_INVALID_OBJECT",
            30 => "KERN_ALREADY_WAITING",
            31 => "KERN_DEFAULT_SET",
            32 => "KERN_EXCEPTION_PROTECTED",
            33 => "KERN_INVALID_LEDGER",
            34 => "KERN_INVALID_MEMORY_CONTROL",
            35 => "KERN_INVALID_SECURITY",
            _ => $"Unknown error ({errorCode})"
        };
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct vm_region_submap_info_64
    {
        public uint protection;
        public uint max_protection;
        public uint inheritance;
        public ulong offset;
        public uint user_tag;
        public uint pages_resident;
        public uint pages_shared_now_private;
        public uint pages_swapped_out;
        public uint pages_dirtied;
        public uint ref_count;
        public short shadow_depth;
        public byte external_pager;
        public byte share_mode;
        public int is_submap;
        public uint behavior;
        public uint object_id;
        public uint user_wired_count;
    }
}
