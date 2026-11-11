# Nhóm TAB — hồ sơ từng feature_id

Sinh 2026-11-10 — phân tích tĩnh thân method (brace-match).

## AUD-055 — ISURVEY_TAB_TO_CAD

### 1. Định danh
- **feature_id:** `AUD-055`
- **Tên:** ISURVEY_TAB_TO_CAD
- **Nhóm:** TAB
- **Audience:** USER
- **Trạng thái:** `IMPLEMENTED` — CommandMethod + handler method found in source

### 2. Điểm kích hoạt
- **Lệnh chính:** `ISURVEY_TAB_TO_CAD` @ `src/Modules/Import/TabImportModule.cs:29` method `TabToCadCommand`
- **Alias:** T2C
- **Ribbon:** Nhập TAB ➔ CAD|ISURVEY_TAB_TO_CAD|en=True|L160
- **Palette Run:** (không thấy PaletteCommands.Run)
- **Thân method:** `src/Modules/Import/TabImportModule.cs:30-82`

### 3. Chuỗi xử lý (tĩnh)
- CommandMethod → `TabToCadCommand` → quan sát: có kiểm tra/auth: EnsureAuthenticated; UI/prompt: WriteMessage; CAD tx: Database; kiểu/service gọi: ActivityLogger, AuthManager, DocumentManager, MapModule, ShowAlertDialog, TabImportWindow, UserSettingsStore; catch (2): System.Exception ex; System.Exception ex
- **Service/Dialog types trong thân:** ActivityLogger, AuthManager, DocumentManager, MapModule, ShowAlertDialog, TabImportWindow, UserSettingsStore
- **Giới hạn đọc:** Đã đọc thân command method; các service được gọi chưa được mở đầy đủ từng file trừ khi nằm trong cùng method.

### 4. Input / Output / CAD
- **UI/prompt:** WriteMessage
- **I/O file/network (trực tiếp):** (không thấy File/Http dialog API trực tiếp trong thân — có thể nằm service)
- **Transaction CAD (trực tiếp):** Database
- **Auth gate:** EnsureAuthenticated
- **Đối tượng CAD / đơn vị / phiên bản phụ thuộc:** CHƯA ĐỦ BẰNG CHỨNG nếu không nằm trong thân command — cần đọc service layer khi quay video.
- **Hành động phá hủy:** CHƯA ĐỦ BẰNG CHỨNG đầy đủ; nếu thân có Delete/Erase sẽ nằm trong notes/services — kiểm tra lại trước pilot.

### 5. Nhánh lỗi
- **catch quan sát:** System.Exception ex; System.Exception ex

### 6. Fixture / đo độc lập (đề xuất tĩnh)
- **MASTER/WORK COPY:** bản vẽ trắng Metric WORK COPY; không đụng file khách.
- **REFERENCE:** đo độc lập ngoài CAD khi feature xuất file; nếu chỉ ghi DWG — REFERENCE bằng checklist đối tượng.
- **Thiếu bằng chứng fixture cụ thể:** ghi nhận cho `AUD-055` — chưa có seed audit riêng trong `_R02`.

### 7. Khả năng quay / rủi ro
- Modal/UI: có dấu hiệu modal/dialog
- Chuột/pick: chưa thấy pick trong thân
- Async: không thấy trong thân
- Ứng viên video người dùng (cờ CSV): YES

### 8. Mức kết luận
- **Source:** `SOURCE_BEHAVIOR_REVIEWED`
- **Runtime:** `NOT_TESTED`
- **R01 refs:** ISURVEY_TAB_TO_CAD

---
