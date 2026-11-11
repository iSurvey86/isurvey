# Nhóm TERRAIN — hồ sơ từng feature_id

Sinh 2026-11-10 — phân tích tĩnh thân method (brace-match).

## AUD-056 — ANTIN

### 1. Định danh
- **feature_id:** `AUD-056`
- **Tên:** ANTIN
- **Nhóm:** TERRAIN
- **Audience:** USER
- **Trạng thái:** `IMPLEMENTED` — CommandMethod + handler method found in source

### 2. Điểm kích hoạt
- **Lệnh chính:** `ANTIN` @ `src/Modules/Terrain/TerrainModule.cs:35` method `ToggleAlias`
- **Alias:** (không)
- **Ribbon:** Ẩn / Hiện Lưới TIN|ANTIN|en=True|L398
- **Palette Run:** (không thấy PaletteCommands.Run)
- **Thân method:** `src/Modules/Terrain/TerrainModule.cs:36-36`

### 3. Chuỗi xử lý (tĩnh)
- CommandMethod → `ToggleAlias` → quan sát: thin-wrapper → Toggle @ src/Modules/Terrain/TerrainModule.cs:273-301; có kiểm tra/auth: EnsureAuthenticated; UI/prompt: Editor.; CAD tx: Database; kiểu/service gọi: AuthManager, DocumentManager, SurveyPaletteManager, TerrainService, TerrainStore; stub/hint trong thân
- **Service/Dialog types trong thân:** AuthManager, DocumentManager, SurveyPaletteManager, TerrainService, TerrainStore
- **Giới hạn đọc:** Đã đọc thân command method; các service được gọi chưa được mở đầy đủ từng file trừ khi nằm trong cùng method.

### 4. Input / Output / CAD
- **UI/prompt:** Editor.
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
- **Thiếu bằng chứng fixture cụ thể:** ghi nhận cho `AUD-056` — chưa có seed audit riêng trong `_R02`.

### 7. Khả năng quay / rủi ro
- Modal/UI: chưa thấy modal trong thân
- Chuột/pick: chưa thấy pick trong thân
- Async: không thấy trong thân
- Ứng viên video người dùng (cờ CSV): YES

### 8. Mức kết luận
- **Source:** `SOURCE_BEHAVIOR_REVIEWED`
- **Runtime:** `NOT_TESTED`
- **R01 refs:** ANTIN

---

## AUD-057 — GOMOHINH

### 1. Định danh
- **feature_id:** `AUD-057`
- **Tên:** GOMOHINH
- **Nhóm:** TERRAIN
- **Audience:** USER
- **Trạng thái:** `IMPLEMENTED` — CommandMethod + handler method found in source

### 2. Điểm kích hoạt
- **Lệnh chính:** `GOMOHINH` @ `src/Modules/Terrain/TerrainModule.cs:47` method `DetachAlias`
- **Alias:** (không)
- **Ribbon:** (không / chưa gắn RibbonBuilder)
- **Palette Run:** (không thấy PaletteCommands.Run)
- **Thân method:** `src/Modules/Terrain/TerrainModule.cs:48-48`

### 3. Chuỗi xử lý (tĩnh)
- CommandMethod → `DetachAlias` → quan sát: thin-wrapper → Detach @ src/Modules/Terrain/TerrainModule.cs:188-218; có kiểm tra/auth: EnsureAuthenticated; UI/prompt: Editor.; CAD tx: Database, ModelSpace; kiểu/service gọi: AuthManager, DocumentManager, SurveyPaletteManager, TerrainService
- **Service/Dialog types trong thân:** AuthManager, DocumentManager, SurveyPaletteManager, TerrainService
- **Giới hạn đọc:** Đã đọc thân command method; các service được gọi chưa được mở đầy đủ từng file trừ khi nằm trong cùng method.

### 4. Input / Output / CAD
- **UI/prompt:** Editor.
- **I/O file/network (trực tiếp):** (không thấy File/Http dialog API trực tiếp trong thân — có thể nằm service)
- **Transaction CAD (trực tiếp):** Database, ModelSpace
- **Auth gate:** EnsureAuthenticated
- **Đối tượng CAD / đơn vị / phiên bản phụ thuộc:** CHƯA ĐỦ BẰNG CHỨNG nếu không nằm trong thân command — cần đọc service layer khi quay video.
- **Hành động phá hủy:** CHƯA ĐỦ BẰNG CHỨNG đầy đủ; nếu thân có Delete/Erase sẽ nằm trong notes/services — kiểm tra lại trước pilot.

### 5. Nhánh lỗi
- **catch quan sát:** CHƯA ĐỦ BẰNG CHỨNG / không có catch trong thân command (lỗi có thể bubble hoặc xử lý ngoài)

### 6. Fixture / đo độc lập (đề xuất tĩnh)
- **MASTER/WORK COPY:** bản vẽ trắng Metric WORK COPY; không đụng file khách.
- **REFERENCE:** đo độc lập ngoài CAD khi feature xuất file; nếu chỉ ghi DWG — REFERENCE bằng checklist đối tượng.
- **Thiếu bằng chứng fixture cụ thể:** ghi nhận cho `AUD-057` — chưa có seed audit riêng trong `_R02`.

