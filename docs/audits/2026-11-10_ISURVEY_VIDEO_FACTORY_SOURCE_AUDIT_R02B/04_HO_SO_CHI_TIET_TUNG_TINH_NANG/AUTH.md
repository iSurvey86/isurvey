# Nhóm AUTH

Sinh 2026-11-10 — R02B.

## AUD-003 — ISURVEY_ABOUT

### 1. Định danh
- **feature_id:** `AUD-003`
- **Hành vi:** ISURVEY_ABOUT
- **Nhóm:** AUTH | **Audience:** USER
- **source_status:** `SOURCE_PRESENT` — có CommandMethod + thân xử lý trong source (runtime NOT_TESTED)
- **R01 cmds:** ISURVEY_ABOUT
- **R02 refs:** AUD-003

### 2. Kích hoạt
- **Lệnh chính:** `ISURVEY_ABOUT` @ `src/Auth/AuthModule.cs:67`
- **Alias module (ngoài CommandAliases.cs):** (không)
- **Alias CommandAliases.cs:** ABT
- **Effective method:** `AboutCommand` span `68-72`
- **Ribbon:** Thông Tin & Giấy Phép|ISURVEY_ABOUT|en=True|L228
- **Palette:** (không)
- **XAML mapped:** (không / UI dialog nội bộ — xem 03D)

### 3. Chuỗi xử lý
- ISURVEY_ABOUT @ src/Auth/AuthModule.cs:67 method `AboutCommand` span 68-72
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

## AUD-004 — ISURVEY_GENOTP

### 1. Định danh
- **feature_id:** `AUD-004`
- **Hành vi:** ISURVEY_GENOTP
- **Nhóm:** AUTH | **Audience:** ADMIN
- **source_status:** `SOURCE_PRESENT` — có CommandMethod + thân xử lý trong source (runtime NOT_TESTED)
- **R01 cmds:** ISURVEY_GENOTP
- **R02 refs:** AUD-004

### 2. Kích hoạt
- **Lệnh chính:** `ISURVEY_GENOTP` @ `src/Auth/AuthModule.cs:106`
- **Alias module (ngoài CommandAliases.cs):** (không)
- **Alias CommandAliases.cs:** (không)
- **Effective method:** `GenerateOtpCommand` span `107-136`
- **Ribbon:** (không)
- **Palette:** (không)
- **XAML mapped:** (không / UI dialog nội bộ — xem 03D)

### 3. Chuỗi xử lý
- ISURVEY_GENOTP @ src/Auth/AuthModule.cs:106 method `GenerateOtpCommand` span 107-136
- types trong handler: DocumentManager;OtpAuthService
- merge: (không)
- service scan: OtpAuthService@src/Auth/OtpAuthService.cs: đã mở src/Auth/OtpAuthService.cs (498 dòng), quét 8000 ký tự đầu; io=[]; catch~0

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
- **muc_ket_luan_source:** `SERVICE_CHAIN_VERIFIED_STATIC`
- **runtime:** `NOT_TESTED`
- **Bằng chứng thủ công độc lập:** KHÔNG — chủ yếu script; một phần file:line từ extractor

---

## AUD-005 — ISURVEY_HDSD

### 1. Định danh
- **feature_id:** `AUD-005`
- **Hành vi:** ISURVEY_HDSD
- **Nhóm:** AUTH | **Audience:** USER
- **source_status:** `SOURCE_PRESENT` — có CommandMethod + thân xử lý trong source (runtime NOT_TESTED)
- **R01 cmds:** ISURVEY_HDSD
- **R02 refs:** AUD-005

### 2. Kích hoạt
- **Lệnh chính:** `ISURVEY_HDSD` @ `src/Auth/AuthModule.cs:35`
- **Alias module (ngoài CommandAliases.cs):** (không)
- **Alias CommandAliases.cs:** HD
- **Effective method:** `HelpCommand` span `36-49`
- **Ribbon:** Hướng dẫn|ISURVEY_HDSD|en=True|L229
- **Palette:** (không)
- **XAML mapped:** (không / UI dialog nội bộ — xem 03D)

### 3. Chuỗi xử lý
- ISURVEY_HDSD @ src/Auth/AuthModule.cs:35 method `HelpCommand` span 36-49
- types trong handler: ActivityLogger;AuthManager
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

## AUD-006 — ISURVEY_LOGIN

### 1. Định danh
- **feature_id:** `AUD-006`
- **Hành vi:** ISURVEY_LOGIN
- **Nhóm:** AUTH | **Audience:** USER
- **source_status:** `SOURCE_PRESENT` — có CommandMethod + thân xử lý trong source (runtime NOT_TESTED)
- **R01 cmds:** ISURVEY_LOGIN
- **R02 refs:** AUD-006

