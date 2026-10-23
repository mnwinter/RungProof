$ErrorActionPreference = 'Stop'
$rpSource = 'C:\Users\matt.winter\AppData\Roaming\Godot\app_userdata\RungProof'
$rpTarget = 'D:\RungProof-Backup-2026-10-22\Godot-userdata-RungProof'
$rpRows = @(Get-ChildItem -LiteralPath $rpSource -File -Recurse -Force | ForEach-Object {
  $rpRelative = [IO.Path]::GetRelativePath($rpSource, $_.FullName)
  $rpDest = Join-Path $rpTarget $rpRelative
  $rpHash = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash
  $rpCopy = Get-Item -LiteralPath $rpDest
  $rpDestHash = (Get-FileHash -LiteralPath $rpDest -Algorithm SHA256).Hash
  [PSCustomObject]@{ relative=$rpRelative; bytes=$_.Length; sha256=$rpHash; matched=($_.Length -eq $rpCopy.Length -and $rpHash -eq $rpDestHash) }
})
$rpRows | ConvertTo-Json -Depth 4 | Set-Content 'D:\RungProof-Backup-2026-10-22\verified-native-userdata-sha256.json' -Encoding UTF8
$rpBad = @($rpRows | Where-Object { -not $_.matched })
[PSCustomObject]@{ result= $(if ($rpBad.Count -eq 0) {'PASS'} else {'FAIL'}); files=$rpRows.Count; mismatch_count=$rpBad.Count; method='Native PowerShell Get-FileHash real Godot APPDATA, not redirected packaged Python view' } | ConvertTo-Json | Tee-Object -FilePath 'D:\RungProof-Backup-2026-10-22\verification-native-userdata.json'
if ($rpBad.Count) { exit 1 }
