# 2026-11-06 — VEMCD UI Zero-Clutter (không bump)

Máy văn phòng, Civil 3D 2026 Metric. Cuối phiên đầy đủ. **Không bump.** Vẫn **1.8.17** (đã phát hành 2026-11-03).

## Đã làm

- **VEMCD** Zero-Clutter UI (đã nhìn hộp trên bản sao `_test_vemcd_ui.dwg`, không ghi đè bản vẽ công trình):
  - Bỏ ô **Cao chữ thước/MSS** → lõi luôn `ProfileTextHeight = 12` (`NormalizeHardCodedOptions`).
  - Bỏ ô **Rộng thước tỷ lệ** → vạch thước `tick = textHeight × 0.55` (clamp theo cột đầu bảng).
  - Cột trái: lưới 2×2 (Tỷ lệ V|H, Cao độ Min|Cách đầu bảng); checkbox; **CTA 2×2** dưới «Vẽ nhiều tuyến».
  - Footer chỉ **Hủy bỏ** / **Lưu thiết lập**.
  - Cột phải: bỏ cột **Layer** trên lưới (Layer vẫn trong mẫu/model); toolbar tách tiêu đề + Nhập/Xuất/+/− và hàng Rộng đầu bảng | Mẫu bảng; cột Nội dung rộng hơn.
  - Giữ: Z=20; luôn MSS + đầu bảng + thước trái; ByLayer; Nội dung chỉ đổi Label; batch mọi tuyến có cọc `Y_min − 50`.
- Catalog Excel, README, CHANGELOG, cập nhật kịch bản `vemcd-1.8.17` (Chưa quay). Không quay, không đăng. Không phát hành lại.

## Catalog

Sửa **VEMCD**. Phiên bản 1.8.17, ngày 06/11/2026.

## Chưa làm

Quay video khi được yêu cầu. YouTube vẫn thiếu `client_secret.json`. Bump / phát hành bản sau khi được bảo.
