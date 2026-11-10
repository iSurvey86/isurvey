# 11 — Ma trận bằng chứng và độ đầy đủ

## 1. Kết luận lớn ↔ path:line

| Kết luận | path:line / tệp | Nhãn |
|---|---|---|
| Root git hợp lệ, HEAD 53c5eb4 | `git rev-parse` lúc 2026-11-10 | CÓ BẰNG CHỨNG |
| Version 1.8.17 | `iSurvey.csproj:11`; `PackageContents.xml:6` | CÓ BẰNG CHỨNG |
| Entry Main IExtensionApplication | `src/Main.cs:16,45–67` | CÓ BẰNG CHỨNG |
| 125 CommandMethod | `_inventory_commands.csv` | CÓ BẰNG CHỨNG |
| 46 alias trong CommandAliases | `src/CommandAliases.cs` | CÓ BẰNG CHỨNG |
| 61 Ribbon Btn; 25 disabled không CommandMethod | `RibbonBuilder.cs` + so khớp | CÓ BẰNG CHỨNG |
| UAV stub alert | `UavModule.cs:58–62` | CÓ BẰNG CHỨNG |
| VEMCD dual command → Draw | `ProfileModule.cs:14–22` | CÓ BẰNG CHỨNG |
| Auth Initialize không dialog | `Main.cs:52–53`; AuthManager | CÓ BẰNG CHỨNG |
| GENOTP cần AdminSecret | `AuthModule.cs:178–189` | CÓ BẰNG CHỨNG (không đọc secret) |
| Tool video edge-tts + upload | `tools/video/*` | CÓ BẰNG CHỨNG |
| Giọng HoaiMy trong quy trình cũ | `QUY_TRINH.md:34`; json voice | CÓ BẰNG CHỨNG |
| thanh-pham / Standard v1.0 | — | CHƯA TÌM THẤY |
| Runtime Civil / 4K / TTS chất lượng | — | CHƯA KIỂM CHỨNG THỰC TẾ |

## 2. Thống kê đọc

| Hạng mục | Số | Ghi chú |
|---|---:|---|
| `.cs` dưới `src/` | 198 | Đã quét CommandMethod toàn bộ |
| File chứa CommandMethod | ~25 path (inventory) | Body đọc sâu không đều |
| `.xaml` | 23 | Liệt kê; không review UI từng control trừ VEMCD context |
| Module folders | 12 dưới Modules | |
| Tính năng CSV | **150** | 125 lệnh + 25 placeholder |
| Phụ lục hồ sơ 04A–04E | 5 | Phủ mọi nhóm CSV |

## 3. Phần chưa đọc sâu (ảnh hưởng đầy đủ)

| Khu vực | Ảnh hưởng |
|---|---|
| `src/Modules/Map/*` helpers (nhiều file tải tile) | Chi tiết thuật toán GE |
| Toàn bộ body `FrameService.*.cs` lớn | Chi tiết layout/plot |
| `Terrain` services ngoài Module entry | Object model TIN |
| `Dgn/*` pipeline đầy đủ | Mọi nhánh lỗi DGN |
| `Auth/SupabaseAuthClient` chi tiết HTTP | Không cố ý đào sâu secret |
| `bin/obj` | Loại trừ |

→ **Không** tuyên bố đã đọc hiểu 100% mọi dòng nghiệp vụ; tuyên bố đã **kiểm kê điểm vào lệnh/UI toàn `src/`** và hồ sơ nhóm.

## 4. Self-check mẫu path

Mở được tại thời điểm viết: `ProfileModule.cs`, `UavModule.cs`, `CommandAliases.cs`, `RibbonBuilder.cs`, `Main.cs`, `iSurvey.csproj`, `PackageContents.xml`, `QUY_TRINH.md`.  
CSV `03` đếm 150 dòng dữ liệu + header.

## 5. Trạng thái truy cập

- Repo local: đọc được.  
- AppData bundle: tồn tại PackageContents (không hash DLL).  
- Secrets YouTube: missing client_secret.  
- Network gửi dữ liệu dự án: **không dùng** trong đợt này ngoài git pull trước đó của phiên người dùng.
