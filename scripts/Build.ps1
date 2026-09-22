param([switch]$SkipTests)
$ErrorActionPreference = 'Stop'
$workspace = Split-Path $PSScriptRoot -Parent
Push-Location $workspace
try {
    dotnet build 'src/Flow.Launcher.Plugin.DoAction' -c Release --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
    if (-not $SkipTests) {
        dotnet run --project 'tests/DoAction.Tests' -c Release -- 'artifacts/qa'
        if ($LASTEXITCODE -ne 0) { throw 'Tests failed.' }
    }
    $manifest = Get-Content -Raw 'src/Flow.Launcher.Plugin.DoAction/plugin.json' | ConvertFrom-Json
    $packageName = "Flow.Launcher.Plugin.DoAction-$($manifest.Version)"
    $package = Join-Path $workspace "artifacts/$packageName"
    New-Item -ItemType Directory -Force $package | Out-Null
    $build = Join-Path $workspace 'src/Flow.Launcher.Plugin.DoAction/bin/Release/net8.0-windows'
    foreach ($file in @('Flow.Launcher.Plugin.DoAction.dll', 'Flow.Launcher.Plugin.DoAction.deps.json', 'DoAction.Core.dll', 'plugin.json')) {
        Copy-Item -LiteralPath (Join-Path $build $file) -Destination $package -Force
    }
    New-Item -ItemType Directory -Force (Join-Path $package 'Images') | Out-Null
    Copy-Item -LiteralPath (Join-Path $build 'Images/doaction.ico') -Destination (Join-Path $package 'Images/doaction.ico') -Force
    Copy-Item -LiteralPath (Join-Path $workspace 'README.md') -Destination $package -Force
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Install.ps1') -Destination $package -Force
    $zip = Join-Path $workspace "artifacts/$packageName.zip"
    Compress-Archive -Path (Join-Path $package '*') -DestinationPath $zip -Force
    Write-Output "Package: $zip"
} finally { Pop-Location }
