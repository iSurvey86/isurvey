# Dong bo CHANGELOG (dev) -> trang HDSD «Tính năng mới» (nguoi dung).
# Usage: powershell -NoProfile -File .\deploy\sync-hdsd-tinh-nang-moi.ps1
param(
    [string]$Changelog = (Join-Path $PSScriptRoot "..\docs\changelog\CHANGELOG.md"),
    [string]$OutFile = (Join-Path $PSScriptRoot "..\..\ksnpsc\docs\hdsd\isurvey\09-tinh-nang-moi.md")
)

$ErrorActionPreference = "Stop"
$Changelog = [IO.Path]::GetFullPath($Changelog)
$OutFile = [IO.Path]::GetFullPath($OutFile)
if (-not (Test-Path $Changelog)) { throw "Khong thay CHANGELOG: $Changelog" }

# Bullet viet lai cho nguoi dung (uu tien hon dong goc). Key = "1.8.24"
$curated = [ordered]@{
    "1.8.26" = @(
        "Hộp cập nhật xếp phiên bản mới nhất trước."
    )
    "1.8.25" = @(
        "Sau cập nhật không tự mở lại Civil — hiện hộp thoại, bạn tự mở CAD.",
        "Hộp thông báo cập nhật tiếng Việt rõ ràng, giao diện gọn hơn."
    )
    "1.8.24" = @(
        "Hộp cập nhật chỉ hiện vài phiên bản gần nhất.",
        "Trên hướng dẫn web có mục Tính năng mới để xem đủ lịch sử."
    )
    "1.8.23" = @(
        "Hộp cập nhật chỉ liệt kê thay đổi từ bản đang cài đến bản mới (mỗi máy một danh sách)."
    )
    "1.8.22" = @(
        "Sửa lỗi không mở lại được Civil / AutoCAD sau khi cập nhật."
    )
    "1.8.21" = @(
        "Hộp cập nhật: số phiên bản in đậm, tính năng chữ thường, phân cấp rõ hơn."
    )
    "1.8.20" = @(
        "VEMCD: thanh chia cột cùng màu khung bảng.",
        "D2C: ổn định hơn khi nhập hàng loạt file DGN V7.",
        "Menu iSurvey trên thanh Menu khi tắt Ribbon (MENUISURVEY).",
        "GHISODM: hộp thoại ghi số đồng mức (chữ / số cố định / click tuần tự).",
        "XAYMOHINH: dựng mô hình địa hình nhanh và ổn định hơn; cảnh báo đồng mức thiếu cao độ.",
        "PHATSINHCOC: lấy cao độ từ mô hình nhanh hơn; hộp thoại gọn.",
        "TAOMOHINH: hộp thoại gọn hơn."
    )
    "1.8.19" = @(
        "Tự kiểm tra và báo có bản mới khi mở AutoCAD / Civil 3D (lệnh UPD vẫn dùng được)."
    )
    "1.8.18" = @(
        "VEMCD: giao diện gọn hơn — cách đầu bảng, khoảng cách 100 m chữ ngang, néo chữ, chữ thập tùy chọn."
    )
    "1.8.17" = @(
        "GOC: chữ góc né tim tuyến; xuất mô tả tuyến (góc độ phút giây).",
        "DUOITUYEN: polyline tim nét đứt sau khi duỗi; bảng góc chuyển sang lệnh GOC.",
        "TAOTUYEN: mặc định chọn mô hình vừa tạo.",
        "THEMMOHINH: bỏ chọn tất cả; tùy chọn điểm/block kèm chữ cao độ cạnh.",
        "Hộp tiến trình dùng chung cho nhiều lệnh (nhập/xuất DGN, UAV, cọc, tuyến, bản đồ…); có nút Hủy.",
        "VEMCD: vẽ mặt cắt dọc trên Model (đầu bảng, đường đất, thước cao độ)."
    )
    "1.8.16" = @(
        "RAIMCD / RAIKHUNG: bố trí hộp thoại gọn; xuất/nhập cấu hình khung Excel.",
        "NHAPDIEM: bảng sửa được; chọn kiểu và cỡ điểm; X/Y theo trắc địa.",
        "Tuyến và cọc: TAOTUYEN (tạo tuyến), PHATSINHCOC (phát sinh cọc), THEMCOC (thêm một cọc), XOACOC1 (xóa một cọc), BANGCOC (bảng danh sách cọc), DAODAUTUYEN (đảo đầu tuyến), XOACOC (xóa hết cọc), THIETLAPCOC (thiết lập cọc).",
        "Mô hình địa hình: TAOMOHINH (tạo mô hình), THEMMOHINH (thêm thành phần), XAYMOHINH (dựng TIN), TRACAOD (tra cao độ), GOMOHINH (gỡ đối tượng khỏi mô hình), XOATHANHPHAN (xóa cả một loại thành phần), ANTIN (ẩn/hiện lưới).",
        "BANGIS: danh sách mô hình đã lưu; chuột phải để xây dựng TIN, ẩn/hiện, tra cao độ.",
        "MENUISURVEY: dựng lại tab Ribbon iSurvey khi tab bị mất."
    )
    "1.8.15" = @(
        "RAIKHUNG và RAIMCD chỉ tạo layout; in PDF bằng lệnh INTUYEN riêng.",
        "INTUYEN: in màu hoặc đen trắng; chèn chữ ký và mộc đỏ trên PDF.",
        "Chữ ký trên PDF lấy theo chức danh ghi trên khung.",
        "Giao diện hộp thoại đồng bộ (nền sáng).",
        "Cấu hình DGN nằm trên hộp Nhập DGN / Xuất DGN (nhớ lựa chọn lần trước).",
        "Mỗi nút trên tab iSurvey có icon riêng.",
        "Nút Hướng dẫn (HD) mở trang hướng dẫn trên web."
    )
    "1.8.14" = @(
        "XUATDIEM: xuất Point, Block, Text/MText; chữ số thành cao độ.",
        "Xuất CSV/TXT theo dấu phân cách Windows; số thập phân dùng dấu chấm.",
        "Sửa in khung: mộc đỏ chỉ trên PDF, không để lại trên bản vẽ.",
        "Sau khi in PDF có nút mở thư mục kết quả.",
        "Nhắp đúp khung mở hộp sửa của iSurvey."
    )
    "1.8.13" = @(
        "Cập nhật: hộp hỏi lưu hiện kịp khi gói đã sẵn sàng cài."
    )
    "1.8.12" = @(
        "Cập nhật: chọn không lưu khi đóng Civil vẫn cài được bản mới."
    )
    "1.8.11" = @(
        "Sau khi tải bản mới, Civil đóng như bấm nút đóng cửa sổ (hỏi lưu nếu cần) rồi cài ngay."
    )
    "1.8.10" = @(
        "NHAPDIEM / XUATDIEM: nhập xuất điểm đo CSV/TXT.",
        "STT: đánh số thứ tự tại từng điểm click."
    )
    "1.8.9" = @(
        "RAIKHUNG / RAIMCD: hộp thoại hai cột, nhập khung tên từ Excel/CSV.",
        "In màu hoặc đen trắng theo bảng nét trên máy."
    )
    "1.8.8" = @(
        "In PDF nét mỏng ổn định hơn.",
        "Chữ trên tờ in đọc ngang; nhớ logo và chữ ký đã chọn trong cùng file.",
        "RAIKHUNG: rải khung theo tuyến; INTUYEN in lại các layout đã tạo.",
        "RAIMCD: rải khung mặt cắt dọc."
    )
    "1.8.7" = @(
        "Dàn trang in: nhập khung tên từ tệp, chèn chữ ký, xuất PDF gộp.",
        "INTUYEN: in lại layout tuyến đã có."
    )
    "1.8.6" = @(
        "DANTRANG: dàn trang in theo tuyến (khổ giấy, tỷ lệ, lưới tờ).",
        "Hộp UPD: xem bản mới và cập nhật ngay trên máy."
    )
    "1.8.5" = @(
        "ANLOGO: ẩn logo chủ đầu tư hoặc đơn vị thiết kế riêng.",
        "CODAN: co chữ ô chủ đầu tư về một dòng."
    )
    "1.8.4" = @(
        "Chèn ảnh vệ tinh: chọn xem lướt hoặc ghép ảnh HD bằng tên tiếng Việt."
    )
    "1.8.3" = @(
        "Hỗ trợ AutoCAD / Civil 3D 2022–2024 trên cùng gói plugin.",
        "Xuất DGN kèm đủ seed V7/V8.",
        "Nhập DGN: ghép một file hoặc tách từng file DWG."
    )
    "1.8.2" = @(
        "Nhập DGN (D2C): bảng nhiều tệp; ghép tọa độ hoặc chuyển đổi rời."
    )
    "1.8.1" = @(
        "Nhập MapInfo TAB (T2C): tô màu, layer theo thuộc tính, nhãn trong thửa."
    )
    "1.8.0" = @(
        "KHUNG / KHUNGBV: chèn khung bản đồ và khung bản vẽ kỹ thuật theo TCVN.",
        "DUOITUYEN: duỗi thẳng tuyến và xuất bảng góc."
    )
    "1.7.2" = @(
        "UPD: tải bản mới và cài sau khi đóng Civil."
    )
    "1.7.1" = @(
        "Sửa lỗi giấy phép hết hạn sớm; người dùng không phải đăng nhập lại oan."
    )
    "1.7.0" = @(
        "Cập nhật trực tuyến: lệnh UPD kiểm tra và tải bản mới."
    )
    "1.6.0" = @(
        "Cải thiện nhập / xuất DGN với seed VN-2000."
    )
    "1.5.2" = @(
        "Chèn ảnh vệ tinh: chế độ xem lướt hoặc ghép một ảnh độ phân giải cao."
    )
    "1.5.1" = @(
        "Sửa hộp chọn kinh tuyến trục khi chèn ảnh / xuất bản đồ."
    )
    "1.5.0" = @(
        "Nhập / xuất KML–KMZ và các lệnh bản đồ liên quan."
    )
    "1.4.0" = @(
        "CATBD: cắt bình đồ theo biên khép kín.",
        "AUTOSCAN: quét cấu trúc bản vẽ và xuất báo cáo."
    )
    "1.3.0" = @(
        "Đăng nhập Gmail bằng mã 6 số.",
        "Tối đa 2 máy mỗi tài khoản; đăng xuất / đổi máy trả suất.",
        "Các lệnh tài khoản: LGI (Đăng nhập ứng dụng), UPD (Cập nhật phiên bản mới), ABT (Thông tin & giấy phép), LGO (Thoát ứng dụng)."
    )
    "1.2.0" = @(
        "Tab Ribbon iSurvey với các nhóm lệnh chính."
    )
    "1.1.1" = @(
        "Sửa xuất KML theo múi TM-3 / TM-6.",
        "Bổ sung danh mục tỉnh/thành cho hệ tọa độ."
    )
    "1.1.0" = @(
        "Xuất CAD sang Google Earth (KML/KMZ).",
        "Hỗ trợ múi 3° (TM-3) và múi 6° (TM-6)."
    )
    "1.0.0" = @(
        "Chèn ảnh vệ tinh Google Earth theo VN-2000.",
        "Bộ cài tự nạp trên AutoCAD / Civil 3D."
    )
}

