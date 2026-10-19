# Cai iSurvey cho user hien tai (khong can Admin).
# Dat script nay cung thu muc iSurvey.bundle (hoac chay tu goi Setup).
# Usage:  chuot phai -> Run with PowerShell
#         hoac: powershell -ExecutionPolicy Bypass -File .\Install-iSurvey.ps1

param(
    [switch]$AllUsers
)

$ErrorActionPreference = "Stop"
$Here = Split-Path -Parent $MyInvocation.MyCommand.Path

$bundleSrc = Join-Path $Here "iSurvey.bundle"
if (-not (Test-Path $bundleSrc)) {
    $alt = Join-Path $Here "deploy\output\iSurvey.bundle"
    if (Test-Path $alt) { $bundleSrc = $alt }
}

if (-not (Test-Path $bundleSrc)) {
    Write-Host "KHONG TIM THAY iSurvey.bundle canh file cai dat." -ForegroundColor Red
    Write-Host "Hay giai nen goi Setup (zip) ra roi chay lai Install-iSurvey.ps1."
    exit 1
}

if ($AllUsers) {
    $plugins = Join-Path $env:ProgramFiles "Autodesk\ApplicationPlugins"
    Write-Host "==> Cai tat ca user: $plugins"
}
else {
    $plugins = Join-Path $env:APPDATA "Autodesk\ApplicationPlugins"
    Write-Host "==> Cai user hien tai: $plugins"
}

$target = Join-Path $plugins "iSurvey.bundle"
New-Item -ItemType Directory -Force -Path $plugins | Out-Null

if (Test-Path $target) {
    try {
        Remove-Item $target -Recurse -Force
    }
    catch {
        Write-Host "KHONG XOA DUOC bundle cu (Civil/AutoCAD dang mo va khoa DLL)." -ForegroundColor Red
        Write-Host "Dong tat ca AutoCAD / Civil 3D roi chay lai bo cai dat."
        exit 1
    }
}

Copy-Item $bundleSrc $target -Recurse -Force
Write-Host "==> Da copy: $target"

$pkg = Join-Path $target "PackageContents.xml"
$ver = "?"
if (Test-Path $pkg) {
    $m = Select-String -Path $pkg -Pattern 'AppVersion="([^"]+)"' | Select-Object -First 1
    if ($m) { $ver = $m.Matches.Groups[1].Value }
}
Write-Host "==> Phien ban: $ver"

if (-not $AllUsers) {
    $trust = Join-Path $Here "Set-iSurveyTrustedPath.ps1"
    if (-not (Test-Path $trust)) {
        $trust = Join-Path (Split-Path $Here) "Set-iSurveyTrustedPath.ps1"
    }
    if (Test-Path $trust) {
        & $trust -BundleRoot $target
    }
    else {
        Write-Host "Canh bao: khong tim thay Set-iSurveyTrustedPath.ps1" -ForegroundColor Yellow
    }
}
else {
    Write-Host "Program Files\Autodesk\ApplicationPlugins: AutoCAD tin cay mac dinh."
}

Write-Host ""
Write-Host "CAI DAT XONG. Hay DONG va MO LAI AutoCAD / Civil 3D 2023-2027." -ForegroundColor Green
Write-Host "Tab Ribbon: iSurvey. Dang nhap Gmail (LGI) de kich hoat."
