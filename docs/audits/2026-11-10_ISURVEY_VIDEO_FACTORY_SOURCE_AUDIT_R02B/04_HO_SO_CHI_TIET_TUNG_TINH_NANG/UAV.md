# Nhóm UAV

Sinh 2026-11-10 — R02B.

## AUD-054 — ISURVEY_PC_FILTER

### 1. Định danh
- **feature_id:** `AUD-054`
- **Hành vi:** ISURVEY_PC_FILTER
- **Nhóm:** UAV | **Audience:** USER
- **source_status:** `STUB` — stub hint/method
- **R01 cmds:** ISURVEY_PC_FILTER
- **R02 refs:** AUD-032

### 2. Kích hoạt
- **Lệnh chính:** `ISURVEY_PC_FILTER` @ `src/Modules/Uav/UavModule.cs:52`
- **Alias module (ngoài CommandAliases.cs):** (không)
- **Alias CommandAliases.cs:** LP
- **Effective method:** `PcFilterStub` span `53-53`
- **Ribbon:** Lọc Điểm Mặt Đất|ISURVEY_PC_FILTER|en=True|L447
- **Palette:** (không)
- **XAML mapped:** (không / UI dialog nội bộ — xem 03D)

### 3. Chuỗi xử lý
- ISURVEY_PC_FILTER @ src/Modules/Uav/UavModule.cs:52 method `PcFilterStub` span 53-53
- types trong handler: (không khớp *Service\|Dialog\|…)
- merge: stub-entry (không gộp qua helper Stub)

### 4. Input / Output / lỗi / rủi ro
- **Input:** CHƯA ĐỦ BẰNG CHỨNG đầy đủ — xem types_in_handler / service_trace CSV; cần đọc sâu khi quay.
- **Output / hậu điều kiện:** CHƯA ĐỦ BẰNG CHỨNG đầy đủ hậu điều kiện đo được.
- **Nhánh lỗi:** CHƯA ĐỦ BẰNG CHỨNG đủ mọi catch trong service layer.
- **Destructive / quyền:** CHƯA ĐỦ BẰNG CHỨNG — kiểm tra Delete/Erase/Purge trước pilot.
- **Giới hạn đọc:** Script đã quét handler + mở file type nếu tìm thấy; chưa đọc 100% service.

### 5. Fixture đề xuất
- WORK COPY Metric trắng; REFERENCE tùy feature — seed chưa có trong _R02B.

### 6. Video / bằng chứng
- **video_candidate:** NO (tham khảo — không phải sẵn sàng sản xuất)
- **muc_ket_luan_source:** `STUB_ONLY`
- **runtime:** `NOT_TESTED`
- **Bằng chứng thủ công độc lập:** KHÔNG — chủ yếu script; một phần file:line từ extractor

---

## AUD-055 — ISURVEY_PC_GRID

### 1. Định danh
- **feature_id:** `AUD-055`
- **Hành vi:** ISURVEY_PC_GRID
- **Nhóm:** UAV | **Audience:** USER
- **source_status:** `STUB` — stub hint/method
- **R01 cmds:** ISURVEY_PC_GRID
- **R02 refs:** AUD-033

### 2. Kích hoạt
- **Lệnh chính:** `ISURVEY_PC_GRID` @ `src/Modules/Uav/UavModule.cs:55`
- **Alias module (ngoài CommandAliases.cs):** (không)
- **Alias CommandAliases.cs:** RL
- **Effective method:** `PcGridStub` span `56-56`
- **Ribbon:** Rải Lưới Điểm|ISURVEY_PC_GRID|en=True|L449
- **Palette:** (không)
- **XAML mapped:** (không / UI dialog nội bộ — xem 03D)

### 3. Chuỗi xử lý
- ISURVEY_PC_GRID @ src/Modules/Uav/UavModule.cs:55 method `PcGridStub` span 56-56
- types trong handler: (không khớp *Service\|Dialog\|…)
- merge: stub-entry (không gộp qua helper Stub)

