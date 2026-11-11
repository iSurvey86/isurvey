# Nhóm ROUTE — hồ sơ từng feature_id

Sinh 2026-11-10 — phân tích tĩnh thân method (brace-match).

## AUD-039 — BANGCOC

### 1. Định danh
- **feature_id:** `AUD-039`
- **Tên:** BANGCOC
- **Nhóm:** ROUTE
- **Audience:** USER
- **Trạng thái:** `IMPLEMENTED` — CommandMethod + handler method found in source

### 2. Điểm kích hoạt
- **Lệnh chính:** `BANGCOC` @ `src/Modules/Tuyen/RouteModule.cs:59` method `ListAlias`
- **Alias:** (không)
- **Ribbon:** (không / chưa gắn RibbonBuilder)
- **Palette Run:** (không thấy PaletteCommands.Run)
- **Thân method:** `src/Modules/Tuyen/RouteModule.cs:60-60`

### 3. Chuỗi xử lý (tĩnh)
- CommandMethod → `ListAlias` → quan sát: thin-wrapper → ListStations @ src/Modules/Tuyen/RouteModule.cs:368-415; có kiểm tra/auth: EnsureAuthenticated; UI/prompt: Editor., Prompt; CAD tx: Database, ModelSpace; kiểu/service gọi: AuthManager, DocumentManager, RouteService, StationListWindow, SurveyPaletteManager; catch (1): System.Exception ex
- **Service/Dialog types trong thân:** AuthManager, DocumentManager, RouteService, StationListWindow, SurveyPaletteManager
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
- **Thiếu bằng chứng fixture cụ thể:** ghi nhận cho `AUD-039` — chưa có seed audit riêng trong `_R02`.

### 7. Khả năng quay / rủi ro
- Modal/UI: chưa thấy modal trong thân
- Chuột/pick: có Prompt/Get*
- Async: không thấy trong thân
- Ứng viên video người dùng (cờ CSV): YES

### 8. Mức kết luận
- **Source:** `SOURCE_BEHAVIOR_REVIEWED`
- **Runtime:** `NOT_TESTED`
- **R01 refs:** BANGCOC

---

## AUD-040 — ISURVEY_NUMBER_VERTICES

### 1. Định danh
- **feature_id:** `AUD-040`
- **Tên:** ISURVEY_NUMBER_VERTICES
- **Nhóm:** ROUTE
- **Audience:** USER
- **Trạng thái:** `IMPLEMENTED` — CommandMethod + handler method found in source

### 2. Điểm kích hoạt
- **Lệnh chính:** `ISURVEY_NUMBER_VERTICES` @ `src/Modules/Tuyen/VertexNumberingService.cs:37` method `NumberVerticesCommand`
- **Alias:** GOC
- **Ribbon:** Đánh Số Góc Tuyến|ISURVEY_NUMBER_VERTICES|en=True|L198
- **Palette Run:** (không thấy PaletteCommands.Run)
- **Thân method:** `src/Modules/Tuyen/VertexNumberingService.cs:38-238`

### 3. Chuỗi xử lý (tĩnh)
- CommandMethod → `NumberVerticesCommand` → quan sát: có kiểm tra/auth: EnsureAuthenticated; UI/prompt: GetEntity, GetString, Prompt, WriteMessage; CAD tx: Commit, Database, StartTransaction; kiểu/service gọi: ActivityLogger, AuthManager, DocumentManager, TransactionManager
- **Service/Dialog types trong thân:** ActivityLogger, AuthManager, DocumentManager, TransactionManager
- **Giới hạn đọc:** Đã đọc thân command method; các service được gọi chưa được mở đầy đủ từng file trừ khi nằm trong cùng method.

### 4. Input / Output / CAD
- **UI/prompt:** GetEntity, GetString, Prompt, WriteMessage
- **I/O file/network (trực tiếp):** (không thấy File/Http dialog API trực tiếp trong thân — có thể nằm service)
- **Transaction CAD (trực tiếp):** Commit, Database, StartTransaction
- **Auth gate:** EnsureAuthenticated
- **Đối tượng CAD / đơn vị / phiên bản phụ thuộc:** CHƯA ĐỦ BẰNG CHỨNG nếu không nằm trong thân command — cần đọc service layer khi quay video.
- **Hành động phá hủy:** CHƯA ĐỦ BẰNG CHỨNG đầy đủ; nếu thân có Delete/Erase sẽ nằm trong notes/services — kiểm tra lại trước pilot.

### 5. Nhánh lỗi
- **catch quan sát:** CHƯA ĐỦ BẰNG CHỨNG / không có catch trong thân command (lỗi có thể bubble hoặc xử lý ngoài)

### 6. Fixture / đo độc lập (đề xuất tĩnh)
- **MASTER/WORK COPY:** bản vẽ trắng Metric WORK COPY; không đụng file khách.
- **REFERENCE:** đo độc lập ngoài CAD khi feature xuất file; nếu chỉ ghi DWG — REFERENCE bằng checklist đối tượng.
- **Thiếu bằng chứng fixture cụ thể:** ghi nhận cho `AUD-040` — chưa có seed audit riêng trong `_R02`.

### 7. Khả năng quay / rủi ro
- Modal/UI: chưa thấy modal trong thân
- Chuột/pick: có Prompt/Get*
- Async: không thấy trong thân
- Ứng viên video người dùng (cờ CSV): YES

