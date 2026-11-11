# Nhóm UAV — hồ sơ từng feature_id

Sinh 2026-11-10 — phân tích tĩnh thân method (brace-match).

## AUD-069 — AUTOSCAN

### 1. Định danh
- **feature_id:** `AUD-069`
- **Tên:** AUTOSCAN
- **Nhóm:** UAV
- **Audience:** USER
- **Trạng thái:** `IMPLEMENTED` — CommandMethod + handler method found in source

### 2. Điểm kích hoạt
- **Lệnh chính:** `AUTOSCAN` @ `src/Modules/Scan/ScanModule.cs:25` method `AutoScanCommand`
- **Alias:** SCN
- **Ribbon:** Quét Cấu Trúc (AutoScan)|AUTOSCAN|en=True|L217
- **Palette Run:** (không thấy PaletteCommands.Run)
- **Thân method:** `src/Modules/Scan/ScanModule.cs:26-88`

### 3. Chuỗi xử lý (tĩnh)
- CommandMethod → `AutoScanCommand` → quan sát: có kiểm tra/auth: EnsureAuthenticated; UI/prompt: GetSelection, Prompt, ShowDialog, WriteMessage; I/O: File.Write; CAD tx: Commit, Database, StartTransaction; kiểu/service gọi: ActivityLogger, AuthManager, DocumentManager, ShowDialog, TransactionManager, Win32SaveFileDialog; catch (1): System.Exception ex
- **Service/Dialog types trong thân:** ActivityLogger, AuthManager, DocumentManager, ShowDialog, TransactionManager, Win32SaveFileDialog
- **Giới hạn đọc:** Đã đọc thân command method; các service được gọi chưa được mở đầy đủ từng file trừ khi nằm trong cùng method.

### 4. Input / Output / CAD
- **UI/prompt:** GetSelection, Prompt, ShowDialog, WriteMessage
- **I/O file/network (trực tiếp):** File.Write
- **Transaction CAD (trực tiếp):** Commit, Database, StartTransaction
- **Auth gate:** EnsureAuthenticated
- **Đối tượng CAD / đơn vị / phiên bản phụ thuộc:** CHƯA ĐỦ BẰNG CHỨNG nếu không nằm trong thân command — cần đọc service layer khi quay video.
- **Hành động phá hủy:** CHƯA ĐỦ BẰNG CHỨNG đầy đủ; nếu thân có Delete/Erase sẽ nằm trong notes/services — kiểm tra lại trước pilot.

### 5. Nhánh lỗi
- **catch quan sát:** System.Exception ex

### 6. Fixture / đo độc lập (đề xuất tĩnh)
- **MASTER/WORK COPY:** bản vẽ trắng Metric WORK COPY; không đụng file khách.
- **REFERENCE:** đo độc lập ngoài CAD khi feature xuất file; nếu chỉ ghi DWG — REFERENCE bằng checklist đối tượng.
- **Thiếu bằng chứng fixture cụ thể:** ghi nhận cho `AUD-069` — chưa có seed audit riêng trong `_R02`.

### 7. Khả năng quay / rủi ro
- Modal/UI: có dấu hiệu modal/dialog
- Chuột/pick: có Prompt/Get*
- Async: không thấy trong thân
- Ứng viên video người dùng (cờ CSV): YES

### 8. Mức kết luận
- **Source:** `SOURCE_BEHAVIOR_REVIEWED`
- **Runtime:** `NOT_TESTED`
- **R01 refs:** AUTOSCAN

---

## AUD-070 — DAODAUTUYEN

### 1. Định danh
- **feature_id:** `AUD-070`
- **Tên:** DAODAUTUYEN
- **Nhóm:** UAV
- **Audience:** USER
- **Trạng thái:** `IMPLEMENTED` — CommandMethod + handler method found in source

### 2. Điểm kích hoạt
- **Lệnh chính:** `DAODAUTUYEN` @ `src/Modules/Tuyen/RouteModule.cs:35` method `ReverseAlias`
- **Alias:** (không)
- **Ribbon:** (không / chưa gắn RibbonBuilder)
- **Palette Run:** (không thấy PaletteCommands.Run)
- **Thân method:** `src/Modules/Tuyen/RouteModule.cs:36-36`

