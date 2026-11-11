# Nhóm DGN

Sinh 2026-11-10 — R02B.

## AUD-010 — ISURVEY_CAD_TO_DGN

### 1. Định danh
- **feature_id:** `AUD-010`
- **Hành vi:** ISURVEY_CAD_TO_DGN
- **Nhóm:** DGN | **Audience:** USER
- **source_status:** `SOURCE_PRESENT` — có CommandMethod + thân xử lý trong source (runtime NOT_TESTED)
- **R01 cmds:** ISURVEY_CAD_TO_DGN
- **R02 refs:** AUD-011

### 2. Kích hoạt
- **Lệnh chính:** `ISURVEY_CAD_TO_DGN` @ `src/Dgn/DgnModule.cs:87`
- **Alias module (ngoài CommandAliases.cs):** (không)
- **Alias CommandAliases.cs:** C2D
- **Effective method:** `CadToDgnCommand` span `88-146`
- **Ribbon:** Xuất CAD ➔ DGN|ISURVEY_CAD_TO_DGN|en=True|L164
- **Palette:** (không)
- **XAML mapped:** (không / UI dialog nội bộ — xem 03D)

### 3. Chuỗi xử lý
- ISURVEY_CAD_TO_DGN @ src/Dgn/DgnModule.cs:87 method `CadToDgnCommand` span 88-146
- types trong handler: ActivityLogger;AuthManager;DgnExportDialog;DgnExportService;DocumentManager;MapModule;TaskProgressWindow
- merge: (không)
- service scan: DgnExportDialog@src/Dgn/DgnExportDialog.cs: đã mở src/Dgn/DgnExportDialog.cs (16 dòng), quét 551 ký tự đầu; io=[]; catch~0
- service scan: DgnExportService@src/Dgn/DgnExportService.cs: đã mở src/Dgn/DgnExportService.cs (697 dòng), quét 8000 ký tự đầu; io=['Directory.Exists', 'File.Delete', 'File.Exists']; catch~2

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

## AUD-011 — ISURVEY_DGN_TO_CAD

### 1. Định danh
- **feature_id:** `AUD-011`
- **Hành vi:** ISURVEY_DGN_TO_CAD
- **Nhóm:** DGN | **Audience:** USER
- **source_status:** `SOURCE_PRESENT` — có CommandMethod + thân xử lý trong source (runtime NOT_TESTED)
- **R01 cmds:** ISURVEY_DGN_TO_CAD
- **R02 refs:** AUD-012

### 2. Kích hoạt
- **Lệnh chính:** `ISURVEY_DGN_TO_CAD` @ `src/Dgn/DgnModule.cs:21`
- **Alias module (ngoài CommandAliases.cs):** (không)
- **Alias CommandAliases.cs:** D2C
- **Effective method:** `DgnToCadCommand` span `22-84`
- **Ribbon:** Nhập DGN ➔ CAD|ISURVEY_DGN_TO_CAD|en=True|L162
- **Palette:** (không)
- **XAML mapped:** (không / UI dialog nội bộ — xem 03D)

### 3. Chuỗi xử lý
- CommandMethod ISURVEY_DGN_TO_CAD @ src/Dgn/DgnModule.cs:21 → DgnToCadCommand
- EnsureAuthenticated(track:false) @ :24
- Model Space @ :32-35
- DgnImportDialog.ShowImport @ :44
- Batch → DgnBatchService.Start @ :66-70
- Single → DgnImportService.Start @ :79 (async Idle)
- Engine -DGNIMPORT FILEDIA=0; Handseed filter; UNDO; explode/rename/TCVN3 @ DgnImportService.cs:45-48, class từ :50
- Timeout/quiet windows @ DgnImportService.cs:59-62

### 4. Input / Output / lỗi / rủi ro
- **Input:** File .dgn V7/V8; options dialog; có thể batch nhiều file.
- **Output / hậu điều kiện:** Entities trong Model (hoặc Drawing mới nếu DGNIMPORTMODE=0); report ImportedEntities/Warnings/LogPath @ DgnImportReport :12-42.
- **Nhánh lỗi:** Dialog catch :46-49; hủy :53-57; report.Error; timeout 300s.
- **Destructive / quyền:** Có thể explode cell, purge block/linetype/layer, rename layer — side effects trên DWG đích.
- **Giới hạn đọc:** Thân DgnImportService >1000 dòng — đã đọc đầu file + chữ ký Start/Job; nhiều nhánh Idle/Finish CHƯA ĐỦ BẰNG CHỨNG từng dòng.

### 5. Fixture đề xuất
- WORK COPY; MASTER DGN seed độc lập; REFERENCE không lấy output iSurvey làm baseline.

### 6. Video / bằng chứng
- **video_candidate:** YES (tham khảo — không phải sẵn sàng sản xuất)
- **muc_ket_luan_source:** `SERVICE_CHAIN_VERIFIED_STATIC`
- **runtime:** `NOT_TESTED`
- **Bằng chứng thủ công độc lập:** CÓ (độc lập script)

---