### 8. Mức kết luận
- **Source:** `SOURCE_BEHAVIOR_REVIEWED`
- **Runtime:** `NOT_TESTED`
- **R01 refs:** ISURVEY_NUMBER_VERTICES

---

## AUD-041 — ISURVEY_ROUTE_CLEAR_STATIONS

### 1. Định danh
- **feature_id:** `AUD-041`
- **Tên:** ISURVEY_ROUTE_CLEAR_STATIONS
- **Nhóm:** ROUTE
- **Audience:** USER
- **Trạng thái:** `IMPLEMENTED` — CommandMethod + handler method found in source

### 2. Điểm kích hoạt
- **Lệnh chính:** `ISURVEY_ROUTE_CLEAR_STATIONS` @ `src/Modules/Tuyen/RouteModule.cs:26` method `ClearStationsCommand`
- **Alias:** (không)
- **Ribbon:** (không / chưa gắn RibbonBuilder)
- **Palette Run:** (không thấy PaletteCommands.Run)
- **Thân method:** `src/Modules/Tuyen/RouteModule.cs:27-27`

### 3. Chuỗi xử lý (tĩnh)
- CommandMethod → `ClearStationsCommand` → quan sát: thin-wrapper → ClearStations @ src/Modules/Tuyen/RouteModule.cs:282-301; có kiểm tra/auth: EnsureAuthenticated; UI/prompt: Editor.; CAD tx: Database, ModelSpace; kiểu/service gọi: AuthManager, DocumentManager, RouteService, SurveyPaletteManager; stub/hint trong thân
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
- **Thiếu bằng chứng fixture cụ thể:** ghi nhận cho `AUD-041` — chưa có seed audit riêng trong `_R02`.

### 7. Khả năng quay / rủi ro
- Modal/UI: chưa thấy modal trong thân
- Chuột/pick: chưa thấy pick trong thân
- Async: không thấy trong thân
- Ứng viên video người dùng (cờ CSV): YES

### 8. Mức kết luận
- **Source:** `SOURCE_BEHAVIOR_REVIEWED`
- **Runtime:** `NOT_TESTED`
- **R01 refs:** ISURVEY_ROUTE_CLEAR_STATIONS

---

## AUD-042 — ISURVEY_ROUTE_NEW

### 1. Định danh
- **feature_id:** `AUD-042`
- **Tên:** ISURVEY_ROUTE_NEW
- **Nhóm:** ROUTE
- **Audience:** USER
- **Trạng thái:** `IMPLEMENTED` — CommandMethod + handler method found in source

### 2. Điểm kích hoạt
- **Lệnh chính:** `ISURVEY_ROUTE_NEW` @ `src/Modules/Tuyen/RouteModule.cs:14` method `RouteNewCommand`
- **Alias:** (không)
- **Ribbon:** (không / chưa gắn RibbonBuilder)
- **Palette Run:** (không thấy PaletteCommands.Run)
- **Thân method:** `src/Modules/Tuyen/RouteModule.cs:15-15`

### 3. Chuỗi xử lý (tĩnh)
- CommandMethod → `RouteNewCommand` → quan sát: thin-wrapper → New @ src/Modules/Tuyen/RouteModule.cs:92-192; có kiểm tra/auth: EnsureAuthenticated; UI/prompt: Editor., Prompt; CAD tx: Commit, Database, ModelSpace, StartTransaction; kiểu/service gọi: AuthManager, CreateRouteWindow, DocumentManager, RouteService, RouteStore, SurveyPaletteManager, TerrainStore, TransactionManager; catch (3): System.Exception; System.Exception; System.Exception ex
- **Service/Dialog types trong thân:** AuthManager, CreateRouteWindow, DocumentManager, RouteService, RouteStore, SurveyPaletteManager, TerrainStore, TransactionManager
- **Giới hạn đọc:** Đã đọc thân command method; các service được gọi chưa được mở đầy đủ từng file trừ khi nằm trong cùng method.

### 4. Input / Output / CAD
- **UI/prompt:** Editor., Prompt
- **I/O file/network (trực tiếp):** (không thấy File/Http dialog API trực tiếp trong thân — có thể nằm service)
- **Transaction CAD (trực tiếp):** Commit, Database, ModelSpace, StartTransaction
- **Auth gate:** EnsureAuthenticated
- **Đối tượng CAD / đơn vị / phiên bản phụ thuộc:** CHƯA ĐỦ BẰNG CHỨNG nếu không nằm trong thân command — cần đọc service layer khi quay video.
- **Hành động phá hủy:** CHƯA ĐỦ BẰNG CHỨNG đầy đủ; nếu thân có Delete/Erase sẽ nằm trong notes/services — kiểm tra lại trước pilot.

### 5. Nhánh lỗi
- **catch quan sát:** System.Exception; System.Exception; System.Exception ex

### 6. Fixture / đo độc lập (đề xuất tĩnh)
- **MASTER/WORK COPY:** bản vẽ trắng Metric WORK COPY; không đụng file khách.
- **REFERENCE:** đo độc lập ngoài CAD khi feature xuất file; nếu chỉ ghi DWG — REFERENCE bằng checklist đối tượng.
- **Thiếu bằng chứng fixture cụ thể:** ghi nhận cho `AUD-042` — chưa có seed audit riêng trong `_R02`.

