# 2026-09-02 — Nền tảng Map tile GE + VN-2000 + handoff

## Ngữ cảnh

Repo **iSurvey**. Cuối phiên đầy đủ: dọn handoff ksnpsc, dựng quy trình phiên riêng dự án.

## Trạng thái đã chốt

- Tile 256×256 + cache + AutoRefresh + FitZoom ≤128.
- Clip polyline khi chèn; xóa GE = xóa hết (confirm).
- Bundle + TRUSTEDPATHS; AppVersion **1.0.0** (không bump — phiên docs).
- `docs/phien-lam-viec`, mẫu tư vấn/vá lỗi, `docs/changelog/CHANGELOG.md`.

## Học được (UX / in)

- Layout/print chỉ thấy tile đã tải theo viewport Model lúc refresh — không phải loader Layout riêng.
- In “một tấm liền” thường cần mosaic/export một file, không phải live nhiều tile 256.

## Việc mở

- Mosaic export cho in (chưa làm).
- Settings DWG / UI AutoRefresh (chưa làm).
- HDSD / workflow module Map (chưa làm).
