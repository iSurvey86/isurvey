# XAYMOHINH — Xây dựng lưới (tiến trình + Thành công) (1.8.19)

## Mục tiêu

Cho thấy xây TIN: chữ tiến trình dễ hiểu, hộp Thành công khi xong, sau đó mở Tạo tuyến không đơ (NOD không còn nhồi tọa độ Mesh).

## Bản vẽ

- Bản vẽ mới hoặc bản sao `docs/MCD/dongmuc3D.dwg` (không ghi đè bản đang sửa của người dùng).
- Mô hình đã có đường đồng mức 3D (và đường bao nếu có).

## Các bước quay

1. Mở Civil 3D Metric, mở bản vẽ thử, tab **iSurvey**.
2. Mở **Bảng dữ liệu** — chọn mô hình có đồng mức.
3. Chạy **Xây dựng TIN** / lệnh `XAYMOHINH`.
4. Giữ khung hình hộp «Xây dựng TIN»: chữ trạng thái đổi (đọc nguồn → gắn đứt gãy/đồng mức → dựng lưới → khớp đường vào lưới → vẽ / lưu), thanh tiến trình không đứng 0% vô hạn.
5. Kết thúc: hộp **Thành công** báo số tam giác; lưới hiện trên bản vẽ.
6. Chạy **TAOTUYEN** (hoặc mở BANGIS) — hộp thoại mở ngay, không đứng im lâu.

## Ghi chú

- File lớn có thể mất vài phút ở bước khớp đường vào lưới; quan trọng là có tiến trình, không đơ im.
- Không dùng video AI giả hộp thoại.