### 7. Khả năng quay / rủi ro
- Modal/UI: chưa thấy modal trong thân
- Chuột/pick: chưa thấy pick trong thân
- Async: không thấy trong thân
- Ứng viên video người dùng (cờ CSV): YES

### 8. Mức kết luận
- **Source:** `SOURCE_BEHAVIOR_REVIEWED`
- **Runtime:** `NOT_TESTED`
- **R01 refs:** GOMOHINH

---

## AUD-058 — ISURVEY_TERRAIN_BUILD

### 1. Định danh
- **feature_id:** `AUD-058`
- **Tên:** ISURVEY_TERRAIN_BUILD
- **Nhóm:** TERRAIN
- **Audience:** USER
- **Trạng thái:** `IMPLEMENTED` — CommandMethod + handler method found in source

### 2. Điểm kích hoạt
- **Lệnh chính:** `ISURVEY_TERRAIN_BUILD` @ `src/Modules/Terrain/TerrainModule.cs:26` method `BuildCommand`
- **Alias:** (không)
- **Ribbon:** (không / chưa gắn RibbonBuilder)
- **Palette Run:** (không thấy PaletteCommands.Run)
- **Thân method:** `src/Modules/Terrain/TerrainModule.cs:27-27`

### 3. Chuỗi xử lý (tĩnh)
- CommandMethod → `BuildCommand` → quan sát: thin-wrapper → Build @ src/Modules/Terrain/TerrainModule.cs:165-186; có kiểm tra/auth: EnsureAuthenticated; UI/prompt: Editor.; CAD tx: Database, ModelSpace; kiểu/service gọi: AuthManager, DocumentManager, SurveyPaletteManager, TerrainService; catch (1): System.Exception ex
- **Service/Dialog types trong thân:** AuthManager, DocumentManager, SurveyPaletteManager, TerrainService
- **Giới hạn đọc:** Đã đọc thân command method; các service được gọi chưa được mở đầy đủ từng file trừ khi nằm trong cùng method.

### 4. Input / Output / CAD
- **UI/prompt:** Editor.
- **I/O file/network (trực tiếp):** (không thấy File/Http dialog API trực tiếp trong thân — có thể nằm service)
- **Transaction CAD (trực tiếp):** Database, ModelSpace
- **Auth gate:** EnsureAuthenticated
- **Đối tượng CAD / đơn vị / phiên bản phụ thuộc:** CHƯA ĐỦ BẰNG CHỨNG nếu không nằm trong thân command — cần đọc service layer khi quay video.
- **Hành động phá hủy:** CHƯA ĐỦ BẰNG CHỨNG đầy đủ; nếu thân có Delete/Erase sẽ nằm trong notes/services — kiểm tra lại trước pilot.

### 5. Nhánh lỗi
- **catch quan sát:** System.Exception ex

### 6. Fixture / đo độc lập (đề xuất tĩnh)
- **MASTER/WORK COPY:** bản vẽ trắng Metric WORK COPY; không đụng file khách.
- **REFERENCE:** đo độc lập ngoài CAD khi feature xuất file; nếu chỉ ghi DWG — REFERENCE bằng checklist đối tượng.
- **Thiếu bằng chứng fixture cụ thể:** ghi nhận cho `AUD-058` — chưa có seed audit riêng trong `_R02`.

### 7. Khả năng quay / rủi ro
- Modal/UI: chưa thấy modal trong thân
- Chuột/pick: chưa thấy pick trong thân
- Async: không thấy trong thân
- Ứng viên video người dùng (cờ CSV): YES

### 8. Mức kết luận
- **Source:** `SOURCE_BEHAVIOR_REVIEWED`
- **Runtime:** `NOT_TESTED`
- **R01 refs:** ISURVEY_TERRAIN_BUILD

---

## AUD-059 — ISURVEY_TERRAIN_CLEAR

### 1. Định danh
- **feature_id:** `AUD-059`
- **Tên:** ISURVEY_TERRAIN_CLEAR
- **Nhóm:** TERRAIN
- **Audience:** USER
- **Trạng thái:** `IMPLEMENTED` — CommandMethod + handler method found in source

### 2. Điểm kích hoạt
- **Lệnh chính:** `ISURVEY_TERRAIN_CLEAR` @ `src/Modules/Terrain/TerrainModule.cs:50` method `ClearCommand`
- **Alias:** (không)
- **Ribbon:** (không / chưa gắn RibbonBuilder)
- **Palette Run:** (không thấy PaletteCommands.Run)
- **Thân method:** `src/Modules/Terrain/TerrainModule.cs:51-51`

### 3. Chuỗi xử lý (tĩnh)
- CommandMethod → `ClearCommand` → quan sát: thin-wrapper → Clear @ src/Modules/Terrain/TerrainModule.cs:220-271; có kiểm tra/auth: EnsureAuthenticated; UI/prompt: Editor.; CAD tx: Database, ModelSpace; kiểu/service gọi: AuthManager, DarkDialog, DocumentManager, SurveyPaletteManager, TerrainService; catch (1): System.Exception ex
- **Service/Dialog types trong thân:** AuthManager, DarkDialog, DocumentManager, SurveyPaletteManager, TerrainService
- **Giới hạn đọc:** Đã đọc thân command method; các service được gọi chưa được mở đầy đủ từng file trừ khi nằm trong cùng method.

