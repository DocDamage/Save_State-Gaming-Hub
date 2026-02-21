# Windows-Only Features

## Overview

SaveStateReborn works on Windows, Linux, and macOS, but some advanced features are only fully functional on Windows due to operating system design differences.

This document explains:
1. Which features are Windows-only
2. Why they're limited on other platforms
3. Workarounds where possible
4. How to get the best experience on each platform

---

## Windows-Only Features

### 1. Memory Writing (Full Speed)

**Windows**: ✅ Instant writes via WriteProcessMemory
**Linux**: ⚠️ Slow writes via process_vm_writev (requires CAP_SYS_PTRACE)
**macOS**: 🚫 Blocked by SIP/Hardened Runtime

#### Why Windows Wins

Windows provides `WriteProcessMemory()` which:
- Automatically handles page protection changes
- Doesn't require stopping the target process
- Completes in ~1 millisecond
- Works on any process you have rights to access

#### Linux Limitations

Linux uses `process_vm_writev()` which:
- Requires `CAP_SYS_PTRACE` capability
- Target page must be writable (many games use read-only pages)
- Slower due to permission checks
- May fail silently on protected memory

**To enable on Linux:**
```bash
# Add capability (persistent)
sudo setcap cap_sys_ptrace=eip ./SaveStateReborn

# Or run with sudo (temporary)
sudo ./SaveStateReborn
```

#### macOS Limitations

macOS provides `vm_write()` but:
- Blocked by System Integrity Protection (SIP)
- Hardened Runtime prevents code injection
- Library validation blocks modification
- Most modern games are protected

**No reliable workaround exists.** This is intentional security design by Apple.

---

### 2. Value Freezing (Real-Time)

**Windows**: ✅ Smooth 10ms interval freezes
**Linux**: ⚠️ Laggy 100ms interval freezes
**macOS**: 🚫 Not possible

#### What is Value Freezing?

Value freezing continuously writes a fixed value to a memory address, keeping it constant regardless of game logic. Useful for:
- Infinite health
- Unlimited ammo
- Permanent power-ups

#### How It Works on Windows

```csharp
// Background thread loop on Windows
while (freezeActive) {
    WriteProcessMemory(handle, address, &frozenValue, size, NULL);
    Sleep(10);  // 10ms = 100 writes/second
}
```

Result: Game stays smooth, value appears permanently frozen.

#### Linux Implementation

```csharp
// Same loop on Linux
while (freezeActive) {
    process_vm_writev(pid, &local, 1, &remote, 1, 0);
    Thread.Sleep(100);  // 100ms = 10 writes/second
}
```

Result: Game may stutter slightly. Value "flickers" between frozen and real.

**Why slower?**
- process_vm_writev has higher overhead
- More permission checks per write
- Can't write as frequently without impacting game performance

#### macOS

Not implemented because vm_write() is blocked by security features.

---

### 3. Page Protection Modification

**Windows**: ✅ VirtualProtectEx changes any page
**Linux**: ⚠️ mprotect works but requires ptrace
**macOS**: 🚫 vm_protect blocked by SIP

#### Use Case

Some games mark health/ammo as read-only to prevent cheating. To write to these:

**Windows:**
```csharp
VirtualProtectEx(handle, address, size, PAGE_EXECUTE_READWRITE, &oldProtect);
WriteProcessMemory(handle, address, data, size, NULL);
VirtualProtectEx(handle, address, size, oldProtect, &oldProtect);
```

Works reliably on any process.

**Linux:**
```csharp
ptrace(PTRACE_ATTACH, pid, 0, 0);
// ... modify memory via /proc/pid/mem ...
ptrace(PTRACE_DETACH, pid, 0, 0);
```

Requires stopping the process temporarily.

**macOS:**
```csharp
vm_protect(task, address, size, false, VM_PROT_READ | VM_PROT_WRITE);
```

Usually fails with KERN_PROTECTION_FAILURE on protected processes.

---