### 4. Input / Output / lỗi / rủi ro
- **Input:** CHƯA ĐỦ BẰNG CHỨNG đầy đủ — xem types_in_handler / service_trace CSV; cần đọc sâu khi quay.
- **Output / hậu điều kiện:** CHƯA ĐỦ BẰNG CHỨNG đầy đủ hậu điều kiện đo được.
- **Nhánh lỗi:** CHƯA ĐỦ BẰNG CHỨNG đủ mọi catch trong service layer.
- **Destructive / quyền:** CHƯA ĐỦ BẰNG CHỨNG — kiểm tra Delete/Erase/Purge trước pilot.
- **Giới hạn đọc:** Script đã quét handler + mở file type nếu tìm thấy; chưa đọc 100% service.

### 5. Fixture đề xuất
- WORK COPY Metric trắng; REFERENCE tùy feature — seed chưa có trong _R02B.

### 6. Video / bằng chứng
- **video_candidate:** NO (tham khảo — không phải sẵn sàng sản xuất)
- **muc_ket_luan_source:** `STUB_ONLY`
- **runtime:** `NOT_TESTED`
- **Bằng chứng thủ công độc lập:** KHÔNG — chủ yếu script; một phần file:line từ extractor

---

## AUD-056 — ISURVEY_PC_IMPORT

### 1. Định danh
- **feature_id:** `AUD-056`
- **Hành vi:** ISURVEY_PC_IMPORT
- **Nhóm:** UAV | **Audience:** USER
- **source_status:** `STUB` — stub hint/method
- **R01 cmds:** ISURVEY_PC_IMPORT
- **R02 refs:** AUD-034

### 2. Kích hoạt
- **Lệnh chính:** `ISURVEY_PC_IMPORT` @ `src/Modules/Uav/UavModule.cs:49`
- **Alias module (ngoài CommandAliases.cs):** (không)
- **Alias CommandAliases.cs:** NP
- **Effective method:** `PcImportStub` span `50-50`
- **Ribbon:** Nhập Point Cloud|ISURVEY_PC_IMPORT|en=True|L445
- **Palette:** (không)
- **XAML mapped:** (không / UI dialog nội bộ — xem 03D)

### 3. Chuỗi xử lý
- ISURVEY_PC_IMPORT @ src/Modules/Uav/UavModule.cs:49 method `PcImportStub` span 50-50
- types trong handler: (không khớp *Service\|Dialog\|…)
- merge: stub-entry (không gộp qua helper Stub)

### 4. Input / Output / lỗi / rủi ro
- **Input:** CHƯA ĐỦ BẰNG CHỨNG đầy đủ — xem types_in_handler / service_trace CSV; cần đọc sâu khi quay.
- **Output / hậu điều kiện:** CHƯA ĐỦ BẰNG CHỨNG đầy đủ hậu điều kiện đo được.
- **Nhánh lỗi:** CHƯA ĐỦ BẰNG CHỨNG đủ mọi catch trong service layer.
- **Destructive / quyền:** CHƯA ĐỦ BẰNG CHỨNG — kiểm tra Delete/Erase/Purge trước pilot.
- **Giới hạn đọc:** Script đã quét handler + mở file type nếu tìm thấy; chưa đọc 100% service.

### 5. Fixture đề xuất
- WORK COPY Metric trắng; REFERENCE tùy feature — seed chưa có trong _R02B.

### 6. Video / bằng chứng
- **video_candidate:** NO (tham khảo — không phải sẵn sàng sản xuất)
- **muc_ket_luan_source:** `STUB_ONLY`
- **runtime:** `NOT_TESTED`
- **Bằng chứng thủ công độc lập:** KHÔNG — chủ yếu script; một phần file:line từ extractor

---

## AUD-057 — ISURVEY_UAV_CLIP

### 1. Định danh
- **feature_id:** `AUD-057`
- **Hành vi:** ISURVEY_UAV_CLIP
- **Nhóm:** UAV | **Audience:** USER
- **source_status:** `SOURCE_PRESENT` — có CommandMethod + thân xử lý trong source (runtime NOT_TESTED)
- **R01 cmds:** ISURVEY_UAV_CLIP
- **R02 refs:** AUD-073

