# Nhóm DIEM

Sinh 2026-11-10 — R02B.

## AUD-012 — NHAPDIEM

### 1. Định danh
- **feature_id:** `AUD-012`
- **Hành vi:** NHAPDIEM
- **Nhóm:** DIEM | **Audience:** USER
- **source_status:** `SOURCE_PRESENT` — có CommandMethod + thân xử lý trong source (runtime NOT_TESTED)
- **R01 cmds:** NHAPDIEM
- **R02 refs:** AUD-013

### 2. Kích hoạt
- **Lệnh chính:** `NHAPDIEM` @ `src/Modules/DiemDo/PointFileModule.cs:13`
- **Alias module (ngoài CommandAliases.cs):** (không)
- **Alias CommandAliases.cs:** ND
- **Effective method:** `ImportPointsCommand` span `14-60`
- **Ribbon:** Nhập điểm đo|NHAPDIEM|en=True|L379
- **Palette:** NHAPDIEM|src/UI/Palette/PaletteViewModel.cs:L118
- **XAML mapped:** (không / UI dialog nội bộ — xem 03D)

### 3. Chuỗi xử lý
- CommandMethod NHAPDIEM @ src/Modules/DiemDo/PointFileModule.cs:13 → ImportPointsCommand
- AuthManager.EnsureAuthenticated @ PointFileModule.cs:16
- Model Space gate @ PointFileModule.cs:22-26
- ImportPointsWindow.Prompt @ PointFileModule.cs:31 / UI/DiemDo/ImportPointsWindow.xaml.cs:31-35
- OpenFileDialog filter *.csv;*.txt @ ImportPointsWindow.xaml.cs:65-68
- PointFileReader.Read(path) @ ImportPointsWindow.xaml.cs:76
- PointDrawService.Draw @ PointFileModule.cs:47-48
- CAD axes: ToCad Y→X, X→Y @ PointDrawService.cs:67-68
- Entities: DBPoint + DBText on layers DiemDo/DiemDoTen/DiemDoCaoDo @ PointDrawService.cs:35-57
- XData AppName ISURVEY_DIEM @ PointDrawService.cs:12,79+

### 4. Input / Output / lỗi / rủi ro
- **Input:** File CSV/TXT tọa độ (OpenFileDialog); cột gồm tên/X/Y/Z (UI DataGrid Cao độ Z). Điều kiện: đã đăng nhập; Model Space.
- **Output / hậu điều kiện:** Số điểm created (made); layer DiemDo*; Regen; ActivityLogger.Result.
- **Nhánh lỗi:** Dialog fail catch :34-36; hủy request null :39-42; Draw catch :56-58; PDMODE/PDSIZE catch nuốt @ PointDrawService.cs:26-28.
- **Destructive / quyền:** Không xóa đối tượng cũ — chỉ thêm. ZoomExtents sau khi made>0.
- **Giới hạn đọc:** Ưu tiên đã đọc thủ công các mắt xích chính.

### 5. Fixture đề xuất
- WORK COPY bản vẽ trắng Metric; MASTER file CSV mẫu N,E,Z; REFERENCE đếm DBPoint + so Z text.

### 6. Video / bằng chứng
- **video_candidate:** YES (tham khảo — không phải sẵn sàng sản xuất)
- **muc_ket_luan_source:** `SERVICE_CHAIN_VERIFIED_STATIC`
- **runtime:** `NOT_TESTED`
- **Bằng chứng thủ công độc lập:** CÓ (độc lập script)

---

## AUD-013 — XUATDIEM

### 1. Định danh
- **feature_id:** `AUD-013`
- **Hành vi:** XUATDIEM
- **Nhóm:** DIEM | **Audience:** USER
- **source_status:** `SOURCE_PRESENT` — có CommandMethod + thân xử lý trong source (runtime NOT_TESTED)
- **R01 cmds:** XUATDIEM
- **R02 refs:** AUD-014

### 2. Kích hoạt
- **Lệnh chính:** `XUATDIEM` @ `src/Modules/DiemDo/PointFileModule.cs:62`
- **Alias module (ngoài CommandAliases.cs):** (không)
- **Alias CommandAliases.cs:** XD
- **Effective method:** `ExportPointsCommand` span `63-91`
- **Ribbon:** Xuất điểm đo|XUATDIEM|en=True|L380
- **Palette:** XUATDIEM|src/UI/Palette/PaletteViewModel.cs:L119
- **XAML mapped:** (không / UI dialog nội bộ — xem 03D)

### 3. Chuỗi xử lý
- CommandMethod XUATDIEM @ PointFileModule.cs:62 → ExportPointsCommand
- EnsureAuthenticated @ :65
- ExportPointsWindow.Prompt @ :75
- SaveFileDialog CSV/TXT @ ExportPointsWindow.xaml.cs:100-110
- PointFileWriter.Write @ ExportPointsWindow.xaml.cs:110
- Lọc theo layer tùy chọn @ ExportPointsWindow.xaml.cs:66-67

### 4. Input / Output / lỗi / rủi ro
- **Input:** Điểm trên bản vẽ (layer tùy chọn); định dạng xuất .csv/.txt.
- **Output / hậu điều kiện:** File trên đĩa; message số điểm; ActivityLogger.Result @ PointFileModule.cs:89-90.
- **Nhánh lỗi:** Dialog catch :76-79; hủy null :83-86.
- **Destructive / quyền:** Không xóa CAD; ghi đè file nếu user chọn trùng tên (Win dialog).
- **Giới hạn đọc:** Ưu tiên đã đọc thủ công các mắt xích chính.

### 5. Fixture đề xuất
- WORK COPY có điểm DiemDo; REFERENCE so file CSV ngoài CAD.

### 6. Video / bằng chứng
- **video_candidate:** YES (tham khảo — không phải sẵn sàng sản xuất)
- **muc_ket_luan_source:** `SERVICE_CHAIN_VERIFIED_STATIC`
- **runtime:** `NOT_TESTED`
- **Bằng chứng thủ công độc lập:** CÓ (độc lập script)

---
