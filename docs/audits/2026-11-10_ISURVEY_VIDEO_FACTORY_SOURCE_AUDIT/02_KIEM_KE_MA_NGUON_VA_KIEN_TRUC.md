# 02 — Kiểm kê mã nguồn và kiến trúc

## 1. Cây phân loại

| Phân loại | Đường dẫn / ghi chú | Đưa vào phân tích chuyên sâu? |
|---|---|---|
| Mã nghiệp vụ add-in | `src/Modules/**`, `src/Dgn/**`, `src/Auth/**`, `src/CommandAliases.cs`, `src/Main.cs` | Có |
| UI / Ribbon / Palette / Dialog | `src/UI/**` (23 XAML + handlers) | Có |
| Core / Data / Models | `src/Core`, `src/Data`, `src/Models`, `src/Compat` | Có (điểm vào + phụ thuộc) |
| Template / Seed / PlotStyle | `src/Templates/*.dwg`, `src/Seeds/*.dgn`, `src/PlotStyles` | Có (tài nguyên đóng gói) |
| Bundle / deploy | `deploy/iSurvey.bundle`, `deploy/*.ps1` | Có (cấu hình nạp plugin) |
| Tool video cũ | `tools/video/*` | Có (đối chiếu Video Factory) |
| Smoke MCP | `deploy/tools/McpSmokeTest` | Có giới hạn (không vào DLL chính) |
| Docs sản phẩm / video | `docs/**` (trừ untracked cục bộ) | Có đối chiếu |
| Build artifacts | `bin/`, `obj/` | Loại trừ chuyên sâu (sinh tự động) |
| Untracked cục bộ | `docs/MCD`, `docs/SEED 1000`, `docs/dau`, `tmp`, `*.bak` | Loại trừ nội dung; chỉ ghi nhận tồn tại |
| AutoLISP | — | `CHƯA TÌM THẤY` file `.lsp/.vlx/.fas` trong repo |

## 2. Thống kê tệp (đếm tĩnh)

| Loại | Số | Phạm vi |
|---|---:|---|
| `.cs` dưới `src/` | 198 | Đã liệt kê / quét CommandMethod toàn bộ |
| `.xaml` dưới `src/` | 23 | Đã liệt kê |
| `[CommandMethod(` | 125 | Toàn `src/` |
| Ribbon `new Btn(...)` | 61 | `RibbonBuilder.cs` |
| Module folders dưới `src/Modules` | Ai(10), DiemDo(7), Export(4), Frame(17), Import(8), Map(22), Plan(3), Profile(10), Scan(2), Terrain(6), Tuyen(12), Uav(3) — số file `.cs` | |

## 3. Điểm nạp plugin — `CÓ BẰNG CHỨNG TỪ MÃ NGUỒN`

1. **Assembly entry:** `[assembly: ExtensionApplication(typeof(iSurvey.Main))]` — `src/Main.cs:16`.
2. **IExtensionApplication:** `Main.Initialize` / `Terminate` — `src/Main.cs:45–116`.
3. **CommandClass đăng ký:** danh sách `[assembly: CommandClass(...)]` — `src/Main.cs:20–39` (bỏ qua khi `DEVRELOAD`).
4. **Autoload bundle:** `deploy/iSurvey.bundle/PackageContents.xml` — `LoadOnAutoCADStartup="True"`, `ModuleName` theo Series 2023/2025/2027.
5. **Ribbon:** `RibbonBuilder.EnsureRibbon()` gọi từ `Main.Initialize` — `src/Main.cs:58`, `src/UI/RibbonBuilder.cs:31`.

`NETLOAD` thủ công: không thấy quy trình bắt buộc trong source; bundle autoload là đường chính theo PackageContents (`CÓ BẰNG CHỨNG`). Việc người dùng thực tế dùng NETLOAD: `CHƯA KIỂM CHỨNG THỰC TẾ`.

## 4. Phụ thuộc Autodesk / thư viện

- AutoCAD.NET theo TFM (xem `01` / `iSurvey.csproj:31–33`).
- Không thấy PackageReference Civil 3D API riêng trong `iSurvey.csproj` — nhiều lệnh ghi Civil trong docs nhưng biên dịch trên AutoCAD.NET; hành vi Civil-only: `SUY LUẬN CẦN XÁC MINH` / `CHƯA KIỂM CHỨNG THỰC TẾ`.
- GDAL (TAB), ProjNet (CRS), System.Text.Json (net48).

## 5. Luồng chính (sơ đồ chữ)

```
AutoCAD/Civil khởi động
  → PackageContents nạp iSurvey.dll (theo Series)
    → Main.Initialize
      → AuthManager.Initialize (đọc license cục bộ, không dialog)
      → Map/Export Initialize
      → RibbonBuilder.EnsureRibbon
      → Idle: sửa FILEDIA/CMDDIA nếu =0; EnsureRibbon lại
      → AuthManager.StartBackgroundRecheck (mạng)
    → Người dùng: Ribbon / gõ lệnh / alias CommandAliases
      → AuthManager.EnsureAuthenticated (hầu hết lệnh nghiệp vụ)
      → Module service (Map/Dgn/Terrain/Route/Profile/…)
```

## 6. Alias vs lệnh đầy đủ

- Lệnh đầy đủ / nghiệp vụ: khai báo trong các `*Module.cs` / `*Service.cs` (**79** khai báo primary theo extractor).
- Alias ngắn: `src/CommandAliases.cs` (**46** `CommandMethod`) ủy quyền sang instance module.
- Một số lệnh vừa có full name vừa có alias cùng file (ví dụ `VEMCD` + `ISURVEY_PROFILE_DRAW` trong `ProfileModule.cs:14–18`).

## 7. Ribbon placeholder không có CommandMethod

Các nút `enabled=false` trên Ribbon trỏ tới chuỗi lệnh **không** xuất hiện trong bất kỳ `[CommandMethod]` nào đã quét — được xử lý bởi `RibbonPlaceholderHandler` (`src/UI/RibbonPlaceholderHandler.cs`). Danh sách đầy đủ trong `08` / phụ lục `_inventory_ribbon.csv` (cột enabled=False và cmd ∉ inventory commands). Đây là **chức năng UI** chưa phải lệnh CAD thật.

## 8. Tập tệp kiểm toán lõi (không liệt kê hết 198 dòng tại đây)

Ưu tiên đã đọc cấu trúc + điểm vào:

- `src/Main.cs`, `src/CommandAliases.cs`, `src/UI/RibbonBuilder.cs`, `src/UI/RibbonCommandHandler.cs`, `src/UI/RibbonPlaceholderHandler.cs`
- Toàn bộ file chứa `[CommandMethod` (inventory CSV)
- `src/Auth/AuthManager.cs`, `AuthModule.cs` (cổng đăng nhập / Admin / OTP)
- Module đại diện sâu hơn: `Profile/*`, `Uav/UavModule.cs`, `Terrain/TerrainModule.cs`, `Tuyen/RouteModule.cs`, `Dgn/DgnModule.cs`, `Frame/*` (điểm vào)
- `deploy/iSurvey.bundle/PackageContents.xml`, `iSurvey.csproj`, `tools/video/*`, `docs/video/*`

Chi tiết bao phủ từng nhóm: `11_MA_TRAN_BANG_CHUNG_VA_DO_DAY_DU.md`.