### 7. Khả năng quay / rủi ro
- Modal/UI: chưa thấy modal trong thân
- Chuột/pick: có Prompt/Get*
- Async: không thấy trong thân
- Ứng viên video người dùng (cờ CSV): YES

### 8. Mức kết luận
- **Source:** `SOURCE_BEHAVIOR_REVIEWED`
- **Runtime:** `NOT_TESTED`
- **R01 refs:** ISURVEY_ROUTE_NEW

---

## AUD-043 — ISURVEY_ROUTE_REVERSE

### 1. Định danh
- **feature_id:** `AUD-043`
- **Tên:** ISURVEY_ROUTE_REVERSE
- **Nhóm:** ROUTE
- **Audience:** USER
- **Trạng thái:** `IMPLEMENTED` — CommandMethod + handler method found in source

### 2. Điểm kích hoạt
- **Lệnh chính:** `ISURVEY_ROUTE_REVERSE` @ `src/Modules/Tuyen/RouteModule.cs:32` method `ReverseCommand`
- **Alias:** (không)
- **Ribbon:** (không / chưa gắn RibbonBuilder)
- **Palette Run:** (không thấy PaletteCommands.Run)
- **Thân method:** `src/Modules/Tuyen/RouteModule.cs:33-33`

### 3. Chuỗi xử lý (tĩnh)
- CommandMethod → `ReverseCommand` → quan sát: thin-wrapper → Reverse @ src/Modules/Tuyen/RouteModule.cs:264-280; có kiểm tra/auth: EnsureAuthenticated; UI/prompt: Editor.; CAD tx: Database, ModelSpace; kiểu/service gọi: AuthManager, DocumentManager, RouteService, SurveyPaletteManager
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
- **Thiếu bằng chứng fixture cụ thể:** ghi nhận cho `AUD-043` — chưa có seed audit riêng trong `_R02`.

### 7. Khả năng quay / rủi ro
- Modal/UI: chưa thấy modal trong thân
- Chuột/pick: chưa thấy pick trong thân
- Async: không thấy trong thân
- Ứng viên video người dùng (cờ CSV): YES

### 8. Mức kết luận
- **Source:** `SOURCE_BEHAVIOR_REVIEWED`
- **Runtime:** `NOT_TESTED`
- **R01 refs:** ISURVEY_ROUTE_REVERSE

---

## AUD-044 — ISURVEY_ROUTE_STATIONS

### 1. Định danh
- **feature_id:** `AUD-044`
- **Tên:** ISURVEY_ROUTE_STATIONS
- **Nhóm:** ROUTE
- **Audience:** USER
- **Trạng thái:** `IMPLEMENTED` — CommandMethod + handler method found in source

### 2. Điểm kích hoạt
- **Lệnh chính:** `ISURVEY_ROUTE_STATIONS` @ `src/Modules/Tuyen/RouteModule.cs:20` method `StationsCommand`
- **Alias:** (không)
- **Ribbon:** (không / chưa gắn RibbonBuilder)
- **Palette Run:** (không thấy PaletteCommands.Run)
- **Thân method:** `src/Modules/Tuyen/RouteModule.cs:21-21`

### 3. Chuỗi xử lý (tĩnh)
- CommandMethod → `StationsCommand` → quan sát: thin-wrapper → Stations @ src/Modules/Tuyen/RouteModule.cs:194-262; có kiểm tra/auth: EnsureAuthenticated; UI/prompt: Editor., Prompt; CAD tx: Database, ModelSpace; kiểu/service gọi: AuthManager, CreateStationsWindow, DocumentManager, RouteService, SurveyPaletteManager; catch (2): System.Exception ex; System.Exception ex
- **Service/Dialog types trong thân:** AuthManager, CreateStationsWindow, DocumentManager, RouteService, SurveyPaletteManager
- **Giới hạn đọc:** Đã đọc thân command method; các service được gọi chưa được mở đầy đủ từng file trừ khi nằm trong cùng method.

### 4. Input / Output / CAD
- **UI/prompt:** Editor., Prompt
- **I/O file/network (trực tiếp):** (không thấy File/Http dialog API trực tiếp trong thân — có thể nằm service)
- **Transaction CAD (trực tiếp):** Database, ModelSpace
- **Auth gate:** EnsureAuthenticated
- **Đối tượng CAD / đơn vị / phiên bản phụ thuộc:** CHƯA ĐỦ BẰNG CHỨNG nếu không nằm trong thân command — cần đọc service layer khi quay video.
- **Hành động phá hủy:** CHƯA ĐỦ BẰNG CHỨNG đầy đủ; nếu thân có Delete/Erase sẽ nằm trong notes/services — kiểm tra lại trước pilot.

### 5. Nhánh lỗi
- **catch quan sát:** System.Exception ex; System.Exception ex

### 6. Fixture / đo độc lập (đề xuất tĩnh)
- **MASTER/WORK COPY:** bản vẽ trắng Metric WORK COPY; không đụng file khách.
- **REFERENCE:** đo độc lập ngoài CAD khi feature xuất file; nếu chỉ ghi DWG — REFERENCE bằng checklist đối tượng.
- **Thiếu bằng chứng fixture cụ thể:** ghi nhận cho `AUD-044` — chưa có seed audit riêng trong `_R02`.

