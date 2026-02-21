# Platform Feature Matrix

Complete comparison of SaveStateReborn features across all supported platforms.

## Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Full support |
| ⚠️ | Partial/Limited support |
| ❌ | Not supported |
| 🚫 | Blocked by OS security |

---

## Core Features

| Feature | Windows | Linux | macOS | Steam Deck |
|---------|---------|-------|-------|------------|
| **Memory Reading** | ✅ Full | ✅ Full | ✅ Full | ✅ Full |
| **Memory Writing** | ✅ Full | ⚠️ Limited | 🚫 Blocked | ⚠️ Limited |
| **Value Freezing** | ✅ Real-time | ⚠️ 100ms interval | 🚫 Blocked | ⚠️ 100ms interval |
| **Process Attachment** | ✅ Easy | ✅ With ptrace | ⚠️ With entitlements | ✅ Same as Linux |
| **Pattern Scanning** | ✅ Fast | ✅ Fast | ✅ Fast | ✅ Fast |
| **Auto-Discovery** | ✅ Full | ✅ Full | ✅ Full | ✅ Full |
| **Signature Database** | ✅ 336 games | ✅ 336 games | ✅ 336 games | ✅ 336 games |

---

## Memory Intelligence Features

| Feature | Windows | Linux | macOS | Steam Deck |
|---------|---------|-------|-------|------------|
| **Read Health** | ✅ | ✅ | ✅ | ✅ |
| **Read Currency** | ✅ | ✅ | ✅ | ✅ |
| **Read Position** | ✅ | ✅ | ✅ | ✅ |
| **Write Health** | ✅ | ⚠️ Slow | 🚫 SIP | ⚠️ Slow |
| **Freeze Health** | ✅ Smooth | ⚠️ Laggy | 🚫 SIP | ⚠️ Laggy |
| **Cheat Engine Import** | ✅ | ✅ | ✅ | ✅ |
| **Pointer Scanning** | ✅ | ✅ | ⚠️ Limited | ✅ |

---

## Cloud & Community

| Feature | Windows | Linux | macOS | Steam Deck |
|---------|---------|-------|-------|------------|
| **Download Signatures** | ✅ | ✅ | ✅ | ✅ |
| **Upload Signatures** | ✅ | ✅ | ✅ | ✅ |
| **Auto-Sync** | ✅ | ✅ | ✅ | ✅ |
| **Vote/Report** | ✅ | ✅ | ✅ | ✅ |

---

## Performance

| Metric | Windows | Linux | macOS | Steam Deck |
|--------|---------|-------|-------|------------|
| **Memory Scan Speed** | 100% | 85% | 80% | 85% |
| **Write Latency** | ~1ms | ~10ms | N/A | ~10ms |
| **Freeze Interval** | 10ms | 100ms | N/A | 100ms |
| **CPU Overhead** | Low | Medium | Medium | Medium |

---

## Why the Differences?

### Windows Advantages
- **Designed for debugging**: OpenProcess with explicit permissions
- **Flexible protection**: VirtualProtectEx changes page permissions
- **Fast syscalls**: WriteProcessMemory is optimized
- **No SIP equivalent**: User has full control with admin rights

### Linux Limitations
- **ptrace overhead**: Must stop process to write
- **Capability requirements**: CAP_SYS_PTRACE needed
- **Slower freeze**: 100ms vs 10ms on Windows
- **Permission complexity**: setcap or sudo required

### macOS Limitations
- **SIP (System Integrity Protection)**: Blocks code injection
- **Hardened Runtime**: Prevents memory modification
- **Library validation**: Signed apps cannot be modified
- **Security-first design**: Intentionally restrictive

---

## Recommendations by Use Case

### Casual Gaming (Read-only)
**Any platform works equally well**
- All platforms support memory reading
- Auto-discovery works everywhere
- Pattern scanning is fast on all platforms

### Speedrunning / Tool-Assisted
**Windows recommended**
- Freeze functionality for frame-perfect timing
- Fast memory writing for instant resets
- Lowest latency for real-time tools

### Game Development / Debugging
**Windows or Linux**
- Full read/write access
- Can attach to own processes easily
- Better debugging tools

### macOS Gaming
**Read-only features only**
- Memory scanning works fine
- Cannot modify game values
- Use for analysis, not cheating
- Consider Windows dual-boot for full features

---

## Future Improvements

### Linux
- [ ] Kernel module for faster writes (risky)
- [ ] eBPF-based memory monitoring
- [ ] Better integration with Wine/Proton

### macOS
- [ ] Limited write support for non-SIP processes
- [ ] Better error messages for blocked operations
- [ ] Documentation for legitimate debugging use cases

---

## Technical Details

### Windows APIs Used
```
OpenProcess(PROCESS_VM_READ | PROCESS_VM_WRITE | PROCESS_VM_OPERATION)
ReadProcessMemory()
WriteProcessMemory()
VirtualProtectEx()
```

### Linux APIs Used
```
ptrace(PTRACE_ATTACH/PTRACE_PEEKDATA/PTRACE_POKEDATA)
process_vm_readv()
process_vm_writev() [requires CAP_SYS_PTRACE]
/proc/{pid}/mem
```

### macOS APIs Used
```
task_for_pid() [requires entitlements]
vm_read()
vm_write() [blocked by SIP/Hardened Runtime]
vm_protect() [limited effectiveness]
```