### 2. Kích hoạt
- **Lệnh chính:** `ISURVEY_UAV_CLIP` @ `src/Modules/Uav/UavModule.cs:10`
- **Alias module (ngoài CommandAliases.cs):** (không)
- **Alias CommandAliases.cs:** UAVCLIP
- **Effective method:** `ClipCommand` span `11-17`
- **Ribbon:** Cắt Ảnh Theo Biên|ISURVEY_UAV_CLIP|en=True|L421
- **Palette:** (không)
- **XAML mapped:** (không / UI dialog nội bộ — xem 03D)

### 3. Chuỗi xử lý
- CommandMethod ISURVEY_UAV_CLIP @ UavModule.cs:10 → ClipCommand
- UavImageService.ClipByBoundary @ UavImageService.cs:23
- RunBatchPolylineClip / RunAutoClip @ :43-45, :117, :254

### 4. Input / Output / lỗi / rủi ro
- **Input:** Raster/image trong DWG + biên polyline (batch hoặc auto).
- **Output / hậu điều kiện:** Ảnh bị clip theo biên; chi tiết entity CHƯA ĐỦ BẰNG CHỨNG đầy đủ mọi nhánh.
- **Nhánh lỗi:** Phụ thuộc Editor prompts trong service — cần đọc tiếp Run* methods.
- **Destructive / quyền:** Clip ảnh có thể đổi boundary clip của image (không xóa file gốc).
- **Giới hạn đọc:** Đã xác nhận entry ClipByBoundary + 2 nhánh; chi tiết từng prompt CHƯA ĐỦ BẰNG CHỨNG 100%.

### 5. Fixture đề xuất
- WORK COPY có ortho + polyline biên; REFERENCE so clip boundary.

### 6. Video / bằng chứng
- **video_candidate:** YES (tham khảo — không phải sẵn sàng sản xuất)
- **muc_ket_luan_source:** `SERVICE_CHAIN_VERIFIED_STATIC`
- **runtime:** `NOT_TESTED`
- **Bằng chứng thủ công độc lập:** CÓ (độc lập script)

---

## AUD-058 — ISURVEY_UAV_CONTOUR

### 1. Định danh
- **feature_id:** `AUD-058`
- **Hành vi:** ISURVEY_UAV_CONTOUR
- **Nhóm:** UAV | **Audience:** USER
- **source_status:** `STUB` — stub hint/method
- **R01 cmds:** ISURVEY_UAV_CONTOUR
- **R02 refs:** AUD-074

### 2. Kích hoạt
- **Lệnh chính:** `ISURVEY_UAV_CONTOUR` @ `src/Modules/Uav/UavModule.cs:43`
- **Alias module (ngoài CommandAliases.cs):** (không)
- **Alias CommandAliases.cs:** DM
- **Effective method:** `ContourStub` span `44-44`
- **Ribbon:** Vẽ Đường Đồng Mức|ISURVEY_UAV_CONTOUR|en=True|L437
- **Palette:** (không)
- **XAML mapped:** (không / UI dialog nội bộ — xem 03D)

### 3. Chuỗi xử lý
- ISURVEY_UAV_CONTOUR @ src/Modules/Uav/UavModule.cs:43 method `ContourStub` span 44-44
- types trong handler: (không khớp *Service\|Dialog\|…)
- merge: stub-entry (không gộp qua helper Stub)

### 4. Input / Output / lỗi / rủi ro
- **Input:** CHƯA ĐỦ BẰNG CHỨNG đầy đủ — xem types_in_handler / service_trace CSV; cần đọc sâu khi quay.
- **Output / hậu điều kiện:** CHƯA ĐỦ BẰNG CHỨNG đầy đủ hậu điều kiện đo được.
- **Nhánh lỗi:** CHƯA ĐỦ BẰNG CHỨNG đủ mọi catch trong service layer.
- **Destructive / quyền:** CHƯA ĐỦ BẰNG CHỨNG — kiểm tra Delete/Erase/Purge trước pilot.
- **Giới hạn đọc:** Script đã quét handler + mở file type nếu tìm thấy; chưa đọc 100% service.