### 7. Khả năng quay / rủi ro
- Modal/UI: chưa thấy modal trong thân
- Chuột/pick: có Prompt/Get*
- Async: không thấy trong thân
- Ứng viên video người dùng (cờ CSV): YES

### 8. Mức kết luận
- **Source:** `SOURCE_BEHAVIOR_REVIEWED`
- **Runtime:** `NOT_TESTED`
- **R01 refs:** ISURVEY_ROUTE_STATIONS

---

## AUD-045 — ISURVEY_STATION_LIST

### 1. Định danh
- **feature_id:** `AUD-045`
- **Tên:** ISURVEY_STATION_LIST
- **Nhóm:** ROUTE
- **Audience:** USER
- **Trạng thái:** `IMPLEMENTED` — CommandMethod + handler method found in source

### 2. Điểm kích hoạt
- **Lệnh chính:** `ISURVEY_STATION_LIST` @ `src/Modules/Tuyen/RouteModule.cs:56` method `ListCommand`
- **Alias:** (không)
- **Ribbon:** (không / chưa gắn RibbonBuilder)
- **Palette Run:** (không thấy PaletteCommands.Run)
- **Thân method:** `src/Modules/Tuyen/RouteModule.cs:57-57`

### 3. Chuỗi xử lý (tĩnh)
- CommandMethod → `ListCommand` → quan sát: thin-wrapper → ListStations @ src/Modules/Tuyen/RouteModule.cs:368-415; có kiểm tra/auth: EnsureAuthenticated; UI/prompt: Editor., Prompt; CAD tx: Database, ModelSpace; kiểu/service gọi: AuthManager, DocumentManager, RouteService, StationListWindow, SurveyPaletteManager; catch (1): System.Exception ex
- **Service/Dialog types trong thân:** AuthManager, DocumentManager, RouteService, StationListWindow, SurveyPaletteManager
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
- **Thiếu bằng chứng fixture cụ thể:** ghi nhận cho `AUD-045` — chưa có seed audit riêng trong `_R02`.

### 7. Khả năng quay / rủi ro
- Modal/UI: chưa thấy modal trong thân
- Chuột/pick: có Prompt/Get*
- Async: không thấy trong thân
- Ứng viên video người dùng (cờ CSV): YES

### 8. Mức kết luận
- **Source:** `SOURCE_BEHAVIOR_REVIEWED`
- **Runtime:** `NOT_TESTED`
- **R01 refs:** ISURVEY_STATION_LIST

---

## AUD-046 — ISURVEY_STATION_REMOVE_ONE

### 1. Định danh
- **feature_id:** `AUD-046`
- **Tên:** ISURVEY_STATION_REMOVE_ONE
- **Nhóm:** ROUTE
- **Audience:** USER
- **Trạng thái:** `IMPLEMENTED` — CommandMethod + handler method found in source

### 2. Điểm kích hoạt
- **Lệnh chính:** `ISURVEY_STATION_REMOVE_ONE` @ `src/Modules/Tuyen/RouteModule.cs:44` method `RemoveOneCommand`
- **Alias:** (không)
- **Ribbon:** (không / chưa gắn RibbonBuilder)
- **Palette Run:** (không thấy PaletteCommands.Run)
- **Thân method:** `src/Modules/Tuyen/RouteModule.cs:45-45`

### 3. Chuỗi xử lý (tĩnh)
- CommandMethod → `RemoveOneCommand` → quan sát: thin-wrapper → RemoveOne @ src/Modules/Tuyen/RouteModule.cs:303-334; có kiểm tra/auth: EnsureAuthenticated; UI/prompt: Editor., Prompt; CAD tx: ModelSpace; kiểu/service gọi: AuthManager, DocumentManager, RouteService, SurveyPaletteManager
- **Service/Dialog types trong thân:** AuthManager, DocumentManager, RouteService, SurveyPaletteManager
- **Giới hạn đọc:** Đã đọc thân command method; các service được gọi chưa được mở đầy đủ từng file trừ khi nằm trong cùng method.

### 4. Input / Output / CAD
- **UI/prompt:** Editor., Prompt
- **I/O file/network (trực tiếp):** (không thấy File/Http dialog API trực tiếp trong thân — có thể nằm service)
- **Transaction CAD (trực tiếp):** ModelSpace
- **Auth gate:** EnsureAuthenticated
- **Đối tượng CAD / đơn vị / phiên bản phụ thuộc:** CHƯA ĐỦ BẰNG CHỨNG nếu không nằm trong thân command — cần đọc service layer khi quay video.
- **Hành động phá hủy:** CHƯA ĐỦ BẰNG CHỨNG đầy đủ; nếu thân có Delete/Erase sẽ nằm trong notes/services — kiểm tra lại trước pilot.

### 5. Nhánh lỗi
- **catch quan sát:** CHƯA ĐỦ BẰNG CHỨNG / không có catch trong thân command (lỗi có thể bubble hoặc xử lý ngoài)

### 6. Fixture / đo độc lập (đề xuất tĩnh)
- **MASTER/WORK COPY:** bản vẽ trắng Metric WORK COPY; không đụng file khách.
- **REFERENCE:** đo độc lập ngoài CAD khi feature xuất file; nếu chỉ ghi DWG — REFERENCE bằng checklist đối tượng.
- **Thiếu bằng chứng fixture cụ thể:** ghi nhận cho `AUD-046` — chưa có seed audit riêng trong `_R02`.

