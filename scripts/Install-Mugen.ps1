# Install-Mugen.ps1
# Automated MUGEN installation script for SaveStateReborn integration testing

param(
    [string]$InstallPath = "C:\mugen",
    [switch]$SkipDownload,
    [switch]$UseLocalArchive
)

$ErrorActionPreference = "Stop"

Write-Host "=== MUGEN Installation Script ===" -ForegroundColor Cyan
Write-Host "Target Installation Path: $InstallPath" -ForegroundColor Yellow
Write-Host ""

# Create installation directory
if (-not (Test-Path $InstallPath)) {
    Write-Host "Creating installation directory..." -ForegroundColor Green
    New-Item -ItemType Directory -Path $InstallPath -Force | Out-Null
}

# MUGEN 1.1 download URL (using archive.org mirror as official site is often down)
$mugenUrl = "https://archive.org/download/mugen-1.1b1/mugen-1.1b1.zip"
$downloadPath = Join-Path $env:TEMP "mugen-1.1b1.zip"

# Check if we should download
if (-not $SkipDownload) {
    Write-Host "Downloading MUGEN 1.1..." -ForegroundColor Green
    Write-Host "URL: $mugenUrl" -ForegroundColor Gray

    try {
        # Use WebClient for better progress reporting
        $webClient = New-Object System.Net.WebClient
        $webClient.DownloadFile($mugenUrl, $downloadPath)
        Write-Host "Download complete!" -ForegroundColor Green
    }
    catch {
        Write-Host "Download failed: $_" -ForegroundColor Red
        Write-Host ""
        Write-Host "Alternative: Download manually from:" -ForegroundColor Yellow
        Write-Host "  1. https://mugen.fandom.com/wiki/M.U.G.E.N" -ForegroundColor Cyan
        Write-Host "  2. Place the ZIP file at: $downloadPath" -ForegroundColor Cyan
        Write-Host "  3. Run this script again with -SkipDownload" -ForegroundColor Cyan
        exit 1
    }
}

# Extract MUGEN
if (Test-Path $downloadPath) {
    Write-Host "Extracting MUGEN to $InstallPath..." -ForegroundColor Green

    try {
        Expand-Archive -Path $downloadPath -DestinationPath $InstallPath -Force
        Write-Host "Extraction complete!" -ForegroundColor Green
    }
    catch {
        Write-Host "Extraction failed: $_" -ForegroundColor Red
        exit 1
    }
}
else {
    Write-Host "Archive not found at: $downloadPath" -ForegroundColor Red
    exit 1
}

# Find the actual MUGEN executable (it might be in a subfolder)
$mugenExe = Get-ChildItem -Path $InstallPath -Filter "mugen.exe" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1

if ($mugenExe) {
    $mugenDir = $mugenExe.DirectoryName
    Write-Host "MUGEN executable found at: $($mugenExe.FullName)" -ForegroundColor Green

    # If MUGEN is in a subfolder, move it to the root
    if ($mugenDir -ne $InstallPath) {
        Write-Host "Moving MUGEN files to installation root..." -ForegroundColor Yellow
        Get-ChildItem -Path $mugenDir | Move-Item -Destination $InstallPath -Force
    }
}
else {
    Write-Host "Warning: mugen.exe not found in extracted files" -ForegroundColor Yellow
}

# Create standard MUGEN directory structure
$directories = @(
    "chars",
    "data",
    "font",
    "sound",
    "stages",
    "save"
)

Write-Host "Creating MUGEN directory structure..." -ForegroundColor Green
foreach ($dir in $directories) {
    $dirPath = Join-Path $InstallPath $dir
    if (-not (Test-Path $dirPath)) {
        New-Item -ItemType Directory -Path $dirPath -Force | Out-Null
        Write-Host "  Created: $dir" -ForegroundColor Gray
    }
}

