# Nhóm FRAME

Sinh 2026-11-10 — R02B.

## AUD-014 — ISURVEY_FIT_TITLE

### 1. Định danh
- **feature_id:** `AUD-014`
- **Hành vi:** ISURVEY_FIT_TITLE
- **Nhóm:** FRAME | **Audience:** USER
- **source_status:** `SOURCE_PRESENT` — có CommandMethod + thân xử lý trong source (runtime NOT_TESTED)
- **R01 cmds:** ISURVEY_FIT_TITLE
- **R02 refs:** AUD-015

### 2. Kích hoạt
- **Lệnh chính:** `ISURVEY_FIT_TITLE` @ `src/Modules/Frame/FrameService.cs:296`
- **Alias module (ngoài CommandAliases.cs):** (không)
- **Alias CommandAliases.cs:** CODAN
- **Effective method:** `FitTitleCommand` span `297-308`
- **Ribbon:** (không)
- **Palette:** (không)
- **XAML mapped:** (không / UI dialog nội bộ — xem 03D)

### 3. Chuỗi xử lý
- ISURVEY_FIT_TITLE @ src/Modules/Frame/FrameService.cs:296 method `FitTitleCommand` span 297-308
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

## AUD-015 — ISURVEY_FRAME_EDIT

### 1. Định danh
- **feature_id:** `AUD-015`
- **Hành vi:** ISURVEY_FRAME_EDIT
- **Nhóm:** FRAME | **Audience:** USER
- **source_status:** `SOURCE_PRESENT` — có CommandMethod + thân xử lý trong source (runtime NOT_TESTED)
- **R01 cmds:** ISURVEY_FRAME_EDIT
- **R02 refs:** AUD-016

### 2. Kích hoạt
- **Lệnh chính:** `ISURVEY_FRAME_EDIT` @ `src/Modules/Frame/FrameService.FrameEdit.cs:16`
- **Alias module (ngoài CommandAliases.cs):** (không)
- **Alias CommandAliases.cs:** SK
- **Effective method:** `EditFrameInfoCommand` span `17-53`
- **Ribbon:** (không)
- **Palette:** (không)
- **XAML mapped:** (không / UI dialog nội bộ — xem 03D)

### 3. Chuỗi xử lý
- ISURVEY_FRAME_EDIT @ src/Modules/Frame/FrameService.FrameEdit.cs:16 method `EditFrameInfoCommand` span 17-53
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

## AUD-016 — ISURVEY_KHUNG

### 1. Định danh
- **feature_id:** `AUD-016`
- **Hành vi:** ISURVEY_KHUNG
- **Nhóm:** FRAME | **Audience:** USER
- **source_status:** `SOURCE_PRESENT` — có CommandMethod + thân xử lý trong source (runtime NOT_TESTED)
- **R01 cmds:** ISURVEY_KHUNG
- **R02 refs:** AUD-017

### 2. Kích hoạt
- **Lệnh chính:** `ISURVEY_KHUNG` @ `src/Modules/Frame/FrameService.cs:54`
- **Alias module (ngoài CommandAliases.cs):** (không)
- **Alias CommandAliases.cs:** KHUNG
- **Effective method:** `FrameCommand` span `55-55`
- **Ribbon:** Khung Bản Đồ|ISURVEY_KHUNG|en=True|L174
- **Palette:** (không)
- **XAML mapped:** (không / UI dialog nội bộ — xem 03D)

### 3. Chuỗi xử lý
- ISURVEY_KHUNG @ src/Modules/Frame/FrameService.cs:54 method `FrameCommand` span 55-55
- types trong handler: (không khớp *Service\|Dialog\|…)
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
- **muc_ket_luan_source:** `METHOD_BODY_SCANNED`
- **runtime:** `NOT_TESTED`
- **Bằng chứng thủ công độc lập:** KHÔNG — chủ yếu script; một phần file:line từ extractor

---

## AUD-017 — ISURVEY_KHUNG_BV

### 1. Định danh
- **feature_id:** `AUD-017`
- **Hành vi:** ISURVEY_KHUNG_BV
- **Nhóm:** FRAME | **Audience:** USER
- **source_status:** `SOURCE_PRESENT` — có CommandMethod + thân xử lý trong source (runtime NOT_TESTED)
- **R01 cmds:** ISURVEY_KHUNG_BV
- **R02 refs:** AUD-018

### 2. Kích hoạt
- **Lệnh chính:** `ISURVEY_KHUNG_BV` @ `src/Modules/Frame/FrameService.cs:58`
- **Alias module (ngoài CommandAliases.cs):** (không)
- **Alias CommandAliases.cs:** KHUNGBV
- **Effective method:** `TechFrameCommand` span `59-59`
- **Ribbon:** Khung Bản Vẽ Kỹ Thuật|ISURVEY_KHUNG_BV|en=True|L176
- **Palette:** (không)
- **XAML mapped:** (không / UI dialog nội bộ — xem 03D)

