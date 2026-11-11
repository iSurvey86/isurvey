# Nhóm MAP — hồ sơ từng feature_id

Sinh 2026-11-10 — phân tích tĩnh thân method (brace-match).

## AUD-028 — ISURVEY_DELETE_GE

### 1. Định danh
- **feature_id:** `AUD-028`
- **Tên:** ISURVEY_DELETE_GE
- **Nhóm:** MAP
- **Audience:** USER
- **Trạng thái:** `IMPLEMENTED` — CommandMethod + handler method found in source

### 2. Điểm kích hoạt
- **Lệnh chính:** `ISURVEY_DELETE_GE` @ `src/Modules/Map/MapModule.cs:169` method `DeleteGeCommand`
- **Alias:** XG
- **Ribbon:** Xóa Ảnh Vệ Tinh|ISURVEY_DELETE_GE|en=True|L156
- **Palette Run:** (không thấy PaletteCommands.Run)
- **Thân method:** `src/Modules/Map/MapModule.cs:170-176`

### 3. Chuỗi xử lý (tĩnh)
- CommandMethod → `DeleteGeCommand` → quan sát: có kiểm tra/auth: EnsureAuthenticated; kiểu/service gọi: AuthManager, DocumentManager
- **Service/Dialog types trong thân:** AuthManager, DocumentManager
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
- **Thiếu bằng chứng fixture cụ thể:** ghi nhận cho `AUD-028` — chưa có seed audit riêng trong `_R02`.

### 7. Khả năng quay / rủi ro
- Modal/UI: chưa thấy modal trong thân
- Chuột/pick: chưa thấy pick trong thân
- Async: không thấy trong thân
- Ứng viên video người dùng (cờ CSV): YES

### 8. Mức kết luận
- **Source:** `SOURCE_BEHAVIOR_REVIEWED`
- **Runtime:** `NOT_TESTED`
- **R01 refs:** ISURVEY_DELETE_GE

---

## AUD-029 — ISURVEY_MAP

### 1. Định danh
- **feature_id:** `AUD-029`
- **Tên:** ISURVEY_MAP
- **Nhóm:** MAP
- **Audience:** USER
- **Trạng thái:** `IMPLEMENTED` — CommandMethod + handler method found in source

### 2. Điểm kích hoạt
- **Lệnh chính:** `ISURVEY_MAP` @ `src/Modules/Map/MapModule.cs:25` method `InsertMapCommand`
- **Alias:** IG
- **Ribbon:** (không / chưa gắn RibbonBuilder)
- **Palette Run:** (không thấy PaletteCommands.Run)
- **Thân method:** `src/Modules/Map/MapModule.cs:26-30`

### 3. Chuỗi xử lý (tĩnh)
- CommandMethod → `InsertMapCommand` → quan sát: có kiểm tra/auth: EnsureAuthenticated; kiểu/service gọi: AuthManager
- **Service/Dialog types trong thân:** AuthManager
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
- **Thiếu bằng chứng fixture cụ thể:** ghi nhận cho `AUD-029` — chưa có seed audit riêng trong `_R02`.

### 7. Khả năng quay / rủi ro
- Modal/UI: chưa thấy modal trong thân
- Chuột/pick: chưa thấy pick trong thân
- Async: không thấy trong thân
- Ứng viên video người dùng (cờ CSV): YES

### 8. Mức kết luận
- **Source:** `SOURCE_BEHAVIOR_REVIEWED`
- **Runtime:** `NOT_TESTED`
- **R01 refs:** ISURVEY_MAP

---

## AUD-030 — ISURVEY_MAP_SAT

### 1. Định danh
- **feature_id:** `AUD-030`
- **Tên:** ISURVEY_MAP_SAT
- **Nhóm:** MAP
- **Audience:** USER
- **Trạng thái:** `IMPLEMENTED` — CommandMethod + handler method found in source

### 2. Điểm kích hoạt
- **Lệnh chính:** `ISURVEY_MAP_SAT` @ `src/Modules/Map/MapModule.cs:32` method `InsertSatelliteCommand`
- **Alias:** SG
- **Ribbon:** Chèn Ảnh Vệ Tinh|ISURVEY_MAP_SAT|en=True|L154
- **Palette Run:** (không thấy PaletteCommands.Run)
- **Thân method:** `src/Modules/Map/MapModule.cs:33-64`

