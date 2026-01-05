# Emulator Setup Guide - SaveState Reborn

## 📋 Current Status

### ✅ Installed Components

- **5,209 ROM files** (organized by platform)
- **206 BIOS files** (all systems covered)
- **4DO Emulator** (3DO console) - extracted to `engines/emulators/4DO/`

### ⚠️ Required: RetroArch Installation

SaveState Reborn uses **RetroArch** as its primary emulation backend. You need to install it separately.

---

## 🚀 Quick Setup Instructions

### Option 1: Install RetroArch (Recommended)

1. **Download RetroArch**:
   - Visit: <https://www.retroarch.com/>
   - Download the Windows installer (64-bit)
   - Or use: `winget install Libretro.RetroArch`

2. **Install Location**:
   - Default: `C:\Program Files\RetroArch\`
   - Or custom location (SaveState will auto-detect)

3. **Download Cores** (from within RetroArch):
   - **GBA**: mGBA
   - **NES**: Mesen or FCEUmm
   - **SNES**: Snes9x
   - **Genesis**: Genesis Plus GX
   - **Neo Geo**: FinalBurn Neo
   - **Arcade**: MAME 2016
   - **Atari 2600**: Stella
   - **NDS**: DeSmuME or melonDS

4. **Configure BIOS Path**:
   - In RetroArch: Settings → Directory → System/BIOS
   - Point to: `C:\Users\Doc\Desktop\SaveStateReborn\data\bios`

---

## 🔧 Alternative: Standalone Emulators

If you prefer standalone emulators, SaveState can integrate with:

### Game Boy Advance

- **mGBA**: <https://mgba.io/>
- **VisualBoy Advance**: <https://visualboyadvance.org/>

### Nintendo DS

- **DeSmuME**: <https://desmume.org/>
- **melonDS**: <https://melonds.kuribo64.net/>

### Arcade/Neo Geo

- **FinalBurn Neo**: <https://github.com/finalburnneo/FBNeo>
- **MAME**: <https://www.mamedev.org/>

### NES/SNES

- **Mesen**: <https://www.mesen.ca/>
- **Snes9x**: <https://www.snes9x.com/>

---

## 📁 BIOS Files Location

All BIOS files are already installed in:

```
C:\Users\Doc\Desktop\SaveStateReborn\data\bios\
```

**Available BIOS Sets**:

- ✅ Game Boy Advance
- ✅ Neo Geo (MVS/AES)
- ✅ SEGA Genesis/32X/Saturn
- ✅ Super Nintendo (DSP chips)
- ✅ TurboGrafx-16/CD
- ✅ Atari 7800/Lynx
- ✅ Colecovision
- ✅ Famicom Disk System

---

## 🎮 SaveState Integration

Once RetroArch is installed:

1. **Launch SaveState Reborn**
2. Go to **Settings** → **Emulators**
3. Click **"Auto-Detect Emulators"**
4. SaveState will find RetroArch and configure all cores
5. Navigate to **Library** → **Scan for Games**
6. All 5,209 ROMs will be indexed and ready to play!

---

## 🔍 Manual Emulator Registration

If auto-detection fails, you can manually register emulators:

1. **Settings** → **Emulators** → **Add Emulator**
2. **Name**: RetroArch - mGBA
3. **Executable**: `C:\Program Files\RetroArch\retroarch.exe`
4. **Arguments**: `-L cores\mgba_libretro.dll "{rom}"`
5. **Platform**: Game Boy Advance
6. **Save**

Repeat for each core/platform combination.

---

## 📊 Installation Checklist

- [x] ROMs installed (5,209 games)
- [x] BIOS files installed (206 files)
- [x] 4DO emulator extracted
- [ ] **RetroArch installed** ← **YOU ARE HERE**
- [ ] RetroArch cores downloaded
- [ ] BIOS path configured in RetroArch
- [ ] SaveState emulator auto-detection run
- [ ] Game library scanned

---

## 🆘 Troubleshooting

### "No emulator found for this ROM"

- Install RetroArch and download the appropriate core
- Run **Auto-Detect Emulators** in SaveState settings

### "Missing BIOS file"

- Check that RetroArch's System/BIOS directory points to:
  `C:\Users\Doc\Desktop\SaveStateReborn\data\bios`

### "Game won't launch"

- Verify the emulator executable path in SaveState settings
- Check that the ROM file isn't corrupted
- Ensure BIOS files are in the correct subdirectories

---

## 🎯 Next Steps

1. **Install RetroArch** (5 minutes)
2. **Download cores** for your favorite platforms (10 minutes)
3. **Configure BIOS path** (1 minute)
4. **Launch SaveState** and enjoy 5,000+ games!

---

**Need Help?** Check the SaveState documentation or RetroArch's official guides.
