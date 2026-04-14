param(
    [string]$Configuration = 'Release'
)

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$appRoot = Split-Path -Parent $scriptRoot
$repoRoot = Split-Path -Parent $appRoot
$projectPath = Join-Path $appRoot 'CodexTldSaveEditor.App.csproj'
$publishDir = Join-Path $repoRoot 'release\TheLongDarkSaveEditor'

if (-not (Test-Path $projectPath)) {
    throw "Project file not found: $projectPath"
}

New-Item -ItemType Directory -Force -Path $publishDir | Out-Null

Get-ChildItem -Path $publishDir -Force -ErrorAction SilentlyContinue | Remove-Item -Recurse -Force

Write-Host "Publishing to $publishDir" -ForegroundColor Cyan

dotnet publish $projectPath -c $Configuration -o $publishDir

if ($LASTEXITCODE -ne 0) {
    throw 'dotnet publish failed.'
}

Write-Host 'Publish completed successfully.' -ForegroundColor Green
