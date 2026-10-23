# Persistent observation-only OCR worker. Requests reference already saved
# native PNGs inside this audit directory; there is no application state API.
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Runtime.WindowsRuntime
$null = [Windows.Storage.StorageFile, Windows.Storage, ContentType=WindowsRuntime]
$null = [Windows.Storage.Streams.IRandomAccessStream, Windows.Storage.Streams, ContentType=WindowsRuntime]
$null = [Windows.Graphics.Imaging.BitmapDecoder, Windows.Graphics.Imaging, ContentType=WindowsRuntime]
$null = [Windows.Graphics.Imaging.SoftwareBitmap, Windows.Graphics.Imaging, ContentType=WindowsRuntime]
$null = [Windows.Media.Ocr.OcrEngine, Windows.Foundation, ContentType=WindowsRuntime]
$null = [Windows.Media.Ocr.OcrResult, Windows.Foundation, ContentType=WindowsRuntime]
$asTask = [System.WindowsRuntimeSystemExtensions].GetMethods() | Where-Object {
    $_.Name -eq 'AsTask' -and $_.IsGenericMethodDefinition -and
    $_.GetGenericArguments().Count -eq 1 -and $_.GetParameters().Count -eq 1 -and
    $_.GetParameters()[0].ParameterType.Name -eq 'IAsyncOperation`1'
} | Select-Object -First 1
function Await-OcrOperation($Operation, [type]$ResultType) {
    $task = $asTask.MakeGenericMethod($ResultType).Invoke($null, @($Operation))
    $task.Wait()
    return $task.Result
}
$engine = [Windows.Media.Ocr.OcrEngine]::TryCreateFromUserProfileLanguages()
if ($null -eq $engine) { throw 'No installed Windows OCR language.' }
$taskOcrRoot = [IO.Path]::GetFullPath($PSScriptRoot).TrimEnd('\') + '\'
$taskCutoff = [DateTimeOffset]::Parse('2026-10-23T11:00:00Z')
while ($null -ne ($taskRequestLine = [Console]::ReadLine())) {
    try {
        if ([DateTimeOffset]::UtcNow -ge $taskCutoff) { throw 'User cutoff reached; no new OCR work.' }
        $taskRequest = $taskRequestLine | ConvertFrom-Json
        $ImagePath = [IO.Path]::GetFullPath([string]$taskRequest.image)
        $OutputPath = [IO.Path]::GetFullPath([string]$taskRequest.output)
        if (-not $ImagePath.StartsWith($taskOcrRoot, [StringComparison]::OrdinalIgnoreCase) -or
            -not $OutputPath.StartsWith($taskOcrRoot, [StringComparison]::OrdinalIgnoreCase) -or
            [IO.Path]::GetExtension($ImagePath) -ne '.png') { throw 'Only this audit folder native PNGs are allowed.' }
$resolvedImage = (Resolve-Path -LiteralPath $ImagePath).Path
$file = Await-OcrOperation ([Windows.Storage.StorageFile]::GetFileFromPathAsync($resolvedImage)) ([Windows.Storage.StorageFile])
$stream = Await-OcrOperation ($file.OpenAsync([Windows.Storage.FileAccessMode]::Read)) ([Windows.Storage.Streams.IRandomAccessStream])
try {
    $decoder = Await-OcrOperation ([Windows.Graphics.Imaging.BitmapDecoder]::CreateAsync($stream)) ([Windows.Graphics.Imaging.BitmapDecoder])
    $bitmap = Await-OcrOperation ($decoder.GetSoftwareBitmapAsync()) ([Windows.Graphics.Imaging.SoftwareBitmap])
    try {
        $result = Await-OcrOperation ($engine.RecognizeAsync($bitmap)) ([Windows.Media.Ocr.OcrResult])
        $lineResults = @($result.Lines | ForEach-Object {
            $line = $_
            $words = @($line.Words | ForEach-Object {
                $bounds = $_.BoundingRect
                [ordered]@{text=$_.Text; x=[math]::Round($bounds.X,1); y=[math]::Round($bounds.Y,1); w=[math]::Round($bounds.Width,1); h=[math]::Round($bounds.Height,1)}
            })
            [ordered]@{text=$line.Text; words=$words}
        })
        $json = [ordered]@{image=$resolvedImage; width=$bitmap.PixelWidth; height=$bitmap.PixelHeight; language=$engine.RecognizerLanguage.LanguageTag; text=$result.Text; lines=$lineResults} | ConvertTo-Json -Depth 8 -Compress
        if ($OutputPath) {
            if (Test-Path -LiteralPath $OutputPath) { throw 'Refusing to overwrite an existing OCR result.' }
            [System.IO.File]::WriteAllText($ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputPath), $json, [System.Text.UTF8Encoding]::new($false))
        }
        $json
    } finally { if ($bitmap) { $bitmap.Dispose() } }
} finally { $stream.Dispose() }
    } catch {
        @{ error = $_.Exception.Message } | ConvertTo-Json -Compress
    }
}
