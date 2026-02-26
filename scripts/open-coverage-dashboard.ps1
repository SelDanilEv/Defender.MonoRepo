Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$indexFile = Join-Path $repoRoot 'artifacts\coverage\dashboard\index.html'

if (-not (Test-Path $indexFile)) {
    Write-Error "Coverage dashboard not found. Run scripts/coverage-dashboard.ps1 first."
    exit 1
}

Start-Process $indexFile
