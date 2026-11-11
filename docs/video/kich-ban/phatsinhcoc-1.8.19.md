# PHATSINHCOC — Phát sinh cọc (tra Z nhanh) (1.8.19)

## Mục tiêu

Cho thấy phát sinh cọc trên tuyến gắn mô hình TIN lớn (nhiều đồng mức) không còn đứng lâu ở từng cọc; thanh tiến trình tăng đều.

## Bản vẽ

- Bản vẽ mới hoặc bản sao có tuyến + mô hình đã dựng TIN (vd. bản thử đồng mức 3D).
- Không ghi đè bản đang sửa của người dùng.

## Các bước quay

1. Mở Civil 3D Metric, bản vẽ thử, tab **iSurvey**.
2. Chọn tuyến đã gắn mô hình địa hình.
3. Chạy **Phát sinh cọc** / `PHATSINHCOC` (bước lý trình vừa phải).
4. Hộp tiến trình: dòng trạng thái tăng số cọc; không đơ nhiều giây giữa các lần cập nhật.
5. Kết thúc: cọc có Z trên chữ (hoặc báo chưa có Z nếu ngoài lưới).

## Ghi chú

- Trước sửa: mỗi cọc quét lại mọi Face TIN → rất chậm với lưới lớn.
- Không dùng video AI giả hộp thoại.
