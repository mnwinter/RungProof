[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$SourceRoot,
    [Parameter(Mandatory = $true)][string]$OutputPath
)

# Independent producer: hashes normalized source bytes, never launches Godot or
# executes learner programs. Source arrays use the RP-CANON-1 representation.
$ErrorActionPreference = 'Stop'
$challengeRoot = [IO.Path]::GetFullPath($SourceRoot)
$challengeOutput = [IO.Path]::GetFullPath($OutputPath)
$utf8 = [Text.UTF8Encoding]::new($false, $true)

function Get-NormalizedBytes([string]$Path) {
    $sourceText = [IO.File]::ReadAllText($Path, $utf8)
    return ,$utf8.GetBytes($sourceText.Replace("`r`n", "`n").Replace("`r", "`n"))
}

function Get-BytesSha256([byte[]]$Bytes) {
    $hasher = [Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($hasher.ComputeHash($Bytes))).Replace('-', '').ToLowerInvariant() }
    finally { $hasher.Dispose() }
}

function Get-SourceEntry([string]$RelativePath) {
    $path = Join-Path $challengeRoot $RelativePath
    if (!(Test-Path -LiteralPath $path -PathType Leaf)) { throw "Missing semantic source: $RelativePath" }
    $bytes = Get-NormalizedBytes $path
    return [ordered]@{ byteLength = [long]$bytes.Length; relativePath = $RelativePath; sha256 = Get-BytesSha256 $bytes }
}

function Convert-JsonString([string]$Value) {
    # System.Text.Json's default encoder matches RP-CANON-1, including escapes
    # for HTML-sensitive and non-ASCII characters; ConvertTo-Json differs here.
    return [System.Text.Json.JsonSerializer]::Serialize($Value, [type][string], [System.Text.Json.JsonSerializerOptions]::new())
}

function Get-CanonicalSourceArray([object[]]$Entries) {
    $rows = foreach ($entry in $Entries) {
        '{"byteLength":' + $entry.byteLength.ToString([Globalization.CultureInfo]::InvariantCulture) +
            ',"relativePath":' + (Convert-JsonString $entry.relativePath) +
            ',"sha256":' + (Convert-JsonString $entry.sha256) + '}'
    }
    return '[' + ($rows -join ',') + ']'
}

$controllerDirectory = Join-Path $challengeRoot 'src/VirtualController'
$contractsDirectory = Join-Path $challengeRoot 'src/Challenges'
if (!(Test-Path -LiteralPath $controllerDirectory -PathType Container) -or
    !(Test-Path -LiteralPath $contractsDirectory -PathType Container)) { throw 'Required semantic source directories are missing.' }
$enginePaths = [Collections.Generic.List[string]]::new()
foreach ($directory in @($controllerDirectory, $contractsDirectory)) {
    foreach ($file in Get-ChildItem -LiteralPath $directory -Filter '*.cs' -File -Recurse) {
        $enginePaths.Add([IO.Path]::GetRelativePath($challengeRoot, $file.FullName).Replace('\', '/'))
    }
}
$enginePaths.Add('src/Scenes/ConveyorPlantModel.cs')
if (!(Test-Path -LiteralPath (Join-Path $challengeRoot 'src/Challenges/ChallengeProgramIdentity.cs') -PathType Leaf)) { throw 'Canonicalizer implementation source is missing.' }
if (!@($enginePaths | Where-Object { $_.StartsWith('src/VirtualController/', [StringComparison]::Ordinal) }).Count) { throw 'Controller source set is empty.' }
$enginePaths.Sort([StringComparer]::Ordinal)
$evaluatorPaths = [string[]]@('src/Challenges/ChallengeEvaluator.cs', 'src/Challenges/ChallengeTestRunner.cs', 'src/Challenges/ConveyorChallengeProtocol.cs')
[Array]::Sort($evaluatorPaths, [StringComparer]::Ordinal)
$engineEntries = @($enginePaths | ForEach-Object { Get-SourceEntry $_ })
$evaluatorEntries = @($evaluatorPaths | ForEach-Object { Get-SourceEntry $_ })
$manifest = [ordered]@{
    schemaVersion = 1
    identityFormat = 'RP-CANON-1'
    engineRevision = 'conveyor-engine-v1'
    engineSha256 = Get-BytesSha256 ($utf8.GetBytes((Get-CanonicalSourceArray $engineEntries)))
    evaluatorRevision = 'conveyor-evaluator-v1'
    evaluatorSha256 = Get-BytesSha256 ($utf8.GetBytes((Get-CanonicalSourceArray $evaluatorEntries)))
    generatorSha256 = Get-BytesSha256 (Get-NormalizedBytes $PSCommandPath)
    engineInputs = $engineEntries
    evaluatorInputs = $evaluatorEntries
}
$parent = [IO.Path]::GetDirectoryName($challengeOutput)
[IO.Directory]::CreateDirectory($parent) | Out-Null
$temporary = $challengeOutput + '.' + [Guid]::NewGuid().ToString('N') + '.tmp'
try {
    [IO.File]::WriteAllText($temporary, (($manifest | ConvertTo-Json -Depth 8) + "`n"), $utf8)
    [IO.File]::Move($temporary, $challengeOutput, $true)
}
finally { if ([IO.File]::Exists($temporary)) { [IO.File]::Delete($temporary) } }
Write-Output "CHALLENGE_SEMANTIC_MANIFEST_PASS engineInputs=$($engineEntries.Count) evaluatorInputs=$($evaluatorEntries.Count) output=$challengeOutput"