### 4. Input / Output / CAD
- **UI/prompt:** Editor.
- **I/O file/network (trực tiếp):** (không thấy File/Http dialog API trực tiếp trong thân — có thể nằm service)
- **Transaction CAD (trực tiếp):** Database, ModelSpace
- **Auth gate:** EnsureAuthenticated
- **Đối tượng CAD / đơn vị / phiên bản phụ thuộc:** CHƯA ĐỦ BẰNG CHỨNG nếu không nằm trong thân command — cần đọc service layer khi quay video.
- **Hành động phá hủy:** CHƯA ĐỦ BẰNG CHỨNG đầy đủ; nếu thân có Delete/Erase sẽ nằm trong notes/services — kiểm tra lại trước pilot.

### 5. Nhánh lỗi
- **catch quan sát:** System.Exception ex

### 6. Fixture / đo độc lập (đề xuất tĩnh)
- **MASTER/WORK COPY:** bản vẽ trắng Metric WORK COPY; không đụng file khách.
- **REFERENCE:** đo độc lập ngoài CAD khi feature xuất file; nếu chỉ ghi DWG — REFERENCE bằng checklist đối tượng.
- **Thiếu bằng chứng fixture cụ thể:** ghi nhận cho `AUD-059` — chưa có seed audit riêng trong `_R02`.

### 7. Khả năng quay / rủi ro
- Modal/UI: có dấu hiệu modal/dialog
- Chuột/pick: chưa thấy pick trong thân
- Async: không thấy trong thân
- Ứng viên video người dùng (cờ CSV): YES

### 8. Mức kết luận
- **Source:** `SOURCE_BEHAVIOR_REVIEWED`
- **Runtime:** `NOT_TESTED`
- **R01 refs:** ISURVEY_TERRAIN_CLEAR

---

## AUD-060 — ISURVEY_TERRAIN_DETACH

### 1. Định danh
- **feature_id:** `AUD-060`
- **Tên:** ISURVEY_TERRAIN_DETACH
- **Nhóm:** TERRAIN
- **Audience:** USER
- **Trạng thái:** `IMPLEMENTED` — CommandMethod + handler method found in source

### 2. Điểm kích hoạt
- **Lệnh chính:** `ISURVEY_TERRAIN_DETACH` @ `src/Modules/Terrain/TerrainModule.cs:44` method `DetachCommand`
- **Alias:** (không)
- **Ribbon:** (không / chưa gắn RibbonBuilder)
- **Palette Run:** (không thấy PaletteCommands.Run)
- **Thân method:** `src/Modules/Terrain/TerrainModule.cs:45-45`

### 3. Chuỗi xử lý (tĩnh)
- CommandMethod → `DetachCommand` → quan sát: thin-wrapper → Detach @ src/Modules/Terrain/TerrainModule.cs:188-218; có kiểm tra/auth: EnsureAuthenticated; UI/prompt: Editor.; CAD tx: Database, ModelSpace; kiểu/service gọi: AuthManager, DocumentManager, SurveyPaletteManager, TerrainService
- **Service/Dialog types trong thân:** AuthManager, DocumentManager, SurveyPaletteManager, TerrainService
- **Giới hạn đọc:** Đã đọc thân command method; các service được gọi chưa được mở đầy đủ từng file trừ khi nằm trong cùng method.

### 4. Input / Output / CAD
- **UI/prompt:** Editor.
- **I/O file/network (trực tiếp):** (không thấy File/Http dialog API trực tiếp trong thân — có thể nằm service)
- **Transaction CAD (trực tiếp):** Database, ModelSpace
- **Auth gate:** EnsureAuthenticated
- **Đối tượng CAD / đơn vị / phiên bản phụ thuộc:** CHƯA ĐỦ BẰNG CHỨNG nếu không nằm trong thân command — cần đọc service layer khi quay video.
- **Hành động phá hủy:** CHƯA ĐỦ BẰNG CHỨNG đầy đủ; nếu thân có Delete/Erase sẽ nằm trong notes/services — kiểm tra lại trước pilot.

### 5. Nhánh lỗi
- **catch quan sát:** CHƯA ĐỦ BẰNG CHỨNG / không có catch trong thân command (lỗi có thể bubble hoặc xử lý ngoài)

### 6. Fixture / đo độc lập (đề xuất tĩnh)
- **MASTER/WORK COPY:** bản vẽ trắng Metric WORK COPY; không đụng file khách.
- **REFERENCE:** đo độc lập ngoài CAD khi feature xuất file; nếu chỉ ghi DWG — REFERENCE bằng checklist đối tượng.
- **Thiếu bằng chứng fixture cụ thể:** ghi nhận cho `AUD-060` — chưa có seed audit riêng trong `_R02`.

### 7. Khả năng quay / rủi ro
- Modal/UI: chưa thấy modal trong thân
- Chuột/pick: chưa thấy pick trong thân
- Async: không thấy trong thân
- Ứng viên video người dùng (cờ CSV): YES