### 5. Fixture đề xuất
- WORK COPY Metric trắng; REFERENCE tùy feature — seed chưa có trong _R02B.

### 6. Video / bằng chứng
- **video_candidate:** NO (tham khảo — không phải sẵn sàng sản xuất)
- **muc_ket_luan_source:** `STUB_ONLY`
- **runtime:** `NOT_TESTED`
- **Bằng chứng thủ công độc lập:** KHÔNG — chủ yếu script; một phần file:line từ extractor

---

## AUD-059 — ISURVEY_UAV_CRS

### 1. Định danh
- **feature_id:** `AUD-059`
- **Hành vi:** ISURVEY_UAV_CRS
- **Nhóm:** UAV | **Audience:** USER
- **source_status:** `STUB` — stub hint/method
- **R01 cmds:** ISURVEY_UAV_CRS
- **R02 refs:** AUD-075

### 2. Kích hoạt
- **Lệnh chính:** `ISURVEY_UAV_CRS` @ `src/Modules/Uav/UavModule.cs:46`
- **Alias module (ngoài CommandAliases.cs):** (không)
- **Alias CommandAliases.cs:** HT
- **Effective method:** `CrsStub` span `47-47`
- **Ribbon:** Hệ Tọa Độ UAV|ISURVEY_UAV_CRS|en=True|L439
- **Palette:** (không)
- **XAML mapped:** (không / UI dialog nội bộ — xem 03D)

### 3. Chuỗi xử lý
- ISURVEY_UAV_CRS @ src/Modules/Uav/UavModule.cs:46 method `CrsStub` span 47-47
- types trong handler: (không khớp *Service\|Dialog\|…)
- merge: stub-entry (không gộp qua helper Stub)

### 4. Input / Output / lỗi / rủi ro
- **Input:** CHƯA ĐỦ BẰNG CHỨNG đầy đủ — xem types_in_handler / service_trace CSV; cần đọc sâu khi quay.
- **Output / hậu điều kiện:** CHƯA ĐỦ BẰNG CHỨNG đầy đủ hậu điều kiện đo được.
- **Nhánh lỗi:** CHƯA ĐỦ BẰNG CHỨNG đủ mọi catch trong service layer.
- **Destructive / quyền:** CHƯA ĐỦ BẰNG CHỨNG — kiểm tra Delete/Erase/Purge trước pilot.
- **Giới hạn đọc:** Script đã quét handler + mở file type nếu tìm thấy; chưa đọc 100% service.

### 5. Fixture đề xuất
- WORK COPY Metric trắng; REFERENCE tùy feature — seed chưa có trong _R02B.

### 6. Video / bằng chứng
- **video_candidate:** NO (tham khảo — không phải sẵn sàng sản xuất)
- **muc_ket_luan_source:** `STUB_ONLY`
- **runtime:** `NOT_TESTED`
- **Bằng chứng thủ công độc lập:** KHÔNG — chủ yếu script; một phần file:line từ extractor

---

## AUD-060 — ISURVEY_UAV_DEM

### 1. Định danh
- **feature_id:** `AUD-060`
- **Hành vi:** ISURVEY_UAV_DEM
- **Nhóm:** UAV | **Audience:** USER
- **source_status:** `STUB` — stub hint/method
- **R01 cmds:** ISURVEY_UAV_DEM
- **R02 refs:** AUD-076

### 2. Kích hoạt
- **Lệnh chính:** `ISURVEY_UAV_DEM` @ `src/Modules/Uav/UavModule.cs:40`
- **Alias module (ngoài CommandAliases.cs):** (không)
- **Alias CommandAliases.cs:** DEM
- **Effective method:** `DemStub` span `41-41`
- **Ribbon:** (không)
- **Palette:** (không)
- **XAML mapped:** (không / UI dialog nội bộ — xem 03D)

### 3. Chuỗi xử lý
- ISURVEY_UAV_DEM @ src/Modules/Uav/UavModule.cs:40 method `DemStub` span 41-41
- types trong handler: (không khớp *Service\|Dialog\|…)
- merge: stub-entry (không gộp qua helper Stub)

