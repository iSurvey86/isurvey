# Nhóm AI — hồ sơ từng feature_id

Sinh 2026-11-10 — phân tích tĩnh thân method (brace-match).

## AUD-001 — ISURVEY_AI_ASSIST

### 1. Định danh
- **feature_id:** `AUD-001`
- **Tên:** ISURVEY_AI_ASSIST
- **Nhóm:** AI
- **Audience:** ADMIN
- **Trạng thái:** `IMPLEMENTED` — CommandMethod + handler method found in source

### 2. Điểm kích hoạt
- **Lệnh chính:** `ISURVEY_AI_ASSIST` @ `src/Modules/Ai/AiAssistantModule.cs:13` method `AssistCommand`
- **Alias:** AIA
- **Ribbon:** Trợ Lý AI|ISURVEY_AI_ASSIST|en=True|L284
- **Palette Run:** (không thấy PaletteCommands.Run)
- **Thân method:** `src/Modules/Ai/AiAssistantModule.cs:14-24`

### 3. Chuỗi xử lý (tĩnh)
- CommandMethod → `AssistCommand` → quan sát: kiểu/service gọi: AiChatWindow, AuthManager, DocumentManager
- **Service/Dialog types trong thân:** AiChatWindow, AuthManager, DocumentManager
- **Giới hạn đọc:** Đã đọc thân command method; các service được gọi chưa được mở đầy đủ từng file trừ khi nằm trong cùng method.

### 4. Input / Output / CAD
- **UI/prompt:** (không thấy ShowModal/Prompt/WriteMessage trong thân đã đọc)
- **I/O file/network (trực tiếp):** (không thấy File/Http dialog API trực tiếp trong thân — có thể nằm service)
- **Transaction CAD (trực tiếp):** (không thấy Transaction trực tiếp trong thân)
- **Auth gate:** (không thấy EnsureAuthenticated trong thân)
- **Đối tượng CAD / đơn vị / phiên bản phụ thuộc:** CHƯA ĐỦ BẰNG CHỨNG nếu không nằm trong thân command — cần đọc service layer khi quay video.
- **Hành động phá hủy:** CHƯA ĐỦ BẰNG CHỨNG đầy đủ; nếu thân có Delete/Erase sẽ nằm trong notes/services — kiểm tra lại trước pilot.

### 5. Nhánh lỗi
- **catch quan sát:** CHƯA ĐỦ BẰNG CHỨNG / không có catch trong thân command (lỗi có thể bubble hoặc xử lý ngoài)

### 6. Fixture / đo độc lập (đề xuất tĩnh)
- **MASTER/WORK COPY:** bản vẽ trắng Metric WORK COPY; không đụng file khách.
- **REFERENCE:** đo độc lập ngoài CAD khi feature xuất file; nếu chỉ ghi DWG — REFERENCE bằng checklist đối tượng.
- **Thiếu bằng chứng fixture cụ thể:** ghi nhận cho `AUD-001` — chưa có seed audit riêng trong `_R02`.

### 7. Khả năng quay / rủi ro
- Modal/UI: chưa thấy modal trong thân
- Chuột/pick: chưa thấy pick trong thân
- Async: không thấy trong thân
- Ứng viên video người dùng (cờ CSV): NO

### 8. Mức kết luận
- **Source:** `SOURCE_BEHAVIOR_REVIEWED`
- **Runtime:** `NOT_TESTED`
- **R01 refs:** ISURVEY_AI_ASSIST

---

## AUD-002 — ISURVEY_AI_SETTINGS

### 1. Định danh
- **feature_id:** `AUD-002`
- **Tên:** ISURVEY_AI_SETTINGS
- **Nhóm:** AI
- **Audience:** ADMIN
- **Trạng thái:** `IMPLEMENTED` — CommandMethod + handler method found in source

### 2. Điểm kích hoạt
- **Lệnh chính:** `ISURVEY_AI_SETTINGS` @ `src/Modules/Ai/AiAssistantModule.cs:26` method `SettingsCommand`
- **Alias:** AIC
- **Ribbon:** Cấu Hình AI|ISURVEY_AI_SETTINGS|en=True|L288
- **Palette Run:** (không thấy PaletteCommands.Run)
- **Thân method:** `src/Modules/Ai/AiAssistantModule.cs:27-58`

### 3. Chuỗi xử lý (tĩnh)
- CommandMethod → `SettingsCommand` → quan sát: kiểu/service gọi: AuthManager, DarkDialog; stub/hint trong thân
- **Service/Dialog types trong thân:** AuthManager, DarkDialog
- **Giới hạn đọc:** Đã đọc thân command method; các service được gọi chưa được mở đầy đủ từng file trừ khi nằm trong cùng method.

### 4. Input / Output / CAD
- **UI/prompt:** (không thấy ShowModal/Prompt/WriteMessage trong thân đã đọc)
- **I/O file/network (trực tiếp):** (không thấy File/Http dialog API trực tiếp trong thân — có thể nằm service)
- **Transaction CAD (trực tiếp):** (không thấy Transaction trực tiếp trong thân)
- **Auth gate:** (không thấy EnsureAuthenticated trong thân)
- **Đối tượng CAD / đơn vị / phiên bản phụ thuộc:** CHƯA ĐỦ BẰNG CHỨNG nếu không nằm trong thân command — cần đọc service layer khi quay video.
- **Hành động phá hủy:** CHƯA ĐỦ BẰNG CHỨNG đầy đủ; nếu thân có Delete/Erase sẽ nằm trong notes/services — kiểm tra lại trước pilot.

### 5. Nhánh lỗi
- **catch quan sát:** CHƯA ĐỦ BẰNG CHỨNG / không có catch trong thân command (lỗi có thể bubble hoặc xử lý ngoài)

### 6. Fixture / đo độc lập (đề xuất tĩnh)
- **MASTER/WORK COPY:** bản vẽ trắng Metric WORK COPY; không đụng file khách.
- **REFERENCE:** đo độc lập ngoài CAD khi feature xuất file; nếu chỉ ghi DWG — REFERENCE bằng checklist đối tượng.
- **Thiếu bằng chứng fixture cụ thể:** ghi nhận cho `AUD-002` — chưa có seed audit riêng trong `_R02`.

### 7. Khả năng quay / rủi ro
- Modal/UI: có dấu hiệu modal/dialog
- Chuột/pick: chưa thấy pick trong thân
- Async: không thấy trong thân
- Ứng viên video người dùng (cờ CSV): NO

### 8. Mức kết luận
- **Source:** `SOURCE_BEHAVIOR_REVIEWED`
- **Runtime:** `NOT_TESTED`
- **R01 refs:** ISURVEY_AI_SETTINGS

---
