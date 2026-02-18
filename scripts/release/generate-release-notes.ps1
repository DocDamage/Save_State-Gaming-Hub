#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Generates release notes from git commits and PRs.
.DESCRIPTION
    Analyzes git history to generate structured release notes in Markdown format.
.PARAMETER Version
    The version number for this release (e.g., "2.4.0")
.PARAMETER FromTag
    The git tag to generate notes from (e.g., "v2.3.0")
.PARAMETER ToRef
    The git ref to generate notes to (default: HEAD)
.PARAMETER OutputPath
    Path to write the release notes file
.EXAMPLE
    .\generate-release-notes.ps1 -Version "2.4.0" -FromTag "v2.3.0"
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Version,
    
    [Parameter(Mandatory = $true)]
    [string]$FromTag,
    
    [string]$ToRef = "HEAD",
    
    [string]$OutputPath = "RELEASE_NOTES.md"
)

$ErrorActionPreference = "Stop"

Write-Host "Generating Release Notes for v$Version" -ForegroundColor Cyan
Write-Host "From: $FromTag To: $ToRef" -ForegroundColor Gray
Write-Host ""

# Ensure we're in a git repository
$gitRoot = git rev-parse --show-toplevel 2>$null
if (-not $gitRoot) {
    Write-Error "Not in a git repository"
    exit 1
}

Set-Location $gitRoot

# Get commit log
$commitLog = git log "$FromTag..$ToRef" --pretty=format:"%H|%s|%b|%an|%ad" --date=short 2>$null

if (-not $commitLog) {
    Write-Error "No commits found between $FromTag and $ToRef"
    exit 1
}

# Parse commits
$commits = @()
foreach ($line in $commitLog -split "\n") {
    $parts = $line -split "\|"
    if ($parts.Count -ge 5) {
        $commits += [PSCustomObject]@{
            Hash = $parts[0].Substring(0, 7)
            Subject = $parts[1]
            Body = $parts[2]
            Author = $parts[3]
            Date = $parts[4]
            Type = ""
            Scope = ""
            IsBreaking = $false
        }
    }
}

# Categorize commits based on conventional commit format
$categories = @{
    "Features" = @()
    "Bug Fixes" = @()
    "Performance" = @()
    "Documentation" = @()
    "Refactoring" = @()
    "Tests" = @()
    "Build" = @()
    "Other" = @()
}

foreach ($commit in $commits) {
    $subject = $commit.Subject
    
    # Check for breaking changes
    if ($subject -match "!:" -or $commit.Body -match "BREAKING CHANGE") {
        $commit.IsBreaking = $true
    }
    
    # Parse conventional commit format
    if ($subject -match "^(feat|fix|perf|docs|refactor|test|build|ci|chore)(\(([^)]+)\))?(!)?:(.+)$") {
        $commit.Type = $matches[1]
        $commit.Scope = $matches[3]
        $subject = $matches[5].Trim()
    }
    
    # Categorize
    switch ($commit.Type) {
        "feat" { $categories["Features"] += $commit }
        "fix" { $categories["Bug Fixes"] += $commit }
        "perf" { $categories["Performance"] += $commit }
        "docs" { $categories["Documentation"] += $commit }
        "refactor" { $categories["Refactoring"] += $commit }
        "test" { $categories["Tests"] += $commit }
        "build" { $categories["Build"] += $commit }
        "ci" { $categories["Build"] += $commit }
        default { $categories["Other"] += $commit }
    }
}

# Get contributors
$contributors = $commits | Select-Object -Property Author -Unique | ForEach-Object { $_.Author }

# Generate release notes
$sb = New-Object System.Text.StringBuilder

[void]$sb.AppendLine("# Release Notes - v$Version")
[void]$sb.AppendLine("")
[void]$sb.AppendLine("**Release Date:** $(Get-Date -Format 'yyyy-MM-dd')")
[void]$sb.AppendLine("")
[void]$sb.AppendLine("## Summary")
[void]$sb.AppendLine("")
[void]$sb.AppendLine("- **Total Commits:** $($commits.Count)")
[void]$sb.AppendLine("- **Contributors:** $($contributors.Count)")
[void]$sb.AppendLine("")

# Breaking changes
$breakingChanges = $commits | Where-Object { $_.IsBreaking }
if ($breakingChanges) {
    [void]$sb.AppendLine("## ⚠️ Breaking Changes")
    [void]$sb.AppendLine("")
    foreach ($change in $breakingChanges) {
        [void]$sb.AppendLine("- **$($change.Scope):** $($change.Subject)")
    }
    [void]$sb.AppendLine("")
}

# Categories
foreach ($category in $categories.Keys | Sort-Object) {
    $items = $categories[$category]
    if ($items.Count -gt 0) {
        [void]$sb.AppendLine("## $category")
        [void]$sb.AppendLine("")
        
        foreach ($item in $items | Sort-Object Scope) {
            $scope = if ($item.Scope) { "**$($item.Scope):** " } else { "" }
            [void]$sb.AppendLine("- $scope$($item.Subject) ($($item.Hash))")
        }
        
        [void]$sb.AppendLine("")
    }
}

# Contributors
[void]$sb.AppendLine("## Contributors")
[void]$sb.AppendLine("")
$contributors | Sort-Object | ForEach-Object { [void]$sb.AppendLine("- $_") }
[void]$sb.AppendLine("")

# Full changelog link
[void]$sb.AppendLine("## Full Changelog")
[void]$sb.AppendLine("")
[void]$sb.AppendLine("**Full Changelog:** [$FromTag...v$Version](../../compare/$FromTag...v$Version)")
[void]$sb.AppendLine("")

# Write to file
$releaseNotes = $sb.ToString()
$releaseNotes | Out-File -FilePath $OutputPath -Encoding UTF8

Write-Host "Release notes generated: $OutputPath" -ForegroundColor Green
Write-Host ""
Write-Host "Preview:" -ForegroundColor Cyan
Write-Host "---"
Write-Host $releaseNotes.Substring(0, [Math]::Min(1000, $releaseNotes.Length))
Write-Host "---"

# Output the path for CI/CD
Write-Output "::set-output name=release_notes_path::$OutputPath"
