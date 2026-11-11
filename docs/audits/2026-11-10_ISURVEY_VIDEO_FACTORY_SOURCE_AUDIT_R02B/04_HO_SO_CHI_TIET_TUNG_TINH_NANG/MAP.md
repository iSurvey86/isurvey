# Nhóm MAP

Sinh 2026-11-10 — R02B.

## AUD-027 — ISURVEY_DELETE_GE

### 1. Định danh
- **feature_id:** `AUD-027`
- **Hành vi:** ISURVEY_DELETE_GE
- **Nhóm:** MAP | **Audience:** USER
- **source_status:** `SOURCE_PRESENT` — có CommandMethod + thân xử lý trong source (runtime NOT_TESTED)
- **R01 cmds:** ISURVEY_DELETE_GE
- **R02 refs:** AUD-028

### 2. Kích hoạt
- **Lệnh chính:** `ISURVEY_DELETE_GE` @ `src/Modules/Map/MapModule.cs:169`
- **Alias module (ngoài CommandAliases.cs):** (không)
- **Alias CommandAliases.cs:** XG
- **Effective method:** `DeleteGeCommand` span `170-176`
- **Ribbon:** Xóa Ảnh Vệ Tinh|ISURVEY_DELETE_GE|en=True|L156
- **Palette:** (không)
- **XAML mapped:** (không / UI dialog nội bộ — xem 03D)

### 3. Chuỗi xử lý
- ISURVEY_DELETE_GE @ src/Modules/Map/MapModule.cs:169 method `DeleteGeCommand` span 170-176
- types trong handler: AuthManager;DocumentManager
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

## AUD-028 — ISURVEY_MAP

### 1. Định danh
- **feature_id:** `AUD-028`
- **Hành vi:** ISURVEY_MAP
- **Nhóm:** MAP | **Audience:** USER
- **source_status:** `SOURCE_PRESENT` — có CommandMethod + thân xử lý trong source (runtime NOT_TESTED)
- **R01 cmds:** ISURVEY_MAP
- **R02 refs:** AUD-029

### 2. Kích hoạt
- **Lệnh chính:** `ISURVEY_MAP` @ `src/Modules/Map/MapModule.cs:25`
- **Alias module (ngoài CommandAliases.cs):** (không)
- **Alias CommandAliases.cs:** IG
- **Effective method:** `InsertMapCommand` span `26-30`
- **Ribbon:** (không)
- **Palette:** (không)
- **XAML mapped:** (không / UI dialog nội bộ — xem 03D)

### 3. Chuỗi xử lý
- ISURVEY_MAP @ src/Modules/Map/MapModule.cs:25 method `InsertMapCommand` span 26-30
- types trong handler: AuthManager
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

## AUD-029 — ISURVEY_MAP_SAT

### 1. Định danh
- **feature_id:** `AUD-029`
- **Hành vi:** ISURVEY_MAP_SAT
- **Nhóm:** MAP | **Audience:** USER
- **source_status:** `SOURCE_PRESENT` — có CommandMethod + thân xử lý trong source (runtime NOT_TESTED)
- **R01 cmds:** ISURVEY_MAP_SAT
- **R02 refs:** AUD-030

### 2. Kích hoạt
- **Lệnh chính:** `ISURVEY_MAP_SAT` @ `src/Modules/Map/MapModule.cs:32`
- **Alias module (ngoài CommandAliases.cs):** (không)
- **Alias CommandAliases.cs:** SG
- **Effective method:** `InsertSatelliteCommand` span `33-64`
- **Ribbon:** Chèn Ảnh Vệ Tinh|ISURVEY_MAP_SAT|en=True|L154
- **Palette:** (không)
- **XAML mapped:** (không / UI dialog nội bộ — xem 03D)

### 3. Chuỗi xử lý
- ISURVEY_MAP_SAT @ src/Modules/Map/MapModule.cs:32 method `InsertSatelliteCommand` span 33-64
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

## AUD-030 — ISURVEY_UPDATE_GE

### 1. Định danh
- **feature_id:** `AUD-030`
- **Hành vi:** ISURVEY_UPDATE_GE
- **Nhóm:** MAP | **Audience:** USER
- **source_status:** `SOURCE_PRESENT` — có CommandMethod + thân xử lý trong source (runtime NOT_TESTED)
- **R01 cmds:** ISURVEY_UPDATE_GE
- **R02 refs:** AUD-010

### 2. Kích hoạt
- **Lệnh chính:** `ISURVEY_UPDATE_GE` @ `src/Modules/Map/MapModule.cs:123`
- **Alias module (ngoài CommandAliases.cs):** (không)
- **Alias CommandAliases.cs:** UG
- **Effective method:** `UpdateGeCommand` span `124-167`
- **Ribbon:** Cập Nhật Ảnh GE|ISURVEY_UPDATE_GE|en=True|L155
- **Palette:** (không)
- **XAML mapped:** (không / UI dialog nội bộ — xem 03D)

### 3. Chuỗi xử lý
- ISURVEY_UPDATE_GE @ src/Modules/Map/MapModule.cs:123 method `UpdateGeCommand` span 124-167
- types trong handler: ActivityLogger;AuthManager;DocumentManager;UserSettingsStore
- merge: (không)
- service scan: UserSettingsStore@src/Core/UserSettingsStore.cs: đã mở src/Core/UserSettingsStore.cs (101 dòng), quét 2723 ký tự đầu; io=['Directory.CreateDirectory', 'File.Exists', 'File.ReadAllText', 'File.WriteAllText']; catch~0

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
