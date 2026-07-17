param(
    [int] $ApiPort = 5202,
    [int] $WebPort = 5173
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$apiUrl = "http://localhost:$ApiPort"
$assetRoot = "C:\Users\User\Desktop\CR\Clash-Royale-assets"
$sfxRoot = "C:\Users\User\Desktop\CR\Clash-Royale-SFX-master"

if ((Test-Path -LiteralPath $assetRoot) -and (Test-Path -LiteralPath $sfxRoot)) {
    Write-Host "Importing local Clash Royale art and SFX"
    try {
        & (Join-Path $PSScriptRoot "import-local-assets.ps1") -AssetRoot $assetRoot -SfxRoot $sfxRoot
    } catch {
        Write-Warning "Local asset import failed. The game will continue with fallback art. $($_.Exception.Message)"
    }
} else {
    Write-Host "Local Clash Royale asset folders were not found. The game will use fallback art."
}

Write-Host "Starting CRPrivateServer API at $apiUrl"
Start-Process powershell -ArgumentList @(
    "-NoExit",
    "-Command",
    "Set-Location '$root'; dotnet run --project src/Game.Api/Game.Api.csproj --urls $apiUrl"
)

Write-Host "Starting CRPrivateServer Web at http://localhost:$WebPort"
Start-Process powershell -ArgumentList @(
    "-NoExit",
    "-Command",
    "Set-Location '$root'; npm.cmd --prefix src/Game.Web run dev -- --host 127.0.0.1 --port $WebPort"
)

Write-Host "Open http://localhost:$WebPort after both windows finish starting."
