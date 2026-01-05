# Active Issues - What's Broken Right Now

**Last Updated**: January 5, 2026
**Next Review**: January 12, 2026

> [!NOTE]
> All critical and high-priority code issues have been resolved.

---

## 🔴 CRITICAL (Fix Immediately)

**None** ✅

---

## 🟠 HIGH (Fix This Sprint)

**None** ✅

---

## 🟡 MEDIUM (Backlog)

Technical debt to address during code review.

| Category | Count | Action |
|----------|-------|--------|
| `TODO` comments | 60+ | Address or remove during reviews |
| Missing XML docs (CS1591) | ~1,200 | Adding progressively |
| ~~`return null` statements~~ | ~~45+~~ | ✅ **Verified correct - using proper nullable patterns** |

---

## ✅ Recently Fixed

| Date | Issue | Resolution |
|------|-------|------------|
| Jan 5 | TwitchStreamingPlugin placeholders | Removed, added env var config |
| Jan 5 | DiscordIntegrationPlugin placeholders | Removed, production-ready |
| Jan 5 | Database Compaction TODO | Implemented with VACUUM/ANALYZE |
| Jan 5 | Emulator Configuration TODO | Implemented folder navigation |
| Jan 5 | Backup History Loading | Implemented with proper sorting |
| Jan 5 | Mod Updates Check | Implemented with scan integration |
| Jan 5 | Mod Browser TODO | Implemented with Nexus Mods |
| Jan 5 | Mod File Picker TODO | Integrated DialogService |
| Jan 5 | Create Mod Pack TODO | Implemented with timestamp naming |
| Jan 5 | Open Mods Folder TODO | Implemented with auto-creation |
| Jan 5 | `return null` analysis | Verified all uses are correct |
| Jan 2 | `TerminalViewModel` async void | Converted to async Task |
| Jan 2 | ViewModels async void methods | Previously fixed |
| Jan 1 | Library tab crash | Fixed XAML bindings |

---

## 🚫 Known Warnings to Ignore

These are intentional or harmless:

| Warning | Count | Why Ignore |
|---------|-------|------------|
| CS1591 | ~1,200 | XML docs being added progressively |
| XAML Designer | ~50 | Work at runtime, designer limitation |
| Nullable reference | ~30 | Handled by Result pattern |

---

## 📝 How to Update This File

When you fix an issue:

1. Move from CRITICAL/HIGH to "Recently Fixed"
2. Update `PROJECT_METRICS.md` critical_issues count
3. Run sync tool: `dotnet run --project tools/SaveState.Docs.Sync`
4. Commit with message: `fix: resolve [issue description]`