### 7. Khả năng quay / rủi ro
- Modal/UI: chưa thấy modal trong thân
- Chuột/pick: có Prompt/Get*
- Async: không thấy trong thân
- Ứng viên video người dùng (cờ CSV): YES

### 8. Mức kết luận
- **Source:** `SOURCE_BEHAVIOR_REVIEWED`
- **Runtime:** `NOT_TESTED`
- **R01 refs:** ISURVEY_STATION_REMOVE_ONE

---

## AUD-047 — ISURVEY_STATION_STYLE

### 1. Định danh
- **feature_id:** `AUD-047`
- **Tên:** ISURVEY_STATION_STYLE
- **Nhóm:** ROUTE
- **Audience:** USER
- **Trạng thái:** `IMPLEMENTED` — CommandMethod + handler method found in source

### 2. Điểm kích hoạt
- **Lệnh chính:** `ISURVEY_STATION_STYLE` @ `src/Modules/Tuyen/RouteModule.cs:38` method `StationStyleCommand`
- **Alias:** (không)
- **Ribbon:** (không / chưa gắn RibbonBuilder)
- **Palette Run:** (không thấy PaletteCommands.Run)
- **Thân method:** `src/Modules/Tuyen/RouteModule.cs:39-39`

### 3. Chuỗi xử lý (tĩnh)
- CommandMethod → `StationStyleCommand` → quan sát: thin-wrapper → StationStyle @ src/Modules/Tuyen/RouteModule.cs:62-90; có kiểm tra/auth: EnsureAuthenticated; UI/prompt: Editor., Prompt; kiểu/service gọi: AuthManager, DocumentManager, StationStyleStore, StationStyleWindow; catch (1): System.Exception ex
- **Service/Dialog types trong thân:** AuthManager, DocumentManager, StationStyleStore, StationStyleWindow
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
- **Thiếu bằng chứng fixture cụ thể:** ghi nhận cho `AUD-047` — chưa có seed audit riêng trong `_R02`.

### 7. Khả năng quay / rủi ro
- Modal/UI: chưa thấy modal trong thân
- Chuột/pick: có Prompt/Get*
- Async: không thấy trong thân
- Ứng viên video người dùng (cờ CSV): YES

### 8. Mức kết luận
- **Source:** `SOURCE_BEHAVIOR_REVIEWED`
- **Runtime:** `NOT_TESTED`
- **R01 refs:** ISURVEY_STATION_STYLE

---

## AUD-048 — PHATSINHCOC

### 1. Định danh
- **feature_id:** `AUD-048`
- **Tên:** PHATSINHCOC
- **Nhóm:** ROUTE
- **Audience:** USER
- **Trạng thái:** `IMPLEMENTED` — CommandMethod + handler method found in source

### 2. Điểm kích hoạt
- **Lệnh chính:** `PHATSINHCOC` @ `src/Modules/Tuyen/RouteModule.cs:23` method `StationsAlias`
- **Alias:** (không)
- **Ribbon:** (không / chưa gắn RibbonBuilder)
- **Palette Run:** (không thấy PaletteCommands.Run)
- **Thân method:** `src/Modules/Tuyen/RouteModule.cs:24-24`

### 3. Chuỗi xử lý (tĩnh)
- CommandMethod → `StationsAlias` → quan sát: thin-wrapper → Stations @ src/Modules/Tuyen/RouteModule.cs:194-262; có kiểm tra/auth: EnsureAuthenticated; UI/prompt: Editor., Prompt; CAD tx: Database, ModelSpace; kiểu/service gọi: AuthManager, CreateStationsWindow, DocumentManager, RouteService, SurveyPaletteManager; catch (2): System.Exception ex; System.Exception ex
- **Service/Dialog types trong thân:** AuthManager, CreateStationsWindow, DocumentManager, RouteService, SurveyPaletteManager
- **Giới hạn đọc:** Đã đọc thân command method; các service được gọi chưa được mở đầy đủ từng file trừ khi nằm trong cùng method.

### 4. Input / Output / CAD
- **UI/prompt:** Editor., Prompt
- **I/O file/network (trực tiếp):** (không thấy File/Http dialog API trực tiếp trong thân — có thể nằm service)
- **Transaction CAD (trực tiếp):** Database, ModelSpace
- **Auth gate:** EnsureAuthenticated
- **Đối tượng CAD / đơn vị / phiên bản phụ thuộc:** CHƯA ĐỦ BẰNG CHỨNG nếu không nằm trong thân command — cần đọc service layer khi quay video.
- **Hành động phá hủy:** CHƯA ĐỦ BẰNG CHỨNG đầy đủ; nếu thân có Delete/Erase sẽ nằm trong notes/services — kiểm tra lại trước pilot.

### 5. Nhánh lỗi
- **catch quan sát:** System.Exception ex; System.Exception ex

### 6. Fixture / đo độc lập (đề xuất tĩnh)
- **MASTER/WORK COPY:** bản vẽ trắng Metric WORK COPY; không đụng file khách.
- **REFERENCE:** đo độc lập ngoài CAD khi feature xuất file; nếu chỉ ghi DWG — REFERENCE bằng checklist đối tượng.
- **Thiếu bằng chứng fixture cụ thể:** ghi nhận cho `AUD-048` — chưa có seed audit riêng trong `_R02`.

