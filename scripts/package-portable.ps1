[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [string]$OutputDir = ""
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = (Resolve-Path (Join-Path $scriptDir "..")).Path

if ([string]::IsNullOrWhiteSpace($OutputDir)) {
    $OutputDir = Join-Path $repoRoot "dist"
} elseif (-not [System.IO.Path]::IsPathRooted($OutputDir)) {
    $OutputDir = Join-Path $repoRoot $OutputDir
}

Write-Host "Building PlayMore35 ($Configuration)..." -ForegroundColor Cyan
$buildArgs = @(
    "build",
    (Join-Path $repoRoot "PlayMore35\PlayMore35.csproj"),
    "-c", $Configuration
)
& dotnet @buildArgs
if ($LASTEXITCODE -ne 0) {
    throw "Build failed with exit code $LASTEXITCODE"
}

$binDir = Join-Path $repoRoot "PlayMore35\bin\$Configuration"
if (-not (Test-Path $binDir)) {
    throw "Output directory not found: $binDir"
}

$stagingDir = Join-Path $OutputDir "PlayMore35"
$zipPath = Join-Path $OutputDir "PlayMore35-portable.zip"

if (Test-Path $stagingDir) {
    Remove-Item -Path $stagingDir -Recurse -Force
}
New-Item -Path $stagingDir -ItemType Directory -Force | Out-Null

if (-not (Test-Path $OutputDir)) {
    New-Item -Path $OutputDir -ItemType Directory -Force | Out-Null
}

Write-Host "Staging portable release files..." -ForegroundColor Cyan

# Copy executable and configuration
Copy-Item (Join-Path $binDir "PlayMore35.exe") -Destination $stagingDir -Force
Copy-Item (Join-Path $binDir "PlayMore35.exe.config") -Destination $stagingDir -Force

# Copy all dependency runtime DLLs (excluding .xml and .pdb)
Get-ChildItem -Path $binDir -Filter "*.dll" | ForEach-Object {
    Copy-Item $_.FullName -Destination $stagingDir -Force
}

# Write README.txt for the portable distribution
$readmeContent = @"
============================================================
 PlayMore 3.5 - D&D 3.5e Character Sheet & Session Manager
============================================================

Portable Version (Zero Installation Required)

HOW TO RUN:
1. Double-click "PlayMore35.exe".
2. Or drag and drop a character JSON file directly onto "PlayMore35.exe"
   to launch and load it immediately.

MANAGING YOUR CHARACTERS:
- Click the "Load" button at the bottom-left of the Character tab
  to open any character JSON file.
- Click the "Save" button to save your character's current state.
- Characters can be kept anywhere on your computer or USB drive.

SYSTEM REQUIREMENTS:
- Windows 10 or Windows 11 (64-bit or 32-bit).
- .NET Framework 4.8 (pre-installed by default on Windows 10/11).

USB / PORTABLE USE:
You can keep this entire folder on a USB flash drive and run it
directly on any Windows computer at the game table without needing
administrator permissions or running an installer.
"@

Set-Content -Path (Join-Path $stagingDir "README.txt") -Value $readmeContent -NoNewline

Write-Host "Creating portable archive: $zipPath..." -ForegroundColor Cyan
if (Test-Path $zipPath) {
    Remove-Item -Path $zipPath -Force
}
Compress-Archive -Path (Join-Path $stagingDir "*") -DestinationPath $zipPath -Force

$zipItem = Get-Item $zipPath
$zipSizeMb = [math]::Round($zipItem.Length / 1MB, 2)

Write-Host "`nSuccessfully created portable package!" -ForegroundColor Green
Write-Host "  Folder:  $stagingDir"
Write-Host "  Archive: $zipPath ($zipSizeMb MB)"
