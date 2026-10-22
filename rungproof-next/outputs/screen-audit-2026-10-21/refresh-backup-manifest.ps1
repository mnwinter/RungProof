# Output-only backup preparation. Never stages, commits, pushes, or modifies raw evidence.
$ErrorActionPreference = 'Stop'
$audit = $PSScriptRoot
$repo = [IO.Path]::GetFullPath((Join-Path $audit '../../..'))
$prefix = 'rungproof-next/outputs/screen-audit-2026-10-21'
$capability = 'http://(?:127\.0\.0\.1|localhost):[0-9]{2,5}/(?!LOCAL_CAPABILITY_REDACTED(?:[^A-Za-z0-9_-]|$))[A-Za-z0-9_-]{16,}'
Push-Location $repo
try {
    $dirty = @(@(git -c core.safecrlf=false diff --name-only 2>$null) + @(git ls-files --others --exclude-standard) | Sort-Object -Unique)
    $source = @($dirty | Where-Object { $_ -notlike 'rungproof-next/outputs/*' })
    $source | Set-Content (Join-Path $audit 'backup-source-paths.txt') -Encoding utf8
    $selected = [Collections.Generic.List[string]]::new()
    foreach ($path in $source) { $selected.Add($path) }
    foreach ($path in $dirty) {
        if ($path -notlike "$prefix/*") { continue }
        if ($path -match '/(backup-sanitized|backup-evidence-logs)/') { continue }
        if ($path -eq "$prefix/SESSION-STATE.json" -or $path -match '\.ocr\.json$') { continue }
        if ($path -match '\.(md|json|jsonl|ps1)$' -or $path -match '(?i)review[^/]*\.(jpg|png)$' -or $path -match '/fix-plant(213|216|219|223|224)[^/]*\.png$') { $selected.Add($path) }
    }
    # Retain the actual screen-control/OCR transport used to reproduce the audit.
    # These helpers receive the same all-selected-text capability redaction below.
    foreach ($helper in @('screen_bridge_v9.py','qa_input_transport.py','load_fixture_ui.py','ui_batch.py','monitored_ui_cases.py','wait_visible_ui.py')) {
        if (Test-Path -LiteralPath (Join-Path $audit $helper) -PathType Leaf) { $selected.Add("$prefix/$helper") }
    }
    # Current local continuity is copied, never overwritten, with local capability redacted.
    $session = [IO.File]::ReadAllText((Join-Path $audit 'SESSION-STATE.json'))
    $session = [regex]::Replace($session, $capability, 'http://127.0.0.1:51946/LOCAL_CAPABILITY_REDACTED')
    [IO.File]::WriteAllText((Join-Path $audit 'backup-session-state-sanitized.json'), $session, [Text.UTF8Encoding]::new($false))
    $selected.Add("$prefix/backup-session-state-sanitized.json")
    # *.log is ignored. Text copies preserve ignored build/test evidence, including B5 once present.
    $logDir = Join-Path $audit 'backup-evidence-logs'
    [IO.Directory]::CreateDirectory($logDir) | Out-Null
    $logManifest = @()
    foreach ($file in Get-ChildItem $audit -Filter 'remediation*.log' -File) {
        $dest = Join-Path $logDir ($file.Name + '.txt')
        $raw = [IO.File]::ReadAllText($file.FullName)
        $safe = [regex]::Replace($raw, $capability, 'http://127.0.0.1:51946/LOCAL_CAPABILITY_REDACTED')
        [IO.File]::WriteAllText($dest, $safe, [Text.UTF8Encoding]::new($false))
        $logManifest += [pscustomobject]@{original=$file.Name;original_sha256=(Get-FileHash $file.FullName -Algorithm SHA256).Hash.ToLower();copy="$prefix/backup-evidence-logs/$($file.Name).txt";copy_sha256=(Get-FileHash $dest -Algorithm SHA256).Hash.ToLower()}
        $selected.Add("$prefix/backup-evidence-logs/$($file.Name).txt")
    }
    $logManifest | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $logDir 'manifest.json') -Encoding utf8
    $selected.Add("$prefix/backup-evidence-logs/manifest.json")
    $selected.Add("$prefix/backup-final-readiness.json")
    $redactions = @()
    $final = [Collections.Generic.List[string]]::new()
    $textCount = 0
    foreach ($path in @($selected | Sort-Object -Unique)) {
        $absolute = Join-Path $repo $path
        if (!(Test-Path -LiteralPath $absolute -PathType Leaf)) { continue }
        if ([IO.Path]::GetExtension($path) -match '^\.(png|jpg|jpeg|gif|webp|glb|dll|exe|zip)$') { $final.Add($path); continue }
        $textCount++
        $raw = [IO.File]::ReadAllText($absolute)
        $hits = [regex]::Matches($raw, $capability)
        if (!$hits.Count) { $final.Add($path); continue }
        $hash = (Get-FileHash $absolute -Algorithm SHA256).Hash.ToLower()
        $leaf = $hash.Substring(0,10) + '-' + [IO.Path]::GetFileName($path)
        $dest = "$prefix/backup-sanitized/$leaf"
        [IO.Directory]::CreateDirectory((Join-Path $audit 'backup-sanitized')) | Out-Null
        [IO.File]::WriteAllText((Join-Path $repo $dest), [regex]::Replace($raw,$capability,'http://127.0.0.1:51946/LOCAL_CAPABILITY_REDACTED'), [Text.UTF8Encoding]::new($false))
        $final.Add($dest)
        $redactions += [pscustomobject]@{original=$path;original_sha256=$hash;sanitized=$dest;url_matches=$hits.Count}
    }
    $manifest = [pscustomobject]@{utc=[DateTime]::UtcNow.ToString('o');selected_text_files_scanned=$textCount;redacted_originals=$redactions;git_mutations=$false;scope='All selected text, arbitrary localhost bridge ports; raw files unchanged'}
    $manifest | ConvertTo-Json -Depth 7 | Set-Content (Join-Path $audit 'backup-capability-redaction-manifest.json') -Encoding utf8
    $final.Add("$prefix/backup-capability-redaction-manifest.json")
    $final.Add("$prefix/backup-source-paths.txt")
    $paths = @($final | Sort-Object -Unique)
    $paths | Set-Content (Join-Path $audit 'backup-proposed-staging-paths.txt') -Encoding utf8
    $bytes = 0L; $max = 0L
    foreach ($path in $paths) { $size = [IO.FileInfo]::new((Join-Path $repo $path)).Length; $bytes += $size; if ($size -gt $max) { $max=$size } }
    $summary = [pscustomobject]@{utc=[DateTime]::UtcNow.ToString('o');source_paths=$source.Count;selected_paths=$paths.Count;bytes=$bytes;largest_bytes=$max;text_files_scanned=$textCount;redacted_files=$redactions.Count;log_copies=$logManifest.Count;B5_logs_present=@($logManifest|Where-Object original -match 'b5').Count;raw_session_excluded=($paths -notcontains "$prefix/SESSION-STATE.json");git_mutations=$false;note='Refresh again after final results/report edits; source+curated backup, raw screenshot bulk retained locally'}
    $summary | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $audit 'backup-final-readiness.json') -Encoding utf8
    $summary | ConvertTo-Json -Compress
} finally { Pop-Location }
