# Performance Analysis Script for SaveState Reborn
# This script runs various performance tests and generates reports

param(
    [string]$OutputPath = "performance-results",
    [switch]$IncludeMemoryAnalysis,
    [switch]$IncludeDatabaseAnalysis,
    [switch]$IncludeAsyncAnalysis
)

# Create output directory
if (!(Test-Path $OutputPath)) {
    New-Item -ItemType Directory -Path $OutputPath | Out-Null
}

Write-Host "🚀 Starting Performance Analysis..." -ForegroundColor Green
Write-Host "Output Path: $OutputPath" -ForegroundColor Yellow

# Function to measure execution time
function Measure-ExecutionTime {
    param([scriptblock]$ScriptBlock)

    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    try {
        & $ScriptBlock
    }
    finally {
        $stopwatch.Stop()
        $executionTime = $stopwatch.Elapsed
        Write-Host "Execution Time: $($executionTime.TotalMilliseconds) ms" -ForegroundColor Cyan
        return $executionTime
    }
}

# 1. Build Performance Analysis
Write-Host "`n📦 Testing Build Performance..." -ForegroundColor Magenta
$buildTime = Measure-ExecutionTime {
    dotnet build SaveStateReborn.sln --verbosity quiet --configuration Release
}

# 2. Test Execution Performance
Write-Host "`n🧪 Testing Unit Test Performance..." -ForegroundColor Magenta
$testTime = Measure-ExecutionTime {
    dotnet test SaveStateReborn.sln --verbosity quiet --configuration Release --no-build
}

# 3. Memory Usage Analysis (if requested)
if ($IncludeMemoryAnalysis) {
    Write-Host "`n🧠 Analyzing Memory Usage..." -ForegroundColor Magenta

    # Start memory monitoring
    $process = Start-Process -FilePath "dotnet" -ArgumentList "run --project src/SaveState.Presentation/SaveState.Presentation.csproj" -PassThru
    Start-Sleep -Seconds 5

    # Get memory info
    $memoryInfo = Get-Process -Id $process.Id | Select-Object -Property Id, Name, WorkingSet64, PrivateMemorySize64, VirtualMemorySize64
    $memoryInfo | Export-Csv -Path "$OutputPath/memory-analysis.csv" -NoTypeInformation

    # Stop the process
    Stop-Process -Id $process.Id -Force

    Write-Host "Memory analysis saved to: $OutputPath/memory-analysis.csv" -ForegroundColor Green
}

# 4. Database Performance Analysis (if requested)
if ($IncludeDatabaseAnalysis) {
    Write-Host "`n🗄️ Analyzing Database Performance..." -ForegroundColor Magenta

    # Run database load tests
    try {
        dotnet test tests/SaveState.LoadTests/SaveState.LoadTests.csproj --verbosity normal --logger "trx;LogFileName=$OutputPath/database-tests.trx"
    } catch {
        Write-Host "Database tests completed with results in: $OutputPath/database-tests.trx" -ForegroundColor Yellow
    }
}

# 5. Generate Performance Report
Write-Host "`n📊 Generating Performance Report..." -ForegroundColor Magenta

$report = @"
# SaveState Reborn Performance Analysis Report
Generated: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")

## Build Performance
- Build Time: $($buildTime.TotalSeconds) seconds
- Build Status: $(if ($LASTEXITCODE -eq 0) { "SUCCESS" } else { "FAILED" })

## Test Performance
- Test Execution Time: $($testTime.TotalSeconds) seconds
- Test Status: $(if ($LASTEXITCODE -eq 0) { "SUCCESS" } else { "FAILED" })

## System Information
- .NET Version: $(dotnet --version)
- OS: $([System.Environment]::OSVersion.VersionString)
- Processor Count: $([System.Environment]::ProcessorCount)
- Total Memory: $([math]::Round((Get-CimInstance Win32_ComputerSystem).TotalPhysicalMemory / 1GB, 2)) GB

## Recommendations

"@

if ($buildTime.TotalSeconds -gt 60) {
    $report += "- Consider optimizing build performance (build took > 1 minute)`n"
}

if ($testTime.TotalSeconds -gt 120) {
    $report += "- Consider optimizing test execution (tests took > 2 minutes)`n"
}

$report += @"
- Review memory usage patterns in production
- Monitor database query performance
- Implement caching for frequently accessed data
- Consider async optimizations for I/O operations

## Next Steps
1. Run benchmarks with BenchmarkDotNet for detailed metrics
2. Profile memory allocations with dotMemory or similar tools
3. Analyze database queries with EF Core logging
4. Implement performance monitoring in production
"@

$report | Out-File -FilePath "$OutputPath/performance-report.md" -Encoding UTF8

Write-Host "`n✅ Performance Analysis Complete!" -ForegroundColor Green
Write-Host "📁 Results saved to: $OutputPath" -ForegroundColor Yellow
Write-Host "📄 Report: $OutputPath/performance-report.md" -ForegroundColor Yellow