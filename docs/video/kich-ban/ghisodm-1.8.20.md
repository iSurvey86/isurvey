# GHISODM — Ghi Số Đồng Mức (1.8.20)

## Mục tiêu

Cho thấy hộp thoại WPF «Ghi Số Đồng Mức» khi gõ `GHISODM`: 3 phương án (Chữ / Số cố định / Click tuần tự), Z gốc và bước nhảy cùng hàng, gán `Elevation`, rồi thêm vào mô hình không còn cảnh báo thiếu cao độ.

## Bản vẽ

- Bản vẽ mới hoặc bản sao có polyline đồng mức 2D (Elevation = 0) và chữ số cao độ cạnh đường.
- Không ghi đè bản đang sửa của người dùng.

## Các bước quay

1. Civil 3D Metric, tab **iSurvey**.
2. **Ghi Số Đồng Mức** / `GHISODM` → hộp thoại: chọn phương án (mặc định Click Tuần tự); Z gốc và Bước nhảy cùng hàng; tăng/giảm.
3. **Chấp nhận** → trên bản vẽ chọn polyline (hoặc polyline + TEXT nếu chế độ Chữ). Đường đổi Magenta; nhãn `iSurvey_NhanDDM`.
4. Thêm đồng mức vào mô hình: không còn hộp cảnh báo thiếu cao độ (hoặc ít hơn).
5. Xây dựng TIN: lưới có thang màu theo Z.

## Ghi chú

- Không dùng video AI giả hộp thoại.