### 8. Mức kết luận
- **Source:** `SOURCE_BEHAVIOR_REVIEWED`
- **Runtime:** `NOT_TESTED`
- **R01 refs:** ISURVEY_TERRAIN_DETACH

---

## AUD-061 — ISURVEY_TERRAIN_NEW

### 1. Định danh
- **feature_id:** `AUD-061`
- **Tên:** ISURVEY_TERRAIN_NEW
- **Nhóm:** TERRAIN
- **Audience:** USER
- **Trạng thái:** `IMPLEMENTED` — CommandMethod + handler method found in source

### 2. Điểm kích hoạt
- **Lệnh chính:** `ISURVEY_TERRAIN_NEW` @ `src/Modules/Terrain/TerrainModule.cs:14` method `NewCommand`
- **Alias:** (không)
- **Ribbon:** (không / chưa gắn RibbonBuilder)
- **Palette Run:** (không thấy PaletteCommands.Run)
- **Thân method:** `src/Modules/Terrain/TerrainModule.cs:15-15`

### 3. Chuỗi xử lý (tĩnh)
- CommandMethod → `NewCommand` → quan sát: thin-wrapper → New @ src/Modules/Terrain/TerrainModule.cs:56-100; có kiểm tra/auth: EnsureAuthenticated; UI/prompt: Editor., Prompt; CAD tx: Database, ModelSpace; kiểu/service gọi: AuthManager, CreateTerrainWindow, DocumentManager, SurveyPaletteManager, TerrainStore; catch (2): System.Exception; System.Exception ex
- **Service/Dialog types trong thân:** AuthManager, CreateTerrainWindow, DocumentManager, SurveyPaletteManager, TerrainStore
- **Giới hạn đọc:** Đã đọc thân command method; các service được gọi chưa được mở đầy đủ từng file trừ khi nằm trong cùng method.

### 4. Input / Output / CAD
- **UI/prompt:** Editor., Prompt
- **I/O file/network (trực tiếp):** (không thấy File/Http dialog API trực tiếp trong thân — có thể nằm service)
- **Transaction CAD (trực tiếp):** Database, ModelSpace
- **Auth gate:** EnsureAuthenticated
- **Đối tượng CAD / đơn vị / phiên bản phụ thuộc:** CHƯA ĐỦ BẰNG CHỨNG nếu không nằm trong thân command — cần đọc service layer khi quay video.
- **Hành động phá hủy:** CHƯA ĐỦ BẰNG CHỨNG đầy đủ; nếu thân có Delete/Erase sẽ nằm trong notes/services — kiểm tra lại trước pilot.

### 5. Nhánh lỗi
- **catch quan sát:** System.Exception; System.Exception ex

### 6. Fixture / đo độc lập (đề xuất tĩnh)
- **MASTER/WORK COPY:** bản vẽ trắng Metric WORK COPY; không đụng file khách.
- **REFERENCE:** đo độc lập ngoài CAD khi feature xuất file; nếu chỉ ghi DWG — REFERENCE bằng checklist đối tượng.
- **Thiếu bằng chứng fixture cụ thể:** ghi nhận cho `AUD-061` — chưa có seed audit riêng trong `_R02`.

### 7. Khả năng quay / rủi ro
- Modal/UI: chưa thấy modal trong thân
- Chuột/pick: có Prompt/Get*
- Async: không thấy trong thân
- Ứng viên video người dùng (cờ CSV): YES

### 8. Mức kết luận
- **Source:** `SOURCE_BEHAVIOR_REVIEWED`
- **Runtime:** `NOT_TESTED`
- **R01 refs:** ISURVEY_TERRAIN_NEW

---

## AUD-062 — ISURVEY_TERRAIN_TOGGLE

### 1. Định danh
- **feature_id:** `AUD-062`
- **Tên:** ISURVEY_TERRAIN_TOGGLE
- **Nhóm:** TERRAIN
- **Audience:** USER
- **Trạng thái:** `IMPLEMENTED` — CommandMethod + handler method found in source

### 2. Điểm kích hoạt
- **Lệnh chính:** `ISURVEY_TERRAIN_TOGGLE` @ `src/Modules/Terrain/TerrainModule.cs:32` method `ToggleCommand`
- **Alias:** (không)
- **Ribbon:** (không / chưa gắn RibbonBuilder)
- **Palette Run:** (không thấy PaletteCommands.Run)
- **Thân method:** `src/Modules/Terrain/TerrainModule.cs:33-33`

### 3. Chuỗi xử lý (tĩnh)
- CommandMethod → `ToggleCommand` → quan sát: thin-wrapper → Toggle @ src/Modules/Terrain/TerrainModule.cs:273-301; có kiểm tra/auth: EnsureAuthenticated; UI/prompt: Editor.; CAD tx: Database; kiểu/service gọi: AuthManager, DocumentManager, SurveyPaletteManager, TerrainService, TerrainStore; stub/hint trong thân
- **Service/Dialog types trong thân:** AuthManager, DocumentManager, SurveyPaletteManager, TerrainService, TerrainStore
- **Giới hạn đọc:** Đã đọc thân command method; các service được gọi chưa được mở đầy đủ từng file trừ khi nằm trong cùng method.

