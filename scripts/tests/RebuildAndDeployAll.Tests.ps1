Describe "rebuild-and-deploy-all" {
    $scriptPath = Join-Path $PSScriptRoot "..\rebuild-and-deploy-all.ps1"
    $scriptContent = Get-Content -Raw $scriptPath

    It "waits for the Docker build before promoting image tags" {
        $buildDispatch = $scriptContent.IndexOf('"Build and Publish Docker Images"')
        $buildWait = $scriptContent.IndexOf('"run", "watch"')
        $promotionDispatch = $scriptContent.IndexOf('"Promote Image Tag"')

        $buildDispatch | Should BeGreaterThan -1
        $buildWait | Should BeGreaterThan $buildDispatch
        $promotionDispatch | Should BeGreaterThan $buildWait
    }

    It "enumerates GitHub workflow runs before selecting a build run" {
        $scriptContent | Should Match 'ConvertFrom-Json\s*\|\s*ForEach-Object\s*\{\s*\$_\s*\}'
    }
}