### 3. Chuỗi xử lý
- ISURVEY_KHUNG_BV @ src/Modules/Frame/FrameService.cs:58 method `TechFrameCommand` span 59-59
- types trong handler: (không khớp *Service\|Dialog\|…)
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
- **muc_ket_luan_source:** `METHOD_BODY_SCANNED`
- **runtime:** `NOT_TESTED`
- **Bằng chứng thủ công độc lập:** KHÔNG — chủ yếu script; một phần file:line từ extractor

---

## AUD-018 — ISURVEY_LOGO

### 1. Định danh
- **feature_id:** `AUD-018`
- **Hành vi:** ISURVEY_LOGO
- **Nhóm:** FRAME | **Audience:** USER
- **source_status:** `SOURCE_PRESENT` — có CommandMethod + thân xử lý trong source (runtime NOT_TESTED)
- **R01 cmds:** ISURVEY_LOGO
- **R02 refs:** AUD-019

### 2. Kích hoạt
- **Lệnh chính:** `ISURVEY_LOGO` @ `src/Modules/Frame/FrameService.cs:62`
- **Alias module (ngoài CommandAliases.cs):** (không)
- **Alias CommandAliases.cs:** ANLOGO
- **Effective method:** `ToggleLogoCommand` span `63-74`
- **Ribbon:** (không)
- **Palette:** (không)
- **XAML mapped:** (không / UI dialog nội bộ — xem 03D)

### 3. Chuỗi xử lý
- ISURVEY_LOGO @ src/Modules/Frame/FrameService.cs:62 method `ToggleLogoCommand` span 63-74
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

## AUD-019 — ISURVEY_LOGO1

### 1. Định danh
- **feature_id:** `AUD-019`
- **Hành vi:** ISURVEY_LOGO1
- **Nhóm:** FRAME | **Audience:** USER
- **source_status:** `SOURCE_PRESENT` — có CommandMethod + thân xử lý trong source (runtime NOT_TESTED)
- **R01 cmds:** ISURVEY_LOGO1
- **R02 refs:** AUD-020

### 2. Kích hoạt
- **Lệnh chính:** `ISURVEY_LOGO1` @ `src/Modules/Frame/FrameService.cs:76`
- **Alias module (ngoài CommandAliases.cs):** (không)
- **Alias CommandAliases.cs:** ANLOGO1
- **Effective method:** `ToggleLogoOwnerCommand` span `77-77`
- **Ribbon:** (không)
- **Palette:** (không)
- **XAML mapped:** (không / UI dialog nội bộ — xem 03D)

### 3. Chuỗi xử lý
- ISURVEY_LOGO1 @ src/Modules/Frame/FrameService.cs:76 method `ToggleLogoOwnerCommand` span 77-77
- types trong handler: (không khớp *Service\|Dialog\|…)
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
- **muc_ket_luan_source:** `METHOD_BODY_SCANNED`
- **runtime:** `NOT_TESTED`
- **Bằng chứng thủ công độc lập:** KHÔNG — chủ yếu script; một phần file:line từ extractor

---

## AUD-020 — ISURVEY_LOGO2

### 1. Định danh
- **feature_id:** `AUD-020`
- **Hành vi:** ISURVEY_LOGO2
- **Nhóm:** FRAME | **Audience:** USER
- **source_status:** `SOURCE_PRESENT` — có CommandMethod + thân xử lý trong source (runtime NOT_TESTED)
- **R01 cmds:** ISURVEY_LOGO2
- **R02 refs:** AUD-021

### 2. Kích hoạt
- **Lệnh chính:** `ISURVEY_LOGO2` @ `src/Modules/Frame/FrameService.cs:79`
- **Alias module (ngoài CommandAliases.cs):** (không)
- **Alias CommandAliases.cs:** ANLOGO2
- **Effective method:** `ToggleLogoDesignCommand` span `80-80`
- **Ribbon:** (không)
- **Palette:** (không)
- **XAML mapped:** (không / UI dialog nội bộ — xem 03D)

### 3. Chuỗi xử lý
- ISURVEY_LOGO2 @ src/Modules/Frame/FrameService.cs:79 method `ToggleLogoDesignCommand` span 80-80
- types trong handler: (không khớp *Service\|Dialog\|…)
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
- **muc_ket_luan_source:** `METHOD_BODY_SCANNED`
- **runtime:** `NOT_TESTED`
- **Bằng chứng thủ công độc lập:** KHÔNG — chủ yếu script; một phần file:line từ extractor

---

## AUD-021 — ISURVEY_LOGO_REPLACE

### 1. Định danh
- **feature_id:** `AUD-021`
- **Hành vi:** ISURVEY_LOGO_REPLACE
- **Nhóm:** FRAME | **Audience:** USER
- **source_status:** `SOURCE_PRESENT` — có CommandMethod + thân xử lý trong source (runtime NOT_TESTED)
- **R01 cmds:** ISURVEY_LOGO_REPLACE
- **R02 refs:** AUD-022