### 3. Chuỗi xử lý (tĩnh)
- CommandMethod → `ReverseAlias` → quan sát: thin-wrapper → Reverse @ src/Modules/Tuyen/RouteModule.cs:264-280; có kiểm tra/auth: EnsureAuthenticated; UI/prompt: Editor.; CAD tx: Database, ModelSpace; kiểu/service gọi: AuthManager, DocumentManager, RouteService, SurveyPaletteManager
- **Service/Dialog types trong thân:** AuthManager, DocumentManager, RouteService, SurveyPaletteManager
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
- **Thiếu bằng chứng fixture cụ thể:** ghi nhận cho `AUD-070` — chưa có seed audit riêng trong `_R02`.

### 7. Khả năng quay / rủi ro
- Modal/UI: chưa thấy modal trong thân
- Chuột/pick: chưa thấy pick trong thân
- Async: không thấy trong thân
- Ứng viên video người dùng (cờ CSV): YES

### 8. Mức kết luận
- **Source:** `SOURCE_BEHAVIOR_REVIEWED`
- **Runtime:** `NOT_TESTED`
- **R01 refs:** DAODAUTUYEN

---

## AUD-071 — ISURVEY_STATION_ADD_ONE

### 1. Định danh
- **feature_id:** `AUD-071`
- **Tên:** ISURVEY_STATION_ADD_ONE
- **Nhóm:** UAV
- **Audience:** USER
- **Trạng thái:** `IMPLEMENTED` — CommandMethod + handler method found in source

### 2. Điểm kích hoạt
- **Lệnh chính:** `ISURVEY_STATION_ADD_ONE` @ `src/Modules/Tuyen/RouteModule.cs:50` method `AddOneCommand`
- **Alias:** (không)
- **Ribbon:** (không / chưa gắn RibbonBuilder)
- **Palette Run:** (không thấy PaletteCommands.Run)
- **Thân method:** `src/Modules/Tuyen/RouteModule.cs:51-51`

### 3. Chuỗi xử lý (tĩnh)
- CommandMethod → `AddOneCommand` → quan sát: thin-wrapper → AddOne @ src/Modules/Tuyen/RouteModule.cs:336-366; có kiểm tra/auth: EnsureAuthenticated; UI/prompt: Editor., Prompt; CAD tx: Database, ModelSpace; kiểu/service gọi: AuthManager, DocumentManager, RouteService, SurveyPaletteManager
- **Service/Dialog types trong thân:** AuthManager, DocumentManager, RouteService, SurveyPaletteManager
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
- **Thiếu bằng chứng fixture cụ thể:** ghi nhận cho `AUD-071` — chưa có seed audit riêng trong `_R02`.

### 7. Khả năng quay / rủi ro
- Modal/UI: chưa thấy modal trong thân
- Chuột/pick: có Prompt/Get*
- Async: không thấy trong thân
- Ứng viên video người dùng (cờ CSV): YES

### 8. Mức kết luận
- **Source:** `SOURCE_BEHAVIOR_REVIEWED`
- **Runtime:** `NOT_TESTED`
- **R01 refs:** ISURVEY_STATION_ADD_ONE

---

## AUD-072 — ISURVEY_TERRAIN_ADD

### 1. Định danh
- **feature_id:** `AUD-072`
- **Tên:** ISURVEY_TERRAIN_ADD
- **Nhóm:** UAV
- **Audience:** USER
- **Trạng thái:** `IMPLEMENTED` — CommandMethod + handler method found in source

### 2. Điểm kích hoạt
- **Lệnh chính:** `ISURVEY_TERRAIN_ADD` @ `src/Modules/Terrain/TerrainModule.cs:20` method `AddCommand`
- **Alias:** (không)
- **Ribbon:** (không / chưa gắn RibbonBuilder)
- **Palette Run:** (không thấy PaletteCommands.Run)
- **Thân method:** `src/Modules/Terrain/TerrainModule.cs:21-21`

### 3. Chuỗi xử lý (tĩnh)
- CommandMethod → `AddCommand` → quan sát: thin-wrapper → Add @ src/Modules/Terrain/TerrainModule.cs:102-163; có kiểm tra/auth: EnsureAuthenticated; UI/prompt: Editor., Prompt; CAD tx: Database, ModelSpace; kiểu/service gọi: AddPointSourcesWindow, AuthManager, DocumentManager, SurveyPaletteManager, TerrainService; catch (1): System.Exception ex
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
- **Thiếu bằng chứng fixture cụ thể:** ghi nhận cho `AUD-072` — chưa có seed audit riêng trong `_R02`.

### 7. Khả năng quay / rủi ro
- Modal/UI: chưa thấy modal trong thân
- Chuột/pick: có Prompt/Get*
- Async: không thấy trong thân
- Ứng viên video người dùng (cờ CSV): YES

