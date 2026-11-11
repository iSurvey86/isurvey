# Nhóm KML

Sinh 2026-11-10 — R02B.

## AUD-025 — ISURVEY_EXPORT_KML

### 1. Định danh
- **feature_id:** `AUD-025`
- **Hành vi:** ISURVEY_EXPORT_KML
- **Nhóm:** KML | **Audience:** USER
- **source_status:** `SOURCE_PRESENT` — có CommandMethod + thân xử lý trong source (runtime NOT_TESTED)
- **R01 cmds:** ISURVEY_EXPORT_KML
- **R02 refs:** AUD-026

### 2. Kích hoạt
- **Lệnh chính:** `ISURVEY_EXPORT_KML` @ `src/Modules/Export/ExportModule.cs:22`
- **Alias module (ngoài CommandAliases.cs):** (không)
- **Alias CommandAliases.cs:** EG;EXK
- **Effective method:** `ExportKmlCommand` span `23-125`
- **Ribbon:** Xuất File KML/KMZ|ISURVEY_EXPORT_KML|en=True|L151
- **Palette:** (không)
- **XAML mapped:** (không / UI dialog nội bộ — xem 03D)

### 3. Chuỗi xử lý
- ISURVEY_EXPORT_KML @ src/Modules/Export/ExportModule.cs:22 method `ExportKmlCommand` span 23-125
- types trong handler: ActivityLogger;AuthManager;DocumentManager;KmlExportService;MapModule;Vn2000SetupDialog
- merge: (không)
- service scan: KmlExportService@src/Modules/Export/KmlExportService.cs: đã mở src/Modules/Export/KmlExportService.cs (104 dòng), quét 3225 ký tự đầu; io=['StartTransaction']; catch~0
- service scan: Vn2000SetupDialog@src/UI/Vn2000/Vn2000SetupDialog.cs: đã mở src/UI/Vn2000/Vn2000SetupDialog.cs (17 dòng), quét 576 ký tự đầu; io=[]; catch~0

### 4. Input / Output / lỗi / rủi ro
- **Input:** CHƯA ĐỦ BẰNG CHỨNG đầy đủ — xem types_in_handler / service_trace CSV; cần đọc sâu khi quay.
- **Output / hậu điều kiện:** CHƯA ĐỦ BẰNG CHỨNG đầy đủ hậu điều kiện đo được.
- **Nhánh lỗi:** CHƯA ĐỦ BẰNG CHỨNG đủ mọi catch trong service layer.
- **Destructive / quyền:** CHƯA ĐỦ BẰNG CHỨNG — kiểm tra Delete/Erase/Purge trước pilot.
- **Giới hạn đọc:** Script đã quét handler + mở file type nếu tìm thấy; chưa đọc 100% service.

### 5. Fixture đề xuất
- WORK COPY Metric trắng; REFERENCE tùy feature — seed chưa có trong _R02B.

### 6. Video / bằng chứng
- **video_candidate:** YES (tham khảo — không phải sẵn sàng sản xuất)
- **muc_ket_luan_source:** `SERVICE_CHAIN_VERIFIED_STATIC`
- **runtime:** `NOT_TESTED`
- **Bằng chứng thủ công độc lập:** KHÔNG — chủ yếu script; một phần file:line từ extractor

---

## AUD-026 — ISURVEY_IMPORT_KML

### 1. Định danh
- **feature_id:** `AUD-026`
- **Hành vi:** ISURVEY_IMPORT_KML
- **Nhóm:** KML | **Audience:** USER
- **source_status:** `SOURCE_PRESENT` — có CommandMethod + thân xử lý trong source (runtime NOT_TESTED)
- **R01 cmds:** ISURVEY_IMPORT_KML
- **R02 refs:** AUD-027

### 2. Kích hoạt
- **Lệnh chính:** `ISURVEY_IMPORT_KML` @ `src/Modules/Import/KmlImportModule.cs:21`
- **Alias module (ngoài CommandAliases.cs):** (không)
- **Alias CommandAliases.cs:** INK
- **Effective method:** `ImportKmlCommand` span `22-130`
- **Ribbon:** Nhập File KML/KMZ|ISURVEY_IMPORT_KML|en=True|L150
- **Palette:** (không)
- **XAML mapped:** (không / UI dialog nội bộ — xem 03D)

### 3. Chuỗi xử lý
- ISURVEY_IMPORT_KML @ src/Modules/Import/KmlImportModule.cs:21 method `ImportKmlCommand` span 22-130
- types trong handler: AcadWindow;ActivityLogger;AuthManager;CoordinateService;DocumentManager;KmlDrawService;MainWindow;MapModule;ShowDialog;Vn2000SetupDialog;WinFormsDialog
- merge: (không)
- service scan: AcadWindow@?: CHƯA ĐỦ BẰNG CHỨNG: không tìm thấy file cho AcadWindow; io=[]; catch~0
- service scan: CoordinateService@src/Modules/Map/CoordinateService.cs: đã mở src/Modules/Map/CoordinateService.cs (234 dòng), quét 8000 ký tự đầu; io=['File.Exists', 'File.ReadAllText']; catch~0
- service scan: KmlDrawService@src/Modules/Import/KmlDrawService.cs: đã mở src/Modules/Import/KmlDrawService.cs (290 dòng), quét 8000 ký tự đầu; io=['StartTransaction']; catch~0

### 4. Input / Output / lỗi / rủi ro
- **Input:** CHƯA ĐỦ BẰNG CHỨNG đầy đủ — xem types_in_handler / service_trace CSV; cần đọc sâu khi quay.
- **Output / hậu điều kiện:** CHƯA ĐỦ BẰNG CHỨNG đầy đủ hậu điều kiện đo được.
- **Nhánh lỗi:** CHƯA ĐỦ BẰNG CHỨNG đủ mọi catch trong service layer.
- **Destructive / quyền:** CHƯA ĐỦ BẰNG CHỨNG — kiểm tra Delete/Erase/Purge trước pilot.
- **Giới hạn đọc:** Script đã quét handler + mở file type nếu tìm thấy; chưa đọc 100% service.

### 5. Fixture đề xuất
- WORK COPY Metric trắng; REFERENCE tùy feature — seed chưa có trong _R02B.

### 6. Video / bằng chứng
- **video_candidate:** YES (tham khảo — không phải sẵn sàng sản xuất)
- **muc_ket_luan_source:** `SERVICE_CHAIN_VERIFIED_STATIC`
- **runtime:** `NOT_TESTED`
- **Bằng chứng thủ công độc lập:** KHÔNG — chủ yếu script; một phần file:line từ extractor

---
