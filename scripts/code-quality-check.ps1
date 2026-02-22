#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Code quality check script for SaveStateReborn
.DESCRIPTION
    Runs various code quality checks to ensure compliance with project standards.
    Should be run before committing code.
.EXAMPLE
    .\scripts\code-quality-check.ps1
#>

[CmdletBinding()]
param(
    [switch]$FailOnWarnings,
    [switch]$AutoFix
)

$ErrorActionPreference = "Stop"
$results = @()
$exitCode = 0

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "SaveStateReborn Code Quality Check" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check 1: Build with warnings as errors
Write-Host "Check 1: Building solution (0 warnings)..." -ForegroundColor Yellow
$buildOutput = dotnet build --verbosity minimal --warnaserror 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "  ❌ Build failed or has warnings" -ForegroundColor Red
    $results += [PSCustomObject]@{ Check = "Build"; Status = "FAIL"; Details = "See build output above" }
    $exitCode = 1
} else {
    Write-Host "  ✅ Build succeeded with 0 warnings" -ForegroundColor Green
    $results += [PSCustomObject]@{ Check = "Build"; Status = "PASS"; Details = "0 warnings" }
}

# Check 2: Null-forgiving operators (null!)
Write-Host ""
Write-Host "Check 2: Checking for null-forgiving operators (null!)..." -ForegroundColor Yellow
$nullForgivingCount = (Get-ChildItem -Path src -Recurse -Filter "*.cs" | 
    Select-String -Pattern "(?<!\w)null!(?!\w)" | 
    Measure-Object).Count

if ($nullForgivingCount -gt 0) {
    Write-Host "  ❌ Found $nullForgivingCount null! usages" -ForegroundColor Red
    Get-ChildItem -Path src -Recurse -Filter "*.cs" | 
        Select-String -Pattern "(?<!\w)null!(?!\w)" | 
        Select-Object -First 5 |
        ForEach-Object { Write-Host "    - $($_.FileName):$($_.LineNumber)" -ForegroundColor Gray }
    $results += [PSCustomObject]@{ Check = "null! Usage"; Status = "FAIL"; Details = "$nullForgivingCount found" }
    $exitCode = 1
} else {
    Write-Host "  ✅ No null! usages found" -ForegroundColor Green
    $results += [PSCustomObject]@{ Check = "null! Usage"; Status = "PASS"; Details = "0 found" }
}

# Check 3: DateTime.Now/UtcNow/Today policy
Write-Host ""
Write-Host "Check 3: Checking DateTime.Now/UtcNow/Today policy..." -ForegroundColor Yellow
$dateTimeViolations = (Get-ChildItem -Path src -Recurse -Filter "*.cs" | 
    Where-Object { $_.Name -ne 'ITimeProvider.cs' } |
    Select-String -Pattern "DateTime\.(Now|UtcNow|Today)" | 
    Where-Object { $_.Line -notmatch "UI-only" -and $_.Line -notmatch "guard-ignore" } |
    Measure-Object).Count

if ($dateTimeViolations -gt 0) {
    Write-Host "  ❌ Found $dateTimeViolations DateTime.Now/UtcNow/Today usages" -ForegroundColor Red
    Get-ChildItem -Path src -Recurse -Filter "*.cs" | 
        Where-Object { $_.Name -ne 'ITimeProvider.cs' } |
        Select-String -Pattern "DateTime\.(Now|UtcNow|Today)" | 
        Where-Object { $_.Line -notmatch "UI-only" -and $_.Line -notmatch "guard-ignore" } | 
        Select-Object -First 5 |
        ForEach-Object { Write-Host "    - $($_.FileName):$($_.LineNumber)" -ForegroundColor Gray }
    $results += [PSCustomObject]@{ Check = "DateTime Policy"; Status = "FAIL"; Details = "$dateTimeViolations found" }
    $exitCode = 1
} else {
    Write-Host "  ✅ No DateTime.Now/UtcNow/Today usages found" -ForegroundColor Green
    $results += [PSCustomObject]@{ Check = "DateTime Policy"; Status = "PASS"; Details = "0 found" }
}

