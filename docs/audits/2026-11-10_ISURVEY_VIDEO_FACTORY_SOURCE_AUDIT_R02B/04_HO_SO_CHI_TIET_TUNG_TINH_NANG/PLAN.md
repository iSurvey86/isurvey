# Nhóm PLAN

Sinh 2026-11-10 — R02B.

## AUD-032 — CATBD

### 1. Định danh
- **feature_id:** `AUD-032`
- **Hành vi:** CATBD
- **Nhóm:** PLAN | **Audience:** USER
- **source_status:** `SOURCE_PRESENT` — có CommandMethod + thân xử lý trong source (runtime NOT_TESTED)
- **R01 cmds:** CATBD
- **R02 refs:** AUD-036

### 2. Kích hoạt
- **Lệnh chính:** `CATBD` @ `src/Modules/Plan/PlanModule.cs:46`
- **Alias module (ngoài CommandAliases.cs):** (không)
- **Alias CommandAliases.cs:** CBD
- **Effective method:** `CutPlanCommand` span `47-112`
- **Ribbon:** Cắt Bình Đồ|CATBD|en=True|L179
- **Palette:** (không)
- **XAML mapped:** (không / UI dialog nội bộ — xem 03D)

### 3. Chuỗi xử lý
- CATBD @ src/Modules/Plan/PlanModule.cs:46 method `CutPlanCommand` span 47-112
- types trong handler: ActivityLogger;AuthManager;DocumentManager;MapModule
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
