# Nhóm ROUTE

Sinh 2026-11-10 — R02B.

## AUD-034 — BANGCOC

### 1. Định danh
- **feature_id:** `AUD-034`
- **Hành vi:** BANGCOC
- **Nhóm:** ROUTE | **Audience:** USER
- **source_status:** `SOURCE_PRESENT` — có CommandMethod + thân xử lý trong source (runtime NOT_TESTED)
- **R01 cmds:** BANGCOC;ISURVEY_STATION_LIST
- **R02 refs:** AUD-039;AUD-045

### 2. Kích hoạt
- **Lệnh chính:** `BANGCOC` @ `src/Modules/Tuyen/RouteModule.cs:59`
- **Alias module (ngoài CommandAliases.cs):** ISURVEY_STATION_LIST
- **Alias CommandAliases.cs:** (không)
- **Effective method:** `ListStations` span `368-415`
- **Ribbon:** (không)
- **Palette:** (không)
- **XAML mapped:** (không / UI dialog nội bộ — xem 03D)

### 3. Chuỗi xử lý
- BANGCOC @ src/Modules/Tuyen/RouteModule.cs:59 method `ListStations` span 368-415
- types trong handler: AuthManager;DocumentManager;RouteService;StationListWindow;SurveyPaletteManager
- merge: thin-wrapper ListAlias→ListStations
- service scan: RouteService@src/Modules/Tuyen/RouteService.cs: đã mở src/Modules/Tuyen/RouteService.cs (806 dòng), quét 8000 ký tự đầu; io=['StartTransaction', 'WriteMessage']; catch~0
- service scan: StationListWindow@src/UI/Tuyen/StationListWindow.xaml.cs: đã mở src/UI/Tuyen/StationListWindow.xaml.cs (122 dòng), quét 3603 ký tự đầu; io=[]; catch~0

### 4. Input / Output / lỗi / rủi ro
- **Input:** CHƯA ĐỦ BẰNG CHỨNG đầy đủ — xem types_in_handler / service_trace CSV; cần đọc sâu khi quay.
- **Output / hậu điều kiện:** CHƯA ĐỦ BẰNG CHỨNG đầy đủ hậu điều kiện đo được.
- **Nhánh lỗi:** CHƯA ĐỦ BẰNG CHỨNG đủ mọi catch trong service layer.
- **Destructive / quyền:** CHƯA ĐỦ BẰNG CHỨNG — kiểm tra Delete/Erase/Purge trước pilot.
- **Giới hạn đọc:** Script đã quét handler + mở file type nếu tìm thấy; chưa đọc 100% service.

### 5. Fixture đề xuất
- WORK COPY Metric trắng; REFERENCE tùy feature — seed chưa có trong _R02B.

### 6. Video / bằng chứng
- **video_candidate:** YES (tham khảo — không phải sẵn sàng sản xuất)
- **muc_ket_luan_source:** `SERVICE_CHAIN_VERIFIED_STATIC`
- **runtime:** `NOT_TESTED`
- **Bằng chứng thủ công độc lập:** KHÔNG — chủ yếu script; một phần file:line từ extractor

---

## AUD-035 — DAODAUTUYEN

### 1. Định danh
- **feature_id:** `AUD-035`
- **Hành vi:** DAODAUTUYEN
- **Nhóm:** ROUTE | **Audience:** USER
- **source_status:** `SOURCE_PRESENT` — có CommandMethod + thân xử lý trong source (runtime NOT_TESTED)
- **R01 cmds:** DAODAUTUYEN;ISURVEY_ROUTE_REVERSE
- **R02 refs:** AUD-043;AUD-070

### 2. Kích hoạt
- **Lệnh chính:** `DAODAUTUYEN` @ `src/Modules/Tuyen/RouteModule.cs:35`
- **Alias module (ngoài CommandAliases.cs):** ISURVEY_ROUTE_REVERSE
- **Alias CommandAliases.cs:** (không)
- **Effective method:** `Reverse` span `264-280`
- **Ribbon:** (không)
- **Palette:** (không)
- **XAML mapped:** (không / UI dialog nội bộ — xem 03D)

