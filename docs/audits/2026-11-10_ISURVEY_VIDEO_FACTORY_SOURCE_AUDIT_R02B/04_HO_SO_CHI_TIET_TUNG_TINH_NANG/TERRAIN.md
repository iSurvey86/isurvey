# Nhóm TERRAIN

Sinh 2026-11-10 — R02B.

## AUD-047 — ANTIN

### 1. Định danh
- **feature_id:** `AUD-047`
- **Hành vi:** ANTIN
- **Nhóm:** TERRAIN | **Audience:** USER
- **source_status:** `SOURCE_PRESENT` — có CommandMethod + thân xử lý trong source (runtime NOT_TESTED)
- **R01 cmds:** ANTIN;ISURVEY_TERRAIN_TOGGLE
- **R02 refs:** AUD-056;AUD-062

### 2. Kích hoạt
- **Lệnh chính:** `ANTIN` @ `src/Modules/Terrain/TerrainModule.cs:35`
- **Alias module (ngoài CommandAliases.cs):** ISURVEY_TERRAIN_TOGGLE
- **Alias CommandAliases.cs:** (không)
- **Effective method:** `Toggle` span `273-301`
- **Ribbon:** Ẩn / Hiện Lưới TIN|ANTIN|en=True|L398
- **Palette:** (không)
- **XAML mapped:** (không / UI dialog nội bộ — xem 03D)

### 3. Chuỗi xử lý
- ANTIN @ src/Modules/Terrain/TerrainModule.cs:35 method `Toggle` span 273-301
- types trong handler: AuthManager;DocumentManager;SurveyPaletteManager;TerrainService;TerrainStore
- merge: thin-wrapper ToggleAlias→Toggle
- service scan: TerrainService@src/Modules/Terrain/TerrainService.cs: đã mở src/Modules/Terrain/TerrainService.cs (1182 dòng), quét 8000 ký tự đầu; io=['StartTransaction', 'WriteMessage']; catch~0
- service scan: TerrainStore@src/Modules/Terrain/TerrainStore.cs: đã mở src/Modules/Terrain/TerrainStore.cs (137 dòng), quét 4397 ký tự đầu; io=['StartTransaction']; catch~1

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

## AUD-048 — GOMOHINH

### 1. Định danh
- **feature_id:** `AUD-048`
- **Hành vi:** GOMOHINH
- **Nhóm:** TERRAIN | **Audience:** USER
- **source_status:** `SOURCE_PRESENT` — có CommandMethod + thân xử lý trong source (runtime NOT_TESTED)
- **R01 cmds:** GOMOHINH;ISURVEY_TERRAIN_DETACH
- **R02 refs:** AUD-057;AUD-060

### 2. Kích hoạt
- **Lệnh chính:** `GOMOHINH` @ `src/Modules/Terrain/TerrainModule.cs:47`
- **Alias module (ngoài CommandAliases.cs):** ISURVEY_TERRAIN_DETACH
- **Alias CommandAliases.cs:** (không)
- **Effective method:** `Detach` span `188-218`
- **Ribbon:** (không)
- **Palette:** (không)
- **XAML mapped:** (không / UI dialog nội bộ — xem 03D)

### 3. Chuỗi xử lý
- GOMOHINH @ src/Modules/Terrain/TerrainModule.cs:47 method `Detach` span 188-218
- types trong handler: AuthManager;DocumentManager;SurveyPaletteManager;TerrainService
- merge: thin-wrapper DetachAlias→Detach
- service scan: TerrainService@src/Modules/Terrain/TerrainService.cs: đã mở src/Modules/Terrain/TerrainService.cs (1182 dòng), quét 8000 ký tự đầu; io=['StartTransaction', 'WriteMessage']; catch~0

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

## AUD-049 — TAOMOHINH

### 1. Định danh
- **feature_id:** `AUD-049`
- **Hành vi:** TAOMOHINH
- **Nhóm:** TERRAIN | **Audience:** USER
- **source_status:** `SOURCE_PRESENT` — có CommandMethod + thân xử lý trong source (runtime NOT_TESTED)
- **R01 cmds:** ISURVEY_TERRAIN_NEW;TAOMOHINH
- **R02 refs:** AUD-061;AUD-064

