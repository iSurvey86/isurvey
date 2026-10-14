# Build Release và đóng gói iSurvey.bundle cho AutoCAD / Civil 3D 2023-2027.
# Usage:
#   .\deploy\build-bundle.ps1              # bundle trong deploy\output
#   .\deploy\build-bundle.ps1 -Install     # copy AppData + TRUSTEDPATHS
#   .\deploy\build-bundle.ps1 -Install -InstallAllUsers

param(
    [switch]$Install,
    [switch]$InstallAllUsers
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$OutDir = Join-Path $Root "deploy\output\iSurvey.bundle"
$TemplateBundle = Join-Path $Root "deploy\iSurvey.bundle"

function Copy-Payload {
    param(
        [string]$BuildDir,
        [string]$DestDir,
        [bool]$Framework
    )
    New-Item -ItemType Directory -Force -Path $DestDir | Out-Null
    $names = @("iSurvey.dll", "ProjNET.dll")
    if (-not $Framework) {
        $names += @("iSurvey.deps.json", "iSurvey.runtimeconfig.json")
    }
    foreach ($name in $names) {
        $src = Join-Path $BuildDir $name
        if (-not (Test-Path $src)) { throw "Thieu file build: $src" }
        Copy-Item $src $DestDir
    }
    if ($Framework) {
        $blocked = @(
            "accoremgd.dll", "acdbmgd.dll", "acmgd.dll", "AdWindows.dll",
            "AcCoreMgd.dll", "AcDbMgd.dll", "AcMgd.dll"
        )
        Get-ChildItem $BuildDir -Filter "*.dll" | Where-Object { $blocked -notcontains $_.Name } | ForEach-Object {
            Copy-Item $_.FullName $DestDir -Force
        }
    }
    $srcData = Join-Path $BuildDir "Data"
    if (-not (Test-Path $srcData)) { throw "Thieu thu muc Data: $srcData" }
    Copy-Item $srcData (Join-Path $DestDir "Data") -Recurse -Force
}

$targets = @(
    @{ Tfm = "net48"; Year = "2023"; Framework = $true },
    @{ Tfm = "net8.0-windows"; Year = "2025"; Framework = $false },
    @{ Tfm = "net10.0-windows"; Year = "2027"; Framework = $false }
)

Write-Host "==> Build Release (net48 + net8 + net10)..."
Push-Location $Root
try {
    dotnet build (Join-Path $Root "iSurvey.csproj") -c Release
    if ($LASTEXITCODE -ne 0) { throw "dotnet build failed." }
}
finally {
    Pop-Location
}

Write-Host "==> Tao bundle..."
if (Test-Path $OutDir) { Remove-Item $OutDir -Recurse -Force }
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
Copy-Item (Join-Path $TemplateBundle "PackageContents.xml") (Join-Path $OutDir "PackageContents.xml")

foreach ($t in $targets) {
    $buildDir = Join-Path $Root "bin\Release\$($t.Tfm)"
    $dest = Join-Path $OutDir "Contents\Win64\$($t.Year)"
    Copy-Payload -BuildDir $buildDir -DestDir $dest -Framework $t.Framework
    Write-Host "    $($t.Year) <= $($t.Tfm)"
}

# Seed DGN cho C2D: Contents\Seeds (DgnSeedLocator tim o ..\..\Seeds so voi iSurvey.dll)
$seedSrc = Join-Path $Root "src\Seeds"
$seedFiles = @()
if (Test-Path $seedSrc) { $seedFiles = @(Get-ChildItem $seedSrc -Filter "*.dgn") }
if ($seedFiles.Count -gt 0) {
    $seedDest = Join-Path $OutDir "Contents\Seeds"
    New-Item -ItemType Directory -Force -Path $seedDest | Out-Null
    foreach ($f in $seedFiles) { Copy-Item $f.FullName $seedDest -Force }
    Write-Host "    Seeds: $($seedFiles.Name -join ', ')"
}
else {
    Write-Host "    (Chua co src\Seeds\*.dgn - C2D se dung seed metric co san cua Civil 3D)"
}

Write-Host "==> Bundle san sang: $OutDir"

if ($Install) {
    if ($InstallAllUsers) {
        $plugins = Join-Path ${env:ProgramFiles} "Autodesk\ApplicationPlugins"
        Write-Host "==> Cai cho tat ca user (Program Files)"
    }
    else {
        $plugins = Join-Path $env:APPDATA "Autodesk\ApplicationPlugins"
    }

    $target = Join-Path $plugins "iSurvey.bundle"
    New-Item -ItemType Directory -Force -Path $plugins | Out-Null
    if (Test-Path $target) { Remove-Item $target -Recurse -Force }
    Copy-Item $OutDir $target -Recurse
    Write-Host "==> Da cai vao: $target"

    if (-not $InstallAllUsers) {
        $trustScript = Join-Path (Split-Path $MyInvocation.MyCommand.Path) "Set-iSurveyTrustedPath.ps1"
        & $trustScript -BundleRoot $target
    }
    else {
        Write-Host "    Program Files\Autodesk\ApplicationPlugins duoc AutoCAD tin cay mac dinh."
    }

    Write-Host "    Mo lai AutoCAD / Civil 3D 2023-2027 de tu nap iSurvey."
}
else {
    Write-Host ""
    Write-Host "Copy folder $OutDir vao %AppData%\Autodesk\ApplicationPlugins\"
    Write-Host "Hoac chay lai voi -Install"
}