### 3. Chuỗi xử lý
- DAODAUTUYEN @ src/Modules/Tuyen/RouteModule.cs:35 method `Reverse` span 264-280
- types trong handler: AuthManager;DocumentManager;RouteService;SurveyPaletteManager
- merge: thin-wrapper ReverseAlias→Reverse
- service scan: RouteService@src/Modules/Tuyen/RouteService.cs: đã mở src/Modules/Tuyen/RouteService.cs (806 dòng), quét 8000 ký tự đầu; io=['StartTransaction', 'WriteMessage']; catch~0

### 4. Input / Output / lỗi / rủi ro
- **Input:** CHƯA ĐỦ BẰNG CHỨNG đầy đủ — xem types_in_handler / service_trace CSV; cần đọc sâu khi quay.
- **Output / hậu điều kiện:** CHƯA ĐỦ BẰNG CHỨNG đầy đủ hậu điều kiện đo được.
- **Nhánh lỗi:** CHƯA ĐỦ BẰNG CHỨNG đủ mọi catch trong service layer.
- **Destructive / quyền:** CHƯA ĐỦ BẰNG CHỨNG — kiểm tra Delete/Erase/Purge trước pilot.
- **Giới hạn đọc:** Script đã quét handler + mở file type nếu tìm thấy; chưa đọc 100% service.

### 5. Fixture đề xuất
- WORK COPY Metric trắng; REFERENCE tùy feature — seed chưa có trong _R02B.

### 6. Video / bằng chứng
- **video_candidate:** YES (tham khảo — không phải sẵn sàng sản xuất)
- **muc_ket_luan_source:** `SERVICE_CHAIN_VERIFIED_STATIC`
- **runtime:** `NOT_TESTED`
- **Bằng chứng thủ công độc lập:** KHÔNG — chủ yếu script; một phần file:line từ extractor

---

## AUD-036 — ISURVEY_NUMBER_VERTICES

### 1. Định danh
- **feature_id:** `AUD-036`
- **Hành vi:** ISURVEY_NUMBER_VERTICES
- **Nhóm:** ROUTE | **Audience:** USER
- **source_status:** `SOURCE_PRESENT` — có CommandMethod + thân xử lý trong source (runtime NOT_TESTED)
- **R01 cmds:** ISURVEY_NUMBER_VERTICES
- **R02 refs:** AUD-040

### 2. Kích hoạt
- **Lệnh chính:** `ISURVEY_NUMBER_VERTICES` @ `src/Modules/Tuyen/VertexNumberingService.cs:37`
- **Alias module (ngoài CommandAliases.cs):** (không)
- **Alias CommandAliases.cs:** GOC
- **Effective method:** `NumberVerticesCommand` span `38-238`
- **Ribbon:** Đánh Số Góc Tuyến|ISURVEY_NUMBER_VERTICES|en=True|L198
- **Palette:** (không)
- **XAML mapped:** (không / UI dialog nội bộ — xem 03D)

### 3. Chuỗi xử lý
- ISURVEY_NUMBER_VERTICES @ src/Modules/Tuyen/VertexNumberingService.cs:37 method `NumberVerticesCommand` span 38-238
- types trong handler: ActivityLogger;AuthManager;DocumentManager;TransactionManager
- merge: (không)

### 4. Input / Output / lỗi / rủi ro
- **Input:** CHƯA ĐỦ BẰNG CHỨNG đầy đủ — xem types_in_handler / service_trace CSV; cần đọc sâu khi quay.
- **Output / hậu điều kiện:** CHƯA ĐỦ BẰNG CHỨNG đầy đủ hậu điều kiện đo được.
- **Nhánh lỗi:** CHƯA ĐỦ BẰNG CHỨNG đủ mọi catch trong service layer.
- **Destructive / quyền:** CHƯA ĐỦ BẰNG CHỨNG — kiểm tra Delete/Erase/Purge trước pilot.
- **Giới hạn đọc:** Script đã quét handler + mở file type nếu tìm thấy; chưa đọc 100% service.

