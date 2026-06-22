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
    $sha = $Ref.Substring(0, 7)
} else {
    & git fetch origin $Ref | Out-Null
    $sha = (& git rev-parse --short=7 "origin/$Ref").Trim()
}

$imageTag = "sha-$sha"

Write-Host "Dispatching rebuild for all services on '$Ref'."
Invoke-Gh @(
    "workflow", "run", "Build and Publish Docker Images",
    "--repo", $Repo,
    "--ref", $Ref,
    "-f", "service=ALL",
    "-f", "force_build=true"
)

Write-Host "Dispatching deploy for all services with image tag '$imageTag'."
Invoke-Gh @(
    "workflow", "run", "Promote Image Tag",
    "--repo", $Repo,
    "--ref", $Ref,
    "-f", "service=ALL",
    "-f", "image_tag=$imageTag"
)

Write-Host "Done. Build runs continue in GitHub Actions; deploy was dispatched without waiting."
