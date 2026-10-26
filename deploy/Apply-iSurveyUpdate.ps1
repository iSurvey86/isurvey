# Cho tien trinh iSurvey goi sau khi tai zip Setup.
# Cho AutoCAD/Civil thoat (ParentPid) roi moi chay Install-iSurvey.ps1 (tranh khoa DLL),
# ghi last-result.json + nhat ky, hien thanh tien trinh, mo lai CAD/Civil.
# Usage: powershell -ExecutionPolicy Bypass -STA -File .\Apply-iSurveyUpdate.ps1 -ParentPid 1234 -SetupDir "C:\...\extracted"

param(
    [Parameter(Mandatory = $true)][int]$ParentPid,
    [Parameter(Mandatory = $true)][string]$SetupDir
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

$UpdatesRoot = Join-Path $env:LOCALAPPDATA "iSurvey\updates"
New-Item -ItemType Directory -Force -Path $UpdatesRoot | Out-Null
$LogFile = Join-Path $UpdatesRoot "update.log"
$ResultFile = Join-Path $UpdatesRoot "last-result.json"
$started = Get-Date

function Write-Trace([string]$Line) {
    $stamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    try { Add-Content -Path $LogFile -Value "$stamp  helper  $Line" -Encoding UTF8 } catch { }
    Write-Host $Line
}

function Show-Msg([string]$Text, [string]$Title) {
    try { [System.Windows.Forms.MessageBox]::Show($Text, $Title) | Out-Null }
    catch { Write-Host $Text }
}

function Write-Result($ok, $stage, $message, $code, $relaunch) {
    $ctxPath = Join-Path $UpdatesRoot "log-context.json"
    $current = ""; $latest = ""; $url = ""; $apikey = ""; $access = ""; $hwid = ""; $machine = $env:COMPUTERNAME
    if (Test-Path $ctxPath) {
        try {
            $ctx = Get-Content $ctxPath -Raw -Encoding UTF8 | ConvertFrom-Json
            $current = [string]$ctx.current
            $latest = [string]$ctx.latest
            $url = [string]$ctx.url
            $apikey = [string]$ctx.apikey
            $access = [string]$ctx.access
            $hwid = [string]$ctx.hwid
            $machine = [string]$ctx.machine
        } catch { }
    }
    $ms = [int]((Get-Date) - $started).TotalMilliseconds
    $obj = [ordered]@{
        ok        = [bool]$ok
        stage     = [string]$stage
        message   = [string]$message
        exitCode  = $code
        elapsedMs = $ms
        current   = $current
        latest    = $latest
        relaunched = [bool]$relaunch
        uploaded  = $false
        at        = (Get-Date).ToString("o")
    }
    ($obj | ConvertTo-Json -Compress) | Set-Content $ResultFile -Encoding UTF8
    Write-Trace "result ok=$ok stage=$stage code=$code $message"

    if ($url -and $access) {
        try {
            $body = @{
                p_event_type   = "update_result"
                p_command      = "UPD"
                p_detail       = $obj
                p_app_version  = $current
                p_hwid         = $hwid
                p_machine_name = $machine
            } | ConvertTo-Json -Compress -Depth 5
            $headers = @{
                Authorization = "Bearer $access"
                apikey        = $apikey
                "Content-Type" = "application/json"
            }
            Invoke-RestMethod -Method Post -Uri $url -Headers $headers -Body $body -TimeoutSec 12 | Out-Null
            $obj.uploaded = $true
            ($obj | ConvertTo-Json -Compress) | Set-Content $ResultFile -Encoding UTF8
            Write-Trace "result_posted"
        }
        catch {
            Write-Trace ("result_post_fail " + $_.Exception.Message)
        }
    }

    try {
        if (Test-Path $ctxPath) {
            $wipe = Get-Content $ctxPath -Raw -Encoding UTF8 | ConvertFrom-Json
            $wipe.access = ""
            ($wipe | ConvertTo-Json -Compress) | Set-Content $ctxPath -Encoding UTF8
        }
    } catch { }
}

function New-ProgressForm([string]$TitleText) {
    $form = New-Object System.Windows.Forms.Form
    $form.Text = "iSurvey"
    $form.Size = New-Object System.Drawing.Size(460, 200)
    $form.StartPosition = "CenterScreen"
    $form.FormBorderStyle = "FixedDialog"
    $form.MaximizeBox = $false
    $form.MinimizeBox = $false
    $form.TopMost = $true
    $form.BackColor = [System.Drawing.Color]::FromArgb(43, 48, 59)
    $lbl = New-Object System.Windows.Forms.Label
    $lbl.Name = "Title"
    $lbl.Text = $TitleText
    $lbl.ForeColor = [System.Drawing.Color]::White
    $lbl.Font = New-Object System.Drawing.Font("Segoe UI", 12, [System.Drawing.FontStyle]::Bold)
    $lbl.AutoSize = $false
    $lbl.Size = New-Object System.Drawing.Size(410, 28)
    $lbl.Location = New-Object System.Drawing.Point(20, 22)
    $sub = New-Object System.Windows.Forms.Label
    $sub.Name = "Detail"
    $sub.Text = ""
    $sub.ForeColor = [System.Drawing.Color]::FromArgb(140, 150, 168)
    $sub.Font = New-Object System.Drawing.Font("Segoe UI", 9)
    $sub.AutoSize = $false
    $sub.Size = New-Object System.Drawing.Size(410, 22)
    $sub.Location = New-Object System.Drawing.Point(20, 52)
    $bar = New-Object System.Windows.Forms.ProgressBar
    $bar.Name = "Bar"
    $bar.Style = "Marquee"
    $bar.MarqueeAnimationSpeed = 30
    $bar.Size = New-Object System.Drawing.Size(410, 16)
    $bar.Location = New-Object System.Drawing.Point(20, 88)
    $form.Controls.Add($lbl)
    $form.Controls.Add($sub)
    $form.Controls.Add($bar)
    $form.Show()
    [System.Windows.Forms.Application]::DoEvents()
    return $form
}

function Set-ProgressForm($form, [string]$TitleText, [string]$Detail) {
    if (-not $form) { return }
    $form.Controls["Title"].Text = $TitleText
    $form.Controls["Detail"].Text = $Detail
    [System.Windows.Forms.Application]::DoEvents()
}

Write-Trace "start ParentPid=$ParentPid SetupDir=$SetupDir"
$SetupDir = [System.IO.Path]::GetFullPath($SetupDir)
$installPs1 = Join-Path $SetupDir "Install-iSurvey.ps1"
$bundle = Join-Path $SetupDir "iSurvey.bundle"

if (-not (Test-Path $installPs1) -or -not (Test-Path $bundle)) {
    Write-Result $false "setup" "Thu muc Setup khong hop le" 1 $false
    Show-Msg "Thu muc Setup khong hop le (thieu Install-iSurvey.ps1 hoac iSurvey.bundle).`n$SetupDir" "iSurvey Cap nhat"
    exit 1
}

$form = New-ProgressForm "Dang dong AutoCAD/Civil..."
$deadline = (Get-Date).AddMinutes(4)
while ($true) {
    $alive = Get-Process -Id $ParentPid -ErrorAction SilentlyContinue
    if (-not $alive) { break }
    if ((Get-Date) -gt $deadline) {
        if ($form) { $form.Close() }
        Write-Result $false "wait" "Civil/AutoCAD chua thoat sau 4 phut (co the hop thoai Luu ban ve dang mo)" 2 $false
        Show-Msg "Civil/AutoCAD van dang mo (PID $ParentPid). Luu va dong hang, roi chay lai Install-iSurvey.bat trong:`n$SetupDir" "iSurvey Cap nhat"
        exit 2
    }
    [System.Windows.Forms.Application]::DoEvents()
    Start-Sleep -Milliseconds 400
}
Write-Trace "acad_exited"
Set-ProgressForm $form "Dang doi file DLL duoc nha khoa..." "Civil vua thoat, cho he thong nha iSurvey.dll"
$dllRoot = Join-Path $env:APPDATA "Autodesk\ApplicationPlugins\iSurvey.bundle"
$unlockDeadline = (Get-Date).AddSeconds(90)
while ((Get-Date) -lt $unlockDeadline) {
    $locked = @()
    if (Test-Path $dllRoot) {
        foreach ($dll in Get-ChildItem $dllRoot -Filter "iSurvey.dll" -Recurse -ErrorAction SilentlyContinue) {
            try {
                $fs = [IO.File]::Open($dll.FullName, "Open", "ReadWrite", "None")
                $fs.Close()
            }
            catch { $locked += $dll.FullName }
        }
    }
    if ($locked.Count -eq 0) { break }
    Write-Trace ("dll_locked " + ($locked | ForEach-Object { Split-Path $_ -Parent | Split-Path -Leaf }))
    [System.Windows.Forms.Application]::DoEvents()
    Start-Sleep -Seconds 2
}
if ($locked.Count -gt 0) {
    Write-Trace "dll_still_locked after 90s"
}

Set-ProgressForm $form "Dang cai dat iSurvey..." "Chep bundle va cau hinh TRUSTEDPATHS"
$installLog = Join-Path $UpdatesRoot "install-last.log"
$code = 1
$tries = 0
while ($tries -lt 8) {
    $tries++
    Write-Trace "install_try $tries"
    try {
        $out = & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $installPs1 2>&1
        $code = $LASTEXITCODE
        if ($null -eq $code) { $code = 0 }
        $out | Set-Content $installLog -Encoding UTF8
        Write-Trace ("install_out " + (($out | Out-String).Trim() -replace '\s+', ' ').Substring(0, [Math]::Min(300, (($out | Out-String).Trim().Length))))
    }
    catch {
        $code = 3
        Write-Trace ("install_ex " + $_.Exception.Message)
        $_.Exception.Message | Set-Content $installLog -Encoding UTF8
    }
    if ($code -eq 0) { break }
    Write-Trace "install_retry code=$code"
    Set-ProgressForm $form "Dang cai dat iSurvey..." "Thu lai lan $tries (file DLL co the con khoa)"
    Start-Sleep -Seconds 2
}

if ($code -ne 0) {
    if ($form) { $form.Close() }
    $tail = ""
    if (Test-Path $installLog) { $tail = " | " + ((Get-Content $installLog -Encoding UTF8 -ErrorAction SilentlyContinue | Select-Object -Last 4) -join " | ") }
    Write-Result $false "install" ("Cai dat that bai (ma $code) sau $tries lan" + $tail) $code $false
    Show-Msg "Cai dat that bai (ma $code). Xem %LocalAppData%\iSurvey\updates\install-last.log`n$SetupDir" "iSurvey Cap nhat"
    exit $code
}

Set-ProgressForm $form "Cai dat thanh cong" "Dang mo lai AutoCAD/Civil..."
Start-Sleep -Seconds 1
if ($form) { $form.Close() }

$relaunch = Join-Path $UpdatesRoot "relaunch.json"
$opened = $false
if (Test-Path $relaunch) {
    try {
        $info = Get-Content $relaunch -Raw -Encoding UTF8 | ConvertFrom-Json
        if ($info.exe -and (Test-Path $info.exe)) {
            $psi = New-Object System.Diagnostics.ProcessStartInfo
            $psi.FileName = [string]$info.exe
            $psi.Arguments = [string]$info.args
            $psi.UseShellExecute = $true
            [System.Diagnostics.Process]::Start($psi) | Out-Null
            $opened = $true
            Write-Trace "relaunch_ok"
        }
        else {
            Write-Trace "relaunch_skip exe missing"
        }
    }
    catch {
        Write-Trace ("relaunch_fail " + $_.Exception.Message)
    }
}

Write-Result $true "done" "Cai dat thanh cong" 0 $opened
if (-not $opened) {
    Show-Msg "Cap nhat iSurvey xong.`nHay mo lai AutoCAD / Civil 3D 2022-2027." "iSurvey Cap nhat"
}
exit 0