### 5. Fixture đề xuất
- WORK COPY Metric trắng; REFERENCE tùy feature — seed chưa có trong _R02B.

### 6. Video / bằng chứng
- **video_candidate:** YES (tham khảo — không phải sẵn sàng sản xuất)
- **muc_ket_luan_source:** `HANDLER_TRACE_COMPLETE`
- **runtime:** `NOT_TESTED`
- **Bằng chứng thủ công độc lập:** KHÔNG — chủ yếu script; một phần file:line từ extractor

---

## AUD-037 — ISURVEY_STRIP_MAP

### 1. Định danh
- **feature_id:** `AUD-037`
- **Hành vi:** ISURVEY_STRIP_MAP
- **Nhóm:** ROUTE | **Audience:** USER
- **source_status:** `SOURCE_PRESENT` — có CommandMethod + thân xử lý trong source (runtime NOT_TESTED)
- **R01 cmds:** ISURVEY_STRIP_MAP
- **R02 refs:** AUD-031

### 2. Kích hoạt
- **Lệnh chính:** `ISURVEY_STRIP_MAP` @ `src/Modules/Tuyen/StripMapService.cs:27`
- **Alias module (ngoài CommandAliases.cs):** (không)
- **Alias CommandAliases.cs:** DUOITUYEN
- **Effective method:** `StripMapCommand` span `28-49`
- **Ribbon:** Duỗi Thẳng Tuyến|ISURVEY_STRIP_MAP|en=True|L200
- **Palette:** (không)
- **XAML mapped:** (không / UI dialog nội bộ — xem 03D)

### 3. Chuỗi xử lý
- ISURVEY_STRIP_MAP @ src/Modules/Tuyen/StripMapService.cs:27 method `StripMapCommand` span 28-49
- types trong handler: ActivityLogger;AuthManager;DocumentManager
- merge: (không)

### 4. Input / Output / lỗi / rủi ro
- **Input:** CHƯA ĐỦ BẰNG CHỨNG đầy đủ — xem types_in_handler / service_trace CSV; cần đọc sâu khi quay.
- **Output / hậu điều kiện:** CHƯA ĐỦ BẰNG CHỨNG đầy đủ hậu điều kiện đo được.
- **Nhánh lỗi:** CHƯA ĐỦ BẰNG CHỨNG đủ mọi catch trong service layer.
- **Destructive / quyền:** CHƯA ĐỦ BẰNG CHỨNG — kiểm tra Delete/Erase/Purge trước pilot.
- **Giới hạn đọc:** Script đã quét handler + mở file type nếu tìm thấy; chưa đọc 100% service.

### 5. Fixture đề xuất
- WORK COPY Metric trắng; REFERENCE tùy feature — seed chưa có trong _R02B.

### 6. Video / bằng chứng
- **video_candidate:** YES (tham khảo — không phải sẵn sàng sản xuất)
- **muc_ket_luan_source:** `HANDLER_TRACE_COMPLETE`
- **runtime:** `NOT_TESTED`
- **Bằng chứng thủ công độc lập:** KHÔNG — chủ yếu script; một phần file:line từ extractor

---

## AUD-038 — PHATSINHCOC

### 1. Định danh
- **feature_id:** `AUD-038`
- **Hành vi:** PHATSINHCOC
- **Nhóm:** ROUTE | **Audience:** USER
- **source_status:** `SOURCE_PRESENT` — có CommandMethod + thân xử lý trong source (runtime NOT_TESTED)
- **R01 cmds:** ISURVEY_ROUTE_STATIONS;PHATSINHCOC
- **R02 refs:** AUD-044;AUD-048

### 2. Kích hoạt
- **Lệnh chính:** `PHATSINHCOC` @ `src/Modules/Tuyen/RouteModule.cs:23`
- **Alias module (ngoài CommandAliases.cs):** ISURVEY_ROUTE_STATIONS
- **Alias CommandAliases.cs:** (không)
- **Effective method:** `Stations` span `194-262`
- **Ribbon:** (không)
- **Palette:** (không)
- **XAML mapped:** (không / UI dialog nội bộ — xem 03D)

