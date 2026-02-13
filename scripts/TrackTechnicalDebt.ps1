#Requires -Version 5.1
<#
.SYNOPSIS
    Tracks technical debt metrics for SaveStateReborn project.

.DESCRIPTION
    Analyzes the codebase for technical debt patterns and generates a report.
    Tracks: return null statements, null-forgiving operators, TODO comments,
    empty catch blocks, and test results.

.EXAMPLE
    .\TrackTechnicalDebt.ps1
    Generates a metrics report and saves it to the project root.

.EXAMPLE
    .\TrackTechnicalDebt.ps1 -Detailed
    Generates a detailed report with file-by-file breakdown.

.EXAMPLE
    .\TrackTechnicalDebt.ps1 -ExportCsv
    Exports metrics to a CSV file for tracking over time.

.PARAMETER Detailed
    Include detailed file-by-file breakdown.

.PARAMETER ExportCsv
    Export metrics to CSV file.

.PARAMETER OutputPath
    Path where the report will be saved. Defaults to project root.
#>

[CmdletBinding()]
param(
    [switch]$Detailed,
    [switch]$ExportCsv,
    [string]$OutputPath = "$PSScriptRoot\.."
)

$ErrorActionPreference = 'Continue'
$ProgressPreference = 'Continue'

#region Configuration
$projectRoot = Resolve-Path "$PSScriptRoot\.."
$srcPath = Join-Path $projectRoot "src"
$testsPath = Join-Path $projectRoot "tests"
$timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
$dateStamp = Get-Date -Format "yyyy-MM-dd"
#endregion

#region Helper Functions
function Write-SectionHeader {
    param([string]$Title)
    Write-Host "`n$('=' * 60)" -ForegroundColor Cyan
    Write-Host "  $Title" -ForegroundColor Cyan
    Write-Host "$('=' * 60)" -ForegroundColor Cyan
}

function Write-Metric {
    param(
        [string]$Label,
        [int]$Value,
        [int]$Target = 0,
        [string]$Status = "Info"
    )
    
    $color = switch ($Status) {
        "Good" { 'Green' }
        "Warning" { 'Yellow' }
        "Critical" { 'Red' }
        default { 'White' }
    }
    
    $statusIcon = switch ($Status) {
        "Good" { '✅' }
        "Warning" { '⚠️' }
        "Critical" { '🔴' }
        default { 'ℹ️' }
    }
    
    Write-Host "  $statusIcon $Label`: " -NoNewline -ForegroundColor $color
    Write-Host $Value -NoNewline -ForegroundColor $color
    
    if ($Target -gt 0) {
        $progress = [math]::Min(100, [math]::Max(0, (1 - ($Value / $Target)) * 100))
        $progressBar = '[' + ('█' * [math]::Floor($progress / 10)).PadRight(10, '░') + ']'
        Write-Host " (Target: $Target) $progressBar" -NoNewline -ForegroundColor Gray
    }
    
    Write-Host ""
}

function Get-FileCount {
    param([string]$Pattern, [string]$Path = $srcPath)
    
    try {
        $files = Get-ChildItem -Path $Path -Recurse -Filter "*.cs" -ErrorAction SilentlyContinue
        $matches = $files | Select-String -Pattern $Pattern -ErrorAction SilentlyContinue
        return $matches.Count
    }
    catch {
        Write-Warning "Error counting pattern '$Pattern': $_"
        return 0
    }
}

function Get-DetailedMatches {
    param([string]$Pattern, [string]$Path = $srcPath)
    
    try {
        $files = Get-ChildItem -Path $Path -Recurse -Filter "*.cs" -ErrorAction SilentlyContinue
        $matches = $files | Select-String -Pattern $Pattern -ErrorAction SilentlyContinue
        
        return $matches | Group-Object Filename | 
            Select-Object Name, @{N='Count'; E={$_.Count}} |
            Sort-Object Count -Descending
    }
    catch {
        Write-Warning "Error getting detailed matches for '$Pattern': $_"
        return @()
    }
}
#endregion

