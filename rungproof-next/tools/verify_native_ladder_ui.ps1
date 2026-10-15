param(
    [int]$WaitSeconds = 20
)

$ErrorActionPreference = 'Stop'

# This is intentionally an OS-level test. It does not call Godot controls,
# emit signals, or invoke editor methods. Every action below becomes a real
# Windows mouse input delivered to the RungProof window.
Add-Type -AssemblyName System.Drawing
Add-Type @'
using System;
using System.Runtime.InteropServices;

public static class RungProofNativeInput {
    [StructLayout(LayoutKind.Sequential)] public struct Rect { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] public struct Point { public int X, Y; }
    [StructLayout(LayoutKind.Sequential)] public struct MouseInput {
        public int dx, dy; public uint mouseData, flags, time; public IntPtr extra;
    }
    [StructLayout(LayoutKind.Sequential)] public struct Input {
        public uint type; public MouseInput mouse;
    }

    [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out Rect rect);
    [DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr h, out Rect rect);
    [DllImport("user32.dll")] public static extern bool ClientToScreen(IntPtr h, ref Point point);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern uint SendInput(uint count, Input[] inputs, int size);

    public static void ClickClient(IntPtr hwnd, int x, int y) {
        SetProcessDPIAware();
        SetForegroundWindow(hwnd);
        var point = new Point { X = x, Y = y };
        ClientToScreen(hwnd, ref point);
        SetCursorPos(point.X, point.Y);
        var inputs = new Input[2];
        inputs[0].type = 0; inputs[0].mouse.flags = 0x0002; // MOUSEEVENTF_LEFTDOWN
        inputs[1].type = 0; inputs[1].mouse.flags = 0x0004; // MOUSEEVENTF_LEFTUP
        SendInput(2, inputs, Marshal.SizeOf(typeof(Input)));
    }

}
'@

function Get-RungProofWindow {
    Get-Process -ErrorAction SilentlyContinue |
        Where-Object { $_.MainWindowTitle -eq 'RungProof (DEBUG)' } |
        Select-Object -First 1
}

function Save-Screenshot([IntPtr]$Handle, [string]$Name) {
    [RungProofNativeInput]::SetProcessDPIAware() | Out-Null
    $client = [RungProofNativeInput+Rect]::new()
    $origin = [RungProofNativeInput+Point]::new()
    [RungProofNativeInput]::GetClientRect($Handle, [ref]$client) | Out-Null
    [RungProofNativeInput]::ClientToScreen($Handle, [ref]$origin) | Out-Null
    $bitmap = New-Object System.Drawing.Bitmap $client.Right, $client.Bottom
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.CopyFromScreen($origin.X, $origin.Y, 0, 0, $bitmap.Size)
    $graphics.Dispose()
    try {
        $path = Join-Path $script:EvidenceRoot "$Name.png"
        $bitmap.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
        return $bitmap
    } catch {
        $bitmap.Dispose()
        throw
    }
}

function Invoke-Click([IntPtr]$Handle, [int]$X, [int]$Y) {
    [RungProofNativeInput]::ClickClient($Handle, $X, $Y)
    Start-Sleep -Milliseconds 350
}

function Assert-Changed([System.Drawing.Bitmap]$Before, [System.Drawing.Bitmap]$After, [int]$Left, [int]$Top, [int]$Right, [int]$Bottom, [string]$Name) {
    $changed = 0
    for ($y = $Top; $y -lt $Bottom; $y += 2) {
        for ($x = $Left; $x -lt $Right; $x += 2) {
            $a = $Before.GetPixel($x, $y)
            $b = $After.GetPixel($x, $y)
            if ([Math]::Abs($a.R - $b.R) + [Math]::Abs($a.G - $b.G) + [Math]::Abs($a.B - $b.B) -gt 60) { $changed++ }
        }
    }
    if ($changed -lt 25) { throw "NATIVE_UI_VERIFY FAIL ${Name}: expected visible pixels to change, changed=$changed" }
    Write-Host "NATIVE_UI_VERIFY PASS $Name changedPixels=$changed"
}

$script:EvidenceRoot = Join-Path $env:TEMP "rungproof-native-ui-$(Get-Date -Format yyyyMMdd-HHmmss)"
New-Item -ItemType Directory -Path $script:EvidenceRoot | Out-Null

# Start clean so the test is repeatable and the document is known.
Get-Process -ErrorAction SilentlyContinue |
    Where-Object { $_.MainWindowTitle -eq 'RungProof (DEBUG)' } |
    Stop-Process -Force

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$godot = Join-Path $projectRoot '.tools\godot\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe'
$process = Start-Process -FilePath $godot -ArgumentList "--path `"$projectRoot`" --app-shell" -WorkingDirectory $projectRoot -PassThru

$window = $null
for ($i = 0; $i -lt ($WaitSeconds * 10); $i++) {
    Start-Sleep -Milliseconds 100
    $window = Get-RungProofWindow
    if ($window) { break }
}
if (-not $window) { throw 'NATIVE_UI_VERIFY FAIL RungProof window did not appear.' }

$handle = [IntPtr]$window.MainWindowHandle
[RungProofNativeInput]::SetForegroundWindow($handle) | Out-Null
Start-Sleep -Seconds 3

# Client coordinates match the 1600x900 project viewport. The first click
# dismisses the simulator notice; the second enters the ladder editor.
Invoke-Click $handle 800 532
Invoke-Click $handle 160 534
$before = Save-Screenshot $handle '01-ladder-open'

# Select the first contact, delete it with the visible DELETE button.
Invoke-Click $handle 450 558
$selected = Save-Screenshot $handle '02-contact-selected'
Assert-Changed $before $selected 220 430 1160 680 'physical-contact-selection'
$before.Dispose()
Invoke-Click $handle 1120 367
$deleted = Save-Screenshot $handle '03-contact-deleted'
Assert-Changed $selected $deleted 220 430 1160 680 'physical-delete-button'
$selected.Dispose()

# Click a wire, add a normally-open contact, then select it and convert it
# to normally-closed through the visible palette buttons.
Invoke-Click $handle 350 558
Invoke-Click $handle 288 367
$added = Save-Screenshot $handle '04-contact-added'
Assert-Changed $deleted $added 220 430 1160 680 'physical-insert-button'
$deleted.Dispose()
Invoke-Click $handle 740 558
Invoke-Click $handle 384 367
$changed = Save-Screenshot $handle '05-contact-converted-nc'
Assert-Changed $added $changed 650 500 820 650 'physical-contact-conversion'
$added.Dispose()

# Move the selected contact left one position and verify the rung visibly
# reorders and the diagnostics text changes.
Invoke-Click $handle 487 287
$moved = Save-Screenshot $handle '06-contact-moved'
Assert-Changed $changed $moved 220 430 1160 680 'physical-move-button'
Assert-Changed $changed $moved 220 740 1160 850 'physical-diagnostics-update'
$changed.Dispose()
$moved.Dispose()

Write-Host "NATIVE_UI_VERIFY PASS all physical mouse actions"
Write-Host "NATIVE_UI_VERIFY evidence=$script:EvidenceRoot"

try { Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue } catch { }
