# Nhóm PLAN — hồ sơ từng feature_id

Sinh 2026-11-10 — phân tích tĩnh thân method (brace-match).

## AUD-036 — CATBD

### 1. Định danh
- **feature_id:** `AUD-036`
- **Tên:** CATBD
- **Nhóm:** PLAN
- **Audience:** USER
- **Trạng thái:** `IMPLEMENTED` — CommandMethod + handler method found in source

### 2. Điểm kích hoạt
- **Lệnh chính:** `CATBD` @ `src/Modules/Plan/PlanModule.cs:46` method `CutPlanCommand`
- **Alias:** CBD
- **Ribbon:** Cắt Bình Đồ|CATBD|en=True|L179
- **Palette Run:** (không thấy PaletteCommands.Run)
- **Thân method:** `src/Modules/Plan/PlanModule.cs:47-112`

### 3. Chuỗi xử lý (tĩnh)
- CommandMethod → `CutPlanCommand` → quan sát: có kiểm tra/auth: EnsureAuthenticated; UI/prompt: GetEntity, GetSelection, Prompt, WriteMessage; CAD tx: Database; kiểu/service gọi: ActivityLogger, AuthManager, DocumentManager, MapModule; catch (1): System.Exception ex
- **Service/Dialog types trong thân:** ActivityLogger, AuthManager, DocumentManager, MapModule
- **Giới hạn đọc:** Đã đọc thân command method; các service được gọi chưa được mở đầy đủ từng file trừ khi nằm trong cùng method.

### 4. Input / Output / CAD
- **UI/prompt:** GetEntity, GetSelection, Prompt, WriteMessage
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
- **Thiếu bằng chứng fixture cụ thể:** ghi nhận cho `AUD-036` — chưa có seed audit riêng trong `_R02`.

### 7. Khả năng quay / rủi ro
- Modal/UI: chưa thấy modal trong thân
- Chuột/pick: có Prompt/Get*
- Async: không thấy trong thân
- Ứng viên video người dùng (cờ CSV): YES

### 8. Mức kết luận
- **Source:** `SOURCE_BEHAVIOR_REVIEWED`
- **Runtime:** `NOT_TESTED`
- **R01 refs:** CATBD

---