### 4. Input / Output / CAD
- **UI/prompt:** Editor.
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
- **Thiếu bằng chứng fixture cụ thể:** ghi nhận cho `AUD-062` — chưa có seed audit riêng trong `_R02`.

### 7. Khả năng quay / rủi ro
- Modal/UI: chưa thấy modal trong thân
- Chuột/pick: chưa thấy pick trong thân
- Async: không thấy trong thân
- Ứng viên video người dùng (cờ CSV): YES

### 8. Mức kết luận
- **Source:** `SOURCE_BEHAVIOR_REVIEWED`
- **Runtime:** `NOT_TESTED`
- **R01 refs:** ISURVEY_TERRAIN_TOGGLE

---

## AUD-063 — ISURVEY_TERRAIN_Z

### 1. Định danh
- **feature_id:** `AUD-063`
- **Tên:** ISURVEY_TERRAIN_Z
- **Nhóm:** TERRAIN
- **Audience:** USER
- **Trạng thái:** `IMPLEMENTED` — CommandMethod + handler method found in source

### 2. Điểm kích hoạt
- **Lệnh chính:** `ISURVEY_TERRAIN_Z` @ `src/Modules/Terrain/TerrainModule.cs:38` method `QueryCommand`
- **Alias:** (không)
- **Ribbon:** (không / chưa gắn RibbonBuilder)
- **Palette Run:** (không thấy PaletteCommands.Run)
- **Thân method:** `src/Modules/Terrain/TerrainModule.cs:39-39`

### 3. Chuỗi xử lý (tĩnh)
- CommandMethod → `QueryCommand` → quan sát: thin-wrapper → Query @ src/Modules/Terrain/TerrainModule.cs:303-344; có kiểm tra/auth: EnsureAuthenticated; UI/prompt: Editor., Prompt; CAD tx: Database, ModelSpace; kiểu/service gọi: AuthManager, DocumentManager, TerrainService; stub/hint trong thân
- **Service/Dialog types trong thân:** AuthManager, DocumentManager, TerrainService
- **Giới hạn đọc:** Đã đọc thân command method; các service được gọi chưa được mở đầy đủ từng file trừ khi nằm trong cùng method.

### 4. Input / Output / CAD
- **UI/prompt:** Editor., Prompt
- **I/O file/network (trực tiếp):** (không thấy File/Http dialog API trực tiếp trong thân — có thể nằm service)
- **Transaction CAD (trực tiếp):** Database, ModelSpace
- **Auth gate:** EnsureAuthenticated
- **Đối tượng CAD / đơn vị / phiên bản phụ thuộc:** CHƯA ĐỦ BẰNG CHỨNG nếu không nằm trong thân command — cần đọc service layer khi quay video.
- **Hành động phá hủy:** CHƯA ĐỦ BẰNG CHỨNG đầy đủ; nếu thân có Delete/Erase sẽ nằm trong notes/services — kiểm tra lại trước pilot.

### 5. Nhánh lỗi
- **catch quan sát:** CHƯA ĐỦ BẰNG CHỨNG / không có catch trong thân command (lỗi có thể bubble hoặc xử lý ngoài)

### 6. Fixture / đo độc lập (đề xuất tĩnh)
- **MASTER/WORK COPY:** bản vẽ trắng Metric WORK COPY; không đụng file khách.
- **REFERENCE:** đo độc lập ngoài CAD khi feature xuất file; nếu chỉ ghi DWG — REFERENCE bằng checklist đối tượng.
- **Thiếu bằng chứng fixture cụ thể:** ghi nhận cho `AUD-063` — chưa có seed audit riêng trong `_R02`.

### 7. Khả năng quay / rủi ro
- Modal/UI: chưa thấy modal trong thân
- Chuột/pick: có Prompt/Get*
- Async: không thấy trong thân
- Ứng viên video người dùng (cờ CSV): YES

### 8. Mức kết luận
- **Source:** `SOURCE_BEHAVIOR_REVIEWED`
- **Runtime:** `NOT_TESTED`
- **R01 refs:** ISURVEY_TERRAIN_Z

---

## AUD-064 — TAOMOHINH

### 1. Định danh
- **feature_id:** `AUD-064`
- **Tên:** TAOMOHINH
- **Nhóm:** TERRAIN
- **Audience:** USER
- **Trạng thái:** `IMPLEMENTED` — CommandMethod + handler method found in source

### 2. Điểm kích hoạt
- **Lệnh chính:** `TAOMOHINH` @ `src/Modules/Terrain/TerrainModule.cs:17` method `NewAlias`
- **Alias:** (không)
- **Ribbon:** (không / chưa gắn RibbonBuilder)
- **Palette Run:** TAOMOHINH|src/UI/Palette/PaletteViewModel.cs:L140
- **Thân method:** `src/Modules/Terrain/TerrainModule.cs:18-18`