### 7. Khả năng quay / rủi ro
- Modal/UI: chưa thấy modal trong thân
- Chuột/pick: có Prompt/Get*
- Async: không thấy trong thân
- Ứng viên video người dùng (cờ CSV): YES

### 8. Mức kết luận
- **Source:** `SOURCE_BEHAVIOR_REVIEWED`
- **Runtime:** `NOT_TESTED`
- **R01 refs:** PHATSINHCOC

---

## AUD-049 — STT

### 1. Định danh
- **feature_id:** `AUD-049`
- **Tên:** STT
- **Nhóm:** ROUTE
- **Audience:** USER
- **Trạng thái:** `IMPLEMENTED` — CommandMethod + handler method found in source

### 2. Điểm kích hoạt
- **Lệnh chính:** `STT` @ `src/Modules/Tuyen/SequenceNumberService.cs:28` method `SequenceCommand`
- **Alias:** (không)
- **Ribbon:** Đánh Số Thứ Tự|STT|en=True|L206
- **Palette Run:** (không thấy PaletteCommands.Run)
- **Thân method:** `src/Modules/Tuyen/SequenceNumberService.cs:29-116`

### 3. Chuỗi xử lý (tĩnh)
- CommandMethod → `SequenceCommand` → quan sát: có kiểm tra/auth: EnsureAuthenticated; UI/prompt: Editor., GetPoint, Prompt, WriteMessage; CAD tx: Commit, Database, StartTransaction; kiểu/service gọi: ActivityLogger, AuthManager, DocumentManager, SequenceWindow, TransactionManager
- **Service/Dialog types trong thân:** ActivityLogger, AuthManager, DocumentManager, SequenceWindow, TransactionManager
- **Giới hạn đọc:** Đã đọc thân command method; các service được gọi chưa được mở đầy đủ từng file trừ khi nằm trong cùng method.

### 4. Input / Output / CAD
- **UI/prompt:** Editor., GetPoint, Prompt, WriteMessage
- **I/O file/network (trực tiếp):** (không thấy File/Http dialog API trực tiếp trong thân — có thể nằm service)
- **Transaction CAD (trực tiếp):** Commit, Database, StartTransaction
- **Auth gate:** EnsureAuthenticated
- **Đối tượng CAD / đơn vị / phiên bản phụ thuộc:** CHƯA ĐỦ BẰNG CHỨNG nếu không nằm trong thân command — cần đọc service layer khi quay video.
- **Hành động phá hủy:** CHƯA ĐỦ BẰNG CHỨNG đầy đủ; nếu thân có Delete/Erase sẽ nằm trong notes/services — kiểm tra lại trước pilot.

### 5. Nhánh lỗi
- **catch quan sát:** CHƯA ĐỦ BẰNG CHỨNG / không có catch trong thân command (lỗi có thể bubble hoặc xử lý ngoài)

### 6. Fixture / đo độc lập (đề xuất tĩnh)
- **MASTER/WORK COPY:** bản vẽ trắng Metric WORK COPY; không đụng file khách.
- **REFERENCE:** đo độc lập ngoài CAD khi feature xuất file; nếu chỉ ghi DWG — REFERENCE bằng checklist đối tượng.
- **Thiếu bằng chứng fixture cụ thể:** ghi nhận cho `AUD-049` — chưa có seed audit riêng trong `_R02`.

### 7. Khả năng quay / rủi ro
- Modal/UI: chưa thấy modal trong thân
- Chuột/pick: có Prompt/Get*
- Async: không thấy trong thân
- Ứng viên video người dùng (cờ CSV): YES

### 8. Mức kết luận
- **Source:** `SOURCE_BEHAVIOR_REVIEWED`
- **Runtime:** `NOT_TESTED`
- **R01 refs:** STT

---

## AUD-050 — TAOTUYEN

### 1. Định danh
- **feature_id:** `AUD-050`
- **Tên:** TAOTUYEN
- **Nhóm:** ROUTE
- **Audience:** USER
- **Trạng thái:** `IMPLEMENTED` — CommandMethod + handler method found in source

### 2. Điểm kích hoạt
- **Lệnh chính:** `TAOTUYEN` @ `src/Modules/Tuyen/RouteModule.cs:17` method `RouteNewAlias`
- **Alias:** (không)
- **Ribbon:** (không / chưa gắn RibbonBuilder)
- **Palette Run:** TAOTUYEN|src/UI/Palette/PaletteViewModel.cs:L210
- **Thân method:** `src/Modules/Tuyen/RouteModule.cs:18-18`

### 3. Chuỗi xử lý (tĩnh)
- CommandMethod → `RouteNewAlias` → quan sát: thin-wrapper → New @ src/Modules/Tuyen/RouteModule.cs:92-192; có kiểm tra/auth: EnsureAuthenticated; UI/prompt: Editor., Prompt; CAD tx: Commit, Database, ModelSpace, StartTransaction; kiểu/service gọi: AuthManager, CreateRouteWindow, DocumentManager, RouteService, RouteStore, SurveyPaletteManager, TerrainStore, TransactionManager; catch (3): System.Exception; System.Exception; System.Exception ex
- **Service/Dialog types trong thân:** AuthManager, CreateRouteWindow, DocumentManager, RouteService, RouteStore, SurveyPaletteManager, TerrainStore, TransactionManager
- **Giới hạn đọc:** Đã đọc thân command method; các service được gọi chưa được mở đầy đủ từng file trừ khi nằm trong cùng method.

