# 08 — Bảo mật, phiên bản và rủi ro

**Cảnh báo:** Báo cáo này **không** chứa mật khẩu, token, private key, nội dung `auth.config`, OTP, hay dữ liệu khách hàng.

## 1. Tệp / vị trí nhạy cảm (chỉ tên loại)

| Loại | Vị trí tương đối / quy ước | Trong đợt này |
|---|---|---|
| OAuth YouTube | `%AppData%\iSurvey\youtube\client_secret.json`, `token.json` (theo `YOUTUBE.md`) | client_secret: **MISSING** trên máy snapshot; không đọc |
| License cục bộ | `%AppData%\iSurvey\license.dat` | EXISTS — không mở nội dung |
| Auth config Admin | `auth.config.json` (tham chiếu code GENOTP) | Không mở; path AppData MISSING trên snapshot |
| Activity / Supabase | `Auth/ActivityLogger.cs`, `SupabaseAuthClient` | Chỉ ghi nhận có gọi mạng |

## 2. Nhánh dễ mất dữ liệu / môi trường (từ tên lệnh + docs)

| Hạng mục | Ví dụ lệnh | Mức |
|---|---|---|
| Xóa đối tượng bản vẽ | XG, XOACOC, XOATHANHPHAN, GOMOHINH | P1 |
| Ghi file ngoài DWG | C2D, XUATDIEM, Export KML | P1 |
| Sửa biến hệ thống CAD | `Main.OnFirstIdle` đặt lại FILEDIA/CMDDIA nếu =0 — `Main.cs:79–91` | P2 (có chủ đích sửa env) |
| Untracked DWG công trình trong workspace | `docs/MCD/` | P1 riêng tư nếu dùng quay |

## 3. P0 / P1 / P2 tổng hợp cho Video Factory

### P0 — chặn tuyệt đối trước pilot có kiểm định

1. **Không có bộ fixture/baseline độc lập trong repo** (`07`).  
2. **Chưa được phép triển khai Factory** (chỉ thị Chủ dự án).  
3. **Không chạy CAD trong đợt audit** → mọi PASS runtime đều cấm.

### P1 — cần xử lý trước pilot

1. Giọng TTS / quy trình duyệt trong repo cũ ≠ KT mới (`05`).  
2. Secret YouTube thiếu trên máy → upload không chạy.  
3. Lệnh stub + Ribbon enabled dễ quay nhầm (`UavModule.cs:58–62`).  
4. Modal/auth trong hầu hết lệnh.  
5. Working tree có untracked dữ liệu công trình.

### P2 — cải thiện sau

1. 25 Ribbon placeholder.  
2. CATALOG không đồng bộ đủ CommandMethod.  
3. Excel quản lý video KT04 missing.

## 4. Phạm vi không kiểm toán được

- Nội dung `bin/`/`obj/` assemblies.  
- Native GDAL binaries hành vi.  
- SDK AutoCAD đóng.  
- Dịch vụ Supabase phía server.  
- MP4 ngoài git.

## 5. Neo phiên bản báo cáo

Báo cáo gắn **HEAD `53c5eb4` + untracked liệt kê ở `01`**. Không chứng nhận bản phát hành đã cài trên mọi máy.
