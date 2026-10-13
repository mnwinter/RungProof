$ErrorActionPreference = 'Stop'
$checker = Join-Path $PSScriptRoot 'verify_demo_package.ps1'
$root = Join-Path $PSScriptRoot "../.tools/studio-verification/t9-checker/fixtures/$([Guid]::NewGuid().ToString('N'))"
$required = @('RungProof.exe','RungProof.console.exe','RungProof.pck','data_RungProof.Next_windows_x86_64/RungProof.Next.dll','Verify-Package.ps1','README.md','GODOT_LICENSE.txt','GODOT_COPYRIGHT.txt','DOTNET_LICENSE.txt','DOTNET_ThirdPartyNotices.txt')
$paths = $required + @('build-evidence/export.log','data_RungProof.Next_windows_x86_64/runtime.dll')
foreach ($path in $paths) {
    $full = Join-Path $root $path
    New-Item -ItemType Directory -Path ([IO.Path]::GetDirectoryName($full)) -Force | Out-Null
    [IO.File]::WriteAllText($full, 'fixture bytes; never execute')
}
Copy-Item -LiteralPath $checker -Destination (Join-Path $root 'Verify-Package.ps1')
$rows = @($paths | ForEach-Object {
    $file = Get-Item -LiteralPath (Join-Path $root $_)
    @{ path = $_; bytes = $file.Length; sha256 = (Get-FileHash -LiteralPath $file.FullName).Hash }
})
function Write-Manifest($entries) {
    $json = @{ schemaVersion = 1; policy = 'offline-demo'; files = @($entries) } | ConvertTo-Json -Depth 6
    [IO.File]::WriteAllText((Join-Path $root 'candidate-manifest.json'), $json, [Text.UTF8Encoding]::new($false, $true))
}
$script:checks = 0
function Check($expected, $label, $scriptPath = $checker) {
    $output = & (Join-Path $PSHOME 'pwsh.exe') -NoProfile -File $scriptPath -PackageDirectory $root 2>&1
    if ($LASTEXITCODE -ne $expected) { throw "$label returned $LASTEXITCODE, expected $expected : $output" }
    $script:checks++
    Write-Output "PASS $label : $output"
}
Write-Manifest $rows
Check 0 'complete inventory including evidence'
Check 0 'copied checker accepts same inventory' (Join-Path $root 'Verify-Package.ps1')
[IO.File]::WriteAllText((Join-Path $root 'unexpected.dll'), 'extra')
Check 1 'unexpected unlisted extra rejected'
Remove-Item -LiteralPath (Join-Path $root 'unexpected.dll')
foreach ($path in $required) {
    Write-Manifest @($rows | Where-Object { $_.path -cne $path })
    Check 1 "omitted critical inventory entry $path rejected"
    $full = Join-Path $root $path
    Move-Item -LiteralPath $full -Destination "$full.withheld"
    # Withhold outside the package so this specifically exercises the required
    # name gate rather than simply another unexpected-inventory failure.
    $withheld = Join-Path ([IO.Path]::GetDirectoryName($root)) ([Guid]::NewGuid().ToString('N'))
    Move-Item -LiteralPath "$full.withheld" -Destination $withheld
    Check 1 "critical file absent from disk and manifest $path rejected"
    Move-Item -LiteralPath $withheld -Destination $full
}
Write-Manifest $rows
$target = Join-Path $root 'RungProof.pck'
[IO.File]::WriteAllText($target, 'fixture bytes; never executf')
Check 1 'same length hash mutation rejected'
[IO.File]::WriteAllText($target, 'fixture bytes; never execute')
Move-Item -LiteralPath $target -Destination "$target.withheld"
Check 1 'listed missing file rejected'
Move-Item -LiteralPath "$target.withheld" -Destination $target
foreach ($alias in @('RungProof.exe','rungproof.exe','./RungProof.exe','data_RungProof.Next_windows_x86_64/../RungProof.exe','../outside','RungProof.exe.','RungProof.exe:stream','data_RungProof.Next_windows_x86_64\RungProof.Next.dll','CON')) {
    Write-Manifest ($rows + @{ path = $alias; bytes = $rows[0].bytes; sha256 = $rows[0].sha256 })
    Check 1 "duplicate or noncanonical path $alias rejected"
}
$wrongCase = @($rows | ForEach-Object { @{ path = $_.path; bytes = $_.bytes; sha256 = $_.sha256 } })
$wrongCase[0].path = 'rungproof.exe'
Write-Manifest $wrongCase
Check 1 'single case alias rejected against actual inventory'
Write-Manifest ($rows + @{ path = 'candidate-manifest.json'; bytes = 1; sha256 = ('A' * 64) })
Check 1 'self manifest entry rejected'
Write-Manifest @()
Check 1 'empty manifest rejected'
foreach ($schemaJson in @('"1"', '1.0')) {
    Write-Manifest $rows
    $manifestPath = Join-Path $root 'candidate-manifest.json'
    $json = [IO.File]::ReadAllText($manifestPath) -replace '"schemaVersion": 1', ('"schemaVersion": ' + $schemaJson)
    [IO.File]::WriteAllText($manifestPath, $json)
    Check 1 "noninteger schemaVersion $schemaJson rejected"
}
foreach ($policyJson in @('"OFFLINE-DEMO"', '1')) {
    Write-Manifest $rows
    $manifestPath = Join-Path $root 'candidate-manifest.json'
    $json = [IO.File]::ReadAllText($manifestPath) -replace '"policy": "offline-demo"', ('"policy": ' + $policyJson)
    [IO.File]::WriteAllText($manifestPath, $json)
    Check 1 "invalid policy $policyJson rejected"
}
# A singleton object used to be accepted through PowerShell enumeration.
$singleton = @{ schemaVersion = 1; policy = 'offline-demo'; files = $rows[0] } | ConvertTo-Json -Depth 6
[IO.File]::WriteAllText((Join-Path $root 'candidate-manifest.json'), $singleton)
Check 1 'files object instead of array rejected'
foreach ($critical in $required) {
    # Preserve parent-directory spelling; rename the actual critical leaf so
    # this reaches the required-name gate rather than a parent case mismatch.
    $slash = $critical.LastIndexOf('/')
    $lower = if ($slash -ge 0) {
        $critical.Substring(0, $slash + 1) + $critical.Substring($slash + 1).ToLowerInvariant()
    } else { $critical.ToLowerInvariant() }
    if ($critical -ceq $lower) { continue }
    $renamed = @($rows | ForEach-Object { @{ path = $_.path; bytes = $_.bytes; sha256 = $_.sha256 } })
    ($renamed | Where-Object { $_.path -ceq $critical }).path = $lower
    $full = Join-Path $root $critical
    $temporary = Join-Path ([IO.Path]::GetDirectoryName($root)) ([Guid]::NewGuid().ToString('N'))
    Move-Item -LiteralPath $full -Destination $temporary
    $lowerFull = Join-Path $root $lower
    New-Item -ItemType Directory -Path ([IO.Path]::GetDirectoryName($lowerFull)) -Force | Out-Null
    Move-Item -LiteralPath $temporary -Destination $lowerFull
    Write-Manifest $renamed
    Check 1 "critical disk and manifest lowercase rename $critical rejected"
    Move-Item -LiteralPath $lowerFull -Destination $temporary
    Move-Item -LiteralPath $temporary -Destination $full
}
Write-Manifest $rows
Check 0 'restored complete inventory'
Write-Output "PACKAGE_CHECKER_TESTS_PASS $script:checks; notices presence and hashes only, not license clearance"
exit 0
