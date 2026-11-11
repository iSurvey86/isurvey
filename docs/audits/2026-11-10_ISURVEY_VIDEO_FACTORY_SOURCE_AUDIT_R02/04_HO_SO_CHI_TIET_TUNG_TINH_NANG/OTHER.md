# Nhóm OTHER — hồ sơ từng feature_id

Sinh 2026-11-10 — phân tích tĩnh thân method (brace-match).

## AUD-032 — ISURVEY_PC_FILTER

### 1. Định danh
- **feature_id:** `AUD-032`
- **Tên:** ISURVEY_PC_FILTER
- **Nhóm:** OTHER
- **Audience:** USER
- **Trạng thái:** `STUB` — method ends Stub / stub hint in body

### 2. Điểm kích hoạt
- **Lệnh chính:** `ISURVEY_PC_FILTER` @ `src/Modules/Uav/UavModule.cs:52` method `PcFilterStub`
- **Alias:** LP
- **Ribbon:** Lọc Điểm Mặt Đất|ISURVEY_PC_FILTER|en=True|L447
- **Palette Run:** (không thấy PaletteCommands.Run)
- **Thân method:** `src/Modules/Uav/UavModule.cs:53-53`

### 3. Chuỗi xử lý (tĩnh)
- CommandMethod → `PcFilterStub` → quan sát: stub/hint trong thân; thin-wrapper → Stub @ src/Modules/Uav/UavModule.cs:58-62; có kiểm tra/auth: EnsureAuthenticated; kiểu/service gọi: AuthManager, ShowAlertDialog
- **Service/Dialog types trong thân:** AuthManager, ShowAlertDialog
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
- **Thiếu bằng chứng fixture cụ thể:** ghi nhận cho `AUD-032` — chưa có seed audit riêng trong `_R02`.

### 7. Khả năng quay / rủi ro
- Modal/UI: có dấu hiệu modal/dialog
- Chuột/pick: chưa thấy pick trong thân
- Async: không thấy trong thân
- Ứng viên video người dùng (cờ CSV): NO

### 8. Mức kết luận
- **Source:** `STUB_ONLY`
- **Runtime:** `NOT_TESTED`
- **R01 refs:** ISURVEY_PC_FILTER

---

## AUD-033 — ISURVEY_PC_GRID

### 1. Định danh
- **feature_id:** `AUD-033`
- **Tên:** ISURVEY_PC_GRID
- **Nhóm:** OTHER
- **Audience:** USER
- **Trạng thái:** `STUB` — method ends Stub / stub hint in body

### 2. Điểm kích hoạt
- **Lệnh chính:** `ISURVEY_PC_GRID` @ `src/Modules/Uav/UavModule.cs:55` method `PcGridStub`
- **Alias:** RL
- **Ribbon:** Rải Lưới Điểm|ISURVEY_PC_GRID|en=True|L449
- **Palette Run:** (không thấy PaletteCommands.Run)
- **Thân method:** `src/Modules/Uav/UavModule.cs:56-56`

### 3. Chuỗi xử lý (tĩnh)
- CommandMethod → `PcGridStub` → quan sát: stub/hint trong thân; thin-wrapper → Stub @ src/Modules/Uav/UavModule.cs:58-62; có kiểm tra/auth: EnsureAuthenticated; kiểu/service gọi: AuthManager, ShowAlertDialog
- **Service/Dialog types trong thân:** AuthManager, ShowAlertDialog
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
- **Thiếu bằng chứng fixture cụ thể:** ghi nhận cho `AUD-033` — chưa có seed audit riêng trong `_R02`.

### 7. Khả năng quay / rủi ro
- Modal/UI: có dấu hiệu modal/dialog
- Chuột/pick: chưa thấy pick trong thân
- Async: không thấy trong thân
- Ứng viên video người dùng (cờ CSV): NO

### 8. Mức kết luận
- **Source:** `STUB_ONLY`
- **Runtime:** `NOT_TESTED`
- **R01 refs:** ISURVEY_PC_GRID

---

## AUD-034 — ISURVEY_PC_IMPORT

### 1. Định danh
- **feature_id:** `AUD-034`
- **Tên:** ISURVEY_PC_IMPORT
- **Nhóm:** OTHER
- **Audience:** USER
- **Trạng thái:** `STUB` — method ends Stub / stub hint in body

### 2. Điểm kích hoạt
- **Lệnh chính:** `ISURVEY_PC_IMPORT` @ `src/Modules/Uav/UavModule.cs:49` method `PcImportStub`
- **Alias:** NP
- **Ribbon:** Nhập Point Cloud|ISURVEY_PC_IMPORT|en=True|L445
- **Palette Run:** (không thấy PaletteCommands.Run)
- **Thân method:** `src/Modules/Uav/UavModule.cs:50-50`

### 3. Chuỗi xử lý (tĩnh)
- CommandMethod → `PcImportStub` → quan sát: stub/hint trong thân; thin-wrapper → Stub @ src/Modules/Uav/UavModule.cs:58-62; có kiểm tra/auth: EnsureAuthenticated; kiểu/service gọi: AuthManager, ShowAlertDialog
- **Service/Dialog types trong thân:** AuthManager, ShowAlertDialog
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
- **Thiếu bằng chứng fixture cụ thể:** ghi nhận cho `AUD-034` — chưa có seed audit riêng trong `_R02`.

### 7. Khả năng quay / rủi ro
- Modal/UI: có dấu hiệu modal/dialog
- Chuột/pick: chưa thấy pick trong thân
- Async: không thấy trong thân
- Ứng viên video người dùng (cờ CSV): NO

### 8. Mức kết luận
- **Source:** `STUB_ONLY`
- **Runtime:** `NOT_TESTED`
- **R01 refs:** ISURVEY_PC_IMPORT

---
