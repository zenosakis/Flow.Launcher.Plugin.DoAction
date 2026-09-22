param(
    [string]$PluginRoot = (Join-Path $env:APPDATA 'FlowLauncher/Plugins'),
    [string]$PackagePath = $PSScriptRoot
)
$ErrorActionPreference = 'Stop'
if (-not (Test-Path -LiteralPath (Join-Path $PackagePath 'plugin.json'))) {
    $workspace = Split-Path $PSScriptRoot -Parent
    $sourceManifest = Join-Path $workspace 'src/Flow.Launcher.Plugin.DoAction/plugin.json'
    if (Test-Path -LiteralPath $sourceManifest) {
        $version = (Get-Content -Raw -LiteralPath $sourceManifest | ConvertFrom-Json).Version
        $PackagePath = Join-Path $workspace "artifacts/Flow.Launcher.Plugin.DoAction-$version"
    }
}
if (-not (Test-Path -LiteralPath (Join-Path $PackagePath 'plugin.json'))) {
    throw 'Package not found. Run scripts/Build.ps1 first, or extract the release zip.'
}
$manifest = Get-Content -Raw -LiteralPath (Join-Path $PackagePath 'plugin.json') | ConvertFrom-Json
if ($manifest.ID -ne 'D5B032E0FE594208B0687010889D6450') { throw 'Unexpected package ID.' }
$target = Join-Path $PluginRoot "Flow.Launcher.Plugin.DoAction-$($manifest.Version)"
$existing = @(Get-ChildItem -LiteralPath $PluginRoot -Filter plugin.json -Recurse -ErrorAction SilentlyContinue | Where-Object {
    try { (Get-Content -Raw -LiteralPath $_.FullName | ConvertFrom-Json).ID -eq $manifest.ID } catch { $false }
})
if ($existing.Count -gt 1) { throw 'Multiple DoAction installations found. Remove duplicates through Flow first.' }
if ($existing.Count -eq 1) {
    $target = $existing[0].DirectoryName
    if (Get-Process -Name 'Flow.Launcher' -ErrorAction SilentlyContinue) {
        throw 'Close Flow Launcher before updating an existing DoAction installation.'
    }
}
if ((Test-Path -LiteralPath $target) -and $existing.Count -eq 0) {
    throw "Target folder exists without a matching DoAction manifest: $target"
}
New-Item -ItemType Directory -Force $target | Out-Null
foreach ($file in @('Flow.Launcher.Plugin.DoAction.dll', 'Flow.Launcher.Plugin.DoAction.deps.json', 'DoAction.Core.dll', 'plugin.json')) {
    Copy-Item -LiteralPath (Join-Path $PackagePath $file) -Destination $target -Force
}
New-Item -ItemType Directory -Force (Join-Path $target 'Images') | Out-Null
Copy-Item -LiteralPath (Join-Path $PackagePath 'Images/doaction.ico') -Destination (Join-Path $target 'Images/doaction.ico') -Force
Write-Output "Installed: $target"
Write-Output 'Restart Flow Launcher, then type da. Both features start OFF.'