### 2. Kích hoạt
- **Lệnh chính:** `TAOMOHINH` @ `src/Modules/Terrain/TerrainModule.cs:17`
- **Alias module (ngoài CommandAliases.cs):** ISURVEY_TERRAIN_NEW
- **Alias CommandAliases.cs:** (không)
- **Effective method:** `New` span `56-100`
- **Ribbon:** (không)
- **Palette:** TAOMOHINH|src/UI/Palette/PaletteViewModel.cs:L140
- **XAML mapped:** (không / UI dialog nội bộ — xem 03D)

### 3. Chuỗi xử lý
- TAOMOHINH @ src/Modules/Terrain/TerrainModule.cs:17 method `New` span 56-100
- types trong handler: AuthManager;CreateTerrainWindow;DocumentManager;SurveyPaletteManager;TerrainStore
- merge: thin-wrapper NewAlias→New
- service scan: CreateTerrainWindow@src/UI/Terrain/CreateTerrainWindow.xaml.cs: đã mở src/UI/Terrain/CreateTerrainWindow.xaml.cs (72 dòng), quét 1931 ký tự đầu; io=[]; catch~0
- service scan: TerrainStore@src/Modules/Terrain/TerrainStore.cs: đã mở src/Modules/Terrain/TerrainStore.cs (137 dòng), quét 4397 ký tự đầu; io=['StartTransaction']; catch~1

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

## AUD-050 — THEMMOHINH

### 1. Định danh
- **feature_id:** `AUD-050`
- **Hành vi:** THEMMOHINH
- **Nhóm:** TERRAIN | **Audience:** USER
- **source_status:** `SOURCE_PRESENT` — có CommandMethod + thân xử lý trong source (runtime NOT_TESTED)
- **R01 cmds:** ISURVEY_TERRAIN_ADD;THEMMOHINH
- **R02 refs:** AUD-065;AUD-072

### 2. Kích hoạt
- **Lệnh chính:** `THEMMOHINH` @ `src/Modules/Terrain/TerrainModule.cs:23`
- **Alias module (ngoài CommandAliases.cs):** ISURVEY_TERRAIN_ADD
- **Alias CommandAliases.cs:** (không)
- **Effective method:** `Add` span `102-163`
- **Ribbon:** (không)
- **Palette:** (không)
- **XAML mapped:** (không / UI dialog nội bộ — xem 03D)

### 3. Chuỗi xử lý
- THEMMOHINH @ src/Modules/Terrain/TerrainModule.cs:23 method `Add` span 102-163
- types trong handler: AddPointSourcesWindow;AuthManager;DocumentManager;SurveyPaletteManager;TerrainService
- merge: thin-wrapper AddAlias→Add
- service scan: AddPointSourcesWindow@src/UI/Terrain/AddPointSourcesWindow.xaml.cs: đã mở src/UI/Terrain/AddPointSourcesWindow.xaml.cs (135 dòng), quét 4070 ký tự đầu; io=[]; catch~0
- service scan: TerrainService@src/Modules/Terrain/TerrainService.cs: đã mở src/Modules/Terrain/TerrainService.cs (1182 dòng), quét 8000 ký tự đầu; io=['StartTransaction', 'WriteMessage']; catch~0

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

## AUD-051 — TRACAOD

### 1. Định danh
- **feature_id:** `AUD-051`
- **Hành vi:** TRACAOD
- **Nhóm:** TERRAIN | **Audience:** USER
- **source_status:** `SOURCE_PRESENT` — có CommandMethod + thân xử lý trong source (runtime NOT_TESTED)
- **R01 cmds:** ISURVEY_TERRAIN_Z;TRACAOD
- **R02 refs:** AUD-063;AUD-066

### 2. Kích hoạt
- **Lệnh chính:** `TRACAOD` @ `src/Modules/Terrain/TerrainModule.cs:41`
- **Alias module (ngoài CommandAliases.cs):** ISURVEY_TERRAIN_Z
- **Alias CommandAliases.cs:** (không)
- **Effective method:** `Query` span `303-344`
- **Ribbon:** (không)
- **Palette:** (không)
- **XAML mapped:** (không / UI dialog nội bộ — xem 03D)

