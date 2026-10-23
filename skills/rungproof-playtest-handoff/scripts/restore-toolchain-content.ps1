param(
    [Parameter(Mandatory = $true)][string]$Root,
    [Parameter(Mandatory = $true)][string]$BundleRoot,
    [switch]$Apply
)

# Expand the public content-addressed backup. Default preview does not write.
# Existing different files are preserved; hashes check blobs and copied files.
$ErrorActionPreference = 'Stop'
$rpRoot = [IO.Path]::GetFullPath((Resolve-Path -LiteralPath $Root).Path).TrimEnd('\','/')
$rpPrefix = $rpRoot + [IO.Path]::DirectorySeparatorChar
$rpBundle = [IO.Path]::GetFullPath((Resolve-Path -LiteralPath $BundleRoot).Path)
$rpManifest = Get-Content -LiteralPath (Join-Path $rpBundle 'toolchain-map.json') -Raw | ConvertFrom-Json
if ($rpManifest.schema -ne 1 -or $rpManifest.layout -ne 'sha256-prefix2') {
    throw 'Unsupported backup manifest schema/layout.'
}
$rpVerified = @{}
$rpSeenPaths = @{}
$rpCopied = 0
$rpSkipped = 0
$rpBytes = [long]0

foreach ($rpEntry in $rpManifest.files) {
    $rpRelative = ([string]$rpEntry.path).Replace('\','/')
    if ($rpRelative.Contains(':') -or $rpRelative.Split('/') -contains '..' -or
        $rpRelative.Split('/') -contains '.') { throw 'Non-canonical manifest path refused.' }
    if ($rpRelative -notmatch '^(rungproof-next/\.(tools|godot)/|build/\.venv-rungproof/)') {
        throw "Path is outside the archived toolchain scope: $rpRelative"
    }
    if ([IO.Path]::IsPathRooted($rpRelative)) { throw 'Absolute manifest path refused.' }
    $rpTarget = [IO.Path]::GetFullPath((Join-Path $rpRoot $rpRelative))
    if (-not $rpTarget.StartsWith($rpPrefix, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Manifest path escapes the destination: $rpRelative"
    }
    if ($rpSeenPaths.ContainsKey($rpTarget)) { throw "Duplicate destination: $rpRelative" }
    $rpSeenPaths[$rpTarget] = $true
    if ($rpEntry.sha256 -notmatch '^[a-f0-9]{64}$' -or [long]$rpEntry.bytes -lt 0) {
        throw "Invalid content identity: $rpRelative"
    }
    $rpBlob = Join-Path $rpBundle ('blobs\' + $rpEntry.sha256.Substring(0,2) + '\' + $rpEntry.sha256 + '.blob')
    if (-not (Test-Path -LiteralPath $rpBlob -PathType Leaf)) { throw "Missing blob: $rpBlob" }
    $rpBytes += [long]$rpEntry.bytes
    if (-not $Apply) { continue }

    # Hash repeated content once, but copy independent destination files so an
    # SDK update cannot mutate another snapshot through a shared hard link.
    if (-not $rpVerified.ContainsKey($rpEntry.sha256)) {
        $rpBlobInfo = Get-Item -LiteralPath $rpBlob
        $rpBlobHash = (Get-FileHash -LiteralPath $rpBlob -Algorithm SHA256).Hash
        if ($rpBlobInfo.Length -ne [long]$rpEntry.bytes -or $rpBlobHash -ne $rpEntry.sha256) {
            throw "Blob size/hash mismatch: $rpBlob"
        }
        $rpVerified[$rpEntry.sha256] = $true
    }
    if (Test-Path -LiteralPath $rpTarget) {
        $rpExisting = Get-Item -LiteralPath $rpTarget
        if ($rpExisting.PSIsContainer -or $rpExisting.Length -ne [long]$rpEntry.bytes -or
            (Get-FileHash -LiteralPath $rpTarget -Algorithm SHA256).Hash -ne $rpEntry.sha256) {
            throw "Existing different file preserved: $rpTarget"
        }
        $rpSkipped++
        continue
    }
    $rpParent = Split-Path -Parent $rpTarget
    if (-not (Test-Path -LiteralPath $rpParent)) {
        New-Item -ItemType Directory -Path $rpParent -Force | Out-Null
    }
    $rpTemporary = Join-Path $rpParent ('.rungproof-restore-' + [guid]::NewGuid().ToString('N') + '.tmp')
    try {
        [IO.File]::Copy($rpBlob, $rpTemporary, $false)
        if ((Get-FileHash -LiteralPath $rpTemporary -Algorithm SHA256).Hash -ne $rpEntry.sha256) {
            throw "Destination verification failed: $rpTarget"
        }
        # Same-directory move publishes a complete file and refuses overwrite.
        [IO.File]::Move($rpTemporary, $rpTarget)
    }
    finally {
        # Delete only the unique temporary file created by this invocation.
        if (Test-Path -LiteralPath $rpTemporary) { [IO.File]::Delete($rpTemporary) }
    }
    $rpCopied++
}

[PSCustomObject]@{
    mode = $(if ($Apply) { 'APPLIED_AND_VERIFIED' } else { 'PREVIEW_NO_WRITES' })
    paths = $rpManifest.files.Count
    expandedBytes = $rpBytes
    copied = $rpCopied
    alreadyMatched = $rpSkipped
    verifiedUniqueBlobs = $rpVerified.Count
} | ConvertTo-Json
