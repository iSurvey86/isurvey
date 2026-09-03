# 2026-09-03 — Export KML/KMZ + TM-6 + .NET 8

## Ngữ cảnh

Máy văn phòng: Civil 3D 2026 (R25.1) host .NET 8 → retarget `net8.0-windows`. Cuối phiên đầy đủ bump **1.1.0**.

## Trạng thái đã chốt

- `ISURVEY_EXPORT_KML` / alias **EG** — CAD → Google Earth (mặc định KMZ).
- Style: màu CAD 1:1 RGB → KML `aabbggrr`; polyline/outline; không explode block; ẩn pin vàng.
- Map + Export: radio **Múi 3° / Múi 6°**; TM-6 snap CM **105 / 111 / 117** theo Thông tư 973/2001/TT-TCĐC; cùng TOWGS84.
- Alias: **IG** / **SG** / **XG** / **EG**.
- Catalog lệnh: `docs/Danh_sach_lenh_iSurvey.xlsx`.
- Deploy: `build-bundle.ps1` path net8; `System.Drawing.Common` optional.

## Học được

- Remap đen→trắng / luminance làm lệch màu CAD (vd xanh đậm → trắng) — bỏ, giữ RGB 1:1.
- Telex: tránh alias 2 ký tự kiểu `IS` → í; kiểm tra `acad.pgp` trước khi gán.

## Việc mở

- Mosaic lớn cho Layout/in (chưa).
- Palette dọc UI (đã thảo luận, hoãn).
- HDSD / workflow user cuối.
