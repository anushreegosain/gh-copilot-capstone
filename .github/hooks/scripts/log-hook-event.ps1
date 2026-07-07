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

# Remove non-printable/control characters that may break JSON parsing
if ($rawInput -ne $null) {
    $rawInput = $rawInput -replace '[^\u0009\u000A\u000D\u0020-\uFFFF]', ''
}

$payload = $null

if (-not [string]::IsNullOrWhiteSpace($rawInput)) {
    try {
        $payload = $rawInput | ConvertFrom-Json -Depth 50
    } catch {
        # Try to handle the case where the incoming payload is a JSON-encoded string (e.g. "{...}")
        try {
            $maybeString = $rawInput | ConvertFrom-Json -Depth 50 -ErrorAction Stop
            if ($maybeString -is [string]) {
                try {
                    $payload = $maybeString | ConvertFrom-Json -Depth 50
                } catch {
                    # Provide the JSON parsing error message as detail, but always preserve raw input
                    $errMsg = $_.Exception.Message
                    $payload = @{ raw = $rawInput; parseErrorDetail = $errMsg }
                }
            } else {
                $payload = @{ raw = $rawInput }
            }
        } catch {
            $payload = @{ raw = $rawInput }
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
