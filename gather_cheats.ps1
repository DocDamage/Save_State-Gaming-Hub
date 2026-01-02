$appData = [System.Environment]::GetFolderPath('LocalApplicationData')
$kbBase = Join-Path $appData "SaveStateReborn\KnowledgeBase"
$tempBase = Join-Path $appData "SaveStateReborn\TempCheats"

if (!(Test-Path $tempBase)) { New-Item -ItemType Directory -Path $tempBase -Force }

function Convert-Cheats($url, $folderName, $subDir) {
    Write-Host "Cloning $url..."
    $clonePath = Join-Path $tempBase $folderName
    $destPath = Join-Path $kbBase $subDir
    if (Test-Path $clonePath) { Remove-Item -Path $clonePath -Recurse -Force }
    git clone --depth 1 $url $clonePath
    if (!(Test-Path $clonePath)) { return }

    $exts = @("*.cht", "*.ini", "*.txt", "*.pht", "*.pat", "*.patch")
    Get-ChildItem -Path $clonePath -Include $exts -Recurse | ForEach-Object {
        $content = Get-Content -LiteralPath $_.FullName -Raw -ErrorAction SilentlyContinue
        if ($null -eq $content) { return }
        $fileName = $_.BaseName
        $parentDir = $_.Directory.Name
        $targetDir = Join-Path $destPath $parentDir
        if (!(Test-Path $targetDir)) { New-Item -ItemType Directory -Path $targetDir -Force }
        $md = "# Chants: $fileName`n**System**: $parentDir`n**URL**: $url`n`n" + '```' + "text`n$content`n" + '```'
        Set-Content -LiteralPath (Join-Path $targetDir "$fileName.md") -Value $md -Encoding UTF8
    }
    Remove-Item -Path $clonePath -Recurse -Force
}

Convert-Cheats "https://github.com/libretro/libretro-database.git" "lsdb" "gaming\cheats\retroarch"
Convert-Cheats "https://github.com/duckstation/chtdb.git" "dsch" "gaming\cheats\psx"
Convert-Cheats "https://github.com/Saramagrean/CWCheat-Database-Plus-.git" "ppss" "gaming\cheats\psp"

Remove-Item -Path $tempBase -Recurse -Force
Write-Host "Complete."
