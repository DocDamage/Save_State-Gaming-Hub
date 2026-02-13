#Requires -Version 5.1
# TrackTechnicalDebt.ps1 - Simplified version for PowerShell 5.1
# Tracks technical debt metrics for SaveStateReborn project

[CmdletBinding()]
param(
    [switch]$ExportCsv,
    [string]$OutputPath = "$PSScriptRoot\.."
)

$ErrorActionPreference = 'Continue'
$projectRoot = Resolve-Path "$PSScriptRoot\.."
$srcPath = Join-Path $projectRoot "src"
$timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
$dateStamp = Get-Date -Format "yyyy-MM-dd"

Write-Host "========================================"
Write-Host "  Technical Debt Tracker"
Write-Host "  Report Date: $dateStamp"
Write-Host "========================================"
Write-Host ""
Write-Host "Project Root: $projectRoot"
Write-Host ""

# Count return null statements
Write-Host "Analyzing code patterns..." -ForegroundColor Yellow
$nullFiles = Get-ChildItem -Path $srcPath -Recurse -Filter "*.cs" -ErrorAction SilentlyContinue
$nullMatches = $nullFiles | Select-String -Pattern "return\s+null;" -ErrorAction SilentlyContinue
$returnNullCount = $nullMatches.Count

# Count null-forgiving operators
$forgivingFiles = Get-ChildItem -Path $srcPath -Recurse -Filter "*.cs" -ErrorAction SilentlyContinue
$forgivingMatches = $forgivingFiles | Select-String -Pattern "!\.|!\[" -ErrorAction SilentlyContinue
$nullForgivingCount = $forgivingMatches.Count

# Count TODO comments
$todoFiles = Get-ChildItem -Path $srcPath -Recurse -Filter "*.cs" -ErrorAction SilentlyContinue
$todoMatches = $todoFiles | Select-String -Pattern "TODO|FIXME|HACK" -ErrorAction SilentlyContinue
$todoCount = $todoMatches.Count

# Count empty catch blocks
$catchFiles = Get-ChildItem -Path $srcPath -Recurse -Filter "*.cs" -ErrorAction SilentlyContinue
$catchMatches = $catchFiles | Select-String -Pattern "catch\s*\([^)]*\)\s*\{\s*\}" -ErrorAction SilentlyContinue
$emptyCatchCount = $catchMatches.Count

# Get file stats
$totalFiles = (Get-ChildItem -Path $srcPath -Recurse -Filter "*.cs" -ErrorAction SilentlyContinue).Count

Write-Host "CODE PATTERN ANALYSIS"
Write-Host "--------------------"
Write-Host "Total C# Files: $totalFiles"
Write-Host "'return null' statements: $returnNullCount"
Write-Host "Null-forgiving operators: $nullForgivingCount"
Write-Host "TODO/FIXME comments: $todoCount"
Write-Host "Empty catch blocks: $emptyCatchCount"
Write-Host ""

# Build status
Write-Host "BUILD STATUS"
Write-Host "------------"
Write-Host "Running build analysis..." -ForegroundColor Yellow
$buildResult = & dotnet build "$projectRoot\SaveStateReborn.sln" --verbosity minimal 2>&1
$buildErrors = $buildResult | Select-String "0 Error\(s\)"
$buildWarnings = $buildResult | Select-String "0 Warning\(s\)"

if ($buildErrors) {
    Write-Host "Build Errors: 0" -ForegroundColor Green
} else {
    Write-Host "Build Errors: DETECTED" -ForegroundColor Red
}

if ($buildWarnings) {
    Write-Host "Build Warnings: 0" -ForegroundColor Green
} else {
    Write-Host "Build Warnings: DETECTED" -ForegroundColor Yellow
}
Write-Host ""

# Test status (quick check)
Write-Host "TEST STATUS (Core Projects)"
Write-Host "---------------------------"

$testProjects = @(
    "SaveState.Core.Tests",
    "SaveState.Application.Tests",
    "SaveState.Infrastructure.Tests"
)