### 3. Chuỗi xử lý
- PHATSINHCOC @ src/Modules/Tuyen/RouteModule.cs:23 method `Stations` span 194-262
- types trong handler: AuthManager;CreateStationsWindow;DocumentManager;RouteService;SurveyPaletteManager
- merge: thin-wrapper StationsAlias→Stations
- service scan: CreateStationsWindow@src/UI/Tuyen/CreateStationsWindow.xaml.cs: đã mở src/UI/Tuyen/CreateStationsWindow.xaml.cs (85 dòng), quét 2393 ký tự đầu; io=[]; catch~0
- service scan: RouteService@src/Modules/Tuyen/RouteService.cs: đã mở src/Modules/Tuyen/RouteService.cs (806 dòng), quét 8000 ký tự đầu; io=['StartTransaction', 'WriteMessage']; catch~0

### 4. Input / Output / lỗi / rủi ro
- **Input:** CHƯA ĐỦ BẰNG CHỨNG đầy đủ — xem types_in_handler / service_trace CSV; cần đọc sâu khi quay.
- **Output / hậu điều kiện:** CHƯA ĐỦ BẰNG CHỨNG đầy đủ hậu điều kiện đo được.
- **Nhánh lỗi:** CHƯA ĐỦ BẰNG CHỨNG đủ mọi catch trong service layer.
- **Destructive / quyền:** CHƯA ĐỦ BẰNG CHỨNG — kiểm tra Delete/Erase/Purge trước pilot.
- **Giới hạn đọc:** Script đã quét handler + mở file type nếu tìm thấy; chưa đọc 100% service.

### 5. Fixture đề xuất
- WORK COPY Metric trắng; REFERENCE tùy feature — seed chưa có trong _R02B.

### 6. Video / bằng chứng
- **video_candidate:** YES (tham khảo — không phải sẵn sàng sản xuất)
- **muc_ket_luan_source:** `SERVICE_CHAIN_VERIFIED_STATIC`
- **runtime:** `NOT_TESTED`
- **Bằng chứng thủ công độc lập:** KHÔNG — chủ yếu script; một phần file:line từ extractor

---

## AUD-039 — STT

### 1. Định danh
- **feature_id:** `AUD-039`
- **Hành vi:** STT
- **Nhóm:** ROUTE | **Audience:** USER
- **source_status:** `SOURCE_PRESENT` — có CommandMethod + thân xử lý trong source (runtime NOT_TESTED)
- **R01 cmds:** STT
- **R02 refs:** AUD-049

### 2. Kích hoạt
- **Lệnh chính:** `STT` @ `src/Modules/Tuyen/SequenceNumberService.cs:28`
- **Alias module (ngoài CommandAliases.cs):** (không)
- **Alias CommandAliases.cs:** (không)
- **Effective method:** `SequenceCommand` span `29-116`
- **Ribbon:** Đánh Số Thứ Tự|STT|en=True|L206
- **Palette:** (không)
- **XAML mapped:** (không / UI dialog nội bộ — xem 03D)

### 3. Chuỗi xử lý
- STT @ src/Modules/Tuyen/SequenceNumberService.cs:28 method `SequenceCommand` span 29-116
- types trong handler: ActivityLogger;AuthManager;DocumentManager;SequenceWindow;TransactionManager
- merge: (không)
- service scan: SequenceWindow@src/UI/Sequence/SequenceWindow.xaml.cs: đã mở src/UI/Sequence/SequenceWindow.xaml.cs (324 dòng), quét 8000 ký tự đầu; io=['StartTransaction']; catch~0

