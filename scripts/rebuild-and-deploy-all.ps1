param(
    [string]$Repo = "SelDanilEv/Defender.MonoRepo",
    [string]$Ref = "main",
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

function Invoke-Gh {
    param([string[]]$Arguments)

    $command = "gh " + (($Arguments | ForEach-Object {
        if ($_ -match "\s") { "`"$_`"" } else { $_ }
    }) -join " ")
    Write-Host $command

    if (-not $DryRun) {
        & gh @Arguments
    }
}

if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    throw "GitHub CLI 'gh' is required."
}

if ($Ref -match "^[a-fA-F0-9]{40}$") {
    $fullSha = $Ref
} else {
    & git fetch origin $Ref | Out-Null
    $fullSha = (& git rev-parse "origin/$Ref").Trim()
}

$sha = $fullSha.Substring(0, 7)
$imageTag = "sha-$sha"

function Get-WorkflowRuns {
    @(& gh run list `
        "--repo", $Repo, `
        "--workflow", "Build and Publish Docker Images", `
        "--branch", $Ref, `
        "--limit", "100", `
        "--json", "databaseId,headSha,event,createdAt" |
        ConvertFrom-Json)
}

$existingRunIds = @(
    Get-WorkflowRuns |
        Where-Object { $_.headSha -eq $fullSha -and $_.event -eq "workflow_dispatch" } |
        ForEach-Object { $_.databaseId }
)

Write-Host "Dispatching rebuild for all services on '$Ref'."
Invoke-Gh @(
    "workflow", "run", "Build and Publish Docker Images",
    "--repo", $Repo,
    "--ref", $Ref,
    "-f", "service=ALL",
    "-f", "force_build=true"
)

if ($DryRun) {
    Write-Host "Would wait for successful Build and Publish Docker Images run before promotion."
} else {
    $buildRun = $null
    for ($attempt = 1; $attempt -le 30 -and $null -eq $buildRun; $attempt++) {
        Start-Sleep -Seconds 2
        $buildRun = Get-WorkflowRuns |
            Where-Object {
                $_.headSha -eq $fullSha -and
                $_.event -eq "workflow_dispatch" -and
                $_.databaseId -notin $existingRunIds
            } |
            Sort-Object createdAt -Descending |
            Select-Object -First 1
    }

    if ($null -eq $buildRun) {
        throw "Timed out waiting for the dispatched Docker build run. Image promotion was not started."
    }

    Write-Host "Waiting for Docker build run $($buildRun.databaseId)."
    Invoke-Gh @("run", "watch", "$($buildRun.databaseId)", "--repo", $Repo, "--exit-status")
}

Write-Host "Dispatching deploy for all services with image tag '$imageTag'."
Invoke-Gh @(
    "workflow", "run", "Promote Image Tag",
    "--repo", $Repo,
    "--ref", $Ref,
    "-f", "service=ALL",
    "-f", "image_tag=$imageTag"
)

Write-Host "Build completed and deployment promotion was dispatched."
