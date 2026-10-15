$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'offline_export_log.ps1')
$escape = [char]27
$coloredPath = "${escape}[32m[ 55% ] savepack | Storing File: res://challenges/catalog.json${escape}[39m${escape}[0m`r`n"
$paths = @(Get-OfflineExportResourcePaths $coloredPath)
if ($paths.Count -ne 1 -or $paths[0] -cne 'res://challenges/catalog.json') { throw 'Colored resource path comparison failed.' }
Assert-OfflineExportLog "${escape}[32mCONVEYOR_CHALLENGE_VERIFY PASS${escape}[0m" 'CONVEYOR_CHALLENGE_VERIFY PASS'
$checks = 2
foreach ($failure in @("${escape}[31mERROR: export plugin failed${escape}[0m`nCONVEYOR_CHALLENGE_VERIFY PASS",
    " `t${escape}[31mERROR: indented export plugin failed${escape}[0m`nCONVEYOR_CHALLENGE_VERIFY PASS",
    "${escape}[31mCONVEYOR_CHALLENGE_VERIFY FAIL${escape}[0m`nCONVEYOR_CHALLENGE_VERIFY PASS",
    "${escape}[31mOFFLINE_BUILD_POLICY FAIL${escape}[0m`nCONVEYOR_CHALLENGE_VERIFY PASS", 'No acceptance marker')) {
    $rejected = $false
    try { Assert-OfflineExportLog $failure 'CONVEYOR_CHALLENGE_VERIFY PASS' } catch { $rejected = $true }
    if (!$rejected) { throw 'Display encoding concealed a failure or missing marker.' }
    $checks++
}
Write-Output "OFFLINE_EXPORT_LOG_TESTS_PASS checks=$checks coloredResource=true coloredErrorRejected=true"
