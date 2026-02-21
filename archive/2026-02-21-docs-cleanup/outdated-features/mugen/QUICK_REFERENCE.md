# 🎮 MUGEN Quick Test Reference

## ✅ Installation Status

- **MUGEN**: ✅ Installed at `C:\mugen`
- **Characters**: ✅ 2 available (KFM, KFM720)
- **Config**: ✅ SaveStateReborn configured
- **Verified**: ✅ Standalone launch successful

---

## 🚀 Quick Test Commands

### 1. Test MUGEN Standalone

```powershell
cd C:\mugen
.\mugen.exe
```

**Expected**: MUGEN launches, shows main menu

---

### 2. Launch SaveStateReborn

```powershell
cd c:\Users\Doc\Desktop\SaveStateReborn
dotnet run --project src\SaveState.Presentation\SaveState.Presentation.csproj
```

**Expected**: App starts, no database errors

---

### 3. Test Character Scanning (In App)

1. Click **MUGEN** tab
2. Click **Scan Characters** button
3. **Expected**: 2 characters detected (kfm, kfm720)

---

### 4. Test Death Battle (In App)

1. Navigate to **MUGEN** → **Roster**
2. Click **KFM** (selects as P1)
3. Click **KFM720** (selects as P2)
4. Navigate to **Death Battle** tab
5. Click **Start Battle**
6. **Expected**: MUGEN launches with selected characters

---

## 🐛 Quick Troubleshooting

### MUGEN Won't Launch

```powershell
# Verify executable exists
Test-Path "C:\mugen\mugen.exe"

# Check for DLL errors
# Install Visual C++ Redistributable if needed
```

### Characters Not Found

```powershell
# Verify character files
Get-ChildItem "C:\mugen\chars" -Recurse -Filter "*.def"

# Should show:
# - C:\mugen\chars\kfm\kfm.def
# - C:\mugen\chars\kfm720\kfm720.def
```

### Database Errors

```powershell
# Delete and recreate database
Remove-Item "c:\Users\Doc\Desktop\SaveStateReborn\src\SaveState.Presentation\bin\Debug\net9.0\savestate.db"
# Then restart app
```

---

## 📊 Test Checklist

- [ ] MUGEN launches standalone
- [ ] SaveStateReborn starts without errors
- [ ] MUGEN tab accessible
- [ ] Character scan detects 2 characters
- [ ] Characters appear in roster
- [ ] Can select P1 and P2
- [ ] Death Battle launches MUGEN
- [ ] Match completes successfully

---

## 📝 Quick Notes

**Default Characters**:

- Kung Fu Man (kfm) - Standard
- Kung Fu Man 720 (kfm720) - HD

**Key Paths**:

- MUGEN: `C:\mugen\mugen.exe`
- Characters: `C:\mugen\chars\`
- Config: `src\SaveState.Presentation\appsettings.json`

**Next Steps**:

1. Test character scanning
2. Test player selection
3. Test Death Battle
4. Add more characters from mugenarchive.com

---

## 🔗 Full Documentation

- **Installation**: `MUGEN_INSTALLATION_SUMMARY.md`
- **Testing Guide**: `MUGEN_TESTING_GUIDE.md`
- **Integration Guide**: `MUGEN_INTEGRATION_GUIDE.md`

---

**Status**: ✅ **READY FOR TESTING**
**Last Updated**: January 4, 2026
