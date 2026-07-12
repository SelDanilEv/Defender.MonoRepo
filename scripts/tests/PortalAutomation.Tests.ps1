Describe "Portal token-efficient automation" {
    $repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
    $verifyPath = Join-Path $repoRoot "scripts\verify-portal.ps1"
    $deployPath = Join-Path $repoRoot "scripts\deploy-portal.ps1"
    $statusPath = Join-Path $repoRoot "scripts\portal-live-status.py"

    It "provides a compact verification wrapper" {
        (Test-Path $verifyPath) | Should Be $true
        $content = Get-Content -Raw $verifyPath
        $content | Should Match 'PORTAL_VERIFY'
        $content | Should Match 'Get-Content.+-Tail'
        $content | Should Match 'IncludeE2E'
        $content | Should Match 'TestPath'
    }

    It "keeps deployment preview-only unless Execute is explicit" {
        (Test-Path $deployPath) | Should Be $true
        $content = Get-Content -Raw $deployPath
        $content | Should Match '\[switch\]\$Execute'
        $content | Should Match 'service=Defender\.Portal'
        $content | Should Match 'gh run watch'
        $content.IndexOf('Watch-Run $buildRun.databaseId "build"') | Should BeLessThan $content.IndexOf('$promoteWorkflow = "promote-image-tag.yml"')
        $content | Should Match 'portal-live-status\.py'
    }

    It "does not treat successful native stderr as a deployment failure" {
        $content = Get-Content -Raw $deployPath
        $content | Should Match 'function Invoke-SilentNative'
        $content | Should Match 'Invoke-SilentNative \{ & git push origin \$Ref \}'
        $content | Should Match '\$ErrorActionPreference = "Continue"'
    }

    It "keeps live credentials out of command output" {
        (Test-Path $statusPath) | Should Be $true
        $content = Get-Content -Raw $statusPath
        $content | Should Match 'argo-cd\.config'
        $content | Should Not Match 'print\(.+password'
        $content | Should Match '20260712-200|expected-tag'
    }

    It "documents compact wrappers in agent instructions" {
        $rootAgents = Get-Content -Raw (Join-Path $repoRoot "AGENTS.md")
        $portalAgents = Get-Content -Raw (Join-Path $repoRoot "src\Defender.Portal\AGENTS.md")
        $rootAgents | Should Match 'verify-portal\.ps1'
        $rootAgents | Should Match 'deploy-portal\.ps1'
        $portalAgents | Should Match 'verify-portal\.ps1'
    }
}