# Check 4: Async method naming
Write-Host ""
Write-Host "Check 3: Checking async method naming convention..." -ForegroundColor Yellow
$asyncMethods = Get-ChildItem -Path src -Recurse -Filter "*.cs" | 
    Select-String -Pattern "async\s+Task\s+(\w+)" | 
    Where-Object { $_.Matches[0].Groups[1].Value -notlike "*Async" }

$asyncViolationCount = ($asyncMethods | Measure-Object).Count
if ($asyncViolationCount -gt 260) {
    Write-Host "  ⚠️ Found $asyncViolationCount async methods without 'Async' suffix (baseline: 260)" -ForegroundColor Yellow
    $results += [PSCustomObject]@{ Check = "Async Naming"; Status = "WARN"; Details = "$asyncViolationCount violations" }
} else {
    Write-Host "  ✅ Async method naming within baseline" -ForegroundColor Green
    $results += [PSCustomObject]@{ Check = "Async Naming"; Status = "PASS"; Details = "$asyncViolationCount violations" }
}

# Check 5: Tests passing
Write-Host ""
Write-Host "Check 4: Running tests..." -ForegroundColor Yellow
$testOutput = dotnet test --verbosity minimal --no-build 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "  ❌ Tests failed" -ForegroundColor Red
    $results += [PSCustomObject]@{ Check = "Tests"; Status = "FAIL"; Details = "See test output above" }
    $exitCode = 1
} else {
    Write-Host "  ✅ All tests passing" -ForegroundColor Green
    $results += [PSCustomObject]@{ Check = "Tests"; Status = "PASS"; Details = "All passing" }
}

# Check 6: Architecture tests
Write-Host ""
Write-Host "Check 5: Running architecture tests..." -ForegroundColor Yellow
$archTestOutput = dotnet test tests/SaveState.Infrastructure.Tests --filter "FullyQualifiedName~ArchitectureTests" --verbosity minimal --no-build 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "  ❌ Architecture tests failed" -ForegroundColor Red
    $results += [PSCustomObject]@{ Check = "Architecture"; Status = "FAIL"; Details = "See test output above" }
    $exitCode = 1
} else {
    Write-Host "  ✅ Architecture tests passing" -ForegroundColor Green
    $results += [PSCustomObject]@{ Check = "Architecture"; Status = "PASS"; Details = "All passing" }
}

# Check 7: Code quality tests
Write-Host ""
Write-Host "Check 6: Running code quality tests..." -ForegroundColor Yellow
$qualityTestOutput = dotnet test tests/SaveState.Infrastructure.Tests --filter "FullyQualifiedName~CodeQualityTests" --verbosity minimal --no-build 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "  ❌ Code quality tests failed" -ForegroundColor Red
    $results += [PSCustomObject]@{ Check = "Code Quality"; Status = "FAIL"; Details = "See test output above" }
    $exitCode = 1
} else {
    Write-Host "  ✅ Code quality tests passing" -ForegroundColor Green
    $results += [PSCustomObject]@{ Check = "Code Quality"; Status = "PASS"; Details = "All passing" }
}

# Summary
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Summary" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

$results | Format-Table -AutoSize

$passCount = ($results | Where-Object { $_.Status -eq "PASS" } | Measure-Object).Count
$failCount = ($results | Where-Object { $_.Status -eq "FAIL" } | Measure-Object).Count
$warnCount = ($results | Where-Object { $_.Status -eq "WARN" } | Measure-Object).Count

Write-Host ""
Write-Host "Results: $passCount passed, $failCount failed, $warnCount warnings" -ForegroundColor $(if ($failCount -gt 0) { "Red" } elseif ($warnCount -gt 0) { "Yellow" } else { "Green" })

if ($exitCode -ne 0) {
    Write-Host ""
    Write-Host "❌ Code quality checks failed. Please fix the issues above before committing." -ForegroundColor Red
    exit 1
} else {
    Write-Host ""
    Write-Host "✅ All code quality checks passed!" -ForegroundColor Green
    exit 0
}
