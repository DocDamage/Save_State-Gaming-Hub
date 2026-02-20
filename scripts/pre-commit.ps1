#!/usr/bin/env pwsh
# Pre-commit hook for SaveStateReborn
# This script runs before each commit to ensure code quality

$ErrorActionPreference = "Stop"
$hasErrors = $false

Write-Host "🔍 Running pre-commit checks..." -ForegroundColor Cyan
Write-Host ""

# Check 1: No 'return null' in public APIs (except acceptable patterns)
Write-Host "✓ Check 1: No 'return null' in public APIs..." -NoNewline
$nullReturns = Select-String -Path "src/**/*.cs" -Pattern "return null;" | 
    Where-Object { $_.FileName -notmatch "(Test|Tests)\.cs$" -and $_.Line -notmatch "(private|Task<\w+\?>)" }
if ($nullReturns) {
    Write-Host " FAILED" -ForegroundColor Red
    Write-Host "  Found 'return null' statements that may violate Result pattern:" -ForegroundColor Yellow
    $nullReturns | Select-Object -First 5 | ForEach-Object { Write-Host "    $($_.FileName):$($_.LineNumber)" }
    $hasErrors = $true
} else {
    Write-Host " PASSED" -ForegroundColor Green
}

# Check 2: No DateTime.Now usage
Write-Host "✓ Check 2: No DateTime.Now usage..." -NoNewline
$dateTimeNow = Select-String -Path "src/**/*.cs" -Pattern "DateTime\.(Now|UtcNow)" |
    Where-Object { $_.FileName -notmatch "(Test|Tests)\.cs$" -and $_.Line -notmatch "TimeProvider" }
if ($dateTimeNow) {
    Write-Host " FAILED" -ForegroundColor Red
    Write-Host "  Found DateTime.Now/DateTime.UtcNow usage (use ITimeProvider instead):" -ForegroundColor Yellow
    $dateTimeNow | Select-Object -First 5 | ForEach-Object { Write-Host "    $($_.FileName):$($_.LineNumber)" }
    $hasErrors = $true
} else {
    Write-Host " PASSED" -ForegroundColor Green
}

# Check 3: Async methods should end with Async suffix (except interface implementations)
Write-Host "✓ Check 3: Async method naming..." -NoNewline
$badAsyncNames = Select-String -Path "src/**/*.cs" -Pattern "public async Task(?!\w*Async)\s+\w+\s*\(" |
    Where-Object { $_.Line -notmatch "Handle\(" } # Exclude MediatR Handle methods
if ($badAsyncNames) {
    Write-Host " FAILED" -ForegroundColor Red
    Write-Host "  Found async methods without Async suffix:" -ForegroundColor Yellow
    $badAsyncNames | Select-Object -First 5 | ForEach-Object { Write-Host "    $($_.FileName):$($_.LineNumber)" }
    $hasErrors = $true
} else {
    Write-Host " PASSED" -ForegroundColor Green
}

# Check 4: No null-forgiving operators
Write-Host "✓ Check 4: No unnecessary null-forgiving operators..." -NoNewline
$nullForgiving = Select-String -Path "src/**/*.cs" -Pattern "\w+!\.(\w|\[)" |
    Where-Object { $_.FileName -notmatch "(Test|Tests)\.cs$" }
if ($nullForgiving) {
    Write-Host " FAILED" -ForegroundColor Red
    Write-Host "  Found null-forgiving operators:" -ForegroundColor Yellow
    $nullForgiving | Select-Object -First 5 | ForEach-Object { Write-Host "    $($_.FileName):$($_.LineNumber)" }
    $hasErrors = $true
} else {
    Write-Host " PASSED" -ForegroundColor Green
}

# Check 5: XML documentation for public APIs
Write-Host "✓ Check 5: Public API documentation..." -NoNewline
$undocumentedPublic = Select-String -Path "src/**/SaveState.Core/**/*.cs" -Pattern "^\s*public.*(class|interface|enum|struct|delegate)" |
    Where-Object { $_ -notmatch "///" }
# This is a warning, not an error
if ($undocumentedPublic) {
    Write-Host " WARNING" -ForegroundColor Yellow
} else {
    Write-Host " PASSED" -ForegroundColor Green
}

# Check 6: Build with 0 warnings
Write-Host "✓ Check 6: Build with 0 warnings..." -NoNewline
try {
    $buildOutput = dotnet build SaveStateReborn.Core.sln --warnaserror 2>&1
    Write-Host " PASSED" -ForegroundColor Green
} catch {
    Write-Host " FAILED" -ForegroundColor Red
    Write-Host "  Build failed with warnings treated as errors" -ForegroundColor Yellow
    $hasErrors = $true
}

# Check 7: Architecture tests pass
Write-Host "✓ Check 7: Architecture tests..." -NoNewline
try {
    $testOutput = dotnet test tests/SaveState.Infrastructure.Tests --filter "FullyQualifiedName~ArchitectureTests" --verbosity minimal 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host " PASSED" -ForegroundColor Green
    } else {
        Write-Host " FAILED" -ForegroundColor Red
        $hasErrors = $true
    }
} catch {
    Write-Host " FAILED" -ForegroundColor Red
    $hasErrors = $true
}

Write-Host ""
if ($hasErrors) {
    Write-Host "❌ Pre-commit checks FAILED" -ForegroundColor Red
    Write-Host "Please fix the issues above before committing." -ForegroundColor Yellow
    exit 1
} else {
    Write-Host "✅ All pre-commit checks passed!" -ForegroundColor Green
    exit 0
}