### 3. Chuỗi xử lý (tĩnh)
- CommandMethod → `InsertSatelliteCommand` → quan sát: có kiểm tra/auth: EnsureAuthenticated; UI/prompt: Editor., Prompt; CAD tx: Database; kiểu/service gọi: ActivityLogger, AuthManager, DocumentManager; stub/hint trong thân
- **Service/Dialog types trong thân:** ActivityLogger, AuthManager, DocumentManager
- **Giới hạn đọc:** Đã đọc thân command method; các service được gọi chưa được mở đầy đủ từng file trừ khi nằm trong cùng method.

### 4. Input / Output / CAD
- **UI/prompt:** Editor., Prompt
- **I/O file/network (trực tiếp):** (không thấy File/Http dialog API trực tiếp trong thân — có thể nằm service)
- **Transaction CAD (trực tiếp):** Database
- **Auth gate:** EnsureAuthenticated
- **Đối tượng CAD / đơn vị / phiên bản phụ thuộc:** CHƯA ĐỦ BẰNG CHỨNG nếu không nằm trong thân command — cần đọc service layer khi quay video.
- **Hành động phá hủy:** CHƯA ĐỦ BẰNG CHỨNG đầy đủ; nếu thân có Delete/Erase sẽ nằm trong notes/services — kiểm tra lại trước pilot.

### 5. Nhánh lỗi
- **catch quan sát:** CHƯA ĐỦ BẰNG CHỨNG / không có catch trong thân command (lỗi có thể bubble hoặc xử lý ngoài)

### 6. Fixture / đo độc lập (đề xuất tĩnh)
- **MASTER/WORK COPY:** bản vẽ trắng Metric WORK COPY; không đụng file khách.
- **REFERENCE:** đo độc lập ngoài CAD khi feature xuất file; nếu chỉ ghi DWG — REFERENCE bằng checklist đối tượng.
- **Thiếu bằng chứng fixture cụ thể:** ghi nhận cho `AUD-030` — chưa có seed audit riêng trong `_R02`.

### 7. Khả năng quay / rủi ro
- Modal/UI: chưa thấy modal trong thân
- Chuột/pick: có Prompt/Get*
- Async: không thấy trong thân
- Ứng viên video người dùng (cờ CSV): YES

### 8. Mức kết luận
- **Source:** `SOURCE_BEHAVIOR_REVIEWED`
- **Runtime:** `NOT_TESTED`
- **R01 refs:** ISURVEY_MAP_SAT

---

## AUD-031 — ISURVEY_STRIP_MAP

### 1. Định danh
- **feature_id:** `AUD-031`
- **Tên:** ISURVEY_STRIP_MAP
- **Nhóm:** MAP
- **Audience:** USER
- **Trạng thái:** `IMPLEMENTED` — CommandMethod + handler method found in source

### 2. Điểm kích hoạt
- **Lệnh chính:** `ISURVEY_STRIP_MAP` @ `src/Modules/Tuyen/StripMapService.cs:27` method `StripMapCommand`
- **Alias:** DUOITUYEN
- **Ribbon:** Duỗi Thẳng Tuyến|ISURVEY_STRIP_MAP|en=True|L200
- **Palette Run:** (không thấy PaletteCommands.Run)
- **Thân method:** `src/Modules/Tuyen/StripMapService.cs:28-49`

### 3. Chuỗi xử lý (tĩnh)
- CommandMethod → `StripMapCommand` → quan sát: có kiểm tra/auth: EnsureAuthenticated; UI/prompt: WriteMessage; CAD tx: Database; kiểu/service gọi: ActivityLogger, AuthManager, DocumentManager; catch (1): System.Exception ex
- **Service/Dialog types trong thân:** ActivityLogger, AuthManager, DocumentManager
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
- **Thiếu bằng chứng fixture cụ thể:** ghi nhận cho `AUD-031` — chưa có seed audit riêng trong `_R02`.

### 7. Khả năng quay / rủi ro
- Modal/UI: chưa thấy modal trong thân
- Chuột/pick: chưa thấy pick trong thân
- Async: không thấy trong thân
- Ứng viên video người dùng (cờ CSV): YES

### 8. Mức kết luận
- **Source:** `SOURCE_BEHAVIOR_REVIEWED`
- **Runtime:** `NOT_TESTED`
- **R01 refs:** ISURVEY_STRIP_MAP

---
