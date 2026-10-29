# Cho tien trinh iSurvey goi sau khi tai zip Setup.
# Gui WM_CLOSE (cung nut dong cua so): ban ve chua luu thi Civil hoi luu.
# Khi tien trinh da tat thi cai ngay, khong cho 4 phut.
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
$logMark = 0
if (Test-Path $LogFile) { $logMark = (Get-Item $LogFile).Length }
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

Add-Type -TypeDefinition @"
using System;
using System.Text;
using System.Runtime.InteropServices;
public static class IswClose {
  public delegate bool EnumProc(IntPtr hWnd, IntPtr lParam);
  [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc cb, IntPtr l);
  [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint pid);
  [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr hWnd);
  [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern int GetClassName(IntPtr hWnd, StringBuilder s, int n);
  [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr w, IntPtr l);
  static bool _hit;
  static bool OnEnum(IntPtr h, IntPtr l) {
    uint p;
    GetWindowThreadProcessId(h, out p);
    if (p != (uint)l.ToInt64() || !IsWindowVisible(h)) return true;
    var sb = new StringBuilder(32);
    GetClassName(h, sb, sb.Capacity);
    if (sb.ToString() == "#32770") _hit = true;
    return true;
  }
  public static bool HasDialog(uint pid) {
    _hit = false;
    EnumWindows(OnEnum, (IntPtr)pid);
    return _hit;
  }
}
"@

Write-Trace "start ParentPid=$ParentPid SetupDir=$SetupDir"
$SetupDir = [System.IO.Path]::GetFullPath($SetupDir)
$installPs1 = Join-Path $SetupDir "Install-iSurvey.ps1"
$bundle = Join-Path $SetupDir "iSurvey.bundle"

if (-not (Test-Path $installPs1) -or -not (Test-Path $bundle)) {
    Write-Result $false "setup" "Thu muc Setup khong hop le" 1 $false
    Show-Msg "Thu muc Setup khong hop le (thieu Install-iSurvey.ps1 hoac iSurvey.bundle).`n$SetupDir" "iSurvey Cap nhat"
    exit 1
}

# Khong hien cua so len tren Civil luc nay: no che hop "Luu ban ve?".
# Cho dong "close_hand_off" moi gui dong — luc do lenh UPD da sap ket thuc.
$handOff = (Get-Date).AddSeconds(8)
while ((Get-Date) -lt $handOff) {
    $aliveNow = Get-Process -Id $ParentPid -ErrorAction SilentlyContinue
    if (-not $aliveNow) { break }
    if ((Test-Path $LogFile) -and (Get-Item $LogFile).Length -gt $logMark) {
        $fs = New-Object System.IO.FileStream($LogFile, 'Open', 'Read', 'ReadWrite')
        [void]$fs.Seek($logMark, 'Begin')
        $reader = New-Object System.IO.StreamReader($fs)
        $tail = $reader.ReadToEnd()
        $reader.Close()
        if ($tail -match 'close_hand_off') { break }
    }
    Start-Sleep -Milliseconds 100
}
Start-Sleep -Milliseconds 200

$alive = Get-Process -Id $ParentPid -ErrorAction SilentlyContinue
$frame = [IntPtr]::Zero
if ($alive) { $frame = $alive.MainWindowHandle }
$posts = 0
$nextPost = Get-Date
$sawDialog = $false
$failAt = (Get-Date).AddSeconds(5)
while ($alive -and -not $alive.HasExited) {
    $dialog = [IswClose]::HasDialog([uint32]$ParentPid)
    if ($dialog) {
        $sawDialog = $true
    }
    elseif ($sawDialog) {
        Start-Sleep -Milliseconds 400
        $still = Get-Process -Id $ParentPid -ErrorAction SilentlyContinue
        if (-not $still) { break }
        Write-Trace "close_cancelled"
        Write-Result $false "wait" "Nguoi dung huy dong Civil" 2 $false
        Show-Msg "Ban da khong dong Civil, nen chua cai dat.`nChay lai UPD khi muon cap nhat." "iSurvey Cap nhat"
        exit 2
    }
    elseif ($posts -lt 2 -and $frame -ne [IntPtr]::Zero -and (Get-Date) -ge $nextPost) {
        # WM_CLOSE = bam nut dong. Ban ve chua luu thi Civil hoi luu hay khong.
        if ($alive.MainWindowHandle -ne [IntPtr]::Zero) { $frame = $alive.MainWindowHandle }
        [void][IswClose]::PostMessage($frame, 0x0010, [IntPtr]::Zero, [IntPtr]::Zero)
        $posts++
        $nextPost = (Get-Date).AddSeconds(2)
        Write-Trace "wm_close $posts"
        $failAt = (Get-Date).AddSeconds(5)
    }
    elseif ((Get-Date) -gt $failAt) {
        Write-Trace "close_timeout posts=$posts"
        Write-Result $false "wait" "Khong dong duoc Civil" 2 $false
        Show-Msg "Khong dong duoc Civil. Hay dong Civil, roi chay Install-iSurvey.bat trong:`n$SetupDir" "iSurvey Cap nhat"
        exit 2
    }
    Start-Sleep -Milliseconds 200
    try { $alive.Refresh() } catch { break }
    if ($alive.HasExited) { break }
    $alive = Get-Process -Id $ParentPid -ErrorAction SilentlyContinue
}
Write-Trace "acad_exited"

$form = New-ProgressForm "Dang cai dat iSurvey..."
$dllRoot = Join-Path $env:APPDATA "Autodesk\ApplicationPlugins\iSurvey.bundle"
$locked = @()
for ($i = 0; $i -lt 8; $i++) {
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
    Start-Sleep -Milliseconds 250
}
if ($locked.Count -gt 0) { Write-Trace "dll_still_locked" }

Set-ProgressForm $form "Dang cai dat iSurvey..." "Chep bundle va cau hinh TRUSTEDPATHS"
$installLog = Join-Path $UpdatesRoot "install-last.log"
$code = 1
$tries = 0
while ($tries -lt 4) {
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
    Start-Sleep -Milliseconds 300
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
Start-Sleep -Milliseconds 300
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
