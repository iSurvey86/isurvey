# Dong goi ban phat hanh: build bundle + zip Setup gui nguoi dung + version.json (update online).
# Usage:  .\deploy\build-release.ps1

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$Deploy = Join-Path $Root "deploy"

# Doc Version tu csproj
$csproj = Join-Path $Root "iSurvey.csproj"
$verMatch = Select-String -Path $csproj -Pattern '<Version>([^<]+)</Version>' | Select-Object -First 1
if (-not $verMatch) { throw "Khong doc duoc <Version> trong iSurvey.csproj" }
$Version = $verMatch.Matches.Groups[1].Value.Trim()
Write-Host "==> Phien ban: $Version"

# Build bundle
& (Join-Path $Deploy "build-bundle.ps1")
if ($LASTEXITCODE -ne 0) { throw "build-bundle that bai" }

$bundle = Join-Path $Deploy "output\iSurvey.bundle"
$stageName = "iSurvey-Setup-$Version"
$stage = Join-Path $Deploy "output\$stageName"
if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
New-Item -ItemType Directory -Force -Path $stage | Out-Null

Copy-Item $bundle (Join-Path $stage "iSurvey.bundle") -Recurse -Force
Copy-Item (Join-Path $Deploy "Install-iSurvey.ps1") $stage -Force
Copy-Item (Join-Path $Deploy "Install-iSurvey.bat") $stage -Force
Copy-Item (Join-Path $Deploy "Uninstall-iSurvey.ps1") $stage -Force
Copy-Item (Join-Path $Deploy "Uninstall-iSurvey.bat") $stage -Force
Copy-Item (Join-Path $Deploy "Set-iSurveyTrustedPath.ps1") $stage -Force
Copy-Item (Join-Path $Deploy "CAI_DAT.txt") $stage -Force

# Manifest update (Admin upload file nay + zip len Supabase Storage bucket isurvey-releases)
$notes = "Ban cai Setup; update online; nhat ky hoat dong; UAVCLIP Auto-Clip; GOC 1:1000."
$manifest = [ordered]@{
    latest      = $Version
    url         = "https://lmraewcashlnfdawrvmg.supabase.co/storage/v1/object/public/isurvey-releases/iSurvey-Setup-$Version.zip"
    released_at = (Get-Date -Format "yyyy-MM-dd")
    notes       = $notes
    min_version = "1.0.0"
}
$manifestPath = Join-Path $stage "version.json"
$manifest | ConvertTo-Json | Set-Content -Path $manifestPath -Encoding UTF8
# Ban copy rieng de Admin upload len Storage (ten co dinh version.json)
$releaseDir = Join-Path $Deploy "output\release"
New-Item -ItemType Directory -Force -Path $releaseDir | Out-Null
Copy-Item $manifestPath (Join-Path $releaseDir "version.json") -Force

$zipPath = Join-Path $Deploy "output\$stageName.zip"
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
Compress-Archive -Path (Join-Path $stage "*") -DestinationPath $zipPath -CompressionLevel Optimal
Copy-Item $zipPath (Join-Path $releaseDir $stageName.zip) -Force

Write-Host ""
Write-Host "==> GOI SETUP: $zipPath" -ForegroundColor Green
Write-Host "==> Upload len Supabase Storage bucket public 'isurvey-releases':"
Write-Host "      - $($stageName).zip"
Write-Host "      - version.json  (tu deploy\output\release\)"
Write-Host "   Xem docs/Huong_dan_Phat_hanh.md"
