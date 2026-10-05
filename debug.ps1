param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"

$scriptDir = $PSScriptRoot
$projectFile = Join-Path $scriptDir "Flow.Launcher.Plugin.Snippets\Flow.Launcher.Plugin.Snippets.csproj"
$publishDir = Join-Path $scriptDir "Flow.Launcher.Plugin.Snippets\bin\$Configuration\win-x64\publish"

Write-Host "Publishing plugin ($Configuration)..." -ForegroundColor Cyan
dotnet publish $projectFile -c $Configuration -r win-x64 --no-self-contained

if ($LASTEXITCODE -ne 0) {
    Write-Error "dotnet publish failed with exit code $LASTEXITCODE."
    exit $LASTEXITCODE
}

$appDataFolder = [Environment]::GetFolderPath("ApplicationData")
$pluginsDir = Join-Path $appDataFolder "FlowLauncher\Plugins"
$targetPluginDir = Join-Path $pluginsDir "Flow.Launcher.Plugin.Snippets"
$flowLauncherExe = "$env:LOCALAPPDATA\FlowLauncher\Flow.Launcher.exe"

# Stop Flow Launcher if running
$flowProcess = Get-Process -Name "Flow.Launcher" -ErrorAction SilentlyContinue
if ($flowProcess) {
    Write-Host "Stopping Flow.Launcher..." -ForegroundColor Cyan
    Stop-Process -Name "Flow.Launcher" -Force -ErrorAction SilentlyContinue
    $timeout = 10
    $elapsed = 0
    while ((Get-Process -Name "Flow.Launcher" -ErrorAction SilentlyContinue) -and ($elapsed -lt $timeout)) {
        Start-Sleep -Milliseconds 500
        $elapsed += 0.5
    }
}

# Clean up any legacy or conflicting plugin folders with the same ID
$manifestFile = Join-Path $scriptDir "Flow.Launcher.Plugin.Snippets\plugin.json"
if (Test-Path $manifestFile) {
    try {
        $pluginId = (Get-Content $manifestFile -Raw | ConvertFrom-Json).ID
        if (Test-Path $pluginsDir) {
            Get-ChildItem -Path $pluginsDir -Directory | ForEach-Object {
                $itemManifest = Join-Path $_.FullName "plugin.json"
                if ((Test-Path $itemManifest) -and ($_.FullName -ne $targetPluginDir)) {
                    $itemJson = Get-Content $itemManifest -Raw | ConvertFrom-Json
                    if ($itemJson.ID -eq $pluginId) {
                        Write-Host "Removing conflicting plugin folder with same ID: $($_.FullName)..." -ForegroundColor Cyan
                        Remove-Item -Recurse -Force $_.FullName
                    }
                }
            }
        }
    } catch {
        Write-Warning "Could not inspect plugin.json for conflicting IDs: $_"
    }
}

# Clean up target folder and deploy new build
if (Test-Path $targetPluginDir) {
    Write-Host "Removing existing plugin folder..." -ForegroundColor Cyan
    Remove-Item -Recurse -Force $targetPluginDir
}

Write-Host "Deploying new build to $targetPluginDir..." -ForegroundColor Cyan
New-Item -ItemType Directory -Force -Path $targetPluginDir | Out-Null
Copy-Item -Path (Join-Path $publishDir "*") -Destination $targetPluginDir -Recurse -Force

# Restart Flow Launcher if executable is found
if (Test-Path $flowLauncherExe) {
    Start-Sleep -Seconds 1
    Write-Host "Restarting Flow.Launcher..." -ForegroundColor Green
    Start-Process $flowLauncherExe
} else {
    Write-Warning "Flow.Launcher.exe not found at '$flowLauncherExe'. Please ensure Flow Launcher is installed."
}