### 4. Lowest Latency Operations

**Windows**: ~1ms per memory operation
**Linux**: ~5-10ms per operation
**macOS**: ~5-10ms per operation (when allowed)

#### Why Windows is Faster

1. **Syscall design**: Windows syscalls are optimized for this use case
2. **No capability checks**: Once process is open, no additional permission checks
3. **Direct memory mapping**: Can map target process memory directly
4. **Kernel support**: Windows kernel designed to support debugging tools

#### Impact

| Operation | Windows | Linux | macOS |
|-----------|---------|-------|-------|
| Single Read | 0.1ms | 0.5ms | 0.5ms |
| Single Write | 0.5ms | 5ms | N/A |
| Full Scan | 50ms | 200ms | 200ms |
| Freeze (1 sec) | 100 writes | 10 writes | 0 writes |

For speedrunning or tool-assisted gameplay, Windows provides the best experience.

---

## Platform-Specific Workarounds

### Linux: Maximizing Write Performance

1. **Use CAP_SYS_PTRACE** (recommended)
   ```bash
   sudo setcap cap_sys_ptrace=eip ./SaveStateReborn
   ```

2. **Run with sudo** (less secure)
   ```bash
   sudo ./SaveStateReborn
   ```

3. **Increase freeze interval** (if lag occurs)
   - Settings > Memory > Freeze Interval: 200ms
   - Less smooth but reduces CPU overhead

### macOS: What You CAN Do

Since memory writing is blocked, focus on:

1. **Memory Analysis**
   - Find addresses and patterns
   - Export to Cheat Engine format
   - Use on Windows for actual editing

2. **Signature Creation**
   - Discover new memory patterns
   - Contribute to cloud database
   - Help other users

3. **Read-Only Tools**
   - Game state monitoring
   - Statistics tracking
   - Achievement hunting

---

## Recommendation Summary

| Use Case | Recommended Platform |
|----------|---------------------|
| Casual scanning | Any platform |
| Game modding | Windows |
| Speedrunning | Windows |
| Tool development | Windows or Linux |
| Security research | Linux |
| macOS games | Read-only features only |

---

## Frequently Asked Questions

### Q: Will macOS ever support full memory editing?

A: Unlikely. Apple's security model intentionally prevents this. Each macOS release adds more protections, not fewer.

### Q: Can I disable SIP to enable macOS features?

A: Yes, but we don't recommend it:
- Reduces overall system security
- Breaks some Apple services
- May cause instability
- Not persistent across updates

### Q: Why not use a kernel driver on Linux?

A: Possible but problematic:
- Requires root to install
- Breaks with kernel updates
- May trigger anti-cheat
- Complex to maintain

### Q: Is Wine/Proton on Linux better than native?

A: Sometimes:
- Wine games may use Windows memory layout
- May bypass some Linux protections
- But also adds translation overhead
- Test both native and Wine versions

---

## Technical Deep Dive

### Windows Memory Architecture

Windows uses a demand-paged virtual memory system with:
- Explicit permission model (OpenProcess flags)
- Copy-on-write semantics
- Support for cross-process memory operations
- No global "system integrity" restrictions

### Linux Security Model

Linux uses:
- Capabilities (fine-grained privileges)
- ptrace for process control
- /proc filesystem for introspection
- SELinux/AppArmor for additional restrictions

ptrace was designed for debugging, not game cheating, so it's slower.

### macOS Security Architecture

macOS uses layered security:
1. SIP (System Integrity Protection)
2. Hardened Runtime
3. Library Validation
4. Code Signing requirements
5. User-approved kernel extensions

This "defense in depth" approach makes memory modification extremely difficult.

---

## Conclusion

SaveStateReborn provides the best possible experience on each platform:

- **Windows**: Full functionality, fastest performance
- **Linux**: Good functionality, requires setup
- **macOS**: Read-only, by design

Choose your platform based on your needs. For full game modification capabilities, Windows remains the best choice.
