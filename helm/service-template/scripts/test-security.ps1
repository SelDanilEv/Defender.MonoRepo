[CmdletBinding()]
param(
    [string]$ChartPath = (Join-Path $PSScriptRoot ".."),
    [string]$HelmCommand = "helm"
)

$ErrorActionPreference = "Stop"

function Assert-RenderedMatch {
    param(
        [string]$Content,
        [string]$Pattern,
        [string]$Description
    )

    if ($Content -notmatch $Pattern) {
        throw "Missing required workload security field: $Description"
    }
}

$chart = (Resolve-Path $ChartPath).Path
$baseValues = Join-Path $chart "values.yaml"
$serviceValues = Get-ChildItem -Path $chart -Filter "values-*.yaml" -File | Sort-Object Name

if ($serviceValues.Count -eq 0) {
    throw "No service values files found in $chart"
}

$defaultValues = Get-Content -Path $baseValues -Raw
Assert-RenderedMatch -Content $defaultValues -Pattern '(?m)^podSecurityContext:\s*$' -Description "podSecurityContext values"
Assert-RenderedMatch -Content $defaultValues -Pattern '(?m)^containerSecurityContext:\s*$' -Description "containerSecurityContext values"

foreach ($valuesFile in $serviceValues) {
    $releaseName = "security-$($valuesFile.BaseName -replace '^values-', '')"
    $rendered = (& $HelmCommand template $releaseName $chart --values $baseValues --values $valuesFile.FullName --show-only templates/deployment.yaml) -join "`n"

    if ($LASTEXITCODE -ne 0) {
        throw "Helm rendering failed for $($valuesFile.Name)"
    }

    Assert-RenderedMatch -Content $rendered -Pattern '(?m)^[ \t]+automountServiceAccountToken: false[ \t]*$' -Description "automountServiceAccountToken=false ($($valuesFile.Name))"
    Assert-RenderedMatch -Content $rendered -Pattern '(?m)^[ \t]+runAsNonRoot: true[ \t]*$' -Description "runAsNonRoot=true ($($valuesFile.Name))"
    Assert-RenderedMatch -Content $rendered -Pattern '(?m)^[ \t]+seccompProfile:[ \t]*$\r?\n^[ \t]+type: RuntimeDefault[ \t]*$' -Description "seccompProfile RuntimeDefault ($($valuesFile.Name))"
    Assert-RenderedMatch -Content $rendered -Pattern '(?m)^[ \t]+allowPrivilegeEscalation: false[ \t]*$' -Description "allowPrivilegeEscalation=false ($($valuesFile.Name))"
    Assert-RenderedMatch -Content $rendered -Pattern '(?m)^[ \t]+readOnlyRootFilesystem: true[ \t]*$' -Description "readOnlyRootFilesystem=true ($($valuesFile.Name))"
    Assert-RenderedMatch -Content $rendered -Pattern '(?m)^[ \t]+capabilities:[ \t]*$\r?\n^[ \t]+drop:[ \t]*$\r?\n^[ \t]+- ALL[ \t]*$' -Description "capabilities.drop=[ALL] ($($valuesFile.Name))"
    Assert-RenderedMatch -Content $rendered -Pattern '(?m)^[ \t]+volumeMounts:[ \t]*$\r?\n^[ \t]+- name: tmp[ \t]*$\r?\n^[ \t]+mountPath: /tmp[ \t]*$' -Description "/tmp mount ($($valuesFile.Name))"
    Assert-RenderedMatch -Content $rendered -Pattern '(?m)^[ \t]+volumes:[ \t]*$\r?\n^[ \t]+- name: tmp[ \t]*$\r?\n^[ \t]+emptyDir: \{\}[ \t]*$' -Description "/tmp emptyDir ($($valuesFile.Name))"
}

Write-Output "Validated workload security fields in $($serviceValues.Count) rendered deployments."