### 4. Input / Output / lỗi / rủi ro
- **Input:** CHƯA ĐỦ BẰNG CHỨNG đầy đủ — xem types_in_handler / service_trace CSV; cần đọc sâu khi quay.
- **Output / hậu điều kiện:** CHƯA ĐỦ BẰNG CHỨNG đầy đủ hậu điều kiện đo được.
- **Nhánh lỗi:** CHƯA ĐỦ BẰNG CHỨNG đủ mọi catch trong service layer.
- **Destructive / quyền:** CHƯA ĐỦ BẰNG CHỨNG — kiểm tra Delete/Erase/Purge trước pilot.
- **Giới hạn đọc:** Script đã quét handler + mở file type nếu tìm thấy; chưa đọc 100% service.

### 5. Fixture đề xuất
- WORK COPY Metric trắng; REFERENCE tùy feature — seed chưa có trong _R02B.

### 6. Video / bằng chứng
- **video_candidate:** NO (tham khảo — không phải sẵn sàng sản xuất)
- **muc_ket_luan_source:** `STUB_ONLY`
- **runtime:** `NOT_TESTED`
- **Bằng chứng thủ công độc lập:** KHÔNG — chủ yếu script; một phần file:line từ extractor

---

## AUD-061 — ISURVEY_UAV_FRAME_TOGGLE

### 1. Định danh
- **feature_id:** `AUD-061`
- **Hành vi:** ISURVEY_UAV_FRAME_TOGGLE
- **Nhóm:** UAV | **Audience:** USER
- **source_status:** `SOURCE_PRESENT` — có CommandMethod + thân xử lý trong source (runtime NOT_TESTED)
- **R01 cmds:** ISURVEY_UAV_FRAME_TOGGLE
- **R02 refs:** AUD-077

### 2. Kích hoạt
- **Lệnh chính:** `ISURVEY_UAV_FRAME_TOGGLE` @ `src/Modules/Uav/UavModule.cs:19`
- **Alias module (ngoài CommandAliases.cs):** (không)
- **Alias CommandAliases.cs:** AK
- **Effective method:** `FrameToggleCommand` span `20-26`
- **Ribbon:** Tắt/Bật Khung Ảnh|ISURVEY_UAV_FRAME_TOGGLE|en=True|L423
- **Palette:** (không)
- **XAML mapped:** (không / UI dialog nội bộ — xem 03D)

### 3. Chuỗi xử lý
- ISURVEY_UAV_FRAME_TOGGLE @ src/Modules/Uav/UavModule.cs:19 method `FrameToggleCommand` span 20-26
- types trong handler: AuthManager;DocumentManager;UavImageService
- merge: (không)
- service scan: UavImageService@src/Modules/Uav/UavImageService.cs: đã mở src/Modules/Uav/UavImageService.cs (878 dòng), quét 8000 ký tự đầu; io=['StartTransaction', 'WriteMessage']; catch~2

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

## AUD-062 — ISURVEY_UAV_INSERT

### 1. Định danh
- **feature_id:** `AUD-062`
- **Hành vi:** ISURVEY_UAV_INSERT
- **Nhóm:** UAV | **Audience:** USER
- **source_status:** `STUB` — stub hint/method
- **R01 cmds:** ISURVEY_UAV_INSERT
- **R02 refs:** AUD-078

### 2. Kích hoạt
- **Lệnh chính:** `ISURVEY_UAV_INSERT` @ `src/Modules/Uav/UavModule.cs:37`
- **Alias module (ngoài CommandAliases.cs):** (không)
- **Alias CommandAliases.cs:** AU
- **Effective method:** `InsertStub` span `38-38`
- **Ribbon:** (không)
- **Palette:** (không)
- **XAML mapped:** (không / UI dialog nội bộ — xem 03D)

### 3. Chuỗi xử lý
- ISURVEY_UAV_INSERT @ src/Modules/Uav/UavModule.cs:37 method `InsertStub` span 38-38
- types trong handler: (không khớp *Service\|Dialog\|…)
- merge: stub-entry (không gộp qua helper Stub)

