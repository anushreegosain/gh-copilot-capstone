$path = "$PSScriptRoot\logs\agent-hooks.jsonl"
if (-not (Test-Path $path)) {
    Write-Error "Log file not found: $path"
    exit 1
}
$backup = "$path.bak.$((Get-Date).ToString('yyyyMMddHHmmss'))"
Copy-Item $path $backup -Force
$content = Get-Content $path -Raw -ErrorAction Stop
# Remove occurrences of the specific parseError token
$fixed = $content -replace '"parseError"\s*:\s*"Invalid JSON input",?',''
Set-Content $path $fixed -Encoding UTF8
Write-Output "Fixed $path (backup at $backup)"