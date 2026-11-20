# Cho tien trinh iSurvey goi sau khi tai zip Setup.
# Gui WM_CLOSE (cung nut dong cua so): ban ve chua luu thi Civil hoi luu.
# Khi tien trinh da tat thi cai ngay, khong cho 4 phut.
# Usage: powershell -ExecutionPolicy Bypass -STA -File .\Apply-iSurveyUpdate.ps1 -ParentPid 1234 -SetupDir "C:\...\extracted"

param(
    [Parameter(Mandatory = $true)][int]$ParentPid,
    [Parameter(Mandatory = $true)][string]$SetupDir,
    [string]$Signal = ""
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

function Show-Msg([string]$Text, [string]$Title, [string]$Kind = "info") {
    # Hop thoai WinForms (UTF-8): khong dung MessageBox he thong (mat dau tieng Viet / giao dien don dieu).
    try {
        $ink = [System.Drawing.Color]::FromArgb(36, 48, 64)
        $muted = [System.Drawing.Color]::FromArgb(92, 107, 122)
        $accent = [System.Drawing.Color]::FromArgb(37, 99, 235)
        $okBg = [System.Drawing.Color]::FromArgb(27, 122, 58)
        $warnBg = [System.Drawing.Color]::FromArgb(180, 83, 9)
        $bar = switch ($Kind) {
            "ok" { $okBg }
            "warn" { $warnBg }
            default { $accent }
        }
        $headline = switch ($Kind) {
            "ok" { "Cập nhật thành công" }
            "warn" { "Chưa cập nhật" }
            default { "Thông báo" }
        }

        $form = New-Object System.Windows.Forms.Form
        $form.Text = $Title
        $form.Size = New-Object System.Drawing.Size(460, 260)
        $form.StartPosition = "CenterScreen"
        $form.FormBorderStyle = "FixedDialog"
        $form.MaximizeBox = $false
        $form.MinimizeBox = $false
        $form.TopMost = $true
        $form.BackColor = [System.Drawing.Color]::FromArgb(243, 245, 248)
        $form.Font = New-Object System.Drawing.Font("Segoe UI", 9.5)

        $card = New-Object System.Windows.Forms.Panel
        $card.BackColor = [System.Drawing.Color]::White
        $card.Location = New-Object System.Drawing.Point(16, 16)
        $card.Size = New-Object System.Drawing.Size(412, 150)
        $card.BorderStyle = "FixedSingle"

        $stripe = New-Object System.Windows.Forms.Panel
        $stripe.BackColor = $bar
        $stripe.Location = New-Object System.Drawing.Point(0, 0)
        $stripe.Size = New-Object System.Drawing.Size(6, 150)

        $h = New-Object System.Windows.Forms.Label
        $h.Text = $headline
        $h.ForeColor = $ink
        $h.Font = New-Object System.Drawing.Font("Segoe UI", 13, [System.Drawing.FontStyle]::Bold)
        $h.Location = New-Object System.Drawing.Point(22, 16)
        $h.Size = New-Object System.Drawing.Size(370, 28)

        $b = New-Object System.Windows.Forms.Label
        $b.Text = $Text
        $b.ForeColor = $muted
        $b.Font = New-Object System.Drawing.Font("Segoe UI", 10)
        $b.Location = New-Object System.Drawing.Point(22, 52)
        $b.Size = New-Object System.Drawing.Size(370, 80)

        $card.Controls.Add($stripe)
        $card.Controls.Add($h)
        $card.Controls.Add($b)

        $btn = New-Object System.Windows.Forms.Button
        $btn.Text = "Đóng"
        $btn.Size = New-Object System.Drawing.Size(120, 34)
        $btn.Location = New-Object System.Drawing.Point(308, 180)
        $btn.FlatStyle = "Flat"
        $btn.BackColor = $accent
        $btn.ForeColor = [System.Drawing.Color]::White
        $btn.Font = New-Object System.Drawing.Font("Segoe UI", 9.5, [System.Drawing.FontStyle]::Bold)
        $btn.FlatAppearance.BorderSize = 0
        $btn.Cursor = [System.Windows.Forms.Cursors]::Hand
        $btn.DialogResult = [System.Windows.Forms.DialogResult]::OK
        $form.AcceptButton = $btn
        $form.Controls.Add($card)
        $form.Controls.Add($btn)
        [void]$form.ShowDialog()
        $form.Dispose()
    }
    catch {
        try { [System.Windows.Forms.MessageBox]::Show($Text, $Title) | Out-Null } catch { Write-Host $Text }
    }
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
    Show-Msg "Thư mục Setup không hợp lệ (thiếu Install-iSurvey.ps1 hoặc iSurvey.bundle).`n$SetupDir" "iSurvey — Cập nhật" "warn"
    exit 1
}

# Khong hien cua so len tren Civil luc nay: no che hop "Luu ban ve?".
# Cho dong "close_hand_off" moi gui dong — luc do lenh UPD da sap ket thuc.
$handOff = (Get-Date).AddSeconds(3)
while ((Get-Date) -lt $handOff) {
    $aliveNow = Get-Process -Id $ParentPid -ErrorAction SilentlyContinue
    if (-not $aliveNow) { break }
    if ($Signal -and (Select-String -Path $LogFile -Pattern $Signal -Quiet -ErrorAction SilentlyContinue)) { break }
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
        # Hop thoai vua dong. No/Yes: Civil con vai giay de luu hoac tat. Cancel: cua so van tuong tac.
        $answerDeadline = (Get-Date).AddSeconds(8)
        $shutdownDeadline = (Get-Date).AddSeconds(60)
        while ($true) {
            $still = Get-Process -Id $ParentPid -ErrorAction SilentlyContinue
            if (-not $still) { break }
            try { if ($still.HasExited) { break } } catch { break }
            if ([IswClose]::HasDialog([uint32]$ParentPid)) { break }
            try { $still.Refresh() } catch { break }
            if (-not $still.Responding) {
                if ((Get-Date) -gt $shutdownDeadline) { break }
                Start-Sleep -Milliseconds 200
                continue
            }
            if ((Get-Date) -gt $answerDeadline) {
                Write-Trace "close_cancelled"
                Write-Result $false "wait" "Nguoi dung huy dong Civil" 2 $false
                Show-Msg "Bạn đã không đóng Civil, nên chưa cài đặt.`nChạy lại UPD khi muốn cập nhật." "iSurvey — Cập nhật" "warn"
                exit 2
            }
            Start-Sleep -Milliseconds 200
        }
        $still = Get-Process -Id $ParentPid -ErrorAction SilentlyContinue
        if (-not $still) { break }
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
        Show-Msg "Không đóng được Civil. Hãy đóng Civil, rồi chạy Install-iSurvey.bat trong:`n$SetupDir" "iSurvey — Cập nhật" "warn"
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
    Show-Msg "Cài đặt thất bại (mã $code). Xem %LocalAppData%\iSurvey\updates\install-last.log`n$SetupDir" "iSurvey — Cập nhật" "warn"
    exit $code
}

Set-ProgressForm $form "Cài đặt thành công" "Không tự mở lại Civil — bạn mở thủ công."
Start-Sleep -Milliseconds 300
if ($form) { $form.Close() }

# Khong tu mo lai CAD (tranh FATAL ASC). Chi thong bao de nguoi dung tu mo.
$relaunch = Join-Path $UpdatesRoot "relaunch.json"
if (Test-Path $relaunch) {
    Remove-Item $relaunch -Force -ErrorAction SilentlyContinue
    Write-Trace "relaunch_cleared_no_autostart"
}

Write-Result $true "done" "Cài đặt thành công" 0 $false
Show-Msg "Cập nhật iSurvey xong.`nHãy mở lại AutoCAD / Civil 3D (2022–2027) từ Start Menu hoặc lối tắt của bạn." "iSurvey — Cập nhật" "ok"
exit 0