### 8. Mức kết luận
- **Source:** `SOURCE_BEHAVIOR_REVIEWED`
- **Runtime:** `NOT_TESTED`
- **R01 refs:** ISURVEY_TERRAIN_ADD

---

## AUD-073 — ISURVEY_UAV_CLIP

### 1. Định danh
- **feature_id:** `AUD-073`
- **Tên:** ISURVEY_UAV_CLIP
- **Nhóm:** UAV
- **Audience:** USER
- **Trạng thái:** `IMPLEMENTED` — CommandMethod + handler method found in source

### 2. Điểm kích hoạt
- **Lệnh chính:** `ISURVEY_UAV_CLIP` @ `src/Modules/Uav/UavModule.cs:10` method `ClipCommand`
- **Alias:** UAVCLIP
- **Ribbon:** Cắt Ảnh Theo Biên|ISURVEY_UAV_CLIP|en=True|L421
- **Palette Run:** (không thấy PaletteCommands.Run)
- **Thân method:** `src/Modules/Uav/UavModule.cs:11-17`

### 3. Chuỗi xử lý (tĩnh)
- CommandMethod → `ClipCommand` → quan sát: có kiểm tra/auth: EnsureAuthenticated; CAD tx: Database; kiểu/service gọi: AuthManager, DocumentManager, UavImageService
- **Service/Dialog types trong thân:** AuthManager, DocumentManager, UavImageService
- **Giới hạn đọc:** Đã đọc thân command method; các service được gọi chưa được mở đầy đủ từng file trừ khi nằm trong cùng method.

### 4. Input / Output / CAD
- **UI/prompt:** (không thấy ShowModal/Prompt/WriteMessage trong thân đã đọc)
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
- **Thiếu bằng chứng fixture cụ thể:** ghi nhận cho `AUD-073` — chưa có seed audit riêng trong `_R02`.

### 7. Khả năng quay / rủi ro
- Modal/UI: chưa thấy modal trong thân
- Chuột/pick: chưa thấy pick trong thân
- Async: không thấy trong thân
- Ứng viên video người dùng (cờ CSV): YES

### 8. Mức kết luận
- **Source:** `SOURCE_BEHAVIOR_REVIEWED`
- **Runtime:** `NOT_TESTED`
- **R01 refs:** ISURVEY_UAV_CLIP

---

## AUD-074 — ISURVEY_UAV_CONTOUR

### 1. Định danh
- **feature_id:** `AUD-074`
- **Tên:** ISURVEY_UAV_CONTOUR
- **Nhóm:** UAV
- **Audience:** USER
- **Trạng thái:** `STUB` — method ends Stub / stub hint in body

### 2. Điểm kích hoạt
- **Lệnh chính:** `ISURVEY_UAV_CONTOUR` @ `src/Modules/Uav/UavModule.cs:43` method `ContourStub`
- **Alias:** DM
- **Ribbon:** Vẽ Đường Đồng Mức|ISURVEY_UAV_CONTOUR|en=True|L437
- **Palette Run:** (không thấy PaletteCommands.Run)
- **Thân method:** `src/Modules/Uav/UavModule.cs:44-44`

### 3. Chuỗi xử lý (tĩnh)
- CommandMethod → `ContourStub` → quan sát: stub/hint trong thân; thin-wrapper → Stub @ src/Modules/Uav/UavModule.cs:58-62; có kiểm tra/auth: EnsureAuthenticated; kiểu/service gọi: AuthManager, ShowAlertDialog
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
- **Thiếu bằng chứng fixture cụ thể:** ghi nhận cho `AUD-074` — chưa có seed audit riêng trong `_R02`.

### 7. Khả năng quay / rủi ro
- Modal/UI: có dấu hiệu modal/dialog
- Chuột/pick: chưa thấy pick trong thân
- Async: không thấy trong thân
- Ứng viên video người dùng (cờ CSV): NO

### 8. Mức kết luận
- **Source:** `STUB_ONLY`
- **Runtime:** `NOT_TESTED`
- **R01 refs:** ISURVEY_UAV_CONTOUR

---

## AUD-075 — ISURVEY_UAV_CRS

### 1. Định danh
- **feature_id:** `AUD-075`
- **Tên:** ISURVEY_UAV_CRS
- **Nhóm:** UAV
- **Audience:** USER
- **Trạng thái:** `STUB` — method ends Stub / stub hint in body

