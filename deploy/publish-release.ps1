# Phat hanh 1 lenh: (build) + upload zip Setup + version.json len Supabase Storage bucket public 'isurvey-releases'.
#
# Khoa service_role KHONG luu vao file / repo. Dat 1 lan tren may (user hien tai), mo lai terminal:
#     setx ISURVEY_SERVICE_ROLE_KEY "<service_role tu Supabase > Project Settings > API>"
# Hoac chi cho phien terminal hien tai:
#     $env:ISURVEY_SERVICE_ROLE_KEY = "<service_role>"
# Neu chua dat, script se hoi nhap (an ky tu, chi dung cho lan chay nay).
#
# Usage:
#   .\deploy\publish-release.ps1 -Notes "Mo ta ngan"     # build-release roi upload
#   .\deploy\publish-release.ps1 -SkipBuild              # chi upload ban da build (deploy\output)
#   .\deploy\publish-release.ps1 -DryRun                 # kiem tra, khong upload
#   .\deploy\publish-release.ps1 -Force                  # cho phep ha version (rollback)

param(
    [string]$Notes = "",
    [switch]$SkipBuild,
    [switch]$DryRun,
    [switch]$Force,
    [string]$Bucket = "isurvey-releases",
    [string]$SupabaseUrl = ""
)

$ErrorActionPreference = "Stop"
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$Root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$Deploy = Join-Path $Root "deploy"

# --- Phien ban (nguon: csproj) ---
$verMatch = Select-String -Path (Join-Path $Root "iSurvey.csproj") -Pattern '<Version>([^<]+)</Version>' | Select-Object -First 1
if (-not $verMatch) { throw "Khong doc duoc <Version> trong iSurvey.csproj" }
$Version = $verMatch.Matches.Groups[1].Value.Trim()

# --- URL Supabase: tham so > bien moi truong > mac dinh trong AuthConfig.cs ---
if ([string]::IsNullOrWhiteSpace($SupabaseUrl)) { $SupabaseUrl = $env:ISURVEY_SUPABASE_URL }
if ([string]::IsNullOrWhiteSpace($SupabaseUrl)) {
    $cfg = Select-String -Path (Join-Path $Root "src\Auth\AuthConfig.cs") -Pattern 'DefaultSupabaseUrl\s*=\s*"([^"]+)"' | Select-Object -First 1
    if ($cfg) { $SupabaseUrl = $cfg.Matches.Groups[1].Value }
}
if ([string]::IsNullOrWhiteSpace($SupabaseUrl)) { throw "Khong xac dinh duoc Supabase URL (dat ISURVEY_SUPABASE_URL hoac -SupabaseUrl)." }
$SupabaseUrl = $SupabaseUrl.Trim().TrimEnd('/')

Write-Host "==> Phat hanh iSurvey $Version  ->  $SupabaseUrl  (bucket: $Bucket)"

# --- Build ---
if (-not $SkipBuild) {
    $args2 = @{}
    if (-not [string]::IsNullOrWhiteSpace($Notes)) { $args2["Notes"] = $Notes }
    & (Join-Path $Deploy "build-release.ps1") @args2
    if ($LASTEXITCODE -ne 0 -and $null -ne $LASTEXITCODE) { throw "build-release that bai" }
}

$zipName = "iSurvey-Setup-$Version.zip"
$zipPath = Join-Path $Deploy "output\$zipName"
$manifestPath = Join-Path $Deploy "output\release\version.json"
if (-not (Test-Path $zipPath)) { throw "Khong thay $zipPath - chay lai khong co -SkipBuild." }
if (-not (Test-Path $manifestPath)) { throw "Khong thay $manifestPath - chay lai khong co -SkipBuild." }

# version.json phai khop version dang build (tranh upload nham manifest cu)
$manifestText = [IO.File]::ReadAllText($manifestPath).TrimStart([char]0xFEFF)
$manifest = $manifestText | ConvertFrom-Json
if ($manifest.latest -ne $Version) {
    throw "version.json ghi latest=$($manifest.latest) nhung csproj la $Version. Chay lai build-release.ps1."
}
$expectedUrl = "$SupabaseUrl/storage/v1/object/public/$Bucket/$zipName"
if ($manifest.url -ne $expectedUrl) {
    throw "version.json url=$($manifest.url) khac $expectedUrl. Kiem tra Supabase URL / bucket."
}

