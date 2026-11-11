# Nhóm TAB

Sinh 2026-11-10 — R02B.

## AUD-046 — ISURVEY_TAB_TO_CAD

### 1. Định danh
- **feature_id:** `AUD-046`
- **Hành vi:** ISURVEY_TAB_TO_CAD
- **Nhóm:** TAB | **Audience:** USER
- **source_status:** `SOURCE_PRESENT` — có CommandMethod + thân xử lý trong source (runtime NOT_TESTED)
- **R01 cmds:** ISURVEY_TAB_TO_CAD
- **R02 refs:** AUD-055

### 2. Kích hoạt
- **Lệnh chính:** `ISURVEY_TAB_TO_CAD` @ `src/Modules/Import/TabImportModule.cs:29`
- **Alias module (ngoài CommandAliases.cs):** (không)
- **Alias CommandAliases.cs:** T2C
- **Effective method:** `TabToCadCommand` span `30-82`
- **Ribbon:** Nhập TAB ➔ CAD|ISURVEY_TAB_TO_CAD|en=True|L160
- **Palette:** (không)
- **XAML mapped:** (không / UI dialog nội bộ — xem 03D)

### 3. Chuỗi xử lý
- ISURVEY_TAB_TO_CAD @ src/Modules/Import/TabImportModule.cs:29 method `TabToCadCommand` span 30-82
- types trong handler: ActivityLogger;AuthManager;DocumentManager;MapModule;ShowAlertDialog;TabImportWindow;UserSettingsStore
- merge: (không)
- service scan: ShowAlertDialog@?: CHƯA ĐỦ BẰNG CHỨNG: không tìm thấy file cho ShowAlertDialog; io=[]; catch~0
- service scan: TabImportWindow@src/UI/Import/TabImportWindow.xaml.cs: đã mở src/UI/Import/TabImportWindow.xaml.cs (147 dòng), quét 4954 ký tự đầu; io=['OpenFileDialog']; catch~1

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
