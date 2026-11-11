# 01 — Phạm vi Git và kiểm kê file (R02)

## Git trước R02

Xem `_git_before.txt`, `_git_status_before.txt`, `_git_diff_before.txt`, `_git_head_before.txt`.

- Working tree có sẵn nhiều file untracked (docs/MCD, SEED, …) — **không** thuộc phạm vi sửa R02.
- Không đọc secret/token.

## Git sau R02

Xem `_git_after.txt` / `_git_status_after.txt` / `_git_diff_after.txt` / `_git_head_after.txt`.

| Kiểm tra | Kết quả |
|---|---|
| HEAD trước | `3a16cb3959dc4e65c08f77b6de26669b309739cd` |
| HEAD sau | `3a16cb3959dc4e65c08f77b6de26669b309739cd` (không đổi) |
| `git diff --name-only` sau | Chỉ `docs/audits/2026-11-10_ISURVEY_VIDEO_FACTORY_SOURCE_AUDIT.zip` — **đã có trước R02** (cùng mục trong `_git_diff_before.txt`); R02 **không** tạo thay đổi tracked product `src/` |
| Untracked mới R02 | Toàn bộ dưới `docs/audits/..._SOURCE_AUDIT_R02/` (+ ZIP R02) |
| `src/` tracked modified | **Không** |

**Kết luận an toàn Git R02:** không có mã nguồn nghiệp vụ C#/XAML hiện hữu bị thay đổi bởi đợt kiểm toán này.

## Kiểm kê file nguồn

| Hạng mục | Số |
|---|---:|
| File `.cs`+`.xaml` dưới `src/` | 223 |
| File có CommandMethod (inventory) | 24 |
| XAML Click matches | 91 |
| PaletteCommands.Run refs | 8 |

Chi tiết: `_inventory_files.csv`.

## Mức đọc

| Phạm vi | Mức |
|---|---|
| Toàn bộ `src/**/*.{cs,xaml}` | Đã quét inventory + regex CommandMethod/Btn/Click/Palette |
| Mỗi canonical feature — thân command method | Brace-match + trích service/UI/IO/catch (tối đa 12KB/method) |
| Service layer phụ thuộc (ProfileDrawService, FrameService.*, …) | **CHƯA ĐỦ BẰNG CHỨNG** đầy đủ từng dòng — liệt kê theo feature trong 04/ và 07 |

## Giới hạn

- Không Roslyn AST chính thức.
- Không đọc secret / config nhạy cảm.
- Dynamic command registration ngoài literal string: **CHƯA ĐỦ BẰNG CHỨNG**.
