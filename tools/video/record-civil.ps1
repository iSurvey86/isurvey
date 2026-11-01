# Quay cua so Civil 3D. Khong thu micro.
param(
    [Parameter(Mandatory = $true)]
    [string]$OutFile,
    [int]$Seconds = 0,
    [switch]$Stop,
    [switch]$Desktop,
    [string]$TitlePattern = "Civil|AutoCAD"
)
$ErrorActionPreference = "Stop"

if (-not ([System.Management.Automation.PSTypeName]"iSurveyVideo.Native").Type) {
    Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;
namespace iSurveyVideo {
  public class Native {
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT { public int Left; public int Top; public int Right; public int Bottom; }
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")] public static extern bool IsIconic(IntPtr hWnd);
  }
}
"@
}

function Find-Ffmpeg {
    $cmd = Get-Command ffmpeg -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }
    $root = Join-Path $env:LOCALAPPDATA "Microsoft\WinGet\Packages"
    if (Test-Path $root) {
        $hit = Get-ChildItem -Path $root -Filter ffmpeg.exe -Recurse -ErrorAction SilentlyContinue |
            Where-Object { $_.FullName -match "Gyan\.FFmpeg" } |
            Select-Object -Last 1
        if ($hit) { return $hit.FullName }
    }
    throw "Khong thay ffmpeg. Chay tools/video/setup.ps1."
}

$OutFile = [System.IO.Path]::GetFullPath($OutFile)
$stopFile = "$OutFile.stop"
$readyFile = "$OutFile.ready"
$doneFile = "$OutFile.done"

if ($Stop) {
    New-Item -ItemType Directory -Force -Path (Split-Path $OutFile) | Out-Null
    Set-Content -Path $stopFile -Value "stop" -Encoding ascii
    $deadline = (Get-Date).AddSeconds(40)
    while (-not (Test-Path $doneFile) -and (Get-Date) -lt $deadline) {
        Start-Sleep -Milliseconds 300
    }
    if (-not (Test-Path $doneFile)) {
        throw "Ban quay chua ket thuc. Kiem tra terminal dang chay record-civil.ps1."
    }
    Write-Host "da dung: $OutFile"
    exit 0
}

New-Item -ItemType Directory -Force -Path (Split-Path $OutFile) | Out-Null
Remove-Item $stopFile, $readyFile, $doneFile -ErrorAction SilentlyContinue

$ffmpeg = Find-Ffmpeg
$offsetX = 0
$offsetY = 0
$width = 0
$height = 0

if ($Desktop) {
    Add-Type -AssemblyName System.Windows.Forms
    $bounds = [System.Windows.Forms.SystemInformation]::VirtualScreen
    $offsetX = $bounds.X
    $offsetY = $bounds.Y
    $width = $bounds.Width
    $height = $bounds.Height
} else {
    $proc = Get-Process -Name acad -ErrorAction SilentlyContinue |
        Where-Object { $_.MainWindowHandle -ne 0 -and $_.MainWindowTitle -match $TitlePattern } |
        Select-Object -First 1
    if (-not $proc) {
        throw "Khong thay cua so AutoCAD hoac Civil 3D. Mo ban ve roi quay lai."
    }
    $hwnd = $proc.MainWindowHandle
    if ([iSurveyVideo.Native]::IsIconic($hwnd)) {
        [iSurveyVideo.Native]::ShowWindow($hwnd, 9) | Out-Null
        Start-Sleep -Milliseconds 400
    }
    [iSurveyVideo.Native]::ShowWindow($hwnd, 3) | Out-Null
    [iSurveyVideo.Native]::SetForegroundWindow($hwnd) | Out-Null
    Start-Sleep -Milliseconds 500
    $rect = New-Object iSurveyVideo.Native+RECT
    [iSurveyVideo.Native]::GetWindowRect($hwnd, [ref]$rect) | Out-Null
    $offsetX = $rect.Left
    $offsetY = $rect.Top
    $width = $rect.Right - $rect.Left
    $height = $rect.Bottom - $rect.Top
    Write-Host "cua so: $($proc.MainWindowTitle)"
}

Add-Type -AssemblyName System.Windows.Forms
$screen = [System.Windows.Forms.SystemInformation]::VirtualScreen
if ($offsetX -lt $screen.X) {
    $width -= ($screen.X - $offsetX)
    $offsetX = $screen.X
}
if ($offsetY -lt $screen.Y) {
    $height -= ($screen.Y - $offsetY)
    $offsetY = $screen.Y
}
if (($offsetX + $width) -gt ($screen.X + $screen.Width)) {
    $width = $screen.X + $screen.Width - $offsetX
}
if (($offsetY + $height) -gt ($screen.Y + $screen.Height)) {
    $height = $screen.Y + $screen.Height - $offsetY
}
$width = $width - ($width % 2)
$height = $height - ($height % 2)
if ($width -lt 200 -or $height -lt 200) {
    throw "Vung quay qua nho (${width}x${height})."
}

Write-Host "quay ${width}x${height} tai $offsetX,$offsetY -> $OutFile"
$ffArgs = "-y -loglevel warning -f gdigrab -framerate 30 -draw_mouse 1 -offset_x $offsetX -offset_y $offsetY -video_size ${width}x${height} -i desktop -c:v libx264 -preset veryfast -pix_fmt yuv420p -an `"$OutFile`""
$psi = New-Object System.Diagnostics.ProcessStartInfo
$psi.FileName = $ffmpeg
$psi.Arguments = $ffArgs
$psi.UseShellExecute = $false
$psi.RedirectStandardInput = $true
$psi.CreateNoWindow = $true
$recorder = [System.Diagnostics.Process]::Start($psi)
Set-Content -Path $readyFile -Value "ready" -Encoding ascii

$started = Get-Date
while ($true) {
    if ($recorder.HasExited) { break }
    if (Test-Path $stopFile) { break }
    if ($Seconds -gt 0 -and ((Get-Date) - $started).TotalSeconds -ge $Seconds) { break }
    Start-Sleep -Milliseconds 200
}

if (-not $recorder.HasExited) {
    try { $recorder.StandardInput.Write("q") } catch { }
    if (-not $recorder.WaitForExit(12000)) {
        try { $recorder.Kill() } catch { }
        $recorder.WaitForExit(5000) | Out-Null
    }
}
Set-Content -Path $doneFile -Value "done" -Encoding ascii
Remove-Item $readyFile -ErrorAction SilentlyContinue
if (-not (Test-Path $OutFile) -or (Get-Item $OutFile).Length -lt 1000) {
    throw "File quay trong hoac qua nho: $OutFile"
}
Write-Host "xong $OutFile"
