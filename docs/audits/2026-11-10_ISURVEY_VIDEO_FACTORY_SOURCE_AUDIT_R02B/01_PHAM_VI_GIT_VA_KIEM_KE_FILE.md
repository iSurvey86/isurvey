# 01 — Phạm vi Git và kiểm kê file (R02B)

## Git trước R02B

Xem `_git_before.txt`, `_git_head_before.txt`, `_git_branch_before.txt`, `_git_status_before.txt`, `_git_diff_before.txt`.

- HEAD hiện tại (= R02 HEAD): `3a16cb3959dc4e65c08f77b6de26669b309739cd`
- R01 HEAD: `53c5eb4644f2dbc0f736e03098c749197a81a93e`
- R02 HEAD: `3a16cb3959dc4e65c08f77b6de26669b309739cd`
- R02 → now: **không khác** (HEAD trùng R02) — xem `_git_diff_r02_to_now.txt`

## So sánh R01 → R02 (không checkout)

Xem `_git_log_r01_to_r02.txt` (6 commit) và `_git_diff_r01_to_r02.txt` (43 tệp).

**Ảnh hưởng source `src/` giữa hai mốc audit:**

| Tệp | Liên quan audit |
|---|---|
| `src/Auth/AuthManager.cs` | Auth/OTP/login |
| `src/Auth/AuthModule.cs` | Lệnh AUTH (ABOUT/HDSD/LOGIN/UPDATE…) |
| `src/Auth/UpdateFlow.cs` | UPD / cập nhật |
| `src/Auth/UpdateStartupChecker.cs` | Tự kiểm tra cập nhật khi mở Civil (1.8.19) |
| `src/Main.cs` | Khởi động / hook startup update |

Các tệp còn lại chủ yếu docs/audit/changelog/bundle/video kịch bản — **không** đổi handler D2C/VEMCD/điểm/UAV giữa R01→R02 theo diff name-only.

**Kết luận:** R01 và R02 **không** cùng snapshot. Kết luận R01 về AUTH/UPDATE có thể lệch hành vi startup so R02/R02B. Các feature DGN/DIEM/PROFILE/UAV **không** nằm trong diff `src/` R01→R02 theo danh sách name-only — vẫn nên tái xác minh khi quay video trên bản hiện hành.

## Git sau R02B

Xem `_git_after.txt`, `_git_head_after.txt`, `_git_status_after.txt`, `_git_diff_after.txt`.

| Kiểm tra | Kết quả |
|---|---|
| HEAD trước/sau | `3a16cb3959dc4e65c08f77b6de26669b309739cd` (không đổi) |
| Tracked `src/` modified | **Không** |
| `git diff --name-only` | Chỉ `docs/audits/2026-11-10_ISURVEY_VIDEO_FACTORY_SOURCE_AUDIT.zip` — **đã có trước** R02/R02B |
| Untracked mới | `docs/audits/..._SOURCE_AUDIT_R02B/` + ZIP R02B |

**Kết luận an toàn:** R02B không thay đổi mã nguồn nghiệp vụ hiện hữu.

## Kiểm kê file

| Hạng mục | Số |
|---|---:|
| `.cs`+`.xaml` | 223 |
| CommandMethod | 125 |
| Canonical features R02B | 63 |

Chi tiết: `_inventory_files.csv`.
