#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Creates a new release with automated version bumping.
.DESCRIPTION
    Bumps version, generates release notes, creates git tag, and optionally creates GitHub release.
.PARAMETER BumpType
    Type of version bump: major, minor, or patch
.PARAMETER CreateGitHubRelease
    Whether to create a GitHub release (requires gh CLI)
.PARAMETER Draft
    Create as draft release
.EXAMPLE
    .\create-release.ps1 -BumpType minor
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("major", "minor", "patch")]
    [string]$BumpType,
    
    [switch]$CreateGitHubRelease,
    
    [switch]$Draft
)

$ErrorActionPreference = "Stop"

Write-Host "Creating New Release" -ForegroundColor Cyan
Write-Host "Bump Type: $BumpType" -ForegroundColor Gray
Write-Host ""

# Get current version from latest tag
$latestTag = git describe --tags --abbrev=0 2>$null
if (-not $latestTag) {
    $currentVersion = [Version]"2.3.0"
    Write-Host "No existing tags found. Starting from v2.3.0" -ForegroundColor Yellow
} else {
    $versionString = $latestTag -replace '^v', ''
    $currentVersion = [Version]$versionString
    Write-Host "Current version: v$currentVersion" -ForegroundColor Gray
}

# Calculate new version
switch ($BumpType) {
    "major" { $newVersion = [Version]"$($currentVersion.Major + 1).0.0" }
    "minor" { $newVersion = [Version]"$($currentVersion.Major).$($currentVersion.Minor + 1).0" }
    "patch" { $newVersion = [Version]"$($currentVersion.Major).$($currentVersion.Minor).$($currentVersion.Build + 1)" }
}

Write-Host "New version: v$newVersion" -ForegroundColor Green
Write-Host ""

# Confirm
$confirm = Read-Host "Continue? (y/n)"
if ($confirm -ne 'y') {
    Write-Host "Aborted." -ForegroundColor Yellow
    exit 0
}

# Generate release notes
$releaseNotesPath = "RELEASE_NOTES_v$newVersion.md"
$fromTag = if ($latestTag) { $latestTag } else { "HEAD~10" }

Write-Host "Generating release notes..." -ForegroundColor Yellow
& "$PSScriptRoot/generate-release-notes.ps1" `
    -Version $newVersion.ToString() `
    -FromTag $fromTag `
    -OutputPath $releaseNotesPath

# Create git tag
Write-Host "Creating git tag v$newVersion..." -ForegroundColor Yellow
git tag -a "v$newVersion" -m "Release v$newVersion"

Write-Host "Pushing tag to origin..." -ForegroundColor Yellow
git push origin "v$newVersion"

# Create GitHub release
if ($CreateGitHubRelease) {
    $ghExists = Get-Command gh -ErrorAction SilentlyContinue
    if (-not $ghExists) {
        Write-Warning "GitHub CLI (gh) not found. Skipping GitHub release creation."
    } else {
        Write-Host "Creating GitHub release..." -ForegroundColor Yellow
        
        $draftFlag = if ($Draft) { "--draft" } else { "" }
        
        gh release create "v$newVersion" `
            --title "v$newVersion" `
            --notes-file $releaseNotesPath `
            $draftFlag
        
        Write-Host "GitHub release created!" -ForegroundColor Green
    }
}

Write-Host ""
Write-Host "Release v$newVersion created successfully!" -ForegroundColor Green
Write-Host "Release notes: $releaseNotesPath" -ForegroundColor Gray