### 3. Chuỗi xử lý
- TRACAOD @ src/Modules/Terrain/TerrainModule.cs:41 method `Query` span 303-344
- types trong handler: AuthManager;DocumentManager;TerrainService
- merge: thin-wrapper QueryAlias→Query
- service scan: TerrainService@src/Modules/Terrain/TerrainService.cs: đã mở src/Modules/Terrain/TerrainService.cs (1182 dòng), quét 8000 ký tự đầu; io=['StartTransaction', 'WriteMessage']; catch~0

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

## AUD-052 — XAYMOHINH

### 1. Định danh
- **feature_id:** `AUD-052`
- **Hành vi:** XAYMOHINH
- **Nhóm:** TERRAIN | **Audience:** USER
- **source_status:** `SOURCE_PRESENT` — có CommandMethod + thân xử lý trong source (runtime NOT_TESTED)
- **R01 cmds:** ISURVEY_TERRAIN_BUILD;XAYMOHINH
- **R02 refs:** AUD-058;AUD-067

### 2. Kích hoạt
- **Lệnh chính:** `XAYMOHINH` @ `src/Modules/Terrain/TerrainModule.cs:29`
- **Alias module (ngoài CommandAliases.cs):** ISURVEY_TERRAIN_BUILD
- **Alias CommandAliases.cs:** (không)
- **Effective method:** `Build` span `165-186`
- **Ribbon:** (không)
- **Palette:** (không)
- **XAML mapped:** (không / UI dialog nội bộ — xem 03D)

### 3. Chuỗi xử lý
- XAYMOHINH @ src/Modules/Terrain/TerrainModule.cs:29 method `Build` span 165-186
- types trong handler: AuthManager;DocumentManager;SurveyPaletteManager;TerrainService
- merge: thin-wrapper BuildAlias→Build
- service scan: TerrainService@src/Modules/Terrain/TerrainService.cs: đã mở src/Modules/Terrain/TerrainService.cs (1182 dòng), quét 8000 ký tự đầu; io=['StartTransaction', 'WriteMessage']; catch~0

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

## AUD-053 — XOATHANHPHAN

### 1. Định danh
- **feature_id:** `AUD-053`
- **Hành vi:** XOATHANHPHAN
- **Nhóm:** TERRAIN | **Audience:** USER
- **source_status:** `SOURCE_PRESENT` — có CommandMethod + thân xử lý trong source (runtime NOT_TESTED)
- **R01 cmds:** ISURVEY_TERRAIN_CLEAR;XOATHANHPHAN
- **R02 refs:** AUD-059;AUD-068

### 2. Kích hoạt
- **Lệnh chính:** `XOATHANHPHAN` @ `src/Modules/Terrain/TerrainModule.cs:53`
- **Alias module (ngoài CommandAliases.cs):** ISURVEY_TERRAIN_CLEAR
- **Alias CommandAliases.cs:** (không)
- **Effective method:** `Clear` span `220-271`
- **Ribbon:** (không)
- **Palette:** (không)
- **XAML mapped:** (không / UI dialog nội bộ — xem 03D)

### 3. Chuỗi xử lý
- XOATHANHPHAN @ src/Modules/Terrain/TerrainModule.cs:53 method `Clear` span 220-271
- types trong handler: AuthManager;DarkDialog;DocumentManager;SurveyPaletteManager;TerrainService
- merge: thin-wrapper ClearAlias→Clear
- service scan: DarkDialog@src/UI/Account/DarkDialog.xaml.cs: đã mở src/UI/Account/DarkDialog.xaml.cs (128 dòng), quét 3904 ký tự đầu; io=[]; catch~0
- service scan: TerrainService@src/Modules/Terrain/TerrainService.cs: đã mở src/Modules/Terrain/TerrainService.cs (1182 dòng), quét 8000 ký tự đầu; io=['StartTransaction', 'WriteMessage']; catch~0

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
