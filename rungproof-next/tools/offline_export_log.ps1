# Godot redirects ANSI CSI display sequences into some export logs. Normalize
# only that observed display encoding; retain the original log as evidence.
function Get-OfflineExportPlainLog([string]$Log) {
    return [regex]::Replace($Log, '\x1B\[[0-?]*[ -/]*[@-~]', '')
}
function Assert-OfflineExportLog([string]$Log, [string]$RequiredMarker = '') {
    $plain = Get-OfflineExportPlainLog $Log
    if ($plain -match '(?m)^[\t ]*ERROR:|_VERIFY FAIL|BUILD_POLICY FAIL') { throw 'Error diagnostic in export/runtime log.' }
    if ($RequiredMarker -and !$plain.Contains($RequiredMarker)) { throw "Missing acceptance marker '$RequiredMarker'." }
}
function Get-OfflineExportResourcePaths([string]$Log) {
    $plain = Get-OfflineExportPlainLog $Log
    return @([regex]::Matches($plain, '(?m)Storing File: (res://[^\r\n]+)\r?$') | ForEach-Object { $_.Groups[1].Value })
}
