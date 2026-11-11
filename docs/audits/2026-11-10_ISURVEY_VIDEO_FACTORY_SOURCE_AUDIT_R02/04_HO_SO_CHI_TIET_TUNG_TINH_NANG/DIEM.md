# Nhóm DIEM — hồ sơ từng feature_id

Sinh 2026-11-10 — phân tích tĩnh thân method (brace-match).

## AUD-013 — NHAPDIEM

### 1. Định danh
- **feature_id:** `AUD-013`
- **Tên:** NHAPDIEM
- **Nhóm:** DIEM
- **Audience:** USER
- **Trạng thái:** `IMPLEMENTED` — CommandMethod + handler method found in source

### 2. Điểm kích hoạt
- **Lệnh chính:** `NHAPDIEM` @ `src/Modules/DiemDo/PointFileModule.cs:13` method `ImportPointsCommand`
- **Alias:** ND
- **Ribbon:** Nhập điểm đo|NHAPDIEM|en=True|L379
- **Palette Run:** NHAPDIEM|src/UI/Palette/PaletteViewModel.cs:L118
- **Thân method:** `src/Modules/DiemDo/PointFileModule.cs:14-60`

### 3. Chuỗi xử lý (tĩnh)
- CommandMethod → `ImportPointsCommand` → quan sát: có kiểm tra/auth: EnsureAuthenticated; UI/prompt: Editor., Prompt; kiểu/service gọi: ActivityLogger, AuthManager, DocumentManager, ImportPointsWindow, PointDrawService; catch (2): System.Exception ex; System.Exception ex
- **Service/Dialog types trong thân:** ActivityLogger, AuthManager, DocumentManager, ImportPointsWindow, PointDrawService
- **Giới hạn đọc:** Đã đọc thân command method; các service được gọi chưa được mở đầy đủ từng file trừ khi nằm trong cùng method.

### 4. Input / Output / CAD
- **UI/prompt:** Editor., Prompt
- **I/O file/network (trực tiếp):** (không thấy File/Http dialog API trực tiếp trong thân — có thể nằm service)
- **Transaction CAD (trực tiếp):** (không thấy Transaction trực tiếp trong thân)
- **Auth gate:** EnsureAuthenticated
- **Đối tượng CAD / đơn vị / phiên bản phụ thuộc:** CHƯA ĐỦ BẰNG CHỨNG nếu không nằm trong thân command — cần đọc service layer khi quay video.
- **Hành động phá hủy:** CHƯA ĐỦ BẰNG CHỨNG đầy đủ; nếu thân có Delete/Erase sẽ nằm trong notes/services — kiểm tra lại trước pilot.

### 5. Nhánh lỗi
- **catch quan sát:** System.Exception ex; System.Exception ex

### 6. Fixture / đo độc lập (đề xuất tĩnh)
- **MASTER/WORK COPY:** bản vẽ trắng Metric WORK COPY; không đụng file khách.
- **REFERENCE:** đo độc lập ngoài CAD khi feature xuất file; nếu chỉ ghi DWG — REFERENCE bằng checklist đối tượng.
- **Thiếu bằng chứng fixture cụ thể:** ghi nhận cho `AUD-013` — chưa có seed audit riêng trong `_R02`.

### 7. Khả năng quay / rủi ro
- Modal/UI: chưa thấy modal trong thân
- Chuột/pick: có Prompt/Get*
- Async: không thấy trong thân
- Ứng viên video người dùng (cờ CSV): YES

### 8. Mức kết luận
- **Source:** `SOURCE_BEHAVIOR_REVIEWED`
- **Runtime:** `NOT_TESTED`
- **R01 refs:** NHAPDIEM

---

## AUD-014 — XUATDIEM

### 1. Định danh
- **feature_id:** `AUD-014`
- **Tên:** XUATDIEM
- **Nhóm:** DIEM
- **Audience:** USER
- **Trạng thái:** `IMPLEMENTED` — CommandMethod + handler method found in source

### 2. Điểm kích hoạt
- **Lệnh chính:** `XUATDIEM` @ `src/Modules/DiemDo/PointFileModule.cs:62` method `ExportPointsCommand`
- **Alias:** XD
- **Ribbon:** Xuất điểm đo|XUATDIEM|en=True|L380
- **Palette Run:** XUATDIEM|src/UI/Palette/PaletteViewModel.cs:L119
- **Thân method:** `src/Modules/DiemDo/PointFileModule.cs:63-91`

### 3. Chuỗi xử lý (tĩnh)
- CommandMethod → `ExportPointsCommand` → quan sát: có kiểm tra/auth: EnsureAuthenticated; UI/prompt: Editor., Prompt; kiểu/service gọi: ActivityLogger, AuthManager, DocumentManager, ExportPointsWindow; catch (1): System.Exception ex
- **Service/Dialog types trong thân:** ActivityLogger, AuthManager, DocumentManager, ExportPointsWindow
- **Giới hạn đọc:** Đã đọc thân command method; các service được gọi chưa được mở đầy đủ từng file trừ khi nằm trong cùng method.

### 4. Input / Output / CAD
- **UI/prompt:** Editor., Prompt
- **I/O file/network (trực tiếp):** (không thấy File/Http dialog API trực tiếp trong thân — có thể nằm service)
- **Transaction CAD (trực tiếp):** (không thấy Transaction trực tiếp trong thân)
- **Auth gate:** EnsureAuthenticated
- **Đối tượng CAD / đơn vị / phiên bản phụ thuộc:** CHƯA ĐỦ BẰNG CHỨNG nếu không nằm trong thân command — cần đọc service layer khi quay video.
- **Hành động phá hủy:** CHƯA ĐỦ BẰNG CHỨNG đầy đủ; nếu thân có Delete/Erase sẽ nằm trong notes/services — kiểm tra lại trước pilot.

### 5. Nhánh lỗi
- **catch quan sát:** System.Exception ex

### 6. Fixture / đo độc lập (đề xuất tĩnh)
- **MASTER/WORK COPY:** bản vẽ trắng Metric WORK COPY; không đụng file khách.
- **REFERENCE:** đo độc lập ngoài CAD khi feature xuất file; nếu chỉ ghi DWG — REFERENCE bằng checklist đối tượng.
- **Thiếu bằng chứng fixture cụ thể:** ghi nhận cho `AUD-014` — chưa có seed audit riêng trong `_R02`.

### 7. Khả năng quay / rủi ro
- Modal/UI: chưa thấy modal trong thân
- Chuột/pick: có Prompt/Get*
- Async: không thấy trong thân
- Ứng viên video người dùng (cờ CSV): YES

### 8. Mức kết luận
- **Source:** `SOURCE_BEHAVIOR_REVIEWED`
- **Runtime:** `NOT_TESTED`
- **R01 refs:** XUATDIEM

---
