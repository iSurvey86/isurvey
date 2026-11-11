# Nhóm PALETTE — hồ sơ từng feature_id

Sinh 2026-11-10 — phân tích tĩnh thân method (brace-match).

## AUD-035 — ISURVEY_PALETTE

### 1. Định danh
- **feature_id:** `AUD-035`
- **Tên:** ISURVEY_PALETTE
- **Nhóm:** PALETTE
- **Audience:** USER
- **Trạng thái:** `IMPLEMENTED` — CommandMethod + handler method found in source

### 2. Điểm kích hoạt
- **Lệnh chính:** `ISURVEY_PALETTE` @ `src/UI/Palette/PaletteModule.cs:9` method `ShowCommand`
- **Alias:** BANGIS
- **Ribbon:** Bảng Dữ liệu|ISURVEY_PALETTE|en=True|L142
- **Palette Run:** (không thấy PaletteCommands.Run)
- **Thân method:** `src/UI/Palette/PaletteModule.cs:10-15`

### 3. Chuỗi xử lý (tĩnh)
- CommandMethod → `ShowCommand` → quan sát: có kiểm tra/auth: EnsureAuthenticated; kiểu/service gọi: AuthManager, SurveyPaletteManager
- **Service/Dialog types trong thân:** AuthManager, SurveyPaletteManager
- **Giới hạn đọc:** Đã đọc thân command method; các service được gọi chưa được mở đầy đủ từng file trừ khi nằm trong cùng method.

### 4. Input / Output / CAD
- **UI/prompt:** (không thấy ShowModal/Prompt/WriteMessage trong thân đã đọc)
- **I/O file/network (trực tiếp):** (không thấy File/Http dialog API trực tiếp trong thân — có thể nằm service)
- **Transaction CAD (trực tiếp):** (không thấy Transaction trực tiếp trong thân)
- **Auth gate:** EnsureAuthenticated
- **Đối tượng CAD / đơn vị / phiên bản phụ thuộc:** CHƯA ĐỦ BẰNG CHỨNG nếu không nằm trong thân command — cần đọc service layer khi quay video.
- **Hành động phá hủy:** CHƯA ĐỦ BẰNG CHỨNG đầy đủ; nếu thân có Delete/Erase sẽ nằm trong notes/services — kiểm tra lại trước pilot.

### 5. Nhánh lỗi
- **catch quan sát:** CHƯA ĐỦ BẰNG CHỨNG / không có catch trong thân command (lỗi có thể bubble hoặc xử lý ngoài)

### 6. Fixture / đo độc lập (đề xuất tĩnh)
- **MASTER/WORK COPY:** bản vẽ trắng Metric WORK COPY; không đụng file khách.
- **REFERENCE:** đo độc lập ngoài CAD khi feature xuất file; nếu chỉ ghi DWG — REFERENCE bằng checklist đối tượng.
- **Thiếu bằng chứng fixture cụ thể:** ghi nhận cho `AUD-035` — chưa có seed audit riêng trong `_R02`.

### 7. Khả năng quay / rủi ro
- Modal/UI: chưa thấy modal trong thân
- Chuột/pick: chưa thấy pick trong thân
- Async: không thấy trong thân
- Ứng viên video người dùng (cờ CSV): YES

### 8. Mức kết luận
- **Source:** `SOURCE_BEHAVIOR_REVIEWED`
- **Runtime:** `NOT_TESTED`
- **R01 refs:** ISURVEY_PALETTE

---