### 2. Điểm kích hoạt
- **Lệnh chính:** `ISURVEY_UAV_CRS` @ `src/Modules/Uav/UavModule.cs:46` method `CrsStub`
- **Alias:** HT
- **Ribbon:** Hệ Tọa Độ UAV|ISURVEY_UAV_CRS|en=True|L439
- **Palette Run:** (không thấy PaletteCommands.Run)
- **Thân method:** `src/Modules/Uav/UavModule.cs:47-47`

### 3. Chuỗi xử lý (tĩnh)
- CommandMethod → `CrsStub` → quan sát: stub/hint trong thân; thin-wrapper → Stub @ src/Modules/Uav/UavModule.cs:58-62; có kiểm tra/auth: EnsureAuthenticated; kiểu/service gọi: AuthManager, ShowAlertDialog
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
- **Thiếu bằng chứng fixture cụ thể:** ghi nhận cho `AUD-075` — chưa có seed audit riêng trong `_R02`.

### 7. Khả năng quay / rủi ro
- Modal/UI: có dấu hiệu modal/dialog
- Chuột/pick: chưa thấy pick trong thân
- Async: không thấy trong thân
- Ứng viên video người dùng (cờ CSV): NO

### 8. Mức kết luận
- **Source:** `STUB_ONLY`
- **Runtime:** `NOT_TESTED`
- **R01 refs:** ISURVEY_UAV_CRS

---

## AUD-076 — ISURVEY_UAV_DEM

### 1. Định danh
- **feature_id:** `AUD-076`
- **Tên:** ISURVEY_UAV_DEM
- **Nhóm:** UAV
- **Audience:** USER
- **Trạng thái:** `STUB` — method ends Stub / stub hint in body

### 2. Điểm kích hoạt
- **Lệnh chính:** `ISURVEY_UAV_DEM` @ `src/Modules/Uav/UavModule.cs:40` method `DemStub`
- **Alias:** DEM
- **Ribbon:** (không / chưa gắn RibbonBuilder)
- **Palette Run:** (không thấy PaletteCommands.Run)
- **Thân method:** `src/Modules/Uav/UavModule.cs:41-41`

### 3. Chuỗi xử lý (tĩnh)
- CommandMethod → `DemStub` → quan sát: stub/hint trong thân; thin-wrapper → Stub @ src/Modules/Uav/UavModule.cs:58-62; có kiểm tra/auth: EnsureAuthenticated; kiểu/service gọi: AuthManager, ShowAlertDialog
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
- **Thiếu bằng chứng fixture cụ thể:** ghi nhận cho `AUD-076` — chưa có seed audit riêng trong `_R02`.

### 7. Khả năng quay / rủi ro
- Modal/UI: có dấu hiệu modal/dialog
- Chuột/pick: chưa thấy pick trong thân
- Async: không thấy trong thân
- Ứng viên video người dùng (cờ CSV): NO

### 8. Mức kết luận
- **Source:** `STUB_ONLY`
- **Runtime:** `NOT_TESTED`
- **R01 refs:** ISURVEY_UAV_DEM

---

## AUD-077 — ISURVEY_UAV_FRAME_TOGGLE

### 1. Định danh
- **feature_id:** `AUD-077`
- **Tên:** ISURVEY_UAV_FRAME_TOGGLE
- **Nhóm:** UAV
- **Audience:** USER
- **Trạng thái:** `IMPLEMENTED` — CommandMethod + handler method found in source

### 2. Điểm kích hoạt
- **Lệnh chính:** `ISURVEY_UAV_FRAME_TOGGLE` @ `src/Modules/Uav/UavModule.cs:19` method `FrameToggleCommand`
- **Alias:** AK
- **Ribbon:** Tắt/Bật Khung Ảnh|ISURVEY_UAV_FRAME_TOGGLE|en=True|L423
- **Palette Run:** (không thấy PaletteCommands.Run)
- **Thân method:** `src/Modules/Uav/UavModule.cs:20-26`

### 3. Chuỗi xử lý (tĩnh)
- CommandMethod → `FrameToggleCommand` → quan sát: có kiểm tra/auth: EnsureAuthenticated; CAD tx: Database; kiểu/service gọi: AuthManager, DocumentManager, UavImageService
- **Service/Dialog types trong thân:** AuthManager, DocumentManager, UavImageService
- **Giới hạn đọc:** Đã đọc thân command method; các service được gọi chưa được mở đầy đủ từng file trừ khi nằm trong cùng method.

