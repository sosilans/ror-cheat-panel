# Cursor stop hook: drain the event payload, then publish cheat updates.
try {
    $null = [Console]::In.ReadToEnd()
} catch {
}

$sync = Join-Path (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)) "scripts\sync-github.ps1"
if (Test-Path -LiteralPath $sync) {
    & powershell -NoProfile -ExecutionPolicy Bypass -File $sync
}

Write-Output "{}"
exit 0