#region Main Execution
Clear-Host
Write-Host @"
╔══════════════════════════════════════════════════════════════╗
║           SaveStateReborn Technical Debt Tracker             ║
║                                                              ║
║                    Report Date: $dateStamp                ║
╚══════════════════════════════════════════════════════════════╝
"@ -ForegroundColor Cyan

Write-Host "  Project Root: $projectRoot" -ForegroundColor Gray
Write-Host "  Source Path:  $srcPath" -ForegroundColor Gray
Write-Host ""

# Get file statistics
$totalCsFiles = (Get-ChildItem -Path $srcPath -Recurse -Filter "*.cs" -ErrorAction SilentlyContinue).Count
$totalLines = (Get-ChildItem -Path $srcPath -Recurse -Filter "*.cs" -ErrorAction SilentlyContinue | 
    Get-Content -ErrorAction SilentlyContinue | Measure-Object).Count

Write-Host "  Total C# Files: $totalCsFiles" -ForegroundColor Gray
Write-Host "  Total Lines of Code: $totalLines" -ForegroundColor Gray
Write-Host ""

#region Code Pattern Analysis
Write-SectionHeader "Code Pattern Analysis"

# return null count
$returnNullCount = Get-FileCount "return\s+null;"
$returnNullStatus = if ($returnNullCount -eq 0) { "Good" } elseif ($returnNullCount -lt 100) { "Warning" } else { "Critical" }
Write-Metric -Label "'return null' statements" -Value $returnNullCount -Target 50 -Status $returnNullStatus

# null-forgiving operator count
$nullForgivingCount = Get-FileCount "![\.\[]"
$nullForgivingStatus = if ($nullForgivingCount -lt 500) { "Good" } elseif ($nullForgivingCount -lt 1000) { "Warning" } else { "Critical" }
Write-Metric -Label "Null-forgiving operators (!)" -Value $nullForgivingCount -Target 500 -Status $nullForgivingStatus

# TODO/FIXME/HACK comments
$todoCount = Get-FileCount "TODO|FIXME|HACK"
$todoStatus = if ($todoCount -lt 10) { "Good" } elseif ($todoCount -lt 25) { "Warning" } else { "Critical" }
Write-Metric -Label "TODO/FIXME/HACK comments" -Value $todoCount -Target 10 -Status $todoStatus

# Empty catch blocks
$emptyCatchCount = Get-FileCount "catch\s*\([^)]*\)\s*\{\s*\}"
$emptyCatchStatus = if ($emptyCatchCount -eq 0) { "Good" } else { "Critical" }
Write-Metric -Label "Empty catch blocks" -Value $emptyCatchCount -Target 0 -Status $emptyCatchStatus

#endregion

#region Build Status
Write-SectionHeader "Build Status"

Write-Host "  Running build analysis..." -ForegroundColor Yellow

$buildOutput = & dotnet build "$projectRoot\SaveStateReborn.sln" --verbosity minimal 2>&1
$buildErrors = ($buildOutput | Select-String "error\(s\)" | Select-Object -First 1)
$buildWarnings = ($buildOutput | Select-String "warning\(s\)" | Select-Object -First 1)

# Parse error/warning counts
$errorCount = 0
if ($buildErrors -match '(\d+)\s+Error') {
    $errorCount = [int]$matches[1]
}

$warningCount = 0
if ($buildWarnings -match '(\d+)\s+Warning') {
    $warningCount = [int]$matches[1]
}

$errorStatus = if ($errorCount -eq 0) { "Good" } else { "Critical" }
$warningStatus = if ($warningCount -eq 0) { "Good" } elseif ($warningCount -lt 10) { "Warning" } else { "Critical" }

Write-Metric -Label "Build Errors" -Value $errorCount -Target 0 -Status $errorStatus
Write-Metric -Label "Build Warnings" -Value $warningCount -Target 0 -Status $warningStatus

#endregion

#region Test Status
Write-SectionHeader "Test Status"

Write-Host "  Running test analysis (this may take a few minutes)..." -ForegroundColor Yellow
Write-Host "  Note: EndToEnd tests may time out" -ForegroundColor Gray