### 4. Input / Output / lỗi / rủi ro
- **Input:** CHƯA ĐỦ BẰNG CHỨNG đầy đủ — xem types_in_handler / service_trace CSV; cần đọc sâu khi quay.
- **Output / hậu điều kiện:** CHƯA ĐỦ BẰNG CHỨNG đầy đủ hậu điều kiện đo được.
- **Nhánh lỗi:** CHƯA ĐỦ BẰNG CHỨNG đủ mọi catch trong service layer.
- **Destructive / quyền:** CHƯA ĐỦ BẰNG CHỨNG — kiểm tra Delete/Erase/Purge trước pilot.
- **Giới hạn đọc:** Script đã quét handler + mở file type nếu tìm thấy; chưa đọc 100% service.

### 5. Fixture đề xuất
- WORK COPY Metric trắng; REFERENCE tùy feature — seed chưa có trong _R02B.

### 6. Video / bằng chứng
- **video_candidate:** YES (tham khảo — không phải sẵn sàng sản xuất)
- **muc_ket_luan_source:** `SERVICE_CHAIN_VERIFIED_STATIC`
- **runtime:** `NOT_TESTED`
- **Bằng chứng thủ công độc lập:** KHÔNG — chủ yếu script; một phần file:line từ extractor

---

## AUD-040 — TAOTUYEN

### 1. Định danh
- **feature_id:** `AUD-040`
- **Hành vi:** TAOTUYEN
- **Nhóm:** ROUTE | **Audience:** USER
- **source_status:** `SOURCE_PRESENT` — có CommandMethod + thân xử lý trong source (runtime NOT_TESTED)
- **R01 cmds:** ISURVEY_ROUTE_NEW;TAOTUYEN
- **R02 refs:** AUD-042;AUD-050

### 2. Kích hoạt
- **Lệnh chính:** `TAOTUYEN` @ `src/Modules/Tuyen/RouteModule.cs:17`
- **Alias module (ngoài CommandAliases.cs):** ISURVEY_ROUTE_NEW
- **Alias CommandAliases.cs:** (không)
- **Effective method:** `New` span `92-192`
- **Ribbon:** (không)
- **Palette:** TAOTUYEN|src/UI/Palette/PaletteViewModel.cs:L210
- **XAML mapped:** (không / UI dialog nội bộ — xem 03D)

### 3. Chuỗi xử lý
- TAOTUYEN @ src/Modules/Tuyen/RouteModule.cs:17 method `New` span 92-192
- types trong handler: AuthManager;CreateRouteWindow;DocumentManager;RouteService;RouteStore;SurveyPaletteManager;TerrainStore;TransactionManager
- merge: thin-wrapper RouteNewAlias→New
- service scan: CreateRouteWindow@src/UI/Tuyen/CreateRouteWindow.xaml.cs: đã mở src/UI/Tuyen/CreateRouteWindow.xaml.cs (114 dòng), quét 3246 ký tự đầu; io=[]; catch~0
- service scan: RouteService@src/Modules/Tuyen/RouteService.cs: đã mở src/Modules/Tuyen/RouteService.cs (806 dòng), quét 8000 ký tự đầu; io=['StartTransaction', 'WriteMessage']; catch~0
- service scan: RouteStore@src/Modules/Tuyen/RouteStore.cs: đã mở src/Modules/Tuyen/RouteStore.cs (113 dòng), quét 3687 ký tự đầu; io=['StartTransaction']; catch~1

### 4. Input / Output / lỗi / rủi ro
- **Input:** CHƯA ĐỦ BẰNG CHỨNG đầy đủ — xem types_in_handler / service_trace CSV; cần đọc sâu khi quay.
- **Output / hậu điều kiện:** CHƯA ĐỦ BẰNG CHỨNG đầy đủ hậu điều kiện đo được.
- **Nhánh lỗi:** CHƯA ĐỦ BẰNG CHỨNG đủ mọi catch trong service layer.
- **Destructive / quyền:** CHƯA ĐỦ BẰNG CHỨNG — kiểm tra Delete/Erase/Purge trước pilot.
- **Giới hạn đọc:** Script đã quét handler + mở file type nếu tìm thấy; chưa đọc 100% service.

### 5. Fixture đề xuất
- WORK COPY Metric trắng; REFERENCE tùy feature — seed chưa có trong _R02B.

