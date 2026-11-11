# 04 — Stop gate & Git

## Thiếu sót còn tồn tại / BLOCKED cho video
- `BF-AUTOSCAN`
- `BF-CATBD`
- `BF-ISURVEY_ABOUT`
- `BF-ISURVEY_DGN_TO_CAD`
- `BF-ISURVEY_EXPORT_KML`
- `BF-ISURVEY_FIT_TITLE`
- `BF-ISURVEY_FRAME_EDIT`
- `BF-ISURVEY_HDSD`
- `BF-ISURVEY_IMPORT_KML`
- `BF-ISURVEY_KHUNG`
- `BF-ISURVEY_KHUNG_BV`
- `BF-ISURVEY_LOGIN`
- `BF-ISURVEY_LOGO`
- `BF-ISURVEY_LOGO1`
- `BF-ISURVEY_LOGO2`
- `BF-ISURVEY_LOGO_REPLACE`
- `BF-ISURVEY_MAP`
- `BF-ISURVEY_MAP_SAT`
- `BF-ISURVEY_PALETTE`
- `BF-VEMCD`
- `BF-ISURVEY_PROFILE_SHEETS`
- `BF-XOACOC`
- `BF-ISURVEY_ROUTE_PLOT`
- `BF-DAODAUTUYEN`
- `BF-ISURVEY_ROUTE_SHEETS`
- `BF-PHATSINHCOC`
- `BF-THEMCOC`
- `BF-BANGCOC`
- `BF-XOACOC1`
- `BF-ISURVEY_STRIP_MAP`
- `BF-ISURVEY_TAB_TO_CAD`
- `BF-THEMMOHINH`
- `BF-XAYMOHINH`
- `BF-XOATHANHPHAN`
- `BF-GOMOHINH`
- `BF-ANTIN`
- `BF-ISURVEY_UAV_CLIP`
- `BF-ISURVEY_UPDATE`
- `BF-ISURVEY_UPDATE_GE`
- `BF-NHAPDIEM`
- `BF-STT`
- `BF-XUATDIEM`

## Không được phép đưa vào video tự động
Mọi `disposition=PARTIAL_BLOCKED` trong `02_FEATURE_DISPOSITION_52_USER.csv`.

## Git
- HEAD trước: xem `_git_head_before.txt` (`3a16cb3959dc4e65c08f77b6de26669b309739cd`)
- HEAD sau: xem `_git_head_after.txt` (kỳ vọng trùng)
- Không sửa tracked `src/`
- Chỉ tạo `docs/audits/..._FINAL_MANUAL/` (+ ZIP)

## Đề xuất kết luận
**GO_FOR_PROMPT_MASTER_DESIGN_WITH_BLOCKED_FEATURES**

Lý do: P0 đã có bằng chứng hoặc BLOCKED rõ; 52 USER có trạng thái riêng; gap liệt kê; runtime NOT_TESTED. Chủ dự án quyết định chấp nhận ngoại lệ để thiết kế Prompt Master.

**DỪNG — không mở R02E/R02F.**

## K?t qu? Git sau d?t FINAL_MANUAL

| Ki?m tra | K?t qu? |
|---|---|
| HEAD tru?c/sau | 3a16cb3959dc4e65c08f77b6de26669b309739cd / 3a16cb3959dc4e65c08f77b6de26669b309739cd |
| Tracked src/ | Kh�ng d?i |
| Diff name-only | docs/audits/2026-11-10_ISURVEY_VIDEO_FACTORY_SOURCE_AUDIT.zip |
| Untracked m?i | docs/audits/..._FINAL_MANUAL/ + ZIP |

