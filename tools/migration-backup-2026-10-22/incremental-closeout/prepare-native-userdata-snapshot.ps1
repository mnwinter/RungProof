#requires -Version 7.0
# Run DIRECTLY in native PowerShell after Godot/bridge writers have stopped.
# PowerShell launched from packaged Python inherits the Python filesystem view.
# This stages private userdata only; it does not touch D:, Git or the application.
[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)]
    [ValidatePattern('^native-userdata-[A-Za-z0-9][A-Za-z0-9_-]{0,79}$')]
    [string]$DestinationName,
    [Parameter(Mandatory=$true)]
    [ValidateRange(1,2147483647)]
    [int]$ExpectedFileCount
)

$ErrorActionPreference = 'Stop'
$rpOwnedRoot = 'C:\Users\matt.winter\AppData\Local\Temp\RungProof-QA-20261023-RootOwned'
$rpSourceRoot = 'C:\Users\matt.winter\AppData\Roaming\Godot\app_userdata\RungProof'
$rpStarted = [DateTime]::UtcNow
$rpErrors = [Collections.Generic.List[object]]::new()
$rpCopies = [Collections.Generic.List[object]]::new()

function Add-RpFailure([string]$Phase, [string]$Relative, $Failure) {
    $rpErrors.Add([ordered]@{phase=$Phase;relative=$Relative;kind=$Failure.GetType().Name;message=$Failure.Message})
}

