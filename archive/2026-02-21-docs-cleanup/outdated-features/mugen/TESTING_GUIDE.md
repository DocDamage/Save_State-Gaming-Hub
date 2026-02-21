# 🎮 MUGEN Integration Testing Guide

**Date**: January 4, 2026
**Status**: ✅ **MUGEN INSTALLED & CONFIGURED**

---

## ✅ Installation Summary

### MUGEN Installation Details

- **Version**: MUGEN 1.1 Beta 1
- **Installation Path**: `C:\mugen`
- **Executable**: `C:\mugen\mugen.exe`
- **Status**: ✅ Successfully Installed

### Directory Structure

```
C:\mugen\
├── mugen.exe           ✅ Main executable
├── chars\              ✅ Character directory
│   ├── kfm\           ✅ Kung Fu Man (default character)
│   └── kfm720\        ✅ Kung Fu Man HD
├── stages\             ✅ Stage directory
├── data\               ✅ Game data
├── save\               ✅ Save files
├── font\               ✅ Fonts
└── sound\              ✅ Sound effects
```

### Default Characters Available

- ✅ **Kung Fu Man** (kfm) - Standard resolution
- ✅ **Kung Fu Man 720** (kfm720) - HD version

---

## 🔧 SaveStateReborn Configuration

### appsettings.json Status

✅ **Automatically Configured**

```json
{
  "Mugen": {
    "ExecutablePath": "C:\\mugen\\mugen.exe",
    "CharactersDirectory": "C:\\mugen\\chars",
    "StagesDirectory": "C:\\mugen\\stages",
    "DataDirectory": "C:\\mugen\\data",
    "SaveDirectory": "C:\\mugen\\save"
  }
}
```

---

## 🧪 Testing Workflow

### Phase 1: Verify MUGEN Installation

#### Test 1.1: Launch MUGEN Standalone

```powershell
# Run MUGEN directly to verify it works
cd C:\mugen
.\mugen.exe
```

**Expected Result**:

- MUGEN launches successfully
- Main menu appears
- Can select characters (KFM should be available)
- Can start a match

**If MUGEN doesn't launch**:

- Check for missing DLL errors
- Install Visual C++ Redistributable 2008/2010
- Check Windows compatibility settings

---

### Phase 2: Test SaveStateReborn Integration

#### Test 2.1: Character Scanning

1. **Launch SaveStateReborn**

   ```powershell
   cd c:\Users\Doc\Desktop\SaveStateReborn
   dotnet run --project src\SaveState.Presentation\SaveState.Presentation.csproj
   ```

2. **Navigate to MUGEN Tab**
   - Click on "MUGEN" in the main navigation
   - Should see the MUGEN shell interface

3. **Scan Characters**
   - Click "Scan Characters" button
   - Should detect KFM and KFM720

**Expected Results**:

- ✅ Character scan completes without errors
- ✅ 2 characters detected (kfm, kfm720)
- ✅ Characters appear in roster
- ✅ Character metadata displayed (name, directory)

**Verification Queries**:

```sql
-- Check database for scanned characters
SELECT * FROM MugenCharacters;
```

---

#### Test 2.2: Character Selection

1. **Select Player 1**
   - Click on "Kung Fu Man" in roster
   - Should highlight as P1

2. **Select Player 2**
   - Click on "Kung Fu Man 720" in roster
   - Should highlight as P2

**Expected Results**:

- ✅ P1 indicator shows selected character
- ✅ P2 indicator shows selected character
- ✅ Selection persists across section switches

---

#### Test 2.3: Death Battle Mode

1. **Navigate to Death Battle Section**
   - Click "Death Battle" tab
   - Should show P1 and P2 selections

2. **Configure Battle**
   - Set rounds: 3
   - Set difficulty: Normal
   - Enable betting (optional)

3. **Start Battle**
   - Click "Start Battle" button

**Expected Results**:

- ✅ MUGEN launches with selected characters
- ✅ Battle starts automatically
- ✅ SaveStateReborn monitors the process
- ✅ Result is captured after match ends

**Known Limitations** (Current Implementation):

- 🔄 Process launching implemented
- 🔄 Result capture needs testing
- 🔄 Automated character selection in MUGEN needs verification

---

#### Test 2.4: Training Mode

1. **Navigate to Training Section**
   - Click "Training" tab

2. **Configure Training**
   - Training Character: KFM
   - Dummy Character: KFM720
   - Dummy AI: Off

3. **Start Training**
   - Click "Start Training" button

**Expected Results**:

- ✅ MUGEN launches in training mode
- ✅ Correct characters loaded
- ✅ Training settings applied

---

### Phase 3: Advanced Features

#### Test 3.1: Tournament Mode

1. **Navigate to Tournament Section**
2. **Create Tournament**
   - Name: "Test Tournament"
   - Format: Single Elimination
   - Add both characters

3. **Generate Bracket**
   - Click "Generate Bracket"

**Expected Results**:

