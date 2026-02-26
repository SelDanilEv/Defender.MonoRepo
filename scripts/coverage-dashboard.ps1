[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',
    [string]$OutputRoot = 'artifacts/coverage',
    [switch]$SkipTestRun,
    [switch]$OpenReport,
    [switch]$ContinueOnFailure,
    [switch]$FailFast
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$outputRootPath = Join-Path $repoRoot $OutputRoot
$rawRoot = Join-Path $outputRootPath 'raw'
$dashboardRoot = Join-Path $outputRootPath 'dashboard'
$scriptsDir = $PSScriptRoot
$shouldContinueOnFailure = $true

function Get-DefenderServiceNames {
    $allSystemsPath = Join-Path $scriptsDir 'all_systems.sh'
    $allLibsPath = Join-Path $scriptsDir 'all_libs.sh'
    $names = [System.Collections.Generic.List[string]]::new()
    foreach ($path in @($allSystemsPath, $allLibsPath)) {
        if (-not (Test-Path $path)) { continue }
        $content = Get-Content $path -Raw
        $matches = [regex]::Matches($content, "'(Defender\.[^']+)'")
        foreach ($m in $matches) { $names.Add($m.Groups[1].Value) }
    }
    if ($names.Count -eq 0) {
        $srcPath = Join-Path $repoRoot 'src'
        Get-ChildItem -Path $srcPath -Directory | Where-Object { $_.Name -like 'Defender.*' } | Sort-Object Name | ForEach-Object { $names.Add($_.Name) }
    }
    $names
}
if ($FailFast) {
    $shouldContinueOnFailure = $false
}
elseif ($PSBoundParameters.ContainsKey('ContinueOnFailure')) {
    $shouldContinueOnFailure = $ContinueOnFailure.IsPresent
}

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

    $failedProjects = [System.Collections.Generic.List[string]]::new()
    $servicesWithoutTests = [System.Collections.Generic.List[string]]::new()
    $projectsWithoutCoverage = [System.Collections.Generic.List[string]]::new()

    $serviceNames = Get-DefenderServiceNames
    $srcPath = Join-Path $repoRoot 'src'
    $serviceDirs = @($serviceNames | ForEach-Object {
        $name = $_
        $dir = Join-Path $srcPath $name
        if (Test-Path $dir -PathType Container) {
            [System.IO.DirectoryInfo]::new($dir)
        }
        else {
            Write-Warning "Skipping $name : folder not found at $dir"
            $null
        }
    } | Where-Object { $_ })
    Write-Host "Processing $($serviceDirs.Count) services/libraries for coverage (from all_systems + all_libs)."

    foreach ($serviceDir in $serviceDirs) {
        $testProjects = @(Get-ChildItem -Path $serviceDir.FullName -Recurse -Filter *.Tests.csproj -File |
            Where-Object { $_.FullName -notmatch '\\(obj|bin)\\' })
        if ($testProjects.Count -eq 0) {
            Write-Warning "Skipping $($serviceDir.Name): no *.Tests.csproj found"
            $servicesWithoutTests.Add($serviceDir.Name)
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
                $projectRef = "$($serviceDir.Name) :: $projectName"
                $failedProjects.Add($projectRef)
                if ($shouldContinueOnFailure) {
                    Write-Warning "Tests failed for $projectRef, continuing..."
                }
                else {
                    throw "dotnet test failed for $projectRef"
                }
            }
            elseif (-not (Test-Path $coverageFile)) {
                $projectRef = "$($serviceDir.Name) :: $projectName"
                $projectsWithoutCoverage.Add($projectRef)
                Write-Warning "No coverage file generated for $projectRef"
            }
        }
    }

    if ($servicesWithoutTests.Count -gt 0) {
        Write-Warning "Services without test projects ($($servicesWithoutTests.Count)): $($servicesWithoutTests -join ', ')"
    }

    if ($failedProjects.Count -gt 0) {
        Write-Warning "Test projects failed ($($failedProjects.Count)): $($failedProjects -join '; ')"
    }

    if ($projectsWithoutCoverage.Count -gt 0) {
        Write-Warning "Test projects without generated coverage ($($projectsWithoutCoverage.Count)): $($projectsWithoutCoverage -join '; ')"
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
