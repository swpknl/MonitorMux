<#
.SYNOPSIS
    Publishes MonitorMux as a self-contained single-file exe and launches it.
#>

param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $repoRoot

$publishDir = Join-Path $repoRoot "bin\$Configuration\net10.0-windows\$Runtime\publish"

$existing = Get-Process MonitorMux -ErrorAction SilentlyContinue
if ($existing) {
    Write-Host "Stopping existing MonitorMux instance(s)..." -ForegroundColor Yellow
    $existing | Stop-Process -Force
    Start-Sleep -Milliseconds 500
}

Write-Host "Publishing MonitorMux ($Configuration, $Runtime)..." -ForegroundColor Cyan
dotnet publish MonitorMux.csproj -c $Configuration -r $Runtime --self-contained false -p:PublishSingleFile=true -o $publishDir
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE"
}

$exePath = Join-Path $publishDir "MonitorMux.exe"
if (-not (Test-Path $exePath)) {
    throw "Publish succeeded but executable was not found at $exePath"
}

Write-Host "Starting MonitorMux..." -ForegroundColor Cyan
Start-Process -FilePath $exePath
