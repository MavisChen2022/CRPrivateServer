param(
    [int] $ApiPort = 5202,
    [int] $WebPort = 5173
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$apiUrl = "http://localhost:$ApiPort"

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
