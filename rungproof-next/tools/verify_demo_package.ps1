[CmdletBinding()]
param([Parameter(Mandatory = $true)][string]$PackageDirectory)

# Read-only check: no engine execution. Every ordinary file is inventoried,
# including build evidence. Only candidate-manifest.json is excluded because
# a manifest cannot contain its own hash; retain that hash independently.
$ErrorActionPreference = 'Stop'
function Assert-CanonicalPath([string]$Relative) {
    if ([string]::IsNullOrWhiteSpace($Relative) -or $Relative.Contains('\') -or
        [IO.Path]::IsPathRooted($Relative)) { throw 'Manifest contains a noncanonical file path.' }
    foreach ($segment in $Relative.Split('/')) {
        if (!$segment -or $segment -in @('.', '..') -or $segment -match '[<>:"|?*\x00-\x1f]' -or
            $segment -match '[ .]$' -or $segment -match '^(?i:CON|PRN|AUX|NUL|COM[1-9¹²³]|LPT[1-9¹²³])(?:\.|$)') {
            throw "Manifest contains a noncanonical Windows file path: $Relative."
        }
    }
}
try {
    $root = [IO.Path]::GetFullPath($PackageDirectory).TrimEnd('\', '/')
    if (!(Test-Path -LiteralPath $root -PathType Container)) { throw 'Package directory is missing.' }
    # Reject redirects before recursion, including a redirected root/ancestor.
    $cursor = $root
    while ($cursor) {
        if (((Get-Item -LiteralPath $cursor -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw 'Package contains a redirected file or directory.'
        }
        $cursor = [IO.Path]::GetDirectoryName($cursor)
    }
    $actual = [Collections.Generic.Dictionary[string,object]]::new([StringComparer]::OrdinalIgnoreCase)
    $pending = [Collections.Generic.Stack[string]]::new()
    $pending.Push($root)
    while ($pending.Count) {
        foreach ($item in Get-ChildItem -LiteralPath $pending.Pop() -Force) {
            if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
                throw 'Package contains a redirected file or directory.'
            }
            if ($item.PSIsContainer) { $pending.Push($item.FullName); continue }
            $relative = [IO.Path]::GetRelativePath($root, $item.FullName).Replace('\', '/')
            Assert-CanonicalPath $relative
            if ($relative -ceq 'candidate-manifest.json') { continue }
            if (!$actual.TryAdd($relative, $item)) { throw 'Package contains case-aliased files.' }
        }
    }
    $manifestPath = Join-Path $root 'candidate-manifest.json'
    $manifestFile = Get-Item -LiteralPath $manifestPath -Force
    if ($manifestFile.PSIsContainer -or $manifestFile.Length -gt 5MB) { throw 'Package manifest is invalid or unexpectedly large.' }
    $manifest = [IO.File]::ReadAllText($manifestPath, [Text.UTF8Encoding]::new($false, $true)) | ConvertFrom-Json
    if (($manifest.schemaVersion -isnot [int] -and $manifest.schemaVersion -isnot [long]) -or
        $manifest.schemaVersion -ne 1 -or $manifest.policy -isnot [string] -or
        $manifest.policy -cne 'offline-demo' -or $manifest.files -isnot [array] -or !$manifest.files.Count) {
        throw 'Unsupported or empty offline-package manifest.'
    }
    $seen = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    $seenExact = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($entry in $manifest.files) {
        if ($entry.path -isnot [string]) { throw 'Manifest file path must be a string.' }
        $relative = $entry.path
        Assert-CanonicalPath $relative
        if ($relative -ieq 'candidate-manifest.json' -or !$seen.Add($relative)) {
            throw 'Manifest contains a self-reference or duplicate file path.'
        }
        [void]$seenExact.Add($relative)
        if (!$actual.ContainsKey($relative)) { throw "Listed file missing: $relative." }
        $file = $actual[$relative]
        $actualRelative = [IO.Path]::GetRelativePath($root, $file.FullName).Replace('\', '/')
        if ($relative -cne $actualRelative) { throw "Manifest case alias: $relative." }
        if ($entry.sha256 -isnot [string] -or $entry.sha256 -notmatch '^[A-Fa-f0-9]{64}$' -or
            ($entry.bytes -isnot [long] -and $entry.bytes -isnot [int]) -or $entry.bytes -lt 0 -or
            $file.Length -ne $entry.bytes -or
            (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash -ne $entry.sha256) {
            throw "Damaged or inconsistent file: $relative. Restore the entire verified package."
        }
    }
    foreach ($relative in $actual.Keys) {
        if (!$seen.Contains($relative)) { throw "Unexpected unlisted file: $relative." }
    }
    # Names are grounded in build_offline_demo.ps1. Notices verification means
    # presence/inventory/hash consistency, never legal clearance or authenticity.
    $critical = @('RungProof.exe', 'RungProof.console.exe', 'RungProof.pck',
        'data_RungProof.Next_windows_x86_64/RungProof.Next.dll', 'Verify-Package.ps1', 'README.md',
        'GODOT_LICENSE.txt', 'GODOT_COPYRIGHT.txt', 'DOTNET_LICENSE.txt', 'DOTNET_ThirdPartyNotices.txt')
    foreach ($relative in $critical) {
        if (!$seenExact.Contains($relative)) { throw "Manifest omits required exact file name: $relative." }
    }
    Write-Output "PACKAGE_INTEGRITY_PASS files=$($seen.Count); engine not launched; notices checked for presence and hashes only; release approval remains separate."
    exit 0
}
catch {
    Write-Output "PACKAGE_INTEGRITY_FAIL $($_.Exception.Message)"
    exit 1
}