### 6. Video / bằng chứng
- **video_candidate:** YES (tham khảo — không phải sẵn sàng sản xuất)
- **muc_ket_luan_source:** `SERVICE_CHAIN_VERIFIED_STATIC`
- **runtime:** `NOT_TESTED`
- **Bằng chứng thủ công độc lập:** KHÔNG — chủ yếu script; một phần file:line từ extractor

---

## AUD-041 — THEMCOC

### 1. Định danh
- **feature_id:** `AUD-041`
- **Hành vi:** THEMCOC
- **Nhóm:** ROUTE | **Audience:** USER
- **source_status:** `SOURCE_PRESENT` — có CommandMethod + thân xử lý trong source (runtime NOT_TESTED)
- **R01 cmds:** ISURVEY_STATION_ADD_ONE;THEMCOC
- **R02 refs:** AUD-051;AUD-071

### 2. Kích hoạt
- **Lệnh chính:** `THEMCOC` @ `src/Modules/Tuyen/RouteModule.cs:53`
- **Alias module (ngoài CommandAliases.cs):** ISURVEY_STATION_ADD_ONE
- **Alias CommandAliases.cs:** (không)
- **Effective method:** `AddOne` span `336-366`
- **Ribbon:** (không)
- **Palette:** (không)
- **XAML mapped:** (không / UI dialog nội bộ — xem 03D)

### 3. Chuỗi xử lý
- THEMCOC @ src/Modules/Tuyen/RouteModule.cs:53 method `AddOne` span 336-366
- types trong handler: AuthManager;DocumentManager;RouteService;SurveyPaletteManager
- merge: thin-wrapper AddOneAlias→AddOne
- service scan: RouteService@src/Modules/Tuyen/RouteService.cs: đã mở src/Modules/Tuyen/RouteService.cs (806 dòng), quét 8000 ký tự đầu; io=['StartTransaction', 'WriteMessage']; catch~0

### 4. Input / Output / lỗi / rủi ro
- **Input:** CHƯA ĐỦ BẰNG CHỨNG đầy đủ — xem types_in_handler / service_trace CSV; cần đọc sâu khi quay.
- **Output / hậu điều kiện:** CHƯA ĐỦ BẰNG CHỨNG đầy đủ hậu điều kiện đo được.
- **Nhánh lỗi:** CHƯA ĐỦ BẰNG CHỨNG đủ mọi catch trong service layer.
- **Destructive / quyền:** CHƯA ĐỦ BẰNG CHỨNG — kiểm tra Delete/Erase/Purge trước pilot.
- **Giới hạn đọc:** Script đã quét handler + mở file type nếu tìm thấy; chưa đọc 100% service.

### 5. Fixture đề xuất
- WORK COPY Metric trắng; REFERENCE tùy feature — seed chưa có trong _R02B.

### 6. Video / bằng chứng
- **video_candidate:** YES (tham khảo — không phải sẵn sàng sản xuất)
- **muc_ket_luan_source:** `SERVICE_CHAIN_VERIFIED_STATIC`
- **runtime:** `NOT_TESTED`
- **Bằng chứng thủ công độc lập:** KHÔNG — chủ yếu script; một phần file:line từ extractor

---

## AUD-042 — THIETLAPCOC

### 1. Định danh
- **feature_id:** `AUD-042`
- **Hành vi:** THIETLAPCOC
- **Nhóm:** ROUTE | **Audience:** USER
- **source_status:** `SOURCE_PRESENT` — có CommandMethod + thân xử lý trong source (runtime NOT_TESTED)
- **R01 cmds:** ISURVEY_STATION_STYLE;THIETLAPCOC
- **R02 refs:** AUD-047;AUD-052

### 2. Kích hoạt
- **Lệnh chính:** `THIETLAPCOC` @ `src/Modules/Tuyen/RouteModule.cs:41`
- **Alias module (ngoài CommandAliases.cs):** ISURVEY_STATION_STYLE
- **Alias CommandAliases.cs:** (không)
- **Effective method:** `StationStyle` span `62-90`
- **Ribbon:** (không)
- **Palette:** THIETLAPCOC|src/UI/Palette/PaletteViewModel.cs:L211;THIETLAPCOC|src/UI/Palette/PaletteViewModel.cs:L222
- **XAML mapped:** (không / UI dialog nội bộ — xem 03D)