foreach ($project in $testProjects) {
    $projectPath = Join-Path "$projectRoot\tests" $project
    if (Test-Path $projectPath) {
        Write-Host "Testing $project..." -NoNewline -ForegroundColor Gray
        $testResult = & dotnet test $projectPath --verbosity minimal --no-build 2>&1 | Out-String
        
        if ($testResult -match "Failed:\s+0") {
            Write-Host " PASS" -ForegroundColor Green
        } else {
            Write-Host " FAIL/UNKNOWN" -ForegroundColor Yellow
        }
    }
}

Write-Host ""

# Summary
Write-Host "SUMMARY"
Write-Host "-------"

$overallScore = 100
$overallScore -= [math]::Min(30, $returnNullCount / 10)
$overallScore -= [math]::Min(30, $nullForgivingCount / 100)
$overallScore -= [math]::Min(20, $todoCount)
$overallScore -= [math]::Min(10, $emptyCatchCount * 5)
$overallScore = [math]::Max(0, [math]::Min(100, $overallScore))

$grade = if ($overallScore -ge 90) { 'A' } elseif ($overallScore -ge 80) { 'B' } elseif ($overallScore -ge 70) { 'C' } elseif ($overallScore -ge 60) { 'D' } else { 'F' }

$gradeColor = if ($grade -eq 'A' -or $grade -eq 'B') { 'Green' } elseif ($grade -eq 'C') { 'Yellow' } else { 'Red' }

Write-Host "Overall Health Score: $overallScore/100" -ForegroundColor $gradeColor
Write-Host "Grade: $grade" -ForegroundColor $gradeColor
Write-Host ""

# Export to CSV if requested
if ($ExportCsv) {
    $csvPath = Join-Path $OutputPath "metrics-$dateStamp.csv"
    $csvData = New-Object PSObject -Property @{
        Date = $timestamp
        ReturnNullCount = $returnNullCount
        NullForgivingCount = $nullForgivingCount
        TodoCount = $todoCount
        EmptyCatchCount = $emptyCatchCount
        OverallScore = $overallScore
        Grade = $grade
    }
    
    $csvData | Export-Csv -Path $csvPath -NoTypeInformation -Append
    Write-Host "Metrics exported to: $csvPath" -ForegroundColor Green
}

# Save report
$reportPath = Join-Path $OutputPath "TECHNICAL_DEBT_REPORT-$dateStamp.md"
$reportContent = @"
# Technical Debt Report - $dateStamp

## Summary

| Metric | Value | Status |
|--------|-------|--------|
| return null Count | $returnNullCount | $(if ($returnNullCount -lt 50) { 'OK' } else { 'NEEDS WORK' }) |
| ! Operator Count | $nullForgivingCount | $(if ($nullForgivingCount -lt 500) { 'OK' } else { 'NEEDS WORK' }) |
| TODO Comments | $todoCount | $(if ($todoCount -lt 10) { 'OK' } else { 'NEEDS WORK' }) |
| Empty Catch Blocks | $emptyCatchCount | $(if ($emptyCatchCount -eq 0) { 'OK' } else { 'NEEDS WORK' }) |
| Overall Score | $overallScore/100 | Grade: $grade |

## Recommendations

$(if ($returnNullCount -gt 50) { "- Migrate return null statements to Result pattern`n" })
$(if ($nullForgivingCount -gt 500) { "- Reduce null-forgiving operator usage`n" })
$(if ($todoCount -gt 10) { "- Address TODO comments`n" })
$(if ($emptyCatchCount -gt 0) { "- Fix empty catch blocks`n" })

---
Generated: $timestamp
"@

$reportContent | Out-File -FilePath $reportPath -Encoding UTF8
Write-Host "Report saved to: $reportPath" -ForegroundColor Green

Write-Host ""
Write-Host "========================================"
Write-Host "  Analysis Complete!"
Write-Host "========================================"
