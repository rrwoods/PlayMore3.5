[CmdletBinding()]
param(
    [switch]$Fix,
    [string]$Path = ""
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = (Resolve-Path (Join-Path $scriptDir "..")).Path

$targetDir = if ([string]::IsNullOrWhiteSpace($Path)) { $repoRoot } else { (Resolve-Path $Path).Path }

$excludeDirs = @(
    ".git",
    "bin",
    "obj",
    ".vs",
    "TestResults",
    "packages",
    ".idea"
)

$textExtensions = @(
    ".cs",
    ".md",
    ".csv",
    ".sln",
    ".slnx",
    ".csproj",
    ".ps1",
    ".json",
    ".xml",
    ".yml",
    ".yaml",
    ".txt",
    ".resx",
    ".config",
    ".editorconfig",
    ".gitattributes",
    ".gitignore"
)

$filesToCheck = Get-ChildItem -Path $targetDir -Recurse -File | Where-Object {
    $item = $_
    $rel = $item.FullName.Substring($repoRoot.Length).TrimStart('\', '/')
    $parts = $rel.Split([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)

    $isExcluded = $false
    foreach ($part in $parts[0..($parts.Length - 2)]) {
        if ($excludeDirs -contains $part) {
            $isExcluded = $true
            break
        }
    }
    if ($isExcluded) { return $false }

    $ext = $item.Extension.ToLowerInvariant()
    $name = $item.Name.ToLowerInvariant()

    if ($textExtensions -contains $ext -or $textExtensions -contains $name) {
        return $true
    }

    return $false
}

$nonCrlfPattern = '(?<!\r)\n|\r(?!\n)'
$violations = @()
$fixedCount = 0

foreach ($file in $filesToCheck) {
    $bytes = [System.IO.File]::ReadAllBytes($file.FullName)
    if ($bytes.Length -eq 0) { continue }

    if ([System.Array]::IndexOf($bytes, [byte]0) -ge 0) { continue }

    $text = [System.Text.Encoding]::UTF8.GetString($bytes)
    $hasViolation = [System.Text.RegularExpressions.Regex]::IsMatch($text, $nonCrlfPattern)

    $relPath = $file.FullName.Substring($repoRoot.Length).TrimStart('\', '/')

    if ($hasViolation) {
        if ($Fix) {
            $normalized = [System.Text.RegularExpressions.Regex]::Replace($text, "\r\n?|\n", "`r`n")
            [System.IO.File]::WriteAllText($file.FullName, $normalized, [System.Text.Encoding]::UTF8)
            Write-Host "FIXED: $relPath" -ForegroundColor Yellow
            $fixedCount++
        } else {
            Write-Host "FAIL:  $relPath" -ForegroundColor Red
            $violations += $relPath
        }
    }
}

if ($Fix) {
    if ($fixedCount -gt 0) {
        Write-Host "`nSuccessfully normalized $fixedCount file(s) to CRLF." -ForegroundColor Green
    } else {
        Write-Host "`nAll $($filesToCheck.Count) checked file(s) already have CRLF line endings." -ForegroundColor Green
    }
    exit 0
} else {
    if ($violations.Count -gt 0) {
        Write-Host "`n$($violations.Count) file(s) have non-CRLF line endings." -ForegroundColor Red
        Write-Host "Run with -Fix to automatically normalize: powershell -ExecutionPolicy Bypass -File scripts/check-crlf.ps1 -Fix" -ForegroundColor Yellow
        exit 1
    } else {
        Write-Host "OK: All $($filesToCheck.Count) checked file(s) have valid CRLF line endings." -ForegroundColor Green
        exit 0
    }
}