### 3. Chuỗi xử lý
- THIETLAPCOC @ src/Modules/Tuyen/RouteModule.cs:41 method `StationStyle` span 62-90
- types trong handler: AuthManager;DocumentManager;StationStyleStore;StationStyleWindow
- merge: thin-wrapper StationStyleAlias→StationStyle
- service scan: StationStyleStore@src/Modules/Tuyen/StationStyleStore.cs: đã mở src/Modules/Tuyen/StationStyleStore.cs (52 dòng), quét 1465 ký tự đầu; io=['Directory.CreateDirectory', 'File.Exists', 'File.ReadAllText', 'File.WriteAllText']; catch~0
- service scan: StationStyleWindow@src/UI/Tuyen/StationStyleWindow.xaml.cs: đã mở src/UI/Tuyen/StationStyleWindow.xaml.cs (99 dòng), quét 3019 ký tự đầu; io=[]; catch~0

### 4. Input / Output / lỗi / rủi ro
- **Input:** CHƯA ĐỦ BẰNG CHỨNG đầy đủ — xem types_in_handler / service_trace CSV; cần đọc sâu khi quay.
- **Output / hậu điều kiện:** CHƯA ĐỦ BẰNG CHỨNG đầy đủ hậu điều kiện đo được.
- **Nhánh lỗi:** CHƯA ĐỦ BẰNG CHỨNG đủ mọi catch trong service layer.
- **Destructive / quyền:** CHƯA ĐỦ BẰNG CHỨNG — kiểm tra Delete/Erase/Purge trước pilot.
- **Giới hạn đọc:** Script đã quét handler + mở file type nếu tìm thấy; chưa đọc 100% service.

### 5. Fixture đề xuất
- WORK COPY Metric trắng; REFERENCE tùy feature — seed chưa có trong _R02B.

### 6. Video / bằng chứng
- **video_candidate:** YES (tham khảo — không phải sẵn sàng sản xuất)
- **muc_ket_luan_source:** `SERVICE_CHAIN_VERIFIED_STATIC`
- **runtime:** `NOT_TESTED`
- **Bằng chứng thủ công độc lập:** KHÔNG — chủ yếu script; một phần file:line từ extractor

---

## AUD-043 — XOACOC

### 1. Định danh
- **feature_id:** `AUD-043`
- **Hành vi:** XOACOC
- **Nhóm:** ROUTE | **Audience:** USER
- **source_status:** `SOURCE_PRESENT` — có CommandMethod + thân xử lý trong source (runtime NOT_TESTED)
- **R01 cmds:** ISURVEY_ROUTE_CLEAR_STATIONS;XOACOC
- **R02 refs:** AUD-041;AUD-053

### 2. Kích hoạt
- **Lệnh chính:** `XOACOC` @ `src/Modules/Tuyen/RouteModule.cs:29`
- **Alias module (ngoài CommandAliases.cs):** ISURVEY_ROUTE_CLEAR_STATIONS
- **Alias CommandAliases.cs:** (không)
- **Effective method:** `ClearStations` span `282-301`
- **Ribbon:** (không)
- **Palette:** (không)
- **XAML mapped:** (không / UI dialog nội bộ — xem 03D)

### 3. Chuỗi xử lý
- XOACOC @ src/Modules/Tuyen/RouteModule.cs:29 method `ClearStations` span 282-301
- types trong handler: AuthManager;DocumentManager;RouteService;SurveyPaletteManager
- merge: thin-wrapper ClearStationsAlias→ClearStations
- service scan: RouteService@src/Modules/Tuyen/RouteService.cs: đã mở src/Modules/Tuyen/RouteService.cs (806 dòng), quét 8000 ký tự đầu; io=['StartTransaction', 'WriteMessage']; catch~0

