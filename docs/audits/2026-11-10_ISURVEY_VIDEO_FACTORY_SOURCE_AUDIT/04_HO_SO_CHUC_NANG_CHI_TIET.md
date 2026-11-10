# 04 — Hồ sơ chức năng chi tiết (chỉ mục)

**Mức xác minh toàn đợt:** tối đa `SOURCE_REVIEWED`. Không có `RUNTIME_VERIFIED`.

## Cách đọc

1. Mỗi tính năng có mã `AUD-xxx` trong `03_DANH_MUC_LENH_VA_TINH_NANG.csv` (**150** dòng sau khi gồm 25 Ribbon placeholder).
2. Phụ lục theo nhóm dưới đây truy vết điểm vào → auth → xử lý chính → I/O → rủi ro video.
3. Với lệnh chỉ là **alias**, xử lý thật nằm ở method được ủy quyền trong `CommandAliases.cs` (file:line từng alias trong CSV).

## Chỉ mục nhóm → phụ lục

| Nhóm | AUD (khoảng / lọc CSV cột `nhom`) | Phụ lục |
|---|---|---|
| He_thong_Auth | lọc `He_thong_Auth` | [04A](./04A_AUTH_HE_THONG.md) |
| Ban_do_GE / KML_KMZ / TAB / DGN | các nhóm tương ứng | [04B](./04B_BAN_DO_KML_TAB_DGN.md) |
| Dia_hinh_TIN / Diem_do / Scan | tương ứng | [04C](./04C_DIA_HINH_DIEM_SCAN.md) |
| Tuyen_Coc / Mat_cat_doc / Khung_Layout | tương ứng | [04D](./04D_TUYEN_MCD_KHUNG.md) |
| UAV_LiDAR / Tro_ly_AI / Palette / Placeholder | tương ứng | [04E](./04E_UAV_AI_PALETTE_PLACEHOLDER.md) |

## Quy ước hồ sơ tối thiểu (áp dụng mọi AUD)

| Trường Mục 05 | Nơi lấy |
|---|---|
| Mã/tên | CSV |
| Nhóm / đối tượng / công khai | CSV |
| Lệnh / alias / UI | CSV + Ribbon inventory |
| Đăng ký / method | `source_path_line`, `class_method` |
| Auth | Hầu hết lệnh nghiệp vụ gọi `AuthManager.EnsureAuthenticated` — đếm tham chiếu theo file ở 04A–04E |
| I/O & tác động | Mô tả theo nhóm; chi tiết sâu cho ứng viên pilot |
| Fixture / pilot / loại video | CSV + `09` |
| Mức xác minh | `SOURCE_REVIEWED` hoặc `CHƯA ĐỦ BẰNG CHỨNG` nếu chưa đọc body |

## Thống kê nhanh

| Metric | Giá trị | Bằng chứng |
|---|---:|---|
| CommandMethod | 125 | `_inventory_commands.csv` |
| Ribbon Btn | 61 | `_inventory_ribbon.csv` |
| Ribbon không có CommandMethod | 25 | So khớp inventory |
| UAV stub (alert only) | 7 full + 7 alias | `UavModule.cs:37–62` |
| Admin-oriented | GENOTP + AI (EnsureAdmin) | `AuthModule.cs`, `AiAssistantModule.cs` |
