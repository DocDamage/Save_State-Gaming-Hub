# Setup script for IKEMEN bundle
# This script initializes the IKEMEN engine and character packs

param(
    [switch]$Force,
    [switch]$SkipDownload
)

Write-Host "Setting up IKEMEN bundle for SaveState Reborn..." -ForegroundColor Green

# Check if IKEMEN is already set up
$ikemenDir = Join-Path $PSScriptRoot "ikemen"
$ikemenExe = Join-Path $ikemenDir "Ikemen_GO.exe"
$configFile = Join-Path $ikemenDir "config.json"

if ((Test-Path $ikemenExe) -and -not $Force) {
    Write-Host "IKEMEN already set up. Use -Force to reinstall." -ForegroundColor Yellow
    exit 0
}

# Create directories
$dirs = @(
    "engines/ikemen",
    "data/characters/streetfighter",
    "data/characters/mvc2",
    "data/characters/builtin",
    "data/stages",
    "data/music"
)

foreach ($dir in $dirs) {
    if (!(Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
    }
}

if (!$SkipDownload) {
    Write-Host "Downloading IKEMEN GO..." -ForegroundColor Cyan

    # Download IKEMEN GO (placeholder - actual download URLs would be added)
    # This would download the latest IKEMEN GO release
    Write-Host "Note: IKEMEN executable would be downloaded here" -ForegroundColor Yellow

    Write-Host "Downloading Street Fighter characters..." -ForegroundColor Cyan
    Write-Host "Note: Street Fighter character pack would be downloaded here" -ForegroundColor Yellow

    Write-Host "Downloading MVC2 characters..." -ForegroundColor Cyan
    Write-Host "Note: MVC2 character pack would be downloaded here" -ForegroundColor Yellow
}

# Create default configuration
Write-Host "Creating IKEMEN configuration..." -ForegroundColor Cyan

$config = @{
    name = "IKEMEN GO"
    version = "0.99"
    executable = "Ikemen_GO.exe"
    arguments = @{
        versus = "-p1 {player1} -p2 {player2} -rounds 3"
        training = "-p1 {player1} -p2 {dummy} -training"
        watch = "-p1 {player1} -p2 {player2} -watch"
        single = "-p1 {player1} -single"
    }
    characterDirectories = @(
        "../../../data/characters/streetfighter"
        "../../../data/characters/mvc2"
        "../../../data/characters/builtin"
    )
    features = @{
        luaScripting = $true
        mugenCompatibility = $true
        trainingMode = $true
    }
}

$config | ConvertTo-Json | Set-Content $configFile

Write-Host "IKEMEN setup complete!" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "1. Place Ikemen_GO.exe in engines/ikemen/"
Write-Host "2. Extract character packs to data/characters/"
Write-Host "3. Run SaveState to scan and catalog characters"
Write-Host ""
Write-Host "SaveState will automatically detect and integrate IKEMEN!" -ForegroundColor Green