### 4. Input / Output / CAD
- **UI/prompt:** Editor., Prompt
- **I/O file/network (trực tiếp):** (không thấy File/Http dialog API trực tiếp trong thân — có thể nằm service)
- **Transaction CAD (trực tiếp):** Commit, Database, ModelSpace, StartTransaction
- **Auth gate:** EnsureAuthenticated
- **Đối tượng CAD / đơn vị / phiên bản phụ thuộc:** CHƯA ĐỦ BẰNG CHỨNG nếu không nằm trong thân command — cần đọc service layer khi quay video.
- **Hành động phá hủy:** CHƯA ĐỦ BẰNG CHỨNG đầy đủ; nếu thân có Delete/Erase sẽ nằm trong notes/services — kiểm tra lại trước pilot.

### 5. Nhánh lỗi
- **catch quan sát:** System.Exception; System.Exception; System.Exception ex

### 6. Fixture / đo độc lập (đề xuất tĩnh)
- **MASTER/WORK COPY:** bản vẽ trắng Metric WORK COPY; không đụng file khách.
- **REFERENCE:** đo độc lập ngoài CAD khi feature xuất file; nếu chỉ ghi DWG — REFERENCE bằng checklist đối tượng.
- **Thiếu bằng chứng fixture cụ thể:** ghi nhận cho `AUD-050` — chưa có seed audit riêng trong `_R02`.

### 7. Khả năng quay / rủi ro
- Modal/UI: chưa thấy modal trong thân
- Chuột/pick: có Prompt/Get*
- Async: không thấy trong thân
- Ứng viên video người dùng (cờ CSV): YES

### 8. Mức kết luận
- **Source:** `SOURCE_BEHAVIOR_REVIEWED`
- **Runtime:** `NOT_TESTED`
- **R01 refs:** TAOTUYEN

---

## AUD-051 — THEMCOC

### 1. Định danh
- **feature_id:** `AUD-051`
- **Tên:** THEMCOC
- **Nhóm:** ROUTE
- **Audience:** USER
- **Trạng thái:** `IMPLEMENTED` — CommandMethod + handler method found in source

### 2. Điểm kích hoạt
- **Lệnh chính:** `THEMCOC` @ `src/Modules/Tuyen/RouteModule.cs:53` method `AddOneAlias`
- **Alias:** (không)
- **Ribbon:** (không / chưa gắn RibbonBuilder)
- **Palette Run:** (không thấy PaletteCommands.Run)
- **Thân method:** `src/Modules/Tuyen/RouteModule.cs:54-54`

### 3. Chuỗi xử lý (tĩnh)
- CommandMethod → `AddOneAlias` → quan sát: thin-wrapper → AddOne @ src/Modules/Tuyen/RouteModule.cs:336-366; có kiểm tra/auth: EnsureAuthenticated; UI/prompt: Editor., Prompt; CAD tx: Database, ModelSpace; kiểu/service gọi: AuthManager, DocumentManager, RouteService, SurveyPaletteManager
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
- **Thiếu bằng chứng fixture cụ thể:** ghi nhận cho `AUD-051` — chưa có seed audit riêng trong `_R02`.

### 7. Khả năng quay / rủi ro
- Modal/UI: chưa thấy modal trong thân
- Chuột/pick: có Prompt/Get*
- Async: không thấy trong thân
- Ứng viên video người dùng (cờ CSV): YES

### 8. Mức kết luận
- **Source:** `SOURCE_BEHAVIOR_REVIEWED`
- **Runtime:** `NOT_TESTED`
- **R01 refs:** THEMCOC

---

## AUD-052 — THIETLAPCOC

### 1. Định danh
- **feature_id:** `AUD-052`
- **Tên:** THIETLAPCOC
- **Nhóm:** ROUTE
- **Audience:** USER
- **Trạng thái:** `IMPLEMENTED` — CommandMethod + handler method found in source

### 2. Điểm kích hoạt
- **Lệnh chính:** `THIETLAPCOC` @ `src/Modules/Tuyen/RouteModule.cs:41` method `StationStyleAlias`
- **Alias:** (không)
- **Ribbon:** (không / chưa gắn RibbonBuilder)
- **Palette Run:** THIETLAPCOC|src/UI/Palette/PaletteViewModel.cs:L211;THIETLAPCOC|src/UI/Palette/PaletteViewModel.cs:L222
- **Thân method:** `src/Modules/Tuyen/RouteModule.cs:42-42`

### 3. Chuỗi xử lý (tĩnh)
- CommandMethod → `StationStyleAlias` → quan sát: thin-wrapper → StationStyle @ src/Modules/Tuyen/RouteModule.cs:62-90; có kiểm tra/auth: EnsureAuthenticated; UI/prompt: Editor., Prompt; kiểu/service gọi: AuthManager, DocumentManager, StationStyleStore, StationStyleWindow; catch (1): System.Exception ex
- **Service/Dialog types trong thân:** AuthManager, DocumentManager, StationStyleStore, StationStyleWindow
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
- **Thiếu bằng chứng fixture cụ thể:** ghi nhận cho `AUD-052` — chưa có seed audit riêng trong `_R02`.

### 7. Khả năng quay / rủi ro
- Modal/UI: chưa thấy modal trong thân
- Chuột/pick: có Prompt/Get*
- Async: không thấy trong thân
- Ứng viên video người dùng (cờ CSV): YES

