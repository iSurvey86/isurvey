# Nhóm PALETTE

Sinh 2026-11-10 — R02B.

## AUD-031 — ISURVEY_PALETTE

### 1. Định danh
- **feature_id:** `AUD-031`
- **Hành vi:** ISURVEY_PALETTE
- **Nhóm:** PALETTE | **Audience:** USER
- **source_status:** `SOURCE_PRESENT` — có CommandMethod + thân xử lý trong source (runtime NOT_TESTED)
- **R01 cmds:** ISURVEY_PALETTE
- **R02 refs:** AUD-035

### 2. Kích hoạt
- **Lệnh chính:** `ISURVEY_PALETTE` @ `src/UI/Palette/PaletteModule.cs:9`
- **Alias module (ngoài CommandAliases.cs):** (không)
- **Alias CommandAliases.cs:** BANGIS
- **Effective method:** `ShowCommand` span `10-15`
- **Ribbon:** Bảng Dữ liệu|ISURVEY_PALETTE|en=True|L142
- **Palette:** (không)
- **XAML mapped:** (không / UI dialog nội bộ — xem 03D)

### 3. Chuỗi xử lý
- ISURVEY_PALETTE @ src/UI/Palette/PaletteModule.cs:9 method `ShowCommand` span 10-15
- types trong handler: AuthManager;SurveyPaletteManager
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
