# AOT gate: for each target framework
#   1. builds MongoDB.Bson and MongoDB.Driver with the trim/AOT analyzers (IsAotCompatible) and fails if any
#      IL2xxx/IL3xxx code is reported more often than recorded in aot-warnings-baseline.json ("library"),
#   2. publishes tests/AotSmokeTests with Native AOT and fails if any code occurs more often than recorded, and
#   3. runs the published app (no workarounds) and fails if a step fails that is not a recorded known failure.
#
#   pwsh tests/AotSmokeTests/check-aot-warnings.ps1            # check against the baseline
#   pwsh tests/AotSmokeTests/check-aot-warnings.ps1 -Update    # record current counts/failures as the new baseline
#   pwsh tests/AotSmokeTests/check-aot-warnings.ps1 -NoRun     # warnings only (no MongoDB server needed)
#
# Warning counts and known failures are only allowed to fall. When they do, run with -Update and commit both
# aot-warnings-baseline.json and the regenerated aot-warnings-baseline.txt.
# The smoke app connects to MONGODB_URI (default mongodb://localhost:27017).
param(
    [string[]]$Frameworks = @('net8.0', 'net10.0'),
    [string]$Runtime = 'win-x64',
    [switch]$Update,
    [switch]$NoRun
)

$ErrorActionPreference = 'Stop'
$projectDir = $PSScriptRoot
$baselinePath = Join-Path $projectDir 'aot-warnings-baseline.json'
$logDir = Join-Path ([System.IO.Path]::GetTempPath()) 'aot-gate'
New-Item -ItemType Directory -Force $logDir | Out-Null

# The .NET 8 ILCompiler locates the MSVC linker with vswhere.exe, which is not on PATH by default.
if ($IsWindows) {
    $installer = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer'
    if ((Test-Path $installer) -and ($env:PATH -notlike "*$installer*")) {
        $env:PATH = "$installer;$env:PATH"
    }
}

# ILC (publish) diagnostics read "Trim/AOT analysis warning"; Roslyn analyzer diagnostics in a library build do not
$warningPattern = '(Trim|AOT) analysis warning (?<code>IL\d{4}):'
$libraryWarningPattern = 'warning (?<code>IL\d{4}):'
$current = [ordered]@{}
$driverProject = Join-Path $projectDir '..\..\src\MongoDB.Driver\MongoDB.Driver.csproj'

function Get-WarningCounts([string]$log, [string]$pattern, [switch]$Unique) {
    $lines = Get-Content $log | Where-Object { $_ -match $pattern }
    if ($Unique) {
        # a library build can log the same diagnostic more than once
        $lines = $lines | ForEach-Object { $_ -replace '^\s*\d+>', '' } | Sort-Object -Unique
    }
    $counts = @{}
    foreach ($line in $lines) {
        if ($line -match $pattern) {
            $counts[$Matches.code] = 1 + ($counts[$Matches.code] ?? 0)
        }
    }
    $byCode = [ordered]@{}
    foreach ($code in ($counts.Keys | Sort-Object)) { $byCode[$code] = $counts[$code] }
    $total = 0
    foreach ($value in $counts.Values) { $total += $value }
    return [ordered]@{ total = $total; byCode = $byCode }
}

foreach ($tfm in $Frameworks) {
    $libraryLog = Join-Path $logDir "$tfm-library.log"
    Write-Host "Building MongoDB.Bson and MongoDB.Driver ($tfm) with the AOT analyzers..."
    dotnet build $driverProject -c Release -f $tfm --no-incremental -nologo "-flp:warningsonly;logfile=$libraryLog" -clp:ErrorsOnly
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet build failed for $tfm"
    }
    $library = Get-WarningCounts $libraryLog $libraryWarningPattern -Unique

    # ILC only reports warnings when it actually compiles, so remove its intermediate output first.
    foreach ($dir in @("obj\Release\$tfm\$Runtime", "bin\Release\$tfm\$Runtime")) {
        $path = Join-Path $projectDir $dir
        if (Test-Path $path) { Remove-Item -Recurse -Force $path }
    }

    $log = Join-Path $logDir "$tfm-warn.log"
    Write-Host "Publishing $tfm ($Runtime)..."
    dotnet publish $projectDir -c Release -f $tfm -r $Runtime -nologo "-flp:warningsonly;logfile=$log" -clp:ErrorsOnly
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed for $tfm"
    }

    $published = Get-WarningCounts $log $warningPattern

    $failedSteps = @()
    if (-not $NoRun) {
        $exe = Join-Path $projectDir "bin\Release\$tfm\$Runtime\publish\AotSmokeTests$(if ($IsWindows) { '.exe' })"
        $runLog = Join-Path $logDir "run-$tfm-w0.txt"
        Write-Host "Running $tfm smoke app..."
        $env:AOT_SMOKE_WORKAROUNDS = '0'
        & $exe *>&1 | Out-File -FilePath $runLog -Encoding utf8
        $steps = Get-Content $runLog | Where-Object { $_ -match '^(PASS|FAIL) ' }
        if (-not $steps) {
            throw "The $tfm smoke app produced no PASS/FAIL lines; see $runLog"
        }
        $failedSteps = @($steps | Where-Object { $_ -match '^FAIL (?<name>[^:]+):' } | ForEach-Object { $Matches.name } | Sort-Object)
    }

    $current[$tfm] = [ordered]@{ total = $published.total; byCode = $published.byCode; knownFailures = $failedSteps; library = $library }
}

