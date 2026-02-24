[CmdletBinding()]
param(
    [string[]]$Services,
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',
    [string]$OutputRoot = 'artifacts/coverage',
    [switch]$SkipTestRun,
    [switch]$OpenReport
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$outputRootPath = Join-Path $repoRoot $OutputRoot
$rawRoot = Join-Path $outputRootPath 'raw'
$dashboardRoot = Join-Path $outputRootPath 'dashboard'

$hasLocalDotnetCoverage = $false
$hasLocalReportGenerator = $false
try {
    dotnet tool run dotnet-coverage --version *> $null
    if ($LASTEXITCODE -eq 0) { $hasLocalDotnetCoverage = $true }
}
catch { }
try {
    dotnet tool run reportgenerator --version *> $null
    if ($LASTEXITCODE -eq 0) { $hasLocalReportGenerator = $true }
}
catch { }

if (-not $SkipTestRun) {
    if (Test-Path $rawRoot) {
        Remove-Item -Path $rawRoot -Recurse -Force
    }

    $serviceDirs = @(Get-ChildItem -Path (Join-Path $repoRoot 'src') -Directory |
        Where-Object { $_.Name -like 'Defender.*' })

    if ($Services -and $Services.Count -gt 0) {
        $selected = @{}
        foreach ($name in $Services) {
            $selected[$name.ToLowerInvariant()] = $true
        }

        $serviceDirs = @($serviceDirs |
            Where-Object { $selected.ContainsKey($_.Name.ToLowerInvariant()) })

        if ($serviceDirs.Count -eq 0) {
            throw "No matching services found for: $($Services -join ', ')"
        }
    }

    foreach ($serviceDir in $serviceDirs) {
        $testProjects = @(Get-ChildItem -Path $serviceDir.FullName -Recurse -Filter *.Tests.csproj -File |
            Where-Object { $_.FullName -notmatch '\\(obj|bin)\\' })
        if ($testProjects.Count -eq 0) {
            Write-Warning "Skipping $($serviceDir.Name): no *.Tests.csproj found"
            continue
        }
        $serviceRaw = Join-Path $rawRoot $serviceDir.Name
        New-Item -ItemType Directory -Path $serviceRaw -Force | Out-Null

        foreach ($testProject in $testProjects) {
            $projectName = [System.IO.Path]::GetFileNameWithoutExtension($testProject.Name)
            $coverageFile = Join-Path $serviceRaw ($projectName + '.cobertura.xml')

            Write-Host "Running tests with coverage for $($serviceDir.Name) :: $projectName"

            $innerCommand = "dotnet test `"$($testProject.FullName)`" -c $Configuration --nologo"
            if ($hasLocalDotnetCoverage) {
                dotnet tool run dotnet-coverage -- collect `
                    $innerCommand `
                    -f cobertura `
                    -o $coverageFile `
                    --nologo | Out-Host
            }
            else {
                dotnet-coverage collect `
                    $innerCommand `
                    -f cobertura `
                    -o $coverageFile `
                    --nologo | Out-Host
            }

            if ($LASTEXITCODE -ne 0) {
                throw "dotnet test failed for $($serviceDir.Name) :: $projectName"
            }
        }
    }
}

$coverageFiles = @(Get-ChildItem -Path $rawRoot -Recurse -Filter *.cobertura.xml -File -ErrorAction SilentlyContinue)
if (-not $coverageFiles -or $coverageFiles.Count -eq 0) {
    throw "No cobertura files found under $rawRoot. Run without -SkipTestRun or verify tests are generating coverage."
}

New-Item -ItemType Directory -Path $dashboardRoot -Force | Out-Null

$reportsPattern = (Join-Path $rawRoot '**/*.cobertura.xml').Replace('\\', '/')

if ($hasLocalReportGenerator) {
    dotnet tool run reportgenerator `
        "-reports:$reportsPattern" `
        "-targetdir:$dashboardRoot" `
        '-assemblyfilters:+Defender.*;-*.Tests' `
        '-reporttypes:Html;HtmlSummary;TextSummary;Badges' `
        '-title:Defender Monorepo Coverage Dashboard' | Out-Host
}
else {
    reportgenerator `
        "-reports:$reportsPattern" `
        "-targetdir:$dashboardRoot" `
        '-assemblyfilters:+Defender.*;-*.Tests' `
        '-reporttypes:Html;HtmlSummary;TextSummary;Badges' `
        '-title:Defender Monorepo Coverage Dashboard' | Out-Host
}

if ($LASTEXITCODE -ne 0) {
    throw 'reportgenerator failed'
}

$indexFile = Join-Path $dashboardRoot 'index.html'
$textSummary = Join-Path $dashboardRoot 'Summary.txt'

Write-Host "Coverage dashboard generated: $indexFile"
if (Test-Path $textSummary) {
    Write-Host ''
    Get-Content $textSummary -TotalCount 15 | Out-Host
    Write-Host '...'
    Write-Host "Full summary: $textSummary"
}

if ($OpenReport) {
    Start-Process $indexFile
}
