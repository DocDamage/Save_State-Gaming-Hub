#!/usr/bin/env pwsh
# Install pre-commit hooks for SaveStateReborn

$ErrorActionPreference = "Stop"

Write-Host "🔧 Installing pre-commit hooks..." -ForegroundColor Cyan

# Check if pre-commit is installed
try {
    $precommitVersion = pre-commit --version
    Write-Host "✓ pre-commit found: $precommitVersion" -ForegroundColor Green
} catch {
    Write-Host "⚠ pre-commit not found. Installing..." -ForegroundColor Yellow
    pip install pre-commit
}

# Install hooks
Write-Host "Installing pre-commit hooks..."
pre-commit install

# Also install as git hook directly
$hookPath = ".git/hooks/pre-commit"
if ($IsWindows -or $PSVersionTable.PSVersion.Major -lt 6) {
    Copy-Item "scripts/pre-commit.ps1" $hookPath -Force
} else {
    Copy-Item "scripts/pre-commit.sh" $hookPath -Force
    chmod +x $hookPath
}

Write-Host ""
Write-Host "✅ Pre-commit hooks installed successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "The following checks will run before each commit:" -ForegroundColor Cyan
Write-Host "  ✓ No 'return null' in public APIs" -ForegroundColor White
Write-Host "  ✓ No DateTime.Now usage (ITimeProvider pattern)" -ForegroundColor White
Write-Host "  ✓ Async methods end with Async suffix" -ForegroundColor White
Write-Host "  ✓ No unnecessary null-forgiving operators" -ForegroundColor White
Write-Host "  ✓ Build with 0 warnings" -ForegroundColor White
Write-Host "  ✓ Architecture tests pass" -ForegroundColor White
Write-Host ""
Write-Host "To bypass hooks in emergencies: git commit --no-verify" -ForegroundColor Yellow