if ($Update) {
    if ($NoRun -and (Test-Path $baselinePath)) {
        # keep the recorded known failures when the smoke app was not run
        $previous = Get-Content $baselinePath -Raw | ConvertFrom-Json -AsHashtable
        foreach ($tfm in $Frameworks) {
            if ($previous[$tfm]) { $current[$tfm].knownFailures = @($previous[$tfm].knownFailures) }
        }
    }
    $current | ConvertTo-Json -Depth 5 | Set-Content -Path $baselinePath -Encoding utf8NoBOM
    Write-Host "Baseline updated: $baselinePath"

    $summarize = Join-Path $projectDir 'summarize-aot-warnings.ps1'
    $repoRoot = (Resolve-Path (Join-Path $projectDir '..\..')).Path
    & $summarize -ScratchDir $logDir -RepoRoot $repoRoot -OutFile (Join-Path $projectDir 'aot-warnings-baseline.txt')
    Write-Host "Summary regenerated: $(Join-Path $projectDir 'aot-warnings-baseline.txt')"
    exit 0
}

if (-not (Test-Path $baselinePath)) {
    throw "No baseline at $baselinePath. Run with -Update to create it."
}

$baseline = Get-Content $baselinePath -Raw | ConvertFrom-Json -AsHashtable

function Compare-Counts($baseCounts, $nowCounts) {
    $result = @{ Failed = $false; Improved = $false }
    $codes = @($baseCounts.Keys) + @($nowCounts.Keys) | Sort-Object -Unique
    foreach ($code in $codes) {
        $was = $baseCounts[$code] ?? 0
        $is = $nowCounts[$code] ?? 0
        $status = ''
        if ($is -gt $was) { $status = 'UP'; $result.Failed = $true }
        elseif ($is -lt $was) { $status = 'down'; $result.Improved = $true }
        Write-Host ('  {0}  {1,5} -> {2,5}  {3}' -f $code, $was, $is, $status)
    }
    return $result
}
$failed = $false
$improved = $false

foreach ($tfm in $Frameworks) {
    $base = $baseline[$tfm]
    if (-not $base) {
        Write-Host "[$tfm] no baseline entry; run with -Update." -ForegroundColor Yellow
        $failed = $true
        continue
    }

    $now = $current[$tfm]
    Write-Host ''
    if (-not $base.library) {
        Write-Host "[$tfm] no library baseline; run with -Update." -ForegroundColor Yellow
        $failed = $true
    }
    else {
        Write-Host ("[{0}] library analyzer warnings: {1} (baseline {2})" -f $tfm, $now.library.total, $base.library.total)
        $result = Compare-Counts $base.library.byCode $now.library.byCode
        if ($result.Failed) { $failed = $true }
        if ($result.Improved) { $improved = $true }
    }
    Write-Host ("[{0}] published app warnings: {1} (baseline {2})" -f $tfm, $now.total, $base.total)
    $result = Compare-Counts $base.byCode $now.byCode
    if ($result.Failed) { $failed = $true }
    if ($result.Improved) { $improved = $true }

    if (-not $NoRun) {
        $known = @($base.knownFailures)
        foreach ($step in $now.knownFailures) {
            if ($known -notcontains $step) {
                Write-Host "  NEW FAILURE: $step" -ForegroundColor Red
                $failed = $true
            }
            else {
                Write-Host "  known failure: $step"
            }
        }
        foreach ($step in $known) {
            if ($now.knownFailures -notcontains $step) {
                Write-Host "  now passing: $step" -ForegroundColor Green
                $improved = $true
            }
        }
    }
}

Write-Host ''
if ($failed) {
    Write-Host "AOT gate FAILED. Publish logs and smoke output are in $logDir" -ForegroundColor Red
    exit 1
}
if ($improved) {
    Write-Host 'AOT gate passed and things improved. Run with -Update and commit the new baseline.' -ForegroundColor Green
}
else {
    Write-Host 'AOT gate passed.' -ForegroundColor Green
}
exit 0