function Test-DropLine([string]$line) {
    $t = $line.Trim()
    if ($t.Length -eq 0) { return $true }
    if ($t -match '^#\s') { return $true }
    if ($t -match '^###\s') { return $true }
    if ($t -match '^Đã phát hành') { return $true }
    if ($t -match '^Chưa phát hành') { return $true }
    if ($t -match 'publish-release|version\.json|service_role|isurvey-releases') { return $true }
    if ($t -match 'HANDOFF|docs/|csproj|PackageContents|schema_') { return $true }
    if ($t -match 'Harness|SelfCheck|MeshTri|deserialize|StringBuilder|StoredTriangle|adjacency|unforced|tip\[\]') { return $true }
    if ($t -match 'Video Factory|audit|kịch bản video|Chưa quay|catalog Excel|README') { return $true }
    if ($t -match '(?i)\bAdmin\b|IsAdmin|chỉ Admin|GENOTP|AIA\b|AICFG|blocklist|ActivityLogger|refresh token|OTP') { return $true }
    if ($t -match 'Đã nhìn trên|Drawing1|bản sao trong phiên|nghiệm thu AutoCAD|Last seen|Ver\b') { return $true }
    if ($t -match 'Xem mục \*\*|đã gộp vào|Sau phát hành|Kiểm toán|chưa đăng|repo ksnpsc|catalog:') { return $true }
    if ($t -match 'Supabase|Storage bucket|TRUSTEDPATHS|net48|net8|net10|TargetFramework|license\.dat') { return $true }
    if ($t -match 'DXF \d+|MiddleCenter|ByLayer|ACI \d+|UOR|TCB|Quản lý hệ thống') { return $true }
    if ($t -match '^\s*-\s*`[^`]+`\s*$') { return $true }
    return $false
}