# --- Khoa service_role ---
$key = $env:ISURVEY_SERVICE_ROLE_KEY
if ([string]::IsNullOrWhiteSpace($key) -and -not $DryRun) {
    Write-Host "Chua co bien moi truong ISURVEY_SERVICE_ROLE_KEY." -ForegroundColor Yellow
    $sec = Read-Host "Nhap service_role (an ky tu, chi dung lan nay)" -AsSecureString
    $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($sec)
    try { $key = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr) }
    finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr) }
}
if ([string]::IsNullOrWhiteSpace($key) -and -not $DryRun) { throw "Thieu service_role - da huy." }
if ($key) { $key = $key.Trim() }

# --- Bao ve: khong ha version tren server ---
$publicManifest = "$SupabaseUrl/storage/v1/object/public/$Bucket/version.json"
$remoteLatest = $null

# Doc noi dung public dang UTF-8 (khong dung .Content: PS 5.1 doc sai charset, BOM thanh "ï»¿")
function Get-RemoteJson([string]$url) {
    $resp = Invoke-WebRequest -Uri ($url + "?t=" + [DateTime]::UtcNow.Ticks) -UseBasicParsing
    $text = [Text.Encoding]::UTF8.GetString($resp.RawContentStream.ToArray())
    return ($text.TrimStart([char]0xFEFF) | ConvertFrom-Json)
}

try {
    $remoteLatest = (Get-RemoteJson $publicManifest).latest
}
catch { Write-Host "    (chua doc duoc version.json tren server: $($_.Exception.Message))" }

function ConvertTo-Ver([string]$s) { try { return [Version]$s } catch { return [Version]"0.0" } }
if ($remoteLatest) {
    Write-Host "    Tren server hien tai: $remoteLatest"
    if ((ConvertTo-Ver $Version) -lt (ConvertTo-Ver $remoteLatest) -and -not $Force) {
        throw "Ban $Version THAP HON ban tren server ($remoteLatest). Dung -Force neu muon ha version (rollback)."
    }
}

$sizeMb = [Math]::Round((Get-Item $zipPath).Length / 1MB, 2)
Write-Host "    Se upload: $zipName ($sizeMb MB), roi version.json"
if ($DryRun) {
    Write-Host "==> DryRun: khong upload gi." -ForegroundColor Green
    exit 0
}

function Send-Object([string]$localPath, [string]$remoteName, [string]$contentType, [string]$cacheControl) {
    $uri = "$SupabaseUrl/storage/v1/object/$Bucket/$remoteName"
    $headers = @{
        "Authorization" = "Bearer $key"
        "apikey"        = $key
        "x-upsert"      = "true"            # ghi de neu da co
        "cache-control" = $cacheControl
    }
    Write-Host "    -> $remoteName ..." -NoNewline
    try {
        Invoke-WebRequest -Uri $uri -Method Post -Headers $headers -InFile $localPath `
            -ContentType $contentType -UseBasicParsing | Out-Null
        Write-Host " OK" -ForegroundColor Green
    }
    catch {
        Write-Host " LOI" -ForegroundColor Red
        $detail = ""
        try {
            $r = $_.Exception.Response
            if ($r) {
                $sr = New-Object IO.StreamReader($r.GetResponseStream())
                $detail = $sr.ReadToEnd()
            }
        }
        catch { }
        throw "Upload $remoteName that bai: $($_.Exception.Message) $detail"
    }
}

# 1) zip truoc  2) kiem tra tai duoc  3) version.json sau cung (app chi thay ban moi khi manifest doi)
Send-Object $zipPath $zipName "application/zip" "max-age=3600"

$zipUrl = $manifest.url
$head = Invoke-WebRequest -Uri ($zipUrl + "?t=" + [DateTime]::UtcNow.Ticks) -Method Head -UseBasicParsing
$remoteLen = [int64]($head.Headers["Content-Length"] | Select-Object -First 1)
$localLen = (Get-Item $zipPath).Length
if ($remoteLen -ne $localLen) { throw "Zip tren server ($remoteLen byte) khac file local ($localLen byte). KHONG upload version.json." }
Write-Host "    Kiem tra zip tren server: $remoteLen byte - khop." -ForegroundColor Green

# version.json cache ngan (60 giay) de may user thay ban moi nhanh
Send-Object $manifestPath "version.json" "application/json" "max-age=60"

# --- Xac nhan cuoi ---
$finalLatest = (Get-RemoteJson $publicManifest).latest
if ($finalLatest -ne $Version) { throw "version.json tren server van la $finalLatest (mong doi $Version)." }

Write-Host ""
Write-Host "==> PHAT HANH XONG: iSurvey $Version" -ForegroundColor Green
Write-Host "    Manifest: $publicManifest"
Write-Host "    Zip:      $zipUrl"
Write-Host "    Kiem tra: tren may ban cu chay ISURVEY_UPDATE."