- ✅ Bracket created
- ✅ Matches scheduled
- 🔄 Match execution (needs implementation)

---

#### Test 3.2: Statistics Tracking

1. **Navigate to Stats Section**
2. **View Character Stats**
   - Select KFM
   - View win/loss record

**Expected Results**:

- ✅ Stats page loads
- 🔄 Match history displayed (after battles)
- 🔄 Win rate calculated

---

## 🐛 Troubleshooting

### Issue: Characters Not Detected

**Symptoms**:

- Scan completes but no characters found
- Error messages during scan

**Solutions**:

1. Verify character directory structure:

   ```powershell
   Get-ChildItem C:\mugen\chars -Recurse -Filter "*.def"
   ```

2. Check character .def files exist:
   - `C:\mugen\chars\kfm\kfm.def`
   - `C:\mugen\chars\kfm720\kfm720.def`

3. Check database configuration:
   - Verify `MugenCharacters` table exists
   - Check for constraint errors in logs

---

### Issue: MUGEN Won't Launch

**Symptoms**:

- "Start Battle" does nothing
- Process error in logs

**Solutions**:

1. Verify executable path:

   ```powershell
   Test-Path "C:\mugen\mugen.exe"
   ```

2. Test manual launch:

   ```powershell
   Start-Process "C:\mugen\mugen.exe"
   ```

3. Check for DLL dependencies:
   - Install Visual C++ 2008 Redistributable
   - Install Visual C++ 2010 Redistributable

---

### Issue: Results Not Captured

**Symptoms**:

- Battle completes but no result in database
- Stats don't update

**Solutions**:

1. Check MUGEN output files:
   - `C:\mugen\save\` directory
   - Look for match result files

2. Enable verbose logging in SaveStateReborn

3. Verify result parsing logic in:
   - `MugenMatchResultService`
   - `CaptureMugenResultCommand`

---

## 📊 Test Results Checklist

### Installation

- [x] MUGEN downloaded
- [x] MUGEN extracted
- [x] Directory structure created
- [x] Configuration updated

### Basic Functionality

- [ ] MUGEN launches standalone
- [ ] Characters appear in MUGEN
- [ ] Can start a match in MUGEN

### SaveStateReborn Integration

- [ ] Character scanning works
- [ ] Characters appear in roster
- [ ] Player selection works
- [ ] Death Battle launches MUGEN
- [ ] Training mode launches MUGEN
- [ ] Results are captured
- [ ] Stats are updated

### Advanced Features

- [ ] Tournament bracket generation
- [ ] Match scheduling
- [ ] Replay recording
- [ ] Character fusion
- [ ] AI coaching

---

## 🚀 Next Steps

### Immediate Testing

1. **Run MUGEN Standalone** - Verify installation
2. **Launch SaveStateReborn** - Test character scanning
3. **Try Death Battle** - Test process launching

### Character Expansion

To add more characters for testing:

1. **Download Characters**:
   - Visit <https://mugenarchive.com/>
   - Download character packs
   - Extract to `C:\mugen\chars\`

2. **Recommended Test Characters**:
   - Ryu (Street Fighter)
   - Ken (Street Fighter)
   - Scorpion (Mortal Kombat)
   - Sub-Zero (Mortal Kombat)

3. **Re-scan in SaveStateReborn**:
   - Click "Scan Characters" again
   - New characters should appear

### Development Tasks

1. **Implement Result Capture**:
   - Parse MUGEN output files
   - Store match results in database
   - Update character statistics

2. **Enhance Process Management**:
   - Monitor MUGEN process
   - Detect match completion
   - Handle crashes gracefully

3. **Add Character Metadata**:
   - Parse .def files
   - Extract character info
   - Display portraits/sprites

---

## 📝 Test Log Template

```
=== MUGEN Integration Test Log ===
Date: [DATE]
Tester: [NAME]

MUGEN Installation:
[ ] Standalone launch successful
[ ] Characters visible
[ ] Can start matches

Character Scanning:
[ ] Scan initiated
[ ] Characters detected: [COUNT]
[ ] Database updated
[ ] Errors: [NONE/DETAILS]

Death Battle:
[ ] P1 selected: [CHARACTER]
[ ] P2 selected: [CHARACTER]
[ ] MUGEN launched
[ ] Match completed
[ ] Result captured: [YES/NO]
[ ] Errors: [NONE/DETAILS]

Notes:
[Add any observations, issues, or suggestions]
```

---

## ✅ Success Criteria

The MUGEN integration is considered **successful** when:

1. ✅ MUGEN installs and runs standalone
2. ✅ SaveStateReborn can scan characters
3. ✅ Characters appear in the roster
4. ✅ Player selection works correctly
5. ✅ Death Battle can launch MUGEN
6. ✅ Match results are captured
7. ✅ Statistics are updated

**Current Status**: **Phase 1 Complete** - MUGEN installed and configured

**Ready for Testing**: ✅ YES
