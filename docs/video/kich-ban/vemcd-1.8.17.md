# Vẽ mặt cắt dọc tự nhiên phiên bản 1.8.17

Lệnh tắt `VEMCD`. Lệnh đủ `ISURVEY_PROFILE_DRAW`. Hành vi có từ iSurvey 1.8.17 (Pha 3, cập nhật UI 2026-11-06): hộp thoại rút gọn — ẩn khoảng cách lấy Z (lõi luôn 20 m), luôn MSS + đầu bảng + thước trái (tắt thước phải); lưới chỉ Nội dung / Layer / Cao chữ / Cao hàng (ByLayer); cột Nội dung chỉ đổi nhãn in; **Vẽ nhiều tuyến** = toàn bộ tuyến có cọc, dàn trang `Y_min − 50`. Mẫu 8 hàng; chữ đầu bảng 12; Cách đầu bảng; chữ dọc cao độ/KC; KC 100m; hướng ADS; khoảng néo; chữ thập. RAIMCD chỉ rải khung.

File máy đọc: `vemcd-1.8.17.json`. Câu thoại nằm trong json. Trang này là việc phải làm trên Civil khi quay.

## Ngoài camera

Máy đã đăng nhập iSurvey. Bản vẽ mới đã có mô hình, tuyến và cọc có Z. Đứng ở Model Space. Không mở bản vẽ công trình. Không ghi đè file người dùng đang sửa.

## Trên camera

Raw ghi vào `docs/video/thanh-pham/vemcd-1.8.17/raw/<id>.mp4`.

| Cảnh | Việc trên màn hình |
|---|---|
| 01-mo-dau | Thẻ chữ, không quay Civil |
| 02-hop | Gõ `VEMCD`. Trái: tỷ lệ 2×2, checkbox, CTA 2×2. Phải: tiêu đề + Nhập/Xuất/+/−; Rộng đầu bảng | Mẫu; lưới STT/Nội dung/Cao chữ/Cao hàng (không Layer). Footer Hủy/Lưu. |
| 03-chen | Chỉ góc dưới-trái cột tên trên Model. Hiện đầu bảng, MSS, thước trái, đường đất. Zoom chữ dọc cao độ / khoảng cách lẻ. Zoom hàng Khoảng cách 100m và hướng tuyến ADS. (Tuỳ chọn: bật Vẽ nhiều tuyến nếu bản vẽ có ≥2 tuyến có cọc — MCD xếp xuống dưới cách 50.) |
| 04-ket | Thẻ chữ, không quay Civil |

Không để hộp đăng nhập, email, hay đường dẫn thư mục cá nhân chiếm khung hình.