### 2. Kích hoạt
- **Lệnh chính:** `ISURVEY_LOGIN` @ `src/Auth/AuthModule.cs:24`
- **Alias module (ngoài CommandAliases.cs):** (không)
- **Alias CommandAliases.cs:** LGI
- **Effective method:** `LoginCommand` span `25-32`
- **Ribbon:** (không)
- **Palette:** (không)
- **XAML mapped:** (không / UI dialog nội bộ — xem 03D)

### 3. Chuỗi xử lý
- ISURVEY_LOGIN @ src/Auth/AuthModule.cs:24 method `LoginCommand` span 25-32
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

## AUD-007 — ISURVEY_LOGOUT

### 1. Định danh
- **feature_id:** `AUD-007`
- **Hành vi:** ISURVEY_LOGOUT
- **Nhóm:** AUTH | **Audience:** USER
- **source_status:** `SOURCE_PRESENT` — có CommandMethod + thân xử lý trong source (runtime NOT_TESTED)
- **R01 cmds:** ISURVEY_LOGOUT
- **R02 refs:** AUD-007

### 2. Kích hoạt
- **Lệnh chính:** `ISURVEY_LOGOUT` @ `src/Auth/AuthModule.cs:77`
- **Alias module (ngoài CommandAliases.cs):** (không)
- **Alias CommandAliases.cs:** LGO
- **Effective method:** `LogoutCommand` span `78-100`
- **Ribbon:** (không)
- **Palette:** (không)
- **XAML mapped:** (không / UI dialog nội bộ — xem 03D)

### 3. Chuỗi xử lý
- ISURVEY_LOGOUT @ src/Auth/AuthModule.cs:77 method `LogoutCommand` span 78-100
- types trong handler: AuthManager;DarkDialog
- merge: (không)
- service scan: DarkDialog@src/UI/Account/DarkDialog.xaml.cs: đã mở src/UI/Account/DarkDialog.xaml.cs (128 dòng), quét 3904 ký tự đầu; io=[]; catch~0

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

## AUD-008 — ISURVEY_RIBBON

### 1. Định danh
- **feature_id:** `AUD-008`
- **Hành vi:** ISURVEY_RIBBON
- **Nhóm:** AUTH | **Audience:** SUPPORT
- **source_status:** `SOURCE_PRESENT` — có CommandMethod + thân xử lý trong source (runtime NOT_TESTED)
- **R01 cmds:** ISURVEY_RIBBON
- **R02 refs:** AUD-008

### 2. Kích hoạt
- **Lệnh chính:** `ISURVEY_RIBBON` @ `src/Auth/AuthModule.cs:52`
- **Alias module (ngoài CommandAliases.cs):** (không)
- **Alias CommandAliases.cs:** MENUISURVEY
- **Effective method:** `RibbonCommand` span `53-65`
- **Ribbon:** Nạp lại Menu|ISURVEY_RIBBON|en=True|L230
- **Palette:** (không)
- **XAML mapped:** (không / UI dialog nội bộ — xem 03D)

### 3. Chuỗi xử lý
- ISURVEY_RIBBON @ src/Auth/AuthModule.cs:52 method `RibbonCommand` span 53-65
- types trong handler: ActivityLogger;AuthManager;RibbonBuilder
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
- **video_candidate:** NO (tham khảo — không phải sẵn sàng sản xuất)
- **muc_ket_luan_source:** `HANDLER_TRACE_COMPLETE`
- **runtime:** `NOT_TESTED`
- **Bằng chứng thủ công độc lập:** KHÔNG — chủ yếu script; một phần file:line từ extractor

---

## AUD-009 — ISURVEY_UPDATE

### 1. Định danh
- **feature_id:** `AUD-009`
- **Hành vi:** ISURVEY_UPDATE
- **Nhóm:** AUTH | **Audience:** USER
- **source_status:** `SOURCE_PRESENT` — có CommandMethod + thân xử lý trong source (runtime NOT_TESTED)
- **R01 cmds:** ISURVEY_UPDATE
- **R02 refs:** AUD-009

### 2. Kích hoạt
- **Lệnh chính:** `ISURVEY_UPDATE` @ `src/Auth/AuthModule.cs:74`
- **Alias module (ngoài CommandAliases.cs):** (không)
- **Alias CommandAliases.cs:** UPD
- **Effective method:** `UpdateCommand` span `75-75`
- **Ribbon:** Kiểm Tra Cập Nhật|ISURVEY_UPDATE|en=True|L227
- **Palette:** (không)
- **XAML mapped:** (không / UI dialog nội bộ — xem 03D)

### 3. Chuỗi xử lý
- ISURVEY_UPDATE @ src/Auth/AuthModule.cs:74 method `UpdateCommand` span 75-75
- types trong handler: UpdateFlow
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