### 4. Input / Output / lỗi / rủi ro
- **Input:** CHƯA ĐỦ BẰNG CHỨNG đầy đủ — xem types_in_handler / service_trace CSV; cần đọc sâu khi quay.
- **Output / hậu điều kiện:** CHƯA ĐỦ BẰNG CHỨNG đầy đủ hậu điều kiện đo được.
- **Nhánh lỗi:** CHƯA ĐỦ BẰNG CHỨNG đủ mọi catch trong service layer.
- **Destructive / quyền:** CHƯA ĐỦ BẰNG CHỨNG — kiểm tra Delete/Erase/Purge trước pilot.
- **Giới hạn đọc:** Script đã quét handler + mở file type nếu tìm thấy; chưa đọc 100% service.

### 5. Fixture đề xuất
- WORK COPY Metric trắng; REFERENCE tùy feature — seed chưa có trong _R02B.

### 6. Video / bằng chứng
- **video_candidate:** YES (tham khảo — không phải sẵn sàng sản xuất)
- **muc_ket_luan_source:** `SERVICE_CHAIN_VERIFIED_STATIC`
- **runtime:** `NOT_TESTED`
- **Bằng chứng thủ công độc lập:** KHÔNG — chủ yếu script; một phần file:line từ extractor

---

## AUD-044 — XOACOC1

### 1. Định danh
- **feature_id:** `AUD-044`
- **Hành vi:** XOACOC1
- **Nhóm:** ROUTE | **Audience:** USER
- **source_status:** `SOURCE_PRESENT` — có CommandMethod + thân xử lý trong source (runtime NOT_TESTED)
- **R01 cmds:** ISURVEY_STATION_REMOVE_ONE;XOACOC1
- **R02 refs:** AUD-046;AUD-054

### 2. Kích hoạt
- **Lệnh chính:** `XOACOC1` @ `src/Modules/Tuyen/RouteModule.cs:47`
- **Alias module (ngoài CommandAliases.cs):** ISURVEY_STATION_REMOVE_ONE
- **Alias CommandAliases.cs:** (không)
- **Effective method:** `RemoveOne` span `303-334`
- **Ribbon:** (không)
- **Palette:** (không)
- **XAML mapped:** (không / UI dialog nội bộ — xem 03D)

### 3. Chuỗi xử lý
- XOACOC1 @ src/Modules/Tuyen/RouteModule.cs:47 method `RemoveOne` span 303-334
- types trong handler: AuthManager;DocumentManager;RouteService;SurveyPaletteManager
- merge: thin-wrapper RemoveOneAlias→RemoveOne
- service scan: RouteService@src/Modules/Tuyen/RouteService.cs: đã mở src/Modules/Tuyen/RouteService.cs (806 dòng), quét 8000 ký tự đầu; io=['StartTransaction', 'WriteMessage']; catch~0

### 4. Input / Output / lỗi / rủi ro
- **Input:** CHƯA ĐỦ BẰNG CHỨNG đầy đủ — xem types_in_handler / service_trace CSV; cần đọc sâu khi quay.
- **Output / hậu điều kiện:** CHƯA ĐỦ BẰNG CHỨNG đầy đủ hậu điều kiện đo được.
- **Nhánh lỗi:** CHƯA ĐỦ BẰNG CHỨNG đủ mọi catch trong service layer.
- **Destructive / quyền:** CHƯA ĐỦ BẰNG CHỨNG — kiểm tra Delete/Erase/Purge trước pilot.
- **Giới hạn đọc:** Script đã quét handler + mở file type nếu tìm thấy; chưa đọc 100% service.

### 5. Fixture đề xuất
- WORK COPY Metric trắng; REFERENCE tùy feature — seed chưa có trong _R02B.

### 6. Video / bằng chứng
- **video_candidate:** YES (tham khảo — không phải sẵn sàng sản xuất)
- **muc_ket_luan_source:** `SERVICE_CHAIN_VERIFIED_STATIC`
- **runtime:** `NOT_TESTED`
- **Bằng chứng thủ công độc lập:** KHÔNG — chủ yếu script; một phần file:line từ extractor

---
