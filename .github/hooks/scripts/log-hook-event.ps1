param(
    [Parameter(Mandatory = $true)]
    [string]$HookName,

    [Parameter(ValueFromPipeline = $true)]
    [AllowNull()]
    [object]$PipelineInput
)

begin {
    $pipelineChunks = @()
}

process {
    if ($null -ne $PipelineInput) {
        $pipelineChunks += [string]$PipelineInput
    }
}

end {

$repoRoot = Get-Location
$logDir = Join-Path $repoRoot ".github/hooks/logs"
$logFile = Join-Path $logDir "agent-hooks.jsonl"

if (-not (Test-Path $logDir)) {
    New-Item -Path $logDir -ItemType Directory -Force | Out-Null
}

$rawInput = if ($pipelineChunks.Count -gt 0) {
    $pipelineChunks -join [Environment]::NewLine
} else {
    [Console]::In.ReadToEnd()
}
$payload = $null

if (-not [string]::IsNullOrWhiteSpace($rawInput)) {
    try {
        $payload = $rawInput | ConvertFrom-Json -Depth 50
    } catch {
        $payload = @{
            parseError = "Invalid JSON input"
            raw = $rawInput
        }
    }
}

$entry = [ordered]@{
    timestampUtc = [DateTime]::UtcNow.ToString("o")
    hook = $HookName
    payload = $payload
}

($entry | ConvertTo-Json -Depth 50 -Compress) | Add-Content -Path $logFile

# Exit 0 so this hook does not block agent execution.
exit 0
}
