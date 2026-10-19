# Go iSurvey khoi may (user hien tai). Dong Civil truoc khi chay.

$ErrorActionPreference = "Stop"
$target = Join-Path $env:APPDATA "Autodesk\ApplicationPlugins\iSurvey.bundle"

if (-not (Test-Path $target)) {
    Write-Host "Khong tim thay iSurvey.bundle trong AppData — co the chua cai."
    exit 0
}

try {
    Remove-Item $target -Recurse -Force
    Write-Host "==> Da go: $target" -ForegroundColor Green
}
catch {
    Write-Host "Khong xoa duoc (Civil dang mo?). Dong CAD roi chay lai." -ForegroundColor Red
    exit 1
}

Write-Host "Mo lai Civil neu dang mo. (TRUSTEDPATHS cu co the de lai — khong anh huong.)"
