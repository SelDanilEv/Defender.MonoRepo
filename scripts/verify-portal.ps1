[CmdletBinding()]
param(
    [string]$TestPath,
    [switch]$IncludeE2E,
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$clientRoot = Join-Path $repoRoot "src\Defender.Portal\src\WebUI\ClientApp"
$npm = if ($IsLinux -or $IsMacOS) { "npm" } else { "npm.cmd" }
$results = [System.Collections.Generic.List[object]]::new()
$started = Get-Date

function Invoke-CompactStep {
    param(
        [string]$Label,
        [string]$FilePath,
        [string[]]$Arguments
    )

    $log = Join-Path ([System.IO.Path]::GetTempPath()) ("portal-{0}-{1}.log" -f $Label, [guid]::NewGuid())
    $stepStarted = Get-Date
    Push-Location $clientRoot
    try {
        & $FilePath @Arguments *> $log
        $exitCode = $LASTEXITCODE
    } finally {
        Pop-Location
    }

    $seconds = [math]::Round(((Get-Date) - $stepStarted).TotalSeconds, 1)
    if ($exitCode -ne 0) {
        Write-Host "PORTAL_VERIFY FAIL step=$Label exit=$exitCode duration=${seconds}s" -ForegroundColor Red
        Get-Content -LiteralPath $log -Tail 80
        Remove-Item -LiteralPath $log -Force -ErrorAction SilentlyContinue
        exit $exitCode
    }

    $results.Add([pscustomobject]@{ Label = $Label; Seconds = $seconds })
    Remove-Item -LiteralPath $log -Force -ErrorAction SilentlyContinue
}

if ($TestPath) {
    Invoke-CompactStep "test-target" $npm @("test", "--", $TestPath)
} else {
    Invoke-CompactStep "typecheck" $npm @("run", "typecheck")
    Invoke-CompactStep "lint" $npm @("run", "lint", "--", "--max-warnings=0")
    Invoke-CompactStep "tests" $npm @("test")
    if (-not $SkipBuild) {
        Invoke-CompactStep "build" $npm @("run", "build")
    }
    Invoke-CompactStep "audit" $npm @("audit", "--audit-level=high")
}

if ($IncludeE2E) {
    Invoke-CompactStep "e2e" $npm @("run", "test:e2e")
}

$total = [math]::Round(((Get-Date) - $started).TotalSeconds, 1)
$steps = ($results | ForEach-Object { "$($_.Label)=$($_.Seconds)s" }) -join " "
Write-Host "PORTAL_VERIFY PASS total=${total}s" -ForegroundColor Green
Write-Host $steps
