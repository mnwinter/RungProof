[CmdletBinding()]
param([string]$EvidenceDirectory = '')
$ErrorActionPreference = 'Stop'
if (!$EvidenceDirectory) { $EvidenceDirectory = Join-Path ([IO.Path]::GetTempPath()) ('rungproof-manifest-' + [Guid]::NewGuid().ToString('N')) }
$fixtureRoot = [IO.Path]::GetFullPath($EvidenceDirectory)
if (Test-Path -LiteralPath $fixtureRoot) { throw 'Use a fresh fixture directory.' }
$utf8 = [Text.UTF8Encoding]::new($false)
foreach ($relative in @('src/VirtualController', 'src/Challenges', 'src/Scenes')) {
    [IO.Directory]::CreateDirectory((Join-Path $fixtureRoot $relative)) | Out-Null
}
$sourcePaths = @('src/VirtualController/Runtime.cs', 'src/Scenes/ConveyorPlantModel.cs',
    'src/Challenges/ChallengeProgramIdentity.cs', 'src/Challenges/ChallengeEvaluator.cs',
    'src/Challenges/ChallengeTestRunner.cs', 'src/Challenges/ConveyorChallengeProtocol.cs')
foreach ($relative in $sourcePaths) { [IO.File]::WriteAllText((Join-Path $fixtureRoot $relative), "// fixture`r`n", $utf8) }
$manifestPath = Join-Path $fixtureRoot 'manifest.json'
$producer = Join-Path $PSScriptRoot 'build_challenge_semantic_manifest.ps1'
& $producer -SourceRoot $fixtureRoot -OutputPath $manifestPath | Out-Null
$firstBytes = [IO.File]::ReadAllText($manifestPath)
$first = $firstBytes | ConvertFrom-Json
if ($first.engineInputs.Count -ne 6 -or $first.evaluatorInputs.Count -ne 3) { throw 'Wrong source coverage.' }
& $producer -SourceRoot $fixtureRoot -OutputPath $manifestPath | Out-Null
if ([IO.File]::ReadAllText($manifestPath) -cne $firstBytes) { throw 'Manifest is nondeterministic.' }
foreach ($relative in $sourcePaths) { [IO.File]::WriteAllText((Join-Path $fixtureRoot $relative), "// fixture`n", $utf8) }
& $producer -SourceRoot $fixtureRoot -OutputPath $manifestPath | Out-Null
if ([IO.File]::ReadAllText($manifestPath) -cne $firstBytes) { throw 'Line-ending normalization differs.' }
[IO.File]::WriteAllText((Join-Path $fixtureRoot 'src/VirtualController/Runtime.cs'), "// changed`n", $utf8)
& $producer -SourceRoot $fixtureRoot -OutputPath $manifestPath | Out-Null
$changed = [IO.File]::ReadAllText($manifestPath) | ConvertFrom-Json
if ($changed.engineSha256 -ceq $first.engineSha256 -or $changed.evaluatorSha256 -cne $first.evaluatorSha256) { throw 'Controller/evaluator identity separation failed.' }
[IO.File]::WriteAllText((Join-Path $fixtureRoot 'src/Challenges/AddedEngine.cs'), "// added source`n", $utf8)
& $producer -SourceRoot $fixtureRoot -OutputPath $manifestPath | Out-Null
$added = [IO.File]::ReadAllText($manifestPath) | ConvertFrom-Json
if ($added.engineInputs.Count -ne 7 -or $added.engineSha256 -ceq $changed.engineSha256 -or $added.evaluatorSha256 -cne $changed.evaluatorSha256) { throw 'Added source membership did not change engine identity.' }
$acceptedOutput = [IO.File]::ReadAllText($manifestPath)
[IO.File]::Move((Join-Path $fixtureRoot 'src/Challenges/ChallengeEvaluator.cs'), (Join-Path $fixtureRoot 'src/Challenges/ChallengeEvaluator.cs.saved'))
$rejected = $false
try { & $producer -SourceRoot $fixtureRoot -OutputPath $manifestPath | Out-Null }
catch { $rejected = $true }
if (!$rejected -or [IO.File]::ReadAllText($manifestPath) -cne $acceptedOutput) { throw 'Missing required source did not preserve accepted output.' }
Write-Output "CHALLENGE_MANIFEST_PRODUCER_TESTS_PASS deterministic LF-normalized source-sensitive complete fail-closed; evidence=$fixtureRoot"
