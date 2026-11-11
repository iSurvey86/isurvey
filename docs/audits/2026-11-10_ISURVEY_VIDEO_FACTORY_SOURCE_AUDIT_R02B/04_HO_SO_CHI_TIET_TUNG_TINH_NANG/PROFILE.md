# Nhóm PROFILE

Sinh 2026-11-10 — R02B.

## AUD-033 — VEMCD

### 1. Định danh
- **feature_id:** `AUD-033`
- **Hành vi:** VEMCD
- **Nhóm:** PROFILE | **Audience:** USER
- **source_status:** `SOURCE_PRESENT` — có CommandMethod + thân xử lý trong source (runtime NOT_TESTED)
- **R01 cmds:** ISURVEY_PROFILE_DRAW;VEMCD
- **R02 refs:** AUD-037;AUD-038

### 2. Kích hoạt
- **Lệnh chính:** `VEMCD` @ `src/Modules/Profile/ProfileModule.cs:17`
- **Alias module (ngoài CommandAliases.cs):** ISURVEY_PROFILE_DRAW
- **Alias CommandAliases.cs:** (không)
- **Effective method:** `Draw` span `20-147`
- **Ribbon:** Vẽ Mặt Cắt Dọc|VEMCD|en=True|L184
- **Palette:** VEMCD|src/UI/Palette/PaletteViewModel.cs:L280;VEMCD|src/UI/Palette/PaletteViewModel.cs:L287
- **XAML mapped:** (không / UI dialog nội bộ — xem 03D)

### 3. Chuỗi xử lý
- ISURVEY_PROFILE_DRAW → DrawCommand → Draw; VEMCD → DrawAlias → Draw @ ProfileModule.cs:14-20
- EnsureAuthenticated("VEMCD") @ :22
- Model Space TileMode @ :28-31
- RouteStore.Load @ :39
- DrawProfileWindow.Prompt @ :88 / hoặc seed demo
- Pick insert point @ :109-120
- ProfileDrawService.Draw / RunUpdate @ :129-139, :179+
- ProfileStore.Load; xóa profile cũ cùng RouteId rồi vẽ lại @ :163-178 comment

### 4. Input / Output / lỗi / rủi ro
- **Input:** Tuyến+cọc có Z (TAOTUYEN/PHATSINHCOC); options hộp thoại; điểm chèn Model.
- **Output / hậu điều kiện:** Hình MCD trên Model; ProfileStore record; có thể xóa bản MCD cũ cùng tuyến.
- **Nhánh lỗi:** RouteStore catch :41-44; empty routes :47-50; dialog catch :90-93; hủy :96-99; draw catch :142-145.
- **Destructive / quyền:** Xóa profile cũ cùng RouteId trước khi vẽ lại (comment :178).
- **Giới hạn đọc:** ProfileDrawService body dài — CHƯA ĐỦ BẰNG CHỨNG toàn bộ geometry band.

### 5. Fixture đề xuất
- WORK COPY có TIN+tuyến+cọc Z; REFERENCE checklist band/datum; fixture dài.

### 6. Video / bằng chứng
- **video_candidate:** YES (tham khảo — không phải sẵn sàng sản xuất)
- **muc_ket_luan_source:** `SERVICE_CHAIN_VERIFIED_STATIC`
- **runtime:** `NOT_TESTED`
- **Bằng chứng thủ công độc lập:** CÓ (độc lập script)

---