function Simplify-Bullet([string]$line) {
    $t = $line.Trim()
    if ($t.StartsWith("-")) { $t = $t.Substring(1).Trim() }
    # Bo phan ky thuat sau dau em dash dai neu co
    if ($t -match '^(.*?)(\s[—–]\s.*(NOD|Mesh|JSON|parse|DLL|API|buffer|Face).*)') {
        $t = $Matches[1].Trim().TrimEnd(';', ',')
    }
    # Bo duong dan file / backtick path
    $t = [regex]::Replace($t, '`[^`]+`', '')
    $t = [regex]::Replace($t, '\s{2,}', ' ').Trim().TrimEnd('.')
    if ($t.Length -lt 12) { return $null }
    if ($t -match '^(Fix|Bump|Build|Deploy|Schema|RPC)\b') { return $null }
    return "- $t."
}

$raw = Get-Content -Raw -Encoding UTF8 $Changelog
$raw = $raw -replace "`r`n", "`n"
$parts = [regex]::Split($raw, '(?m)^##\s+')
$sections = New-Object System.Collections.Generic.List[string]

foreach ($part in $parts) {
    if ([string]::IsNullOrWhiteSpace($part)) { continue }
    if ($part -match '^Changelog') { continue }

    $m = [regex]::Match($part, '^(?<v>\d+(?:\.\d+){1,3})\s*[—\-–]\s*(?<d>\d{4}-\d{2}-\d{2})')
    if (-not $m.Success) { continue }
    $ver = $m.Groups['v'].Value
    $iso = $m.Groups['d'].Value
    $dt = [datetime]::ParseExact($iso, 'yyyy-MM-dd', $null)
    $vnDate = $dt.ToString('dd-MM-yyyy')

    $body = $part.Substring($m.Length)
    $bullets = New-Object System.Collections.Generic.List[string]

    # Chi dung ban viet lai cho nguoi dung — khong copy nguyen CHANGELOG (tranh lo ky thuat/admin).
    if (-not $curated.Contains($ver)) { continue }
    foreach ($b in $curated[$ver]) { $bullets.Add("- $b") }

    if ($bullets.Count -eq 0) { continue }

    $sb = New-Object System.Text.StringBuilder
    [void]$sb.AppendLine("## Phiên bản: $ver - ngày phát hành: $vnDate")
    [void]$sb.AppendLine()
    foreach ($b in $bullets) { [void]$sb.AppendLine($b) }
    [void]$sb.AppendLine()
    $sections.Add($sb.ToString().TrimEnd() + "`n")
}

$out = New-Object System.Text.StringBuilder
[void]$out.AppendLine("# Tính năng mới")
[void]$out.AppendLine()
foreach ($s in $sections) { [void]$out.AppendLine($s.TrimEnd()); [void]$out.AppendLine() }

$dir = Split-Path $OutFile -Parent
if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Force -Path $dir | Out-Null }
[IO.File]::WriteAllText($OutFile, $out.ToString().TrimEnd() + "`n", [Text.UTF8Encoding]::new($false))
Write-Host "OK -> $OutFile ($($sections.Count) phien ban)"