### 2. Kích hoạt
- **Lệnh chính:** `ISURVEY_LOGO_REPLACE` @ `src/Modules/Frame/FrameService.cs:130`
- **Alias module (ngoài CommandAliases.cs):** (không)
- **Alias CommandAliases.cs:** THAYLOGO
- **Effective method:** `ReplaceLogoCommand` span `131-148`
- **Ribbon:** (không)
- **Palette:** (không)
- **XAML mapped:** (không / UI dialog nội bộ — xem 03D)

### 3. Chuỗi xử lý
- ISURVEY_LOGO_REPLACE @ src/Modules/Frame/FrameService.cs:130 method `ReplaceLogoCommand` span 131-148
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

## AUD-022 — ISURVEY_PROFILE_SHEETS

### 1. Định danh
- **feature_id:** `AUD-022`
- **Hành vi:** ISURVEY_PROFILE_SHEETS
- **Nhóm:** FRAME | **Audience:** USER
- **source_status:** `SOURCE_PRESENT` — có CommandMethod + thân xử lý trong source (runtime NOT_TESTED)
- **R01 cmds:** ISURVEY_PROFILE_SHEETS
- **R02 refs:** AUD-023

### 2. Kích hoạt
- **Lệnh chính:** `ISURVEY_PROFILE_SHEETS` @ `src/Modules/Frame/FrameService.ProfileLayout.cs:28`
- **Alias module (ngoài CommandAliases.cs):** (không)
- **Alias CommandAliases.cs:** RAIMCD
- **Effective method:** `ProfileLayoutCommand` span `29-29`
- **Ribbon:** Rải Khung Mặt Cắt|RAIMCD|en=True|L186
- **Palette:** (không)
- **XAML mapped:** (không / UI dialog nội bộ — xem 03D)

### 3. Chuỗi xử lý
- ISURVEY_PROFILE_SHEETS @ src/Modules/Frame/FrameService.ProfileLayout.cs:28 method `ProfileLayoutCommand` span 29-29
- types trong handler: (không khớp *Service\|Dialog\|…)
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
- **muc_ket_luan_source:** `METHOD_BODY_SCANNED`
- **runtime:** `NOT_TESTED`
- **Bằng chứng thủ công độc lập:** KHÔNG — chủ yếu script; một phần file:line từ extractor

---

## AUD-023 — ISURVEY_ROUTE_PLOT

### 1. Định danh
- **feature_id:** `AUD-023`
- **Hành vi:** ISURVEY_ROUTE_PLOT
- **Nhóm:** FRAME | **Audience:** USER
- **source_status:** `SOURCE_PRESENT` — có CommandMethod + thân xử lý trong source (runtime NOT_TESTED)
- **R01 cmds:** ISURVEY_ROUTE_PLOT
- **R02 refs:** AUD-024

### 2. Kích hoạt
- **Lệnh chính:** `ISURVEY_ROUTE_PLOT` @ `src/Modules/Frame/FrameService.RouteLayout.cs:33`
- **Alias module (ngoài CommandAliases.cs):** (không)
- **Alias CommandAliases.cs:** INTUYEN
- **Effective method:** `RoutePlotCommand` span `34-50`
- **Ribbon:** (không)
- **Palette:** (không)
- **XAML mapped:** (không / UI dialog nội bộ — xem 03D)

### 3. Chuỗi xử lý
- ISURVEY_ROUTE_PLOT @ src/Modules/Frame/FrameService.RouteLayout.cs:33 method `RoutePlotCommand` span 34-50
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

## AUD-024 — ISURVEY_ROUTE_SHEETS

### 1. Định danh
- **feature_id:** `AUD-024`
- **Hành vi:** ISURVEY_ROUTE_SHEETS
- **Nhóm:** FRAME | **Audience:** USER
- **source_status:** `SOURCE_PRESENT` — có CommandMethod + thân xử lý trong source (runtime NOT_TESTED)
- **R01 cmds:** ISURVEY_ROUTE_SHEETS
- **R02 refs:** AUD-025

### 2. Kích hoạt
- **Lệnh chính:** `ISURVEY_ROUTE_SHEETS` @ `src/Modules/Frame/FrameService.RouteLayout.cs:29`
- **Alias module (ngoài CommandAliases.cs):** (không)
- **Alias CommandAliases.cs:** RAIKHUNG
- **Effective method:** `RouteLayoutCommand` span `30-30`
- **Ribbon:** Rải Khung Theo Tuyến|RAIKHUNG|en=True|L182
- **Palette:** (không)
- **XAML mapped:** (không / UI dialog nội bộ — xem 03D)

### 3. Chuỗi xử lý
- ISURVEY_ROUTE_SHEETS @ src/Modules/Frame/FrameService.RouteLayout.cs:29 method `RouteLayoutCommand` span 30-30
- types trong handler: (không khớp *Service\|Dialog\|…)
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
- **muc_ket_luan_source:** `METHOD_BODY_SCANNED`
- **runtime:** `NOT_TESTED`
- **Bằng chứng thủ công độc lập:** KHÔNG — chủ yếu script; một phần file:line từ extractor

---
