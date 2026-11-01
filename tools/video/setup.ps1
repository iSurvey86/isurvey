# Cai ffmpeg (neu thieu) va venv Python cho giong noi + YouTube.
$ErrorActionPreference = "Stop"
$here = Split-Path -Parent $MyInvocation.MyCommand.Path

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
    return $null
}

$ffmpeg = Find-Ffmpeg
if (-not $ffmpeg) {
    Write-Host "Dang cai ffmpeg..."
    winget install --id Gyan.FFmpeg -e --accept-package-agreements --accept-source-agreements
    $ffmpeg = Find-Ffmpeg
}
if (-not $ffmpeg) {
    throw "Khong thay ffmpeg sau khi cai. Mo terminal moi roi chay lai."
}
Write-Host "ffmpeg: $ffmpeg"

function Find-Python {
    $candidates = @(
        "$env:LOCALAPPDATA\Programs\Python\Python312\python.exe",
        "$env:LOCALAPPDATA\Programs\Python\Python313\python.exe",
        "C:\Program Files\Python312\python.exe",
        "C:\Program Files\Python313\python.exe"
    )
    foreach ($candidate in $candidates) {
        if (Test-Path $candidate) { return $candidate }
    }
    $cmd = Get-Command python -ErrorAction SilentlyContinue
    if ($cmd -and $cmd.Source -notmatch "LibreOffice|WindowsApps") { return $cmd.Source }
    return $null
}

$python = Find-Python
if (-not $python) {
    Write-Host "Dang cai Python 3.12..."
    winget install --id Python.Python.3.12 -e --accept-package-agreements --accept-source-agreements
    $python = Find-Python
}
if (-not $python) {
    throw "Khong thay Python 3.12. Mo terminal moi roi chay lai."
}
Write-Host "python: $python"

$venv = Join-Path $here ".venv"
$venvPy = Join-Path $venv "Scripts\python.exe"
if (-not (Test-Path $venvPy)) {
    & $python -m venv $venv
}
& $venvPy -m pip install --upgrade pip
& $venvPy -m pip install -r (Join-Path $here "requirements.txt")
Write-Host "venv: $venvPy"