### 4. Input / Output / lỗi / rủi ro
- **Input:** CHƯA ĐỦ BẰNG CHỨNG đầy đủ — xem types_in_handler / service_trace CSV; cần đọc sâu khi quay.
- **Output / hậu điều kiện:** CHƯA ĐỦ BẰNG CHỨNG đầy đủ hậu điều kiện đo được.
- **Nhánh lỗi:** CHƯA ĐỦ BẰNG CHỨNG đủ mọi catch trong service layer.
- **Destructive / quyền:** CHƯA ĐỦ BẰNG CHỨNG — kiểm tra Delete/Erase/Purge trước pilot.
- **Giới hạn đọc:** Script đã quét handler + mở file type nếu tìm thấy; chưa đọc 100% service.

### 5. Fixture đề xuất
- WORK COPY Metric trắng; REFERENCE tùy feature — seed chưa có trong _R02B.

### 6. Video / bằng chứng
- **video_candidate:** NO (tham khảo — không phải sẵn sàng sản xuất)
- **muc_ket_luan_source:** `STUB_ONLY`
- **runtime:** `NOT_TESTED`
- **Bằng chứng thủ công độc lập:** KHÔNG — chủ yếu script; một phần file:line từ extractor

---

## AUD-063 — ISURVEY_UAV_SEND_BACK

### 1. Định danh
- **feature_id:** `AUD-063`
- **Hành vi:** ISURVEY_UAV_SEND_BACK
- **Nhóm:** UAV | **Audience:** USER
- **source_status:** `STUB` — stub hint/method
- **R01 cmds:** ISURVEY_UAV_SEND_BACK
- **R02 refs:** AUD-079

### 2. Kích hoạt
- **Lệnh chính:** `ISURVEY_UAV_SEND_BACK` @ `src/Modules/Uav/UavModule.cs:28`
- **Alias module (ngoài CommandAliases.cs):** (không)
- **Alias CommandAliases.cs:** AD
- **Effective method:** `SendBackCommand` span `29-35`
- **Ribbon:** Đưa Ảnh Xuống Dưới|ISURVEY_UAV_SEND_BACK|en=True|L425
- **Palette:** (không)
- **XAML mapped:** (không / UI dialog nội bộ — xem 03D)

### 3. Chuỗi xử lý
- ISURVEY_UAV_SEND_BACK @ src/Modules/Uav/UavModule.cs:28 method `SendBackCommand` span 29-35
- types trong handler: AuthManager;DocumentManager;UavImageService
- merge: (không)
- service scan: UavImageService@src/Modules/Uav/UavImageService.cs: đã mở src/Modules/Uav/UavImageService.cs (878 dòng), quét 8000 ký tự đầu; io=['StartTransaction', 'WriteMessage']; catch~2

### 4. Input / Output / lỗi / rủi ro
- **Input:** CHƯA ĐỦ BẰNG CHỨNG đầy đủ — xem types_in_handler / service_trace CSV; cần đọc sâu khi quay.
- **Output / hậu điều kiện:** CHƯA ĐỦ BẰNG CHỨNG đầy đủ hậu điều kiện đo được.
- **Nhánh lỗi:** CHƯA ĐỦ BẰNG CHỨNG đủ mọi catch trong service layer.
- **Destructive / quyền:** CHƯA ĐỦ BẰNG CHỨNG — kiểm tra Delete/Erase/Purge trước pilot.
- **Giới hạn đọc:** Script đã quét handler + mở file type nếu tìm thấy; chưa đọc 100% service.

### 5. Fixture đề xuất
- WORK COPY Metric trắng; REFERENCE tùy feature — seed chưa có trong _R02B.

### 6. Video / bằng chứng
- **video_candidate:** NO (tham khảo — không phải sẵn sàng sản xuất)
- **muc_ket_luan_source:** `STUB_ONLY`
- **runtime:** `NOT_TESTED`
- **Bằng chứng thủ công độc lập:** KHÔNG — chủ yếu script; một phần file:line từ extractor

---
