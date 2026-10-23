# Cho tien trinh iSurvey goi sau khi tai zip Setup.
# Cho AutoCAD/Civil thoat (ParentPid) roi moi chay Install-iSurvey.ps1 (tranh khoa DLL).
# Usage: powershell -ExecutionPolicy Bypass -File .\Apply-iSurveyUpdate.ps1 -ParentPid 1234 -SetupDir "C:\...\extracted"

param(
    [Parameter(Mandatory = $true)][int]$ParentPid,
    [Parameter(Mandatory = $true)][string]$SetupDir
)

$ErrorActionPreference = "Stop"

function Show-Msg([string]$Text, [string]$Title) {
    try {
        Add-Type -AssemblyName System.Windows.Forms -ErrorAction SilentlyContinue
        [System.Windows.Forms.MessageBox]::Show($Text, $Title) | Out-Null
    }
    catch {
        Write-Host $Text
    }
}

$SetupDir = [System.IO.Path]::GetFullPath($SetupDir)
$installPs1 = Join-Path $SetupDir "Install-iSurvey.ps1"
$bundle = Join-Path $SetupDir "iSurvey.bundle"

if (-not (Test-Path $installPs1) -or -not (Test-Path $bundle)) {
    Show-Msg "Thu muc Setup khong hop le (thieu Install-iSurvey.ps1 hoac iSurvey.bundle).`n$SetupDir" "iSurvey Cap nhat"
    exit 1
}

Write-Host "==> Cho AutoCAD / Civil thoat (PID $ParentPid)..."
$deadline = (Get-Date).AddMinutes(30)
while ($true) {
    $alive = Get-Process -Id $ParentPid -ErrorAction SilentlyContinue
    if (-not $alive) { break }
    if ((Get-Date) -gt $deadline) {
        Show-Msg "Het thoi gian cho. Hay dong AutoCAD/Civil roi chay lai Install-iSurvey.bat trong:`n$SetupDir" "iSurvey Cap nhat"
        exit 2
    }
    Start-Sleep -Seconds 1
}

# Cho he thong nha khoa file DLL
Start-Sleep -Seconds 2

Write-Host "==> Cai dat tu: $SetupDir"
try {
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $installPs1
    $code = $LASTEXITCODE
}
catch {
    Show-Msg ("Loi cai dat: " + $_.Exception.Message) "iSurvey Cap nhat"
    exit 3
}

if ($code -ne 0 -and $null -ne $code) {
    Show-Msg "Cai dat that bai (ma $code). Mo thu muc Setup va chay Install-iSurvey.bat.`n$SetupDir" "iSurvey Cap nhat"
    exit $code
}

Show-Msg "Cap nhat iSurvey xong.`nHay mo lai AutoCAD / Civil 3D 2022-2027." "iSurvey Cap nhat"
exit 0