### 4. Input / Output / CAD
- **UI/prompt:** (không thấy ShowModal/Prompt/WriteMessage trong thân đã đọc)
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
- **Thiếu bằng chứng fixture cụ thể:** ghi nhận cho `AUD-077` — chưa có seed audit riêng trong `_R02`.

### 7. Khả năng quay / rủi ro
- Modal/UI: chưa thấy modal trong thân
- Chuột/pick: chưa thấy pick trong thân
- Async: không thấy trong thân
- Ứng viên video người dùng (cờ CSV): YES

### 8. Mức kết luận
- **Source:** `SOURCE_BEHAVIOR_REVIEWED`
- **Runtime:** `NOT_TESTED`
- **R01 refs:** ISURVEY_UAV_FRAME_TOGGLE

---

## AUD-078 — ISURVEY_UAV_INSERT

### 1. Định danh
- **feature_id:** `AUD-078`
- **Tên:** ISURVEY_UAV_INSERT
- **Nhóm:** UAV
- **Audience:** USER
- **Trạng thái:** `STUB` — method ends Stub / stub hint in body

### 2. Điểm kích hoạt
- **Lệnh chính:** `ISURVEY_UAV_INSERT` @ `src/Modules/Uav/UavModule.cs:37` method `InsertStub`
- **Alias:** AU
- **Ribbon:** (không / chưa gắn RibbonBuilder)
- **Palette Run:** (không thấy PaletteCommands.Run)
- **Thân method:** `src/Modules/Uav/UavModule.cs:38-38`

### 3. Chuỗi xử lý (tĩnh)
- CommandMethod → `InsertStub` → quan sát: stub/hint trong thân; thin-wrapper → Stub @ src/Modules/Uav/UavModule.cs:58-62; có kiểm tra/auth: EnsureAuthenticated; kiểu/service gọi: AuthManager, ShowAlertDialog
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
- **Thiếu bằng chứng fixture cụ thể:** ghi nhận cho `AUD-078` — chưa có seed audit riêng trong `_R02`.

### 7. Khả năng quay / rủi ro
- Modal/UI: có dấu hiệu modal/dialog
- Chuột/pick: chưa thấy pick trong thân
- Async: không thấy trong thân
- Ứng viên video người dùng (cờ CSV): NO

### 8. Mức kết luận
- **Source:** `STUB_ONLY`
- **Runtime:** `NOT_TESTED`
- **R01 refs:** ISURVEY_UAV_INSERT

---

## AUD-079 — ISURVEY_UAV_SEND_BACK

### 1. Định danh
- **feature_id:** `AUD-079`
- **Tên:** ISURVEY_UAV_SEND_BACK
- **Nhóm:** UAV
- **Audience:** USER
- **Trạng thái:** `STUB` — method ends Stub / stub hint in body

### 2. Điểm kích hoạt
- **Lệnh chính:** `ISURVEY_UAV_SEND_BACK` @ `src/Modules/Uav/UavModule.cs:28` method `SendBackCommand`
- **Alias:** AD
- **Ribbon:** Đưa Ảnh Xuống Dưới|ISURVEY_UAV_SEND_BACK|en=True|L425
- **Palette Run:** (không thấy PaletteCommands.Run)
- **Thân method:** `src/Modules/Uav/UavModule.cs:29-35`

### 3. Chuỗi xử lý (tĩnh)
- CommandMethod → `SendBackCommand` → quan sát: có kiểm tra/auth: EnsureAuthenticated; CAD tx: Database; kiểu/service gọi: AuthManager, DocumentManager, UavImageService
- **Service/Dialog types trong thân:** AuthManager, DocumentManager, UavImageService
- **Giới hạn đọc:** Đã đọc thân command method; các service được gọi chưa được mở đầy đủ từng file trừ khi nằm trong cùng method.

### 4. Input / Output / CAD
- **UI/prompt:** (không thấy ShowModal/Prompt/WriteMessage trong thân đã đọc)
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
- **Thiếu bằng chứng fixture cụ thể:** ghi nhận cho `AUD-079` — chưa có seed audit riêng trong `_R02`.

### 7. Khả năng quay / rủi ro
- Modal/UI: chưa thấy modal trong thân
- Chuột/pick: chưa thấy pick trong thân
- Async: không thấy trong thân
- Ứng viên video người dùng (cờ CSV): NO

### 8. Mức kết luận
- **Source:** `STUB_ONLY`
- **Runtime:** `NOT_TESTED`
- **R01 refs:** ISURVEY_UAV_SEND_BACK

---