$testProjects = @(
    "SaveState.Core.Tests",
    "SaveState.Application.Tests",
    "SaveState.Infrastructure.Tests",
    "SaveState.CrossPlatform.Tests",
    "SaveState.Configuration.Tests",
    "SaveState.Monitoring.Tests",
    "SaveState.Accessibility.Tests",
    "SaveState.Presentation.Tests",
    "SaveState.LoadTests"
)

$totalTests = 0
$passedTests = 0
$failedTests = 0
$skippedTests = 0

foreach ($project in $testProjects) {
    $projectPath = Join-Path $testsPath $project
    if (Test-Path $projectPath) {
        $testOutput = & dotnet test $projectPath --verbosity minimal --no-build 2>&1 | Out-String
        
        if ($testOutput -match 'Total:\s+(\d+)') {
            $totalTests += [int]$matches[1]
        }
        if ($testOutput -match 'Passed:\s+(\d+)') {
            $passedTests += [int]$matches[1]
        }
        if ($testOutput -match 'Failed:\s+(\d+)') {
            $failedTests += [int]$matches[1]
        }
        if ($testOutput -match 'Skipped:\s+(\d+)') {
            $skippedTests += [int]$matches[1]
        }
        
        Write-Host "    $project`: " -NoNewline -ForegroundColor Gray
        if ($testOutput -match 'Failed:\s+0') {
            Write-Host "✅ PASS" -ForegroundColor Green
        } else {
            Write-Host "❌ FAIL" -ForegroundColor Red
        }
    }
}

Write-Host ""
Write-Metric -Label "Total Tests" -Value $totalTests -Status "Info"
Write-Metric -Label "Passed Tests" -Value $passedTests -Status $(if ($failedTests -eq 0) { "Good" } else { "Warning" })
Write-Metric -Label "Failed Tests" -Value $failedTests -Target 0 -Status $(if ($failedTests -eq 0) { "Good" } else { "Critical" })
Write-Metric -Label "Skipped Tests" -Value $skippedTests -Status "Info"

if ($totalTests -gt 0) {
    $passRate = [math]::Round(($passedTests / $totalTests) * 100, 2)
    $passRateColor = if ($passRate -ge 95) { 'Green' } elseif ($passRate -ge 85) { 'Yellow' } else { 'Red' }
    Write-Host "  Pass Rate: $passRate%" -ForegroundColor $passRateColor
}

#endregion

#region Detailed Analysis
if ($Detailed) {
    Write-SectionHeader "Detailed Analysis"
    
    Write-Host "`n  Top 10 Files with 'return null':" -ForegroundColor Yellow
    $nullReturns = Get-DetailedMatches "return\s+null;"
    $nullReturns | Select-Object -First 10 | ForEach-Object {
        Write-Host "    $($_.Name): $($_.Count) occurrences" -ForegroundColor Gray
    }
    
    Write-Host "`n  Top 10 Files with '!' operators:" -ForegroundColor Yellow
    $nullForgiving = Get-DetailedMatches "![\.\[]"
    $nullForgiving | Select-Object -First 10 | ForEach-Object {
        Write-Host "    $($_.Name): $($_.Count) occurrences" -ForegroundColor Gray
    }
}
#endregion

#region Summary
Write-SectionHeader "Summary"

$overallScore = 100
$overallScore -= [math]::Min(30, $returnNullCount / 10)
$overallScore -= [math]::Min(30, $nullForgivingCount / 100)
$overallScore -= [math]::Min(20, $todoCount)
$overallScore -= [math]::Min(20, $failedTests * 2)
$overallScore -= [math]::Min(10, $warningCount)
$overallScore -= [math]::Min(10, $errorCount * 10)

$overallScore = [math]::Max(0, [math]::Min(100, $overallScore))

$grade = switch ($overallScore) {
    { $_ -ge 90 } { 'A' }
    { $_ -ge 80 } { 'B' }
    { $_ -ge 70 } { 'C' }
    { $_ -ge 60 } { 'D' }
    default { 'F' }
}

$gradeColor = switch ($grade) {
    'A' { 'Green' }
    'B' { 'Green' }
    'C' { 'Yellow' }
    'D' { 'Red' }
    'F' { 'Red' }
}

