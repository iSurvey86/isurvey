# Nhóm PROFILE — hồ sơ từng feature_id

Sinh 2026-11-10 — phân tích tĩnh thân method (brace-match).

## AUD-037 — ISURVEY_PROFILE_DRAW

### 1. Định danh
- **feature_id:** `AUD-037`
- **Tên:** ISURVEY_PROFILE_DRAW
- **Nhóm:** PROFILE
- **Audience:** USER
- **Trạng thái:** `IMPLEMENTED` — CommandMethod + handler method found in source

### 2. Điểm kích hoạt
- **Lệnh chính:** `ISURVEY_PROFILE_DRAW` @ `src/Modules/Profile/ProfileModule.cs:14` method `DrawCommand`
- **Alias:** (không)
- **Ribbon:** (không / chưa gắn RibbonBuilder)
- **Palette Run:** (không thấy PaletteCommands.Run)
- **Thân method:** `src/Modules/Profile/ProfileModule.cs:15-15`

### 3. Chuỗi xử lý (tĩnh)
- CommandMethod → `DrawCommand` → quan sát: thin-wrapper → Draw @ src/Modules/Profile/ProfileModule.cs:20-147; có kiểm tra/auth: EnsureAuthenticated; UI/prompt: GetPoint, Prompt, WriteMessage; CAD tx: Database; kiểu/service gọi: ActivityLogger, AuthManager, DocumentManager, DrawProfileWindow, ProfileDrawService, ProfileSampleService, ProfileSettingsStore, RouteStore; catch (3): System.Exception ex; System.Exception ex; System.Exception ex
- **Service/Dialog types trong thân:** ActivityLogger, AuthManager, DocumentManager, DrawProfileWindow, ProfileDrawService, ProfileSampleService, ProfileSettingsStore, RouteStore
- **Giới hạn đọc:** Đã đọc thân command method; các service được gọi chưa được mở đầy đủ từng file trừ khi nằm trong cùng method.

### 4. Input / Output / CAD
- **UI/prompt:** GetPoint, Prompt, WriteMessage
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
- **Thiếu bằng chứng fixture cụ thể:** ghi nhận cho `AUD-037` — chưa có seed audit riêng trong `_R02`.

### 7. Khả năng quay / rủi ro
- Modal/UI: chưa thấy modal trong thân
- Chuột/pick: có Prompt/Get*
- Async: không thấy trong thân
- Ứng viên video người dùng (cờ CSV): YES

### 8. Mức kết luận
- **Source:** `SOURCE_BEHAVIOR_REVIEWED`
- **Runtime:** `NOT_TESTED`
- **R01 refs:** ISURVEY_PROFILE_DRAW

---

## AUD-038 — VEMCD

### 1. Định danh
- **feature_id:** `AUD-038`
- **Tên:** VEMCD
- **Nhóm:** PROFILE
- **Audience:** USER
- **Trạng thái:** `IMPLEMENTED` — CommandMethod + handler method found in source

### 2. Điểm kích hoạt
- **Lệnh chính:** `VEMCD` @ `src/Modules/Profile/ProfileModule.cs:17` method `DrawAlias`
- **Alias:** (không)
- **Ribbon:** Vẽ Mặt Cắt Dọc|VEMCD|en=True|L184
- **Palette Run:** VEMCD|src/UI/Palette/PaletteViewModel.cs:L280;VEMCD|src/UI/Palette/PaletteViewModel.cs:L287
- **Thân method:** `src/Modules/Profile/ProfileModule.cs:18-18`

### 3. Chuỗi xử lý (tĩnh)
- CommandMethod → `DrawAlias` → quan sát: thin-wrapper → Draw @ src/Modules/Profile/ProfileModule.cs:20-147; có kiểm tra/auth: EnsureAuthenticated; UI/prompt: GetPoint, Prompt, WriteMessage; CAD tx: Database; kiểu/service gọi: ActivityLogger, AuthManager, DocumentManager, DrawProfileWindow, ProfileDrawService, ProfileSampleService, ProfileSettingsStore, RouteStore; catch (3): System.Exception ex; System.Exception ex; System.Exception ex
- **Service/Dialog types trong thân:** ActivityLogger, AuthManager, DocumentManager, DrawProfileWindow, ProfileDrawService, ProfileSampleService, ProfileSettingsStore, RouteStore
- **Giới hạn đọc:** Đã đọc thân command method; các service được gọi chưa được mở đầy đủ từng file trừ khi nằm trong cùng method.

### 4. Input / Output / CAD
- **UI/prompt:** GetPoint, Prompt, WriteMessage
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
- **Thiếu bằng chứng fixture cụ thể:** ghi nhận cho `AUD-038` — chưa có seed audit riêng trong `_R02`.

### 7. Khả năng quay / rủi ro
- Modal/UI: chưa thấy modal trong thân
- Chuột/pick: có Prompt/Get*
- Async: không thấy trong thân
- Ứng viên video người dùng (cờ CSV): YES

### 8. Mức kết luận
- **Source:** `SOURCE_BEHAVIOR_REVIEWED`
- **Runtime:** `NOT_TESTED`
- **R01 refs:** VEMCD

---
