# SaveState .NET 6 Upgrade - Completion Plan

## Overview

Complete the remaining 2% of the .NET 6 migration to achieve a fully building and runnable SaveState application.

**Current Status:** ~2-5 errors remaining in Playnite library  
**Estimated Time:** 2-3 hours total

---

## Phase 1: Fix Remaining Compilation Errors (~15 min)

### Step 1.1: Fix `new()` Expression Errors

Search and replace all remaining target-typed `new()` expressions that aren't supported.

**Files to check:**
- [ ] [AdvancedShaderService.cs](file:///c:/Users/Doc/Desktop/SaveState/source/Playnite/Services/SaveState/AdvancedShaderService.cs) - Line 87
- [ ] All files in `Services\SaveState\` matching pattern `= new()`

**Action:**
```powershell
# Find all remaining new() expressions
Get-ChildItem -Path "source\Playnite\Services" -Recurse -Filter "*.cs" | 
    Select-String -Pattern "= new\(\)" | 
    Select-Object Filename, LineNumber, Line
```

Replace each with explicit type:
```diff
- var data = new();
+ var data = new Dictionary<string, object>();
```

### Step 1.2: Fix Any Remaining Broken Variable Names

Check for `$2` or `$` variable names from earlier regex fixes:
```powershell
Get-ChildItem -Path "source\Playnite\Services" -Recurse -Filter "*.cs" | 
    Select-String -Pattern '\$\d+' | Select-Object Filename, LineNumber
```

### Step 1.3: Verify Playnite Library Builds

```powershell
dotnet build source\Playnite\Playnite.csproj -c Debug
```

**Expected:** Build succeeded (0 errors)

---

## Phase 2: Convert Remaining Solution Projects (~30 min)

### Step 2.1: Convert Playnite.DesktopApp

**File:** [Playnite.DesktopApp.csproj](file:///c:/Users/Doc/Desktop/SaveState/source/Playnite.DesktopApp/Playnite.DesktopApp.csproj)

1. Backup original: `Playnite.DesktopApp.csproj.net462.backup`
2. Convert to SDK-style format:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net6.0-windows</TargetFramework>
    <OutputType>WinExe</OutputType>
    <UseWPF>true</UseWPF>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <GenerateAssemblyInfo>false</GenerateAssemblyInfo>
  </PropertyGroup>
  
  <ItemGroup>
    <ProjectReference Include="..\Playnite\Playnite.csproj" />
    <ProjectReference Include="..\PlayniteSDK\Playnite.SDK.csproj" />
  </ItemGroup>
  
  <!-- Copy package references from packages.config -->
</Project>
```

### Step 2.2: Convert Playnite.FullscreenApp

**File:** [Playnite.FullscreenApp.csproj](file:///c:/Users/Doc/Desktop/SaveState/source/Playnite.FullscreenApp/Playnite.FullscreenApp.csproj)

Same process as DesktopApp.

### Step 2.3: Convert Test Projects (Optional)

- Playnite.Tests.csproj
- Playnite.SDK.Tests.csproj

---

## Phase 3: Fix CefSharp Browser Integration (~20 min)

### Step 3.1: Restore WebViewWindow Browser Control

**File:** [WebViewWindow.xaml.cs](file:///c:/Users/Doc/Desktop/SaveState/source/Playnite/Windows/WebViewWindow.xaml.cs)

Add browser control programmatically in constructor:

```csharp
using CefSharp.Wpf;

public partial class WebViewWindow : WindowBase
{
    private ChromiumWebBrowser _browser;
    
    public WebViewWindow() : base()
    {
        InitializeComponent();
        
        // Add browser control programmatically
        _browser = new ChromiumWebBrowser();
        BrowserContainer.Child = _browser;
    }
    
    // Reference _browser instead of Browser in existing methods
}
```

### Step 3.2: Update WebViewWindow Event Handlers

Replace `Browser` references with `_browser`:
- [Window_Loaded](file:///c:/Users/Doc/Desktop/SaveState/source/Playnite/Windows/WebViewWindow.xaml.cs#17-21): `_browser.Focus();`
- [WindowBase_KeyUp](file:///c:/Users/Doc/Desktop/SaveState/source/Playnite/Windows/WebViewWindow.xaml.cs#22-29): `_browser.IsInitialized`, `_browser.ShowDevTools()`

---

## Phase 4: Handle Missing Assembly References (~15 min)

### Step 4.1: Verify Reference DLLs Exist

```powershell
Test-Path "references\IronPython.dll"
Test-Path "references\IronPython.SQLite.dll"
Test-Path "references\IronPython.Wpf.dll"
Test-Path "references\Microsoft.Scripting.AspNet.dll"
```

### Step 4.2: Update Reference Paths (if needed)

If references are missing, either:
- Remove unused references from Playnite.csproj
- Or add IronPython NuGet packages:
```xml
<PackageReference Include="IronPython" Version="3.4.1" />
```

---

## Phase 5: Full Solution Build (~10 min)

### Step 5.1: Build Complete Solution

```powershell
dotnet build source\SaveState.sln -c Debug
```

### Step 5.2: Document Any Remaining Errors

If errors remain, categorize:
- Compilation errors (must fix)
- Warnings (can defer)

---

## Phase 6: Runtime Verification (~30 min)

### Step 6.1: Launch Desktop Application

```powershell
dotnet run --project source\Playnite.DesktopApp\Playnite.DesktopApp.csproj
```

### Step 6.2: Test Core Functionality

| Test | Expected Result |
|------|-----------------|
| App launches | Main window appears |
| Library loads | Game list populates |
| WebView works | Browser opens in-app |
| Settings accessible | Options dialog opens |

### Step 6.3: Test SaveState-Specific Features

| Feature | Service | Test |
|---------|---------|------|
| ROM Browser | RomManagerService | Open ROM browser tab |
| Achievements | RetroAchievementsService | Check achievements panel |
| Shaders | AdvancedShaderService | Toggle shader preset |
| Screenshot | ScreenshotService | Take screenshot |

---

## Phase 7: Cleanup & Documentation (~15 min)

### Step 7.1: Remove Backup Files (optional)

```powershell
Remove-Item "source\Playnite\Playnite.csproj.*.backup"
Remove-Item "source\PlayniteSDK\Playnite.SDK.csproj.*.backup"
```

### Step 7.2: Delete packages.config Files

SDK-style projects don't need these:
```powershell
Remove-Item "source\Playnite\packages.config"
```

### Step 7.3: Update README

Add .NET 6 runtime requirement to documentation.

---

## Verification Checklist

### Build Verification
- [ ] Playnite.SDK builds (0 errors)
- [ ] Playnite library builds (0 errors)
- [ ] Playnite.DesktopApp builds (0 errors)
- [ ] Playnite.FullscreenApp builds (0 errors)
- [ ] Full solution builds (0 errors)

### Runtime Verification
- [ ] Desktop app launches successfully
- [ ] No runtime exceptions on startup
- [ ] Core Playnite features work
- [ ] SaveState services load correctly
- [ ] CefSharp browser renders pages

---

## Rollback Plan

If issues arise, restore from backups:

```powershell
# Restore .NET 4.6.2 projects
Copy-Item "source\Playnite\Playnite.csproj.net462.backup" "source\Playnite\Playnite.csproj"
Copy-Item "source\PlayniteSDK\Playnite.SDK.csproj.net462.backup" "source\PlayniteSDK\Playnite.SDK.csproj"

# Rebuild with old MSBuild
& 'C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe' source\SaveState.sln
```