### 3. Chuỗi xử lý (tĩnh)
- CommandMethod → `NewAlias` → quan sát: thin-wrapper → New @ src/Modules/Terrain/TerrainModule.cs:56-100; có kiểm tra/auth: EnsureAuthenticated; UI/prompt: Editor., Prompt; CAD tx: Database, ModelSpace; kiểu/service gọi: AuthManager, CreateTerrainWindow, DocumentManager, SurveyPaletteManager, TerrainStore; catch (2): System.Exception; System.Exception ex
- **Service/Dialog types trong thân:** AuthManager, CreateTerrainWindow, DocumentManager, SurveyPaletteManager, TerrainStore
- **Giới hạn đọc:** Đã đọc thân command method; các service được gọi chưa được mở đầy đủ từng file trừ khi nằm trong cùng method.

### 4. Input / Output / CAD
- **UI/prompt:** Editor., Prompt
- **I/O file/network (trực tiếp):** (không thấy File/Http dialog API trực tiếp trong thân — có thể nằm service)
- **Transaction CAD (trực tiếp):** Database, ModelSpace
- **Auth gate:** EnsureAuthenticated
- **Đối tượng CAD / đơn vị / phiên bản phụ thuộc:** CHƯA ĐỦ BẰNG CHỨNG nếu không nằm trong thân command — cần đọc service layer khi quay video.
- **Hành động phá hủy:** CHƯA ĐỦ BẰNG CHỨNG đầy đủ; nếu thân có Delete/Erase sẽ nằm trong notes/services — kiểm tra lại trước pilot.

### 5. Nhánh lỗi
- **catch quan sát:** System.Exception; System.Exception ex

### 6. Fixture / đo độc lập (đề xuất tĩnh)
- **MASTER/WORK COPY:** bản vẽ trắng Metric WORK COPY; không đụng file khách.
- **REFERENCE:** đo độc lập ngoài CAD khi feature xuất file; nếu chỉ ghi DWG — REFERENCE bằng checklist đối tượng.
- **Thiếu bằng chứng fixture cụ thể:** ghi nhận cho `AUD-064` — chưa có seed audit riêng trong `_R02`.

### 7. Khả năng quay / rủi ro
- Modal/UI: chưa thấy modal trong thân
- Chuột/pick: có Prompt/Get*
- Async: không thấy trong thân
- Ứng viên video người dùng (cờ CSV): YES

### 8. Mức kết luận
- **Source:** `SOURCE_BEHAVIOR_REVIEWED`
- **Runtime:** `NOT_TESTED`
- **R01 refs:** TAOMOHINH

---

## AUD-065 — THEMMOHINH

### 1. Định danh
- **feature_id:** `AUD-065`
- **Tên:** THEMMOHINH
- **Nhóm:** TERRAIN
- **Audience:** USER
- **Trạng thái:** `IMPLEMENTED` — CommandMethod + handler method found in source

### 2. Điểm kích hoạt
- **Lệnh chính:** `THEMMOHINH` @ `src/Modules/Terrain/TerrainModule.cs:23` method `AddAlias`
- **Alias:** (không)
- **Ribbon:** (không / chưa gắn RibbonBuilder)
- **Palette Run:** (không thấy PaletteCommands.Run)
- **Thân method:** `src/Modules/Terrain/TerrainModule.cs:24-24`

### 3. Chuỗi xử lý (tĩnh)
- CommandMethod → `AddAlias` → quan sát: thin-wrapper → Add @ src/Modules/Terrain/TerrainModule.cs:102-163; có kiểm tra/auth: EnsureAuthenticated; UI/prompt: Editor., Prompt; CAD tx: Database, ModelSpace; kiểu/service gọi: AddPointSourcesWindow, AuthManager, DocumentManager, SurveyPaletteManager, TerrainService; catch (1): System.Exception ex
- **Service/Dialog types trong thân:** AddPointSourcesWindow, AuthManager, DocumentManager, SurveyPaletteManager, TerrainService
- **Giới hạn đọc:** Đã đọc thân command method; các service được gọi chưa được mở đầy đủ từng file trừ khi nằm trong cùng method.

### 4. Input / Output / CAD
- **UI/prompt:** Editor., Prompt
- **I/O file/network (trực tiếp):** (không thấy File/Http dialog API trực tiếp trong thân — có thể nằm service)
- **Transaction CAD (trực tiếp):** Database, ModelSpace
- **Auth gate:** EnsureAuthenticated
- **Đối tượng CAD / đơn vị / phiên bản phụ thuộc:** CHƯA ĐỦ BẰNG CHỨNG nếu không nằm trong thân command — cần đọc service layer khi quay video.
- **Hành động phá hủy:** CHƯA ĐỦ BẰNG CHỨNG đầy đủ; nếu thân có Delete/Erase sẽ nằm trong notes/services — kiểm tra lại trước pilot.

### 5. Nhánh lỗi
- **catch quan sát:** System.Exception ex

### 6. Fixture / đo độc lập (đề xuất tĩnh)
- **MASTER/WORK COPY:** bản vẽ trắng Metric WORK COPY; không đụng file khách.
- **REFERENCE:** đo độc lập ngoài CAD khi feature xuất file; nếu chỉ ghi DWG — REFERENCE bằng checklist đối tượng.
- **Thiếu bằng chứng fixture cụ thể:** ghi nhận cho `AUD-065` — chưa có seed audit riêng trong `_R02`.

