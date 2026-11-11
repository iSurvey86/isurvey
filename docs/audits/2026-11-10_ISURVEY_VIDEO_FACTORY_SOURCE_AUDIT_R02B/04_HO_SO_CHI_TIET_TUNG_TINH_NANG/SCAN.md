# Nhóm SCAN

Sinh 2026-11-10 — R02B.

## AUD-045 — AUTOSCAN

### 1. Định danh
- **feature_id:** `AUD-045`
- **Hành vi:** AUTOSCAN
- **Nhóm:** SCAN | **Audience:** USER
- **source_status:** `SOURCE_PRESENT` — có CommandMethod + thân xử lý trong source (runtime NOT_TESTED)
- **R01 cmds:** AUTOSCAN
- **R02 refs:** AUD-069

### 2. Kích hoạt
- **Lệnh chính:** `AUTOSCAN` @ `src/Modules/Scan/ScanModule.cs:25`
- **Alias module (ngoài CommandAliases.cs):** (không)
- **Alias CommandAliases.cs:** SCN
- **Effective method:** `AutoScanCommand` span `26-88`
- **Ribbon:** Quét Cấu Trúc (AutoScan)|AUTOSCAN|en=True|L217
- **Palette:** (không)
- **XAML mapped:** (không / UI dialog nội bộ — xem 03D)

### 3. Chuỗi xử lý
- AUTOSCAN @ src/Modules/Scan/ScanModule.cs:25 method `AutoScanCommand` span 26-88
- types trong handler: ActivityLogger;AuthManager;DocumentManager;ShowDialog;TransactionManager;Win32SaveFileDialog
- merge: (không)
- service scan: ShowDialog@?: CHƯA ĐỦ BẰNG CHỨNG: không tìm thấy file cho ShowDialog; io=[]; catch~0
- service scan: Win32SaveFileDialog@?: CHƯA ĐỦ BẰNG CHỨNG: không tìm thấy file cho Win32SaveFileDialog; io=[]; catch~0

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
