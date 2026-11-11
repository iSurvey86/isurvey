# Nhóm DGN — hồ sơ từng feature_id

Sinh 2026-11-10 — phân tích tĩnh thân method (brace-match).

## AUD-011 — ISURVEY_CAD_TO_DGN

### 1. Định danh
- **feature_id:** `AUD-011`
- **Tên:** ISURVEY_CAD_TO_DGN
- **Nhóm:** DGN
- **Audience:** USER
- **Trạng thái:** `IMPLEMENTED` — CommandMethod + handler method found in source

### 2. Điểm kích hoạt
- **Lệnh chính:** `ISURVEY_CAD_TO_DGN` @ `src/Dgn/DgnModule.cs:87` method `CadToDgnCommand`
- **Alias:** C2D
- **Ribbon:** Xuất CAD ➔ DGN|ISURVEY_CAD_TO_DGN|en=True|L164
- **Palette Run:** (không thấy PaletteCommands.Run)
- **Thân method:** `src/Dgn/DgnModule.cs:88-146`

### 3. Chuỗi xử lý (tĩnh)
- CommandMethod → `CadToDgnCommand` → quan sát: có kiểm tra/auth: EnsureAuthenticated; UI/prompt: Prompt, WriteMessage; CAD tx: Database; kiểu/service gọi: ActivityLogger, AuthManager, DgnExportDialog, DgnExportService, DocumentManager, MapModule, TaskProgressWindow; catch (1): System.Exception ex
- **Service/Dialog types trong thân:** ActivityLogger, AuthManager, DgnExportDialog, DgnExportService, DocumentManager, MapModule, TaskProgressWindow
- **Giới hạn đọc:** Đã đọc thân command method; các service được gọi chưa được mở đầy đủ từng file trừ khi nằm trong cùng method.

### 4. Input / Output / CAD
- **UI/prompt:** Prompt, WriteMessage
- **I/O file/network (trực tiếp):** (không thấy File/Http dialog API trực tiếp trong thân — có thể nằm service)
- **Transaction CAD (trực tiếp):** Database
- **Auth gate:** EnsureAuthenticated
- **Đối tượng CAD / đơn vị / phiên bản phụ thuộc:** CHƯA ĐỦ BẰNG CHỨNG nếu không nằm trong thân command — cần đọc service layer khi quay video.
- **Hành động phá hủy:** CHƯA ĐỦ BẰNG CHỨNG đầy đủ; nếu thân có Delete/Erase sẽ nằm trong notes/services — kiểm tra lại trước pilot.

### 5. Nhánh lỗi
- **catch quan sát:** System.Exception ex

### 6. Fixture / đo độc lập (đề xuất tĩnh)
- **MASTER/WORK COPY:** bản vẽ trắng Metric WORK COPY; không đụng file khách.
- **REFERENCE:** đo độc lập ngoài CAD khi feature xuất file; nếu chỉ ghi DWG — REFERENCE bằng checklist đối tượng.
- **Thiếu bằng chứng fixture cụ thể:** ghi nhận cho `AUD-011` — chưa có seed audit riêng trong `_R02`.

### 7. Khả năng quay / rủi ro
- Modal/UI: có dấu hiệu modal/dialog
- Chuột/pick: có Prompt/Get*
- Async: không thấy trong thân
- Ứng viên video người dùng (cờ CSV): YES

### 8. Mức kết luận
- **Source:** `SOURCE_BEHAVIOR_REVIEWED`
- **Runtime:** `NOT_TESTED`
- **R01 refs:** ISURVEY_CAD_TO_DGN

---

## AUD-012 — ISURVEY_DGN_TO_CAD

### 1. Định danh
- **feature_id:** `AUD-012`
- **Tên:** ISURVEY_DGN_TO_CAD
- **Nhóm:** DGN
- **Audience:** USER
- **Trạng thái:** `IMPLEMENTED` — CommandMethod + handler method found in source

### 2. Điểm kích hoạt
- **Lệnh chính:** `ISURVEY_DGN_TO_CAD` @ `src/Dgn/DgnModule.cs:21` method `DgnToCadCommand`
- **Alias:** D2C
- **Ribbon:** Nhập DGN ➔ CAD|ISURVEY_DGN_TO_CAD|en=True|L162
- **Palette Run:** (không thấy PaletteCommands.Run)
- **Thân method:** `src/Dgn/DgnModule.cs:22-84`

### 3. Chuỗi xử lý (tĩnh)
- CommandMethod → `DgnToCadCommand` → quan sát: có kiểm tra/auth: EnsureAuthenticated; UI/prompt: WriteMessage; CAD tx: Database; kiểu/service gọi: ActivityLogger, AuthManager, DgnBatchService, DgnImportDialog, DgnImportService, DocumentManager, MapModule, TaskProgressWindow; catch (1): System.Exception ex
- **Service/Dialog types trong thân:** ActivityLogger, AuthManager, DgnBatchService, DgnImportDialog, DgnImportService, DocumentManager, MapModule, TaskProgressWindow
- **Giới hạn đọc:** Đã đọc thân command method; các service được gọi chưa được mở đầy đủ từng file trừ khi nằm trong cùng method.

### 4. Input / Output / CAD
- **UI/prompt:** WriteMessage
- **I/O file/network (trực tiếp):** (không thấy File/Http dialog API trực tiếp trong thân — có thể nằm service)
- **Transaction CAD (trực tiếp):** Database
- **Auth gate:** EnsureAuthenticated
- **Đối tượng CAD / đơn vị / phiên bản phụ thuộc:** CHƯA ĐỦ BẰNG CHỨNG nếu không nằm trong thân command — cần đọc service layer khi quay video.
- **Hành động phá hủy:** CHƯA ĐỦ BẰNG CHỨNG đầy đủ; nếu thân có Delete/Erase sẽ nằm trong notes/services — kiểm tra lại trước pilot.

### 5. Nhánh lỗi
- **catch quan sát:** System.Exception ex

### 6. Fixture / đo độc lập (đề xuất tĩnh)
- **MASTER/WORK COPY:** bản vẽ trắng Metric WORK COPY; không đụng file khách.
- **REFERENCE:** đo độc lập ngoài CAD khi feature xuất file; nếu chỉ ghi DWG — REFERENCE bằng checklist đối tượng.
- **Thiếu bằng chứng fixture cụ thể:** ghi nhận cho `AUD-012` — chưa có seed audit riêng trong `_R02`.

### 7. Khả năng quay / rủi ro
- Modal/UI: có dấu hiệu modal/dialog
- Chuột/pick: chưa thấy pick trong thân
- Async: không thấy trong thân
- Ứng viên video người dùng (cờ CSV): YES

### 8. Mức kết luận
- **Source:** `SOURCE_BEHAVIOR_REVIEWED`
- **Runtime:** `NOT_TESTED`
- **R01 refs:** ISURVEY_DGN_TO_CAD

---
