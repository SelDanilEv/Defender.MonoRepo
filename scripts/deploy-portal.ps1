[CmdletBinding()]
param(
    [string]$Repo = "SelDanilEv/Defender.MonoRepo",
    [string]$Ref = "main",
    [string]$HomeServerRoot = "E:\MyApps\Defender.HomeServer",
    [int]$TimeoutMinutes = 15,
    [switch]$SkipLiveCheck,
    [switch]$Execute
)

$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$liveScript = Join-Path $PSScriptRoot "portal-live-status.py"

function Require-Command([string]$Name) {
    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "Required command '$Name' was not found."
    }
}

function Invoke-SilentNative([scriptblock]$Command) {
    $previousPreference = $ErrorActionPreference
    try {
        $ErrorActionPreference = "Continue"
        & $Command *> $null
        return $LASTEXITCODE
    } finally {
        $ErrorActionPreference = $previousPreference
    }
}

function Get-Runs([string]$Workflow) {
    $runs = & gh run list --repo $Repo --workflow $Workflow --branch $Ref --limit 30 --json databaseId,headSha,event,createdAt,status,conclusion | ConvertFrom-Json
    foreach ($run in $runs) {
        $run
    }
}

function Wait-NewRun {
    param([string]$Workflow, [string]$HeadSha, [long[]]$ExistingIds)
    $deadline = (Get-Date).AddMinutes($TimeoutMinutes)
    do {
        Start-Sleep -Seconds 2
        $run = Get-Runs $Workflow |
            Where-Object { $_.headSha -eq $HeadSha -and $_.event -eq "workflow_dispatch" -and $_.databaseId -notin $ExistingIds } |
            Sort-Object createdAt -Descending |
            Select-Object -First 1
        if ($run) { return $run }
    } while ((Get-Date) -lt $deadline)
    throw "Timed out waiting for workflow '$Workflow'."
}

function Watch-Run([long]$RunId, [string]$Label) {
    $log = Join-Path ([System.IO.Path]::GetTempPath()) ("portal-deploy-{0}-{1}.log" -f $Label, [guid]::NewGuid())
    $previousPreference = $ErrorActionPreference
    try {
        $ErrorActionPreference = "Continue"
        & gh run watch "$RunId" --repo $Repo --exit-status --interval 10 *> $log
        $exitCode = $LASTEXITCODE
    } finally {
        $ErrorActionPreference = $previousPreference
    }
    if ($exitCode -ne 0) {
        Write-Host "PORTAL_DEPLOY FAIL step=$Label run=$RunId" -ForegroundColor Red
        Get-Content -LiteralPath $log -Tail 100
        Remove-Item -LiteralPath $log -Force -ErrorAction SilentlyContinue
        exit 1
    }
    Remove-Item -LiteralPath $log -Force -ErrorAction SilentlyContinue
    Write-Host "PORTAL_DEPLOY PASS step=$Label run=$RunId"
}

Require-Command "git"
Require-Command "gh"
Push-Location $repoRoot
try {
    $branch = (& git branch --show-current).Trim()
    $headSha = (& git rev-parse HEAD).Trim()
    $shortSha = $headSha.Substring(0, 7)
    $imageTag = "sha-$shortSha"

    if (-not $Execute) {
        Write-Host "PORTAL_DEPLOY PREVIEW"
        Write-Host "repo=$Repo ref=$Ref branch=$branch head=$shortSha image=$imageTag"
        Write-Host "would=push,build Defender.Portal,promote,live-check"
        Write-Host "Run again with -Execute to mutate GitHub and ArgoCD state."
        exit 0
    }

    if ($branch -ne $Ref) { throw "Current branch '$branch' must match ref '$Ref'." }
    if (& git status --porcelain) { throw "Worktree must be clean before deployment." }

    if ((Invoke-SilentNative { & gh auth status }) -ne 0) { throw "GitHub CLI authentication failed." }

    if ((Invoke-SilentNative { & git push origin $Ref }) -ne 0) { throw "git push failed." }

    $buildWorkflow = "docker-build-publish.yml"
    $existingBuildIds = @(Get-Runs $buildWorkflow | ForEach-Object { [long]$_.databaseId })
    if ((Invoke-SilentNative { & gh workflow run $buildWorkflow --repo $Repo --ref $Ref -f service=Defender.Portal -f force_build=true }) -ne 0) {
        throw "Portal build dispatch failed."
    }
    $buildRun = Wait-NewRun $buildWorkflow $headSha $existingBuildIds
    Watch-Run $buildRun.databaseId "build"

    $promoteWorkflow = "promote-image-tag.yml"
    $existingPromoteIds = @(Get-Runs $promoteWorkflow | ForEach-Object { [long]$_.databaseId })
    if ((Invoke-SilentNative { & gh workflow run $promoteWorkflow --repo $Repo --ref $Ref -f service=Defender.Portal -f "image_tag=$imageTag" }) -ne 0) {
        throw "Portal promotion dispatch failed."
    }
    $promoteRun = Wait-NewRun $promoteWorkflow $headSha $existingPromoteIds
    Watch-Run $promoteRun.databaseId "promote"

    if (-not $SkipLiveCheck) {
        Require-Command "python"
        $liveJson = & python $liveScript --home-server-root $HomeServerRoot --expected-tag $imageTag --timeout-seconds ($TimeoutMinutes * 60)
        if ($LASTEXITCODE -ne 0) { throw "Live Portal verification failed." }
        $live = $liveJson | ConvertFrom-Json
        Write-Host "PORTAL_DEPLOY PASS live image=$($live.image) portal=$($live.portal_http) health=$($live.health_http)"
    }

    Write-Host "PORTAL_DEPLOY PASS head=$shortSha image=$imageTag build=$($buildRun.databaseId) promote=$($promoteRun.databaseId)"
} finally {
    Pop-Location
}
