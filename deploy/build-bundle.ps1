# Build Release và đóng gói iSurvey.bundle (tự nạp ApplicationPlugins).
# Usage:
#   .\deploy\build-bundle.ps1              # chỉ tạo bundle trong deploy\output
#   .\deploy\build-bundle.ps1 -Install     # copy AppData + them TRUSTEDPATHS
#   .\deploy\build-bundle.ps1 -Install -InstallAllUsers  # copy Program Files (can admin, tu tin)

param(
    [switch]$Install,
    [switch]$InstallAllUsers,
    [string]$AcadDir = ""
)

function Resolve-AcadDir {
    param([string]$Candidate)
    if ($Candidate -and (Test-Path (Join-Path $Candidate "accoremgd.dll"))) {
        if (-not $Candidate.EndsWith("\")) { $Candidate += "\" }
        return $Candidate
    }
    $candidates = @(
        "C:\Program Files\Autodesk\AutoCAD 2026",
        "D:\Autodesk\AutoCAD 2026"
    )
    foreach ($dir in $candidates) {
        if (Test-Path (Join-Path $dir "accoremgd.dll")) {
            return ($dir + "\")
        }
    }
    throw "Khong tim thay AutoCAD 2026 (accoremgd.dll). Truyen -AcadDir 'C:\...\AutoCAD 2026\'"
}

if (-not $AcadDir) {
    $AcadDir = Resolve-AcadDir ""
} else {
    $AcadDir = Resolve-AcadDir $AcadDir
}

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$OutDir = Join-Path $Root "deploy\output\iSurvey.bundle"
$PayloadDir = Join-Path $OutDir "Contents\Win64\2026"
$TemplateBundle = Join-Path $Root "deploy\iSurvey.bundle"

Write-Host "==> Build Release (AcadDir: $($AcadDir.TrimEnd('\')))..."
Push-Location $Root
try {
    dotnet build (Join-Path $Root "iSurvey.sln") -c Release "-p:AcadDir=$($AcadDir.TrimEnd('\'))"
    $buildExit = $LASTEXITCODE
}
finally {
    Pop-Location
}

$BinDir = Join-Path $Root "bin\Release\net10.0-windows"
$ObjDir = Join-Path $Root "obj\Release\net10.0-windows"
$BuildDir = $BinDir
$DllFromObj = $false

if ($buildExit -ne 0) {
    if (Test-Path (Join-Path $ObjDir "iSurvey.dll")) {
        Write-Host "==> Build copy failed (AutoCAD lock?) - dung iSurvey.dll tu obj"
        $DllFromObj = $true
    }
    else {
        throw "dotnet build failed."
    }
}
elseif (-not (Test-Path (Join-Path $BinDir "iSurvey.dll"))) {
    if (Test-Path (Join-Path $ObjDir "iSurvey.dll")) {
        $DllFromObj = $true
    }
    else {
        throw "Khong tim thay iSurvey.dll sau build."
    }
}

Write-Host "==> Tao bundle..."
if (Test-Path $OutDir) {
    Remove-Item $OutDir -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $PayloadDir | Out-Null

Copy-Item (Join-Path $TemplateBundle "PackageContents.xml") (Join-Path $OutDir "PackageContents.xml")

$files = @(
    "iSurvey.dll",
    "iSurvey.deps.json",
    "iSurvey.runtimeconfig.json",
    "ProjNET.dll"
)
$optionalFiles = @(
    "System.Drawing.Common.dll"
)
foreach ($f in $files) {
    if ($DllFromObj -and $f -eq "iSurvey.dll") {
        $src = Join-Path $ObjDir $f
    }
    else {
        $src = Join-Path $BuildDir $f
    }
    if (-not (Test-Path $src)) {
        throw "Thieu file build: $f"
    }
    Copy-Item $src $PayloadDir
}
foreach ($f in $optionalFiles) {
    $src = Join-Path $BuildDir $f
    if (Test-Path $src) {
        Copy-Item $src $PayloadDir
    }
    else {
        Write-Host "==> Bo qua (framework cung cap): $f"
    }
}

$srcData = Join-Path $BuildDir "Data"
$dstData = Join-Path $PayloadDir "Data"
Copy-Item $srcData $dstData -Recurse

Write-Host "==> Bundle san sang: $OutDir"

if ($Install) {
    if ($InstallAllUsers) {
        $plugins = Join-Path ${env:ProgramFiles} "Autodesk\ApplicationPlugins"
        Write-Host "==> Cai cho tat ca user (Program Files - tu dong trusted)"
    }
    else {
        $plugins = Join-Path $env:APPDATA "Autodesk\ApplicationPlugins"
    }

    $target = Join-Path $plugins "iSurvey.bundle"
    New-Item -ItemType Directory -Force -Path $plugins | Out-Null
    if (Test-Path $target) {
        Remove-Item $target -Recurse -Force
    }
    Copy-Item $OutDir $target -Recurse
    Write-Host "==> Da cai vao: $target"

    if (-not $InstallAllUsers) {
        $trustScript = Join-Path (Split-Path $MyInvocation.MyCommand.Path) "Set-iSurveyTrustedPath.ps1"
        & $trustScript -BundleRoot $target
    }
    else {
        Write-Host "    Program Files\Autodesk\ApplicationPlugins da duoc AutoCAD 2026 tin cay mac dinh."
    }

    Write-Host "    Mo lai Civil 3D / AutoCAD 2026 de tu nap iSurvey."
}
else {
    Write-Host ""
    Write-Host "Cai thu cong: copy folder"
    Write-Host "  $OutDir"
    Write-Host "vao:"
    Write-Host "  $env:APPDATA\Autodesk\ApplicationPlugins\"
    Write-Host ""
    Write-Host "Hoac chay lai voi -Install"
}