### 7. Khả năng quay / rủi ro
- Modal/UI: chưa thấy modal trong thân
- Chuột/pick: có Prompt/Get*
- Async: không thấy trong thân
- Ứng viên video người dùng (cờ CSV): YES

### 8. Mức kết luận
- **Source:** `SOURCE_BEHAVIOR_REVIEWED`
- **Runtime:** `NOT_TESTED`
- **R01 refs:** THEMMOHINH

---

## AUD-066 — TRACAOD

### 1. Định danh
- **feature_id:** `AUD-066`
- **Tên:** TRACAOD
- **Nhóm:** TERRAIN
- **Audience:** USER
- **Trạng thái:** `IMPLEMENTED` — CommandMethod + handler method found in source

### 2. Điểm kích hoạt
- **Lệnh chính:** `TRACAOD` @ `src/Modules/Terrain/TerrainModule.cs:41` method `QueryAlias`
- **Alias:** (không)
- **Ribbon:** (không / chưa gắn RibbonBuilder)
- **Palette Run:** (không thấy PaletteCommands.Run)
- **Thân method:** `src/Modules/Terrain/TerrainModule.cs:42-42`

### 3. Chuỗi xử lý (tĩnh)
- CommandMethod → `QueryAlias` → quan sát: thin-wrapper → Query @ src/Modules/Terrain/TerrainModule.cs:303-344; có kiểm tra/auth: EnsureAuthenticated; UI/prompt: Editor., Prompt; CAD tx: Database, ModelSpace; kiểu/service gọi: AuthManager, DocumentManager, TerrainService; stub/hint trong thân
- **Service/Dialog types trong thân:** AuthManager, DocumentManager, TerrainService
- **Giới hạn đọc:** Đã đọc thân command method; các service được gọi chưa được mở đầy đủ từng file trừ khi nằm trong cùng method.

### 4. Input / Output / CAD
- **UI/prompt:** Editor., Prompt
- **I/O file/network (trực tiếp):** (không thấy File/Http dialog API trực tiếp trong thân — có thể nằm service)
- **Transaction CAD (trực tiếp):** Database, ModelSpace
- **Auth gate:** EnsureAuthenticated
- **Đối tượng CAD / đơn vị / phiên bản phụ thuộc:** CHƯA ĐỦ BẰNG CHỨNG nếu không nằm trong thân command — cần đọc service layer khi quay video.
- **Hành động phá hủy:** CHƯA ĐỦ BẰNG CHỨNG đầy đủ; nếu thân có Delete/Erase sẽ nằm trong notes/services — kiểm tra lại trước pilot.

### 5. Nhánh lỗi
- **catch quan sát:** CHƯA ĐỦ BẰNG CHỨNG / không có catch trong thân command (lỗi có thể bubble hoặc xử lý ngoài)

### 6. Fixture / đo độc lập (đề xuất tĩnh)
- **MASTER/WORK COPY:** bản vẽ trắng Metric WORK COPY; không đụng file khách.
- **REFERENCE:** đo độc lập ngoài CAD khi feature xuất file; nếu chỉ ghi DWG — REFERENCE bằng checklist đối tượng.
- **Thiếu bằng chứng fixture cụ thể:** ghi nhận cho `AUD-066` — chưa có seed audit riêng trong `_R02`.

### 7. Khả năng quay / rủi ro
- Modal/UI: chưa thấy modal trong thân
- Chuột/pick: có Prompt/Get*
- Async: không thấy trong thân
- Ứng viên video người dùng (cờ CSV): YES

### 8. Mức kết luận
- **Source:** `SOURCE_BEHAVIOR_REVIEWED`
- **Runtime:** `NOT_TESTED`
- **R01 refs:** TRACAOD

---

## AUD-067 — XAYMOHINH

### 1. Định danh
- **feature_id:** `AUD-067`
- **Tên:** XAYMOHINH
- **Nhóm:** TERRAIN
- **Audience:** USER
- **Trạng thái:** `IMPLEMENTED` — CommandMethod + handler method found in source

### 2. Điểm kích hoạt
- **Lệnh chính:** `XAYMOHINH` @ `src/Modules/Terrain/TerrainModule.cs:29` method `BuildAlias`
- **Alias:** (không)
- **Ribbon:** (không / chưa gắn RibbonBuilder)
- **Palette Run:** (không thấy PaletteCommands.Run)
- **Thân method:** `src/Modules/Terrain/TerrainModule.cs:30-30`

### 3. Chuỗi xử lý (tĩnh)
- CommandMethod → `BuildAlias` → quan sát: thin-wrapper → Build @ src/Modules/Terrain/TerrainModule.cs:165-186; có kiểm tra/auth: EnsureAuthenticated; UI/prompt: Editor.; CAD tx: Database, ModelSpace; kiểu/service gọi: AuthManager, DocumentManager, SurveyPaletteManager, TerrainService; catch (1): System.Exception ex
- **Service/Dialog types trong thân:** AuthManager, DocumentManager, SurveyPaletteManager, TerrainService
- **Giới hạn đọc:** Đã đọc thân command method; các service được gọi chưa được mở đầy đủ từng file trừ khi nằm trong cùng method.