function Assert-RpPlainPath([string]$Path, [string]$Root) {
    $full = [IO.Path]::GetFullPath($Path)
    $base = [IO.Path]::GetFullPath($Root).TrimEnd('\')
    if (-not $full.Equals($base,[StringComparison]::OrdinalIgnoreCase) -and
        -not $full.StartsWith($base+'\',[StringComparison]::OrdinalIgnoreCase)) {
        throw [IO.IOException]::new('Path escaped its approved root')
    }
    $cursor = $full
    while ($true) {
        $item = Get-Item -LiteralPath $cursor -Force
        if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw [IO.IOException]::new('Reparse point must not be traversed')
        }
        # Check the approved root's ancestors too: a plain child of a junction
        # must not make an apparently approved root silently traverse it.
        $parent = [IO.Path]::GetDirectoryName($cursor)
        if ([string]::IsNullOrEmpty($parent)) { break }
        $cursor = $parent
    }
}

function Read-RpTree([string]$Root, [string]$Phase) {
    $files = [Collections.Generic.List[object]]::new()
    $excluded = [Collections.Generic.List[object]]::new()
    $stack = [Collections.Generic.Stack[string]]::new()
    try {
        Assert-RpPlainPath $Root $Root
        if (-not (Test-Path -LiteralPath $Root -PathType Container)) { throw [IO.DirectoryNotFoundException]::new('Source or payload root missing') }
        $stack.Push($Root)
    } catch { Add-RpFailure $Phase '.' $_.Exception }
    while ($stack.Count) {
        $directory = $stack.Pop()
        try {
            Assert-RpPlainPath $directory $Root
            foreach ($entry in ([IO.DirectoryInfo]::new($directory)).EnumerateFileSystemInfos()) {
                $relative = [IO.Path]::GetRelativePath($Root,$entry.FullName).Replace('\','/')
                try {
                    if (($entry.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
                        $excluded.Add([ordered]@{relative=$relative;reason='REPARSE_NOT_TRAVERSED'})
                        continue
                    }
                    if ($entry -is [IO.DirectoryInfo]) { $stack.Push($entry.FullName); continue }
                    if ($entry -isnot [IO.FileInfo]) {
                        $excluded.Add([ordered]@{relative=$relative;reason='NOT_REGULAR'})
                        continue
                    }
                    Assert-RpPlainPath $entry.FullName $Root
                    $before = Get-Item -LiteralPath $entry.FullName -Force
                    $bytes = [long]$before.Length
                    $mtime = $before.LastWriteTimeUtc.Ticks
                    $hash = (Get-FileHash -LiteralPath $entry.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
                    $after = Get-Item -LiteralPath $entry.FullName -Force
                    if ($bytes -ne $after.Length -or $mtime -ne $after.LastWriteTimeUtc.Ticks) {
                        throw [IO.IOException]::new('File changed while being hashed')
                    }
                    $files.Add([ordered]@{relative=$relative;bytes=$bytes;sha256=$hash})
                } catch { Add-RpFailure $Phase $relative $_.Exception }
            }
        } catch { Add-RpFailure $Phase ([IO.Path]::GetRelativePath($Root,$directory)) $_.Exception }
    }
    return [ordered]@{files=$files.ToArray();excluded=$excluded.ToArray()}
}

function Test-RpSameFiles($Left, $Right) {
    $map = [Collections.Generic.Dictionary[string,object]]::new([StringComparer]::Ordinal)
    foreach ($row in $Left) {
        if ($map.ContainsKey($row.relative)) { return $false }
        $map.Add($row.relative,$row)
    }
    if ($map.Count -ne $Right.Count) { return $false }
    $seen = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($row in $Right) {
        if (-not $seen.Add($row.relative) -or -not $map.ContainsKey($row.relative)) { return $false }
        $old = $map[$row.relative]
        if ($old.bytes -ne $row.bytes -or $old.sha256 -cne $row.sha256) { return $false }
    }
    return $true
}

# Guard the exact destination before creating anything. Never reuse a directory.
$rpOwnedRoot = [IO.Path]::GetFullPath($rpOwnedRoot).TrimEnd('\')
Assert-RpPlainPath $rpOwnedRoot $rpOwnedRoot
$rpTarget = [IO.Path]::GetFullPath((Join-Path $rpOwnedRoot $DestinationName))
if (-not [IO.Path]::GetDirectoryName($rpTarget).Equals($rpOwnedRoot,[StringComparison]::OrdinalIgnoreCase)) {
    throw 'Destination must be one new child of the exact approved private root'
}
if (Test-Path -LiteralPath $rpTarget) { throw 'No overwrite: destination already exists' }
$null = New-Item -ItemType Directory -Path $rpTarget -ErrorAction Stop
$rpPayload = Join-Path $rpTarget 'userdata'
$null = New-Item -ItemType Directory -Path $rpPayload -ErrorAction Stop
$rpBefore = Read-RpTree $rpSourceRoot 'source_before'

if ($rpBefore.files.Count -ne $ExpectedFileCount) {
    Add-RpFailure 'source_before' '.' ([IO.IOException]::new('Expected native file count mismatch; do not substitute the redirected Python view'))
}
if ($rpBefore.excluded.Count) {
    Add-RpFailure 'source_before' '.' ([IO.IOException]::new('Excluded entries require review; a complete staging PASS is not justified'))
}

if (-not $rpErrors.Count) {
    foreach ($row in $rpBefore.files) {
        try {
            $source = Join-Path $rpSourceRoot $row.relative
            Assert-RpPlainPath $source $rpSourceRoot
            $dest = [IO.Path]::GetFullPath((Join-Path $rpPayload $row.relative))
            if (-not $dest.StartsWith($rpPayload+'\',[StringComparison]::OrdinalIgnoreCase)) {
                throw [IO.IOException]::new('Payload path escaped its fresh root')
            }
            $null = [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($dest))
            Assert-RpPlainPath ([IO.Path]::GetDirectoryName($dest)) $rpPayload
            $inputStream = $null
            $outputStream = $null
            try {
                # Deny active writers on the source and overwrite on the copy.
                $inputStream = [IO.File]::Open($source,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read)
                $outputStream = [IO.File]::Open($dest,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None)
                $inputStream.CopyTo($outputStream,1048576)
                $outputStream.Flush()
            } finally {
                if ($outputStream) { $outputStream.Dispose() }
                if ($inputStream) { $inputStream.Dispose() }
            }
            $copied = Get-Item -LiteralPath $dest -Force
            $hash = (Get-FileHash -LiteralPath $dest -Algorithm SHA256).Hash.ToLowerInvariant()
            if ($copied.Length -ne $row.bytes -or $hash -cne $row.sha256) {
                throw [IO.IOException]::new('Paired native source/copy hash mismatch')
            }
            $rpCopies.Add([ordered]@{relative=$row.relative;bytes=[long]$copied.Length;sha256=$hash})
        } catch { Add-RpFailure 'copy' $row.relative $_.Exception }
    }
}

# Re-enumerate AND rehash both trees; no source-driven-only/vacuous set check.
$rpAfter = Read-RpTree $rpSourceRoot 'source_after'
$rpDestination = Read-RpTree $rpPayload 'destination_after'
$rpStable = Test-RpSameFiles $rpBefore.files $rpAfter.files
$rpExact = Test-RpSameFiles $rpBefore.files $rpDestination.files
$rpPaired = Test-RpSameFiles $rpBefore.files $rpCopies.ToArray()
if (-not $rpStable -or -not $rpExact -or -not $rpPaired -or $rpAfter.excluded.Count -or $rpDestination.excluded.Count) {
    Add-RpFailure 'final_comparison' '.' ([IO.IOException]::new('Source/copy/destination full pathsets, sizes and SHA256 must match without exclusions'))
}
$rpResult = if ($rpErrors.Count -eq 0 -and $rpBefore.files.Count -gt 0) { 'PASS' } else { 'INCOMPLETE' }
$rpReceipt = [ordered]@{
    schema=1;kind='PRIVATE_NATIVE_USERDATA_STAGING_ONLY';result=$rpResult
    started_utc=$rpStarted.ToString('o');completed_utc=[DateTime]::UtcNow.ToString('o')
    execution_requirement='Direct native PowerShell tool; not a PowerShell subprocess launched from packaged Python. Namespace is not automatically detected.'
    source_root=$rpSourceRoot;destination_root=$rpTarget;payload_relative='userdata';expected_native_file_count=$ExpectedFileCount
    source_before=$rpBefore;copied=$rpCopies.ToArray();source_after=$rpAfter;destination_after=$rpDestination
    checks=@{source_stable=$rpStable;destination_exact=$rpExact;paired_copy_exact=$rpPaired};errors=$rpErrors.ToArray()
    next_required_check='Packaged Python must independently see every staged payload relative path, byte size and SHA256 from this native receipt (144 observed in preflight, or the explicitly reviewed fresh native count). Then inventory the exact approved RootOwned root and COPY_ALL; verify final D payload against these native hashes.'
    boundary='Private C staging only; no final D backup, public release, Git bundle or app acceptance is established.'
}
$rpReceiptPath = Join-Path $rpTarget 'PRIVATE-NATIVE-USERDATA-RECEIPT.json'
$rpJsonBytes = [Text.Encoding]::UTF8.GetBytes(($rpReceipt | ConvertTo-Json -Depth 9))
$rpReceiptStream = [IO.File]::Open($rpReceiptPath,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write)
try { $rpReceiptStream.Write($rpJsonBytes) } finally { $rpReceiptStream.Dispose() }
[ordered]@{result=$rpResult;source_files=$rpBefore.files.Count;copied_files=$rpCopies.Count;destination_files=$rpDestination.files.Count;errors=$rpErrors.Count;excluded=($rpBefore.excluded.Count+$rpAfter.excluded.Count+$rpDestination.excluded.Count);receipt=$rpReceiptPath} | ConvertTo-Json -Compress
if ($rpResult -ne 'PASS') { exit 2 }
