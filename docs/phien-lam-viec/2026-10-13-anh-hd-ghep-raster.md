# 2026-10-13 — Ảnh HD ghép một raster

## Ngữ cảnh

Civil 3D 2026. Ảnh xem lướt đã chạy, nhưng còn mờ (zoom theo màn hình, trần 128 ô) và còn rãnh giữa các `RasterImage`. User muốn một chế độ in nét, không phá AutoRefresh.

## Trạng thái đã chốt

- `ISURVEY_MAP_SAT`: 1 = xem lướt, 2 = ghép HD theo đường bao.
- Zoom 18 / 19 / 20. Trần 400 ô và 32768 px mỗi cạnh.
- `lyrs=s&scale=2`, một JPEG, một raster, clip, send to back, không AutoRefresh.
- Refresh bỏ qua tiền tố `ISURVEY_HD_`. Xóa GE vẫn xóa ảnh HD.
- Bump **1.5.2**. Bundle đã cài sau khi tắt Civil.

## Việc mở

- Test chế độ 2 trên đường bao thật.
- Các mục test còn mở từ 1.5.0 / 1.5.1 (INK, EXK, lọc đỉnh, CAD năm khác, giấy phép).
