[CmdletBinding()]
param([string]$SourceRoot = '')
$ErrorActionPreference = 'Stop'
if (!$SourceRoot) { $SourceRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..')) }
$strictUtf8 = [Text.UTF8Encoding]::new($false,$true)
$sourceFiles = @(Get-ChildItem -LiteralPath (Join-Path $SourceRoot 'src') -Filter '*.cs' -File -Recurse)
$failures = @()
foreach ($sourceFile in $sourceFiles) {
    try { $null = $strictUtf8.GetString([IO.File]::ReadAllBytes($sourceFile.FullName)) }
    catch [Text.DecoderFallbackException] { $failures += $sourceFile.FullName + ':' + $_.Exception.Index }
}
if ($failures.Count) { throw ('Godot source contains invalid UTF8: ' + ($failures -join ', ')) }
Write-Output "GODOT_SOURCE_UTF8_PASS files=$($sourceFiles.Count)"
