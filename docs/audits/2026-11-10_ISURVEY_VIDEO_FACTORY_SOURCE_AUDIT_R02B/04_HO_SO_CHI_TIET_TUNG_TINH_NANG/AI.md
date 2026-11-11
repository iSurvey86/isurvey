# Nhóm AI

Sinh 2026-11-10 — R02B.

## AUD-001 — ISURVEY_AI_ASSIST

### 1. Định danh
- **feature_id:** `AUD-001`
- **Hành vi:** ISURVEY_AI_ASSIST
- **Nhóm:** AI | **Audience:** ADMIN
- **source_status:** `SOURCE_PRESENT` — có CommandMethod + thân xử lý trong source (runtime NOT_TESTED)
- **R01 cmds:** ISURVEY_AI_ASSIST
- **R02 refs:** AUD-001

### 2. Kích hoạt
- **Lệnh chính:** `ISURVEY_AI_ASSIST` @ `src/Modules/Ai/AiAssistantModule.cs:13`
- **Alias module (ngoài CommandAliases.cs):** (không)
- **Alias CommandAliases.cs:** AIA
- **Effective method:** `AssistCommand` span `14-24`
- **Ribbon:** Trợ Lý AI|ISURVEY_AI_ASSIST|en=True|L284
- **Palette:** (không)
- **XAML mapped:** (không / UI dialog nội bộ — xem 03D)

### 3. Chuỗi xử lý
- ISURVEY_AI_ASSIST @ src/Modules/Ai/AiAssistantModule.cs:13 method `AssistCommand` span 14-24
- types trong handler: AiChatWindow;AuthManager;DocumentManager
- merge: (không)
- service scan: AiChatWindow@src/UI/Ai/AiChatWindow.xaml.cs: đã mở src/UI/Ai/AiChatWindow.xaml.cs (274 dòng), quét 8000 ký tự đầu; io=[]; catch~2

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

## AUD-002 — ISURVEY_AI_SETTINGS

### 1. Định danh
- **feature_id:** `AUD-002`
- **Hành vi:** ISURVEY_AI_SETTINGS
- **Nhóm:** AI | **Audience:** ADMIN
- **source_status:** `SOURCE_PRESENT` — có CommandMethod + thân xử lý trong source (runtime NOT_TESTED)
- **R01 cmds:** ISURVEY_AI_SETTINGS
- **R02 refs:** AUD-002

### 2. Kích hoạt
- **Lệnh chính:** `ISURVEY_AI_SETTINGS` @ `src/Modules/Ai/AiAssistantModule.cs:26`
- **Alias module (ngoài CommandAliases.cs):** (không)
- **Alias CommandAliases.cs:** AIC
- **Effective method:** `SettingsCommand` span `27-58`
- **Ribbon:** Cấu Hình AI|ISURVEY_AI_SETTINGS|en=True|L288
- **Palette:** (không)
- **XAML mapped:** (không / UI dialog nội bộ — xem 03D)

### 3. Chuỗi xử lý
- ISURVEY_AI_SETTINGS @ src/Modules/Ai/AiAssistantModule.cs:26 method `SettingsCommand` span 27-58
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
- **video_candidate:** NO (tham khảo — không phải sẵn sàng sản xuất)
- **muc_ket_luan_source:** `SERVICE_CHAIN_VERIFIED_STATIC`
- **runtime:** `NOT_TESTED`
- **Bằng chứng thủ công độc lập:** KHÔNG — chủ yếu script; một phần file:line từ extractor

---