### 4. Input / Output / CAD
- **UI/prompt:** Editor.
- **I/O file/network (trực tiếp):** (không thấy File/Http dialog API trực tiếp trong thân — có thể nằm service)
- **Transaction CAD (trực tiếp):** Database, ModelSpace
- **Auth gate:** EnsureAuthenticated
- **Đối tượng CAD / đơn vị / phiên bản phụ thuộc:** CHƯA ĐỦ BẰNG CHỨNG nếu không nằm trong thân command — cần đọc service layer khi quay video.
- **Hành động phá hủy:** CHƯA ĐỦ BẰNG CHỨNG đầy đủ; nếu thân có Delete/Erase sẽ nằm trong notes/services — kiểm tra lại trước pilot.

### 5. Nhánh lỗi
- **catch quan sát:** System.Exception ex

### 6. Fixture / đo độc lập (đề xuất tĩnh)
- **MASTER/WORK COPY:** bản vẽ trắng Metric WORK COPY; không đụng file khách.
- **REFERENCE:** đo độc lập ngoài CAD khi feature xuất file; nếu chỉ ghi DWG — REFERENCE bằng checklist đối tượng.
- **Thiếu bằng chứng fixture cụ thể:** ghi nhận cho `AUD-067` — chưa có seed audit riêng trong `_R02`.

### 7. Khả năng quay / rủi ro
- Modal/UI: chưa thấy modal trong thân
- Chuột/pick: chưa thấy pick trong thân
- Async: không thấy trong thân
- Ứng viên video người dùng (cờ CSV): YES

### 8. Mức kết luận
- **Source:** `SOURCE_BEHAVIOR_REVIEWED`
- **Runtime:** `NOT_TESTED`
- **R01 refs:** XAYMOHINH

---

## AUD-068 — XOATHANHPHAN

### 1. Định danh
- **feature_id:** `AUD-068`
- **Tên:** XOATHANHPHAN
- **Nhóm:** TERRAIN
- **Audience:** USER
- **Trạng thái:** `IMPLEMENTED` — CommandMethod + handler method found in source

### 2. Điểm kích hoạt
- **Lệnh chính:** `XOATHANHPHAN` @ `src/Modules/Terrain/TerrainModule.cs:53` method `ClearAlias`
- **Alias:** (không)
- **Ribbon:** (không / chưa gắn RibbonBuilder)
- **Palette Run:** (không thấy PaletteCommands.Run)
- **Thân method:** `src/Modules/Terrain/TerrainModule.cs:54-54`

### 3. Chuỗi xử lý (tĩnh)
- CommandMethod → `ClearAlias` → quan sát: thin-wrapper → Clear @ src/Modules/Terrain/TerrainModule.cs:220-271; có kiểm tra/auth: EnsureAuthenticated; UI/prompt: Editor.; CAD tx: Database, ModelSpace; kiểu/service gọi: AuthManager, DarkDialog, DocumentManager, SurveyPaletteManager, TerrainService; catch (1): System.Exception ex
- **Service/Dialog types trong thân:** AuthManager, DarkDialog, DocumentManager, SurveyPaletteManager, TerrainService
- **Giới hạn đọc:** Đã đọc thân command method; các service được gọi chưa được mở đầy đủ từng file trừ khi nằm trong cùng method.

### 4. Input / Output / CAD
- **UI/prompt:** Editor.
- **I/O file/network (trực tiếp):** (không thấy File/Http dialog API trực tiếp trong thân — có thể nằm service)
- **Transaction CAD (trực tiếp):** Database, ModelSpace
- **Auth gate:** EnsureAuthenticated
- **Đối tượng CAD / đơn vị / phiên bản phụ thuộc:** CHƯA ĐỦ BẰNG CHỨNG nếu không nằm trong thân command — cần đọc service layer khi quay video.
- **Hành động phá hủy:** CHƯA ĐỦ BẰNG CHỨNG đầy đủ; nếu thân có Delete/Erase sẽ nằm trong notes/services — kiểm tra lại trước pilot.

### 5. Nhánh lỗi
- **catch quan sát:** System.Exception ex

### 6. Fixture / đo độc lập (đề xuất tĩnh)
- **MASTER/WORK COPY:** bản vẽ trắng Metric WORK COPY; không đụng file khách.
- **REFERENCE:** đo độc lập ngoài CAD khi feature xuất file; nếu chỉ ghi DWG — REFERENCE bằng checklist đối tượng.
- **Thiếu bằng chứng fixture cụ thể:** ghi nhận cho `AUD-068` — chưa có seed audit riêng trong `_R02`.

### 7. Khả năng quay / rủi ro
- Modal/UI: có dấu hiệu modal/dialog
- Chuột/pick: chưa thấy pick trong thân
- Async: không thấy trong thân
- Ứng viên video người dùng (cờ CSV): YES

### 8. Mức kết luận
- **Source:** `SOURCE_BEHAVIOR_REVIEWED`
- **Runtime:** `NOT_TESTED`
- **R01 refs:** XOATHANHPHAN

---
