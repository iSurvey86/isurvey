# Them thu muc bundle iSurvey vao TRUSTEDPATHS (AutoCAD / Civil 3D 2026+).
# Chay sau khi cai bundle vao AppData de khong hien hop thoai "Unsigned Executable".

param(
    [string]$BundleRoot = (Join-Path $env:APPDATA "Autodesk\ApplicationPlugins\iSurvey.bundle")
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $BundleRoot)) {
    throw "Khong tim thay bundle: $BundleRoot"
}

$trustPath = (Join-Path $BundleRoot "Contents\Win64\2026") + '\...'
Write-Host "==> Them TRUSTEDPATHS: $trustPath"

$acadRoot = "HKCU:\Software\Autodesk\AutoCAD"
if (-not (Test-Path $acadRoot)) {
    throw "Khong tim thay registry AutoCAD. Hay mo Civil 3D it nhat mot lan."
}

$updated = 0
Get-ChildItem $acadRoot -ErrorAction SilentlyContinue | ForEach-Object {
    $versionKey = $_.PSPath
    Get-ChildItem $versionKey -ErrorAction SilentlyContinue | ForEach-Object {
        $profilesRoot = Join-Path $_.PSPath "Profiles"
        if (-not (Test-Path $profilesRoot)) { return }

        Get-ChildItem $profilesRoot -ErrorAction SilentlyContinue | ForEach-Object {
            $varsPath = Join-Path $_.PSPath "Variables"
            if (-not (Test-Path $varsPath)) {
                New-Item -ItemType Directory -Path $varsPath -Force | Out-Null
            }

            $profileName = $_.PSChildName
            $current = (Get-ItemProperty -Path $varsPath -Name TRUSTEDPATHS -ErrorAction SilentlyContinue).TRUSTEDPATHS

            $already = $false
            if ($current) {
                foreach ($part in ($current -split ';')) {
                    if ($part -eq $trustPath) { $already = $true; break }
                }
            }

            if ($already) {
                Write-Host ('  [OK] ' + $profileName + ' - da co')
                return
            }

            if ([string]::IsNullOrWhiteSpace($current)) {
                $newValue = $trustPath
            }
            else {
                $newValue = $current + ';' + $trustPath
            }

            Set-ItemProperty -Path $varsPath -Name TRUSTEDPATHS -Value $newValue
            Write-Host ('  [+] ' + $profileName)
            $script:updated++
        }
    }
}

if ($updated -eq 0) {
    Write-Host "Khong cap nhat profile nao (co the da co san)."
}
else {
    Write-Host ('==> Da cap nhat ' + $updated + ' profile. Mo lai Civil 3D - khong can xac nhan DLL nua.')
}
