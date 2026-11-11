# Nhóm KML — hồ sơ từng feature_id

Sinh 2026-11-10 — phân tích tĩnh thân method (brace-match).

## AUD-026 — ISURVEY_EXPORT_KML

### 1. Định danh
- **feature_id:** `AUD-026`
- **Tên:** ISURVEY_EXPORT_KML
- **Nhóm:** KML
- **Audience:** USER
- **Trạng thái:** `IMPLEMENTED` — CommandMethod + handler method found in source

### 2. Điểm kích hoạt
- **Lệnh chính:** `ISURVEY_EXPORT_KML` @ `src/Modules/Export/ExportModule.cs:22` method `ExportKmlCommand`
- **Alias:** EG;EXK
- **Ribbon:** Xuất File KML/KMZ|ISURVEY_EXPORT_KML|en=True|L151
- **Palette Run:** (không thấy PaletteCommands.Run)
- **Thân method:** `src/Modules/Export/ExportModule.cs:23-125`

### 3. Chuỗi xử lý (tĩnh)
- CommandMethod → `ExportKmlCommand` → quan sát: có kiểm tra/auth: EnsureAuthenticated; UI/prompt: WriteMessage; CAD tx: Database; kiểu/service gọi: ActivityLogger, AuthManager, DocumentManager, KmlExportService, MapModule, Vn2000SetupDialog; catch (3): System.Exception ex; System.Exception ex; System.Exception ex
- **Service/Dialog types trong thân:** ActivityLogger, AuthManager, DocumentManager, KmlExportService, MapModule, Vn2000SetupDialog
- **Giới hạn đọc:** Đã đọc thân command method; các service được gọi chưa được mở đầy đủ từng file trừ khi nằm trong cùng method.

### 4. Input / Output / CAD
- **UI/prompt:** WriteMessage
- **I/O file/network (trực tiếp):** (không thấy File/Http dialog API trực tiếp trong thân — có thể nằm service)
- **Transaction CAD (trực tiếp):** Database
- **Auth gate:** EnsureAuthenticated
- **Đối tượng CAD / đơn vị / phiên bản phụ thuộc:** CHƯA ĐỦ BẰNG CHỨNG nếu không nằm trong thân command — cần đọc service layer khi quay video.
- **Hành động phá hủy:** CHƯA ĐỦ BẰNG CHỨNG đầy đủ; nếu thân có Delete/Erase sẽ nằm trong notes/services — kiểm tra lại trước pilot.

### 5. Nhánh lỗi
- **catch quan sát:** System.Exception ex; System.Exception ex; System.Exception ex

### 6. Fixture / đo độc lập (đề xuất tĩnh)
- **MASTER/WORK COPY:** bản vẽ trắng Metric WORK COPY; không đụng file khách.
- **REFERENCE:** đo độc lập ngoài CAD khi feature xuất file; nếu chỉ ghi DWG — REFERENCE bằng checklist đối tượng.
- **Thiếu bằng chứng fixture cụ thể:** ghi nhận cho `AUD-026` — chưa có seed audit riêng trong `_R02`.

### 7. Khả năng quay / rủi ro
- Modal/UI: có dấu hiệu modal/dialog
- Chuột/pick: chưa thấy pick trong thân
- Async: không thấy trong thân
- Ứng viên video người dùng (cờ CSV): YES

### 8. Mức kết luận
- **Source:** `SOURCE_BEHAVIOR_REVIEWED`
- **Runtime:** `NOT_TESTED`
- **R01 refs:** ISURVEY_EXPORT_KML

---

## AUD-027 — ISURVEY_IMPORT_KML

### 1. Định danh
- **feature_id:** `AUD-027`
- **Tên:** ISURVEY_IMPORT_KML
- **Nhóm:** KML
- **Audience:** USER
- **Trạng thái:** `IMPLEMENTED` — CommandMethod + handler method found in source

### 2. Điểm kích hoạt
- **Lệnh chính:** `ISURVEY_IMPORT_KML` @ `src/Modules/Import/KmlImportModule.cs:21` method `ImportKmlCommand`
- **Alias:** INK
- **Ribbon:** Nhập File KML/KMZ|ISURVEY_IMPORT_KML|en=True|L150
- **Palette Run:** (không thấy PaletteCommands.Run)
- **Thân method:** `src/Modules/Import/KmlImportModule.cs:22-130`

### 3. Chuỗi xử lý (tĩnh)
- CommandMethod → `ImportKmlCommand` → quan sát: có kiểm tra/auth: EnsureAuthenticated; UI/prompt: ShowDialog, WriteMessage; CAD tx: Database; kiểu/service gọi: AcadWindow, ActivityLogger, AuthManager, CoordinateService, DocumentManager, KmlDrawService, MainWindow, MapModule, ShowDialog, Vn2000SetupDialog, WinFormsDialog; catch (3): System.Exception ex; IOException ex; System.Exception ex
- **Service/Dialog types trong thân:** AcadWindow, ActivityLogger, AuthManager, CoordinateService, DocumentManager, KmlDrawService, MainWindow, MapModule, ShowDialog, Vn2000SetupDialog, WinFormsDialog
- **Giới hạn đọc:** Đã đọc thân command method; các service được gọi chưa được mở đầy đủ từng file trừ khi nằm trong cùng method.

### 4. Input / Output / CAD
- **UI/prompt:** ShowDialog, WriteMessage
- **I/O file/network (trực tiếp):** (không thấy File/Http dialog API trực tiếp trong thân — có thể nằm service)
- **Transaction CAD (trực tiếp):** Database
- **Auth gate:** EnsureAuthenticated
- **Đối tượng CAD / đơn vị / phiên bản phụ thuộc:** CHƯA ĐỦ BẰNG CHỨNG nếu không nằm trong thân command — cần đọc service layer khi quay video.
- **Hành động phá hủy:** CHƯA ĐỦ BẰNG CHỨNG đầy đủ; nếu thân có Delete/Erase sẽ nằm trong notes/services — kiểm tra lại trước pilot.

### 5. Nhánh lỗi
- **catch quan sát:** System.Exception ex; IOException ex; System.Exception ex

### 6. Fixture / đo độc lập (đề xuất tĩnh)
- **MASTER/WORK COPY:** bản vẽ trắng Metric WORK COPY; không đụng file khách.
- **REFERENCE:** đo độc lập ngoài CAD khi feature xuất file; nếu chỉ ghi DWG — REFERENCE bằng checklist đối tượng.
- **Thiếu bằng chứng fixture cụ thể:** ghi nhận cho `AUD-027` — chưa có seed audit riêng trong `_R02`.

### 7. Khả năng quay / rủi ro
- Modal/UI: có dấu hiệu modal/dialog
- Chuột/pick: chưa thấy pick trong thân
- Async: không thấy trong thân
- Ứng viên video người dùng (cờ CSV): YES

### 8. Mức kết luận
- **Source:** `SOURCE_BEHAVIOR_REVIEWED`
- **Runtime:** `NOT_TESTED`
- **R01 refs:** ISURVEY_IMPORT_KML

---