Write-Host "`n  Overall Health Score: $overallScore/100" -ForegroundColor $gradeColor
Write-Host "  Grade: $grade" -ForegroundColor $gradeColor
Write-Host "  Report Generated: $timestamp" -ForegroundColor Gray

#endregion

#region Export
if ($ExportCsv) {
    $csvPath = Join-Path $OutputPath "metrics-$dateStamp.csv"
    $csvData = [PSCustomObject]@{
        Date = $timestamp
        ReturnNullCount = $returnNullCount
        NullForgivingCount = $nullForgivingCount
        TodoCount = $todoCount
        EmptyCatchCount = $emptyCatchCount
        BuildErrors = $errorCount
        BuildWarnings = $warningCount
        TotalTests = $totalTests
        PassedTests = $passedTests
        FailedTests = $failedTests
        SkippedTests = $skippedTests
        PassRate = if ($totalTests -gt 0) { [math]::Round(($passedTests / $totalTests) * 100, 2) } else { 0 }
        OverallScore = $overallScore
        Grade = $grade
    }
    
    $csvData | Export-Csv -Path $csvPath -NoTypeInformation -Append
    Write-Host "`n  📊 Metrics exported to: $csvPath" -ForegroundColor Green
}

# Save report
$reportPath = Join-Path $OutputPath "TECHNICAL_DEBT_REPORT-$dateStamp.md"
$reportContent = @"
# Technical Debt Report - $dateStamp

## Summary

| Metric | Value | Status |
|--------|-------|--------|
| Build Errors | $errorCount | $(if ($errorCount -eq 0) { '✅' } else { '❌' }) |
| Build Warnings | $warningCount | $(if ($warningCount -eq 0) { '✅' } else { '⚠️' }) |
| return null Count | $returnNullCount | $(if ($returnNullCount -lt 50) { '✅' } else { '⚠️' }) |
| ! Operator Count | $nullForgivingCount | $(if ($nullForgivingCount -lt 500) { '✅' } else { '⚠️' }) |
| TODO Comments | $todoCount | $(if ($todoCount -lt 10) { '✅' } else { '⚠️' }) |
| Empty Catch Blocks | $emptyCatchCount | $(if ($emptyCatchCount -eq 0) { '✅' } else { '❌' }) |
| Test Pass Rate | $(if ($totalTests -gt 0) { [math]::Round(($passedTests / $totalTests) * 100, 2) } else { 0 })% | $(if ($failedTests -eq 0) { '✅' } else { '❌' }) |
| Overall Score | $overallScore/100 | Grade: $grade |

## Build Status

- **Errors:** $errorCount
- **Warnings:** $warningCount

## Code Quality

- **return null statements:** $returnNullCount (Target: <50)
- **Null-forgiving operators:** $nullForgivingCount (Target: <500)
- **TODO/FIXME comments:** $todoCount (Target: <10)
- **Empty catch blocks:** $emptyCatchCount (Target: 0)

## Test Results

- **Total Tests:** $totalTests
- **Passed:** $passedTests
- **Failed:** $failedTests
- **Skipped:** $skippedTests
- **Pass Rate:** $(if ($totalTests -gt 0) { [math]::Round(($passedTests / $totalTests) * 100, 2) } else { 0 })%

## Recommendations

$(if ($failedTests -gt 0) { "- ⚠️ Fix failing tests before proceeding with new features`n" })
$(if ($returnNullCount -gt 50) { "- 📝 Migrate return null statements to Result pattern`n" })
$(if ($nullForgivingCount -gt 500) { "- 🔍 Reduce null-forgiving operator usage`n" })
$(if ($todoCount -gt 10) { "- 📋 Address TODO comments`n" })
$(if ($warningCount -gt 0) { "- ⚠️ Fix build warnings`n" })

---
*Report generated by TrackTechnicalDebt.ps1*
"@

$reportContent | Out-File -FilePath $reportPath -Encoding UTF8
Write-Host "  📝 Report saved to: $reportPath" -ForegroundColor Green

#endregion

Write-Host "`n" -NoNewline
Write-Host "╔══════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║              Analysis Complete!                              ║" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""