### 8. Mức kết luận
- **Source:** `SOURCE_BEHAVIOR_REVIEWED`
- **Runtime:** `NOT_TESTED`
- **R01 refs:** THIETLAPCOC

---

## AUD-053 — XOACOC

### 1. Định danh
- **feature_id:** `AUD-053`
- **Tên:** XOACOC
- **Nhóm:** ROUTE
- **Audience:** USER
- **Trạng thái:** `IMPLEMENTED` — CommandMethod + handler method found in source

### 2. Điểm kích hoạt
- **Lệnh chính:** `XOACOC` @ `src/Modules/Tuyen/RouteModule.cs:29` method `ClearStationsAlias`
- **Alias:** (không)
- **Ribbon:** (không / chưa gắn RibbonBuilder)
- **Palette Run:** (không thấy PaletteCommands.Run)
- **Thân method:** `src/Modules/Tuyen/RouteModule.cs:30-30`

### 3. Chuỗi xử lý (tĩnh)
- CommandMethod → `ClearStationsAlias` → quan sát: thin-wrapper → ClearStations @ src/Modules/Tuyen/RouteModule.cs:282-301; có kiểm tra/auth: EnsureAuthenticated; UI/prompt: Editor.; CAD tx: Database, ModelSpace; kiểu/service gọi: AuthManager, DocumentManager, RouteService, SurveyPaletteManager; stub/hint trong thân
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
- **Thiếu bằng chứng fixture cụ thể:** ghi nhận cho `AUD-053` — chưa có seed audit riêng trong `_R02`.

### 7. Khả năng quay / rủi ro
- Modal/UI: chưa thấy modal trong thân
- Chuột/pick: chưa thấy pick trong thân
- Async: không thấy trong thân
- Ứng viên video người dùng (cờ CSV): YES

### 8. Mức kết luận
- **Source:** `SOURCE_BEHAVIOR_REVIEWED`
- **Runtime:** `NOT_TESTED`
- **R01 refs:** XOACOC

---

## AUD-054 — XOACOC1

### 1. Định danh
- **feature_id:** `AUD-054`
- **Tên:** XOACOC1
- **Nhóm:** ROUTE
- **Audience:** USER
- **Trạng thái:** `IMPLEMENTED` — CommandMethod + handler method found in source

### 2. Điểm kích hoạt
- **Lệnh chính:** `XOACOC1` @ `src/Modules/Tuyen/RouteModule.cs:47` method `RemoveOneAlias`
- **Alias:** (không)
- **Ribbon:** (không / chưa gắn RibbonBuilder)
- **Palette Run:** (không thấy PaletteCommands.Run)
- **Thân method:** `src/Modules/Tuyen/RouteModule.cs:48-48`

### 3. Chuỗi xử lý (tĩnh)
- CommandMethod → `RemoveOneAlias` → quan sát: thin-wrapper → RemoveOne @ src/Modules/Tuyen/RouteModule.cs:303-334; có kiểm tra/auth: EnsureAuthenticated; UI/prompt: Editor., Prompt; CAD tx: ModelSpace; kiểu/service gọi: AuthManager, DocumentManager, RouteService, SurveyPaletteManager
- **Service/Dialog types trong thân:** AuthManager, DocumentManager, RouteService, SurveyPaletteManager
- **Giới hạn đọc:** Đã đọc thân command method; các service được gọi chưa được mở đầy đủ từng file trừ khi nằm trong cùng method.

### 4. Input / Output / CAD
- **UI/prompt:** Editor., Prompt
- **I/O file/network (trực tiếp):** (không thấy File/Http dialog API trực tiếp trong thân — có thể nằm service)
- **Transaction CAD (trực tiếp):** ModelSpace
- **Auth gate:** EnsureAuthenticated
- **Đối tượng CAD / đơn vị / phiên bản phụ thuộc:** CHƯA ĐỦ BẰNG CHỨNG nếu không nằm trong thân command — cần đọc service layer khi quay video.
- **Hành động phá hủy:** CHƯA ĐỦ BẰNG CHỨNG đầy đủ; nếu thân có Delete/Erase sẽ nằm trong notes/services — kiểm tra lại trước pilot.

### 5. Nhánh lỗi
- **catch quan sát:** CHƯA ĐỦ BẰNG CHỨNG / không có catch trong thân command (lỗi có thể bubble hoặc xử lý ngoài)

### 6. Fixture / đo độc lập (đề xuất tĩnh)
- **MASTER/WORK COPY:** bản vẽ trắng Metric WORK COPY; không đụng file khách.
- **REFERENCE:** đo độc lập ngoài CAD khi feature xuất file; nếu chỉ ghi DWG — REFERENCE bằng checklist đối tượng.
- **Thiếu bằng chứng fixture cụ thể:** ghi nhận cho `AUD-054` — chưa có seed audit riêng trong `_R02`.

### 7. Khả năng quay / rủi ro
- Modal/UI: chưa thấy modal trong thân
- Chuột/pick: có Prompt/Get*
- Async: không thấy trong thân
- Ứng viên video người dùng (cờ CSV): YES

### 8. Mức kết luận
- **Source:** `SOURCE_BEHAVIOR_REVIEWED`
- **Runtime:** `NOT_TESTED`
- **R01 refs:** XOACOC1

---
