$hookPath = ".git/hooks/pre-commit"
$hookContent = @'
#!/bin/sh
powershell.exe -ExecutionPolicy Bypass -File scripts/check-crlf.ps1 -Fix
git update-index --again
'@

[System.IO.File]::WriteAllText($hookPath, $hookContent)
Write-Host "Installed pre-commit hook to $hookPath"