# Download sample characters (KFM is usually included)
Write-Host ""
Write-Host "Checking for default characters..." -ForegroundColor Green
$kfmPath = Join-Path $InstallPath "chars\kfm"
if (-not (Test-Path $kfmPath)) {
    Write-Host "  Note: Default characters not found. MUGEN typically includes Kung Fu Man." -ForegroundColor Yellow
    Write-Host "  If missing, you can download characters from:" -ForegroundColor Yellow
    Write-Host "    - https://mugenarchive.com/" -ForegroundColor Cyan
    Write-Host "    - https://mugenguild.com/" -ForegroundColor Cyan
}

# Update SaveStateReborn configuration
Write-Host ""
Write-Host "Updating SaveStateReborn configuration..." -ForegroundColor Green

$configPath = Join-Path (Split-Path $PSScriptRoot -Parent) "src\SaveState.Presentation\appsettings.json"

if (Test-Path $configPath) {
    try {
        $config = Get-Content $configPath -Raw | ConvertFrom-Json

        # Add or update Mugen section
        if (-not $config.Mugen) {
            $config | Add-Member -MemberType NoteProperty -Name "Mugen" -Value @{} -Force
        }

        $config.Mugen = @{
            ExecutablePath      = Join-Path $InstallPath "mugen.exe"
            CharactersDirectory = Join-Path $InstallPath "chars"
            StagesDirectory     = Join-Path $InstallPath "stages"
            DataDirectory       = Join-Path $InstallPath "data"
            SaveDirectory       = Join-Path $InstallPath "save"
        }

        $config | ConvertTo-Json -Depth 10 | Set-Content $configPath
        Write-Host "Configuration updated successfully!" -ForegroundColor Green
    }
    catch {
        Write-Host "Warning: Could not update configuration: $_" -ForegroundColor Yellow
        Write-Host "You may need to manually update appsettings.json" -ForegroundColor Yellow
    }
}
else {
    Write-Host "Warning: appsettings.json not found at: $configPath" -ForegroundColor Yellow
}

# Create a test batch file to verify MUGEN works
$testBatchPath = Join-Path $InstallPath "test-mugen.bat"
@"
@echo off
echo Testing MUGEN installation...
echo.
echo If MUGEN launches successfully, close it and return here.
echo.
pause
cd /d "%~dp0"
mugen.exe
"@ | Set-Content $testBatchPath

Write-Host ""
Write-Host "=== Installation Complete! ===" -ForegroundColor Green
Write-Host ""
Write-Host "MUGEN Installation Summary:" -ForegroundColor Cyan
Write-Host "  Installation Path: $InstallPath" -ForegroundColor White
Write-Host "  Executable: $(Join-Path $InstallPath 'mugen.exe')" -ForegroundColor White
Write-Host "  Characters: $(Join-Path $InstallPath 'chars')" -ForegroundColor White
Write-Host "  Stages: $(Join-Path $InstallPath 'stages')" -ForegroundColor White
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Cyan
Write-Host "  1. Test MUGEN: Run $testBatchPath" -ForegroundColor White
Write-Host "  2. Add characters to: $(Join-Path $InstallPath 'chars')" -ForegroundColor White
Write-Host "  3. Launch SaveStateReborn and navigate to MUGEN tab" -ForegroundColor White
Write-Host "  4. Click 'Scan Characters' to import your roster" -ForegroundColor White
Write-Host ""
Write-Host "Character Resources:" -ForegroundColor Cyan
Write-Host "  - MUGEN Archive: https://mugenarchive.com/" -ForegroundColor White
Write-Host "  - MUGEN Guild: https://mugenguild.com/" -ForegroundColor White
Write-Host "  - MUGEN Free For All: https://mugenfreeforall.com/" -ForegroundColor White
Write-Host ""

# Cleanup
if (Test-Path $downloadPath) {
    Write-Host "Cleaning up temporary files..." -ForegroundColor Gray
    Remove-Item $downloadPath -Force -ErrorAction SilentlyContinue
}

Write-Host "Installation script completed!" -ForegroundColor Green
