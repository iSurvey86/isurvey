# Vẽ mặt cắt dọc tự nhiên phiên bản 1.8.17

Lệnh tắt `VEMCD`. Lệnh đủ `ISURVEY_PROFILE_DRAW`. Hành vi có từ iSurvey 1.8.17 (Pha 3, cập nhật 2026-11-05): hộp thoại 2 cột + footer; mẫu bảng 8 hàng (sơ họa 200, còn lại 50); chữ đầu bảng cao 12, lệch mép 40; **Cách đầu bảng** + đường dóng nét đứt; chữ dọc cao độ/KC; hướng ADS; không cắt A3; mỗi lần vẽ chỉ điểm chèn. RAIMCD chỉ rải khung in lên hình đã vẽ.

File máy đọc: `vemcd-1.8.17.json`. Câu thoại nằm trong json. Trang này là việc phải làm trên Civil khi quay.

## Ngoài camera

Máy đã đăng nhập iSurvey. Bản vẽ mới đã có mô hình, tuyến và cọc có Z. Đứng ở Model Space. Không mở bản vẽ công trình. Không ghi đè file người dùng đang sửa.

## Trên camera

Raw ghi vào `docs/video/thanh-pham/vemcd-1.8.17/raw/<id>.mp4`.

| Cảnh | Việc trên màn hình |
|---|---|
| 01-mo-dau | Thẻ chữ, không quay Civil |
| 02-hop | Gõ `VEMCD`. Hộp 2 cột: trái (tuyến, khoảng cách Z, tỷ lệ V/H, mức so sánh, **Cách đầu bảng**, tùy chọn); phải (mẫu bảng 8 hàng, Nhập/Xuất). Footer: **Vẽ trắc dọc tự nhiên**. |
| 03-chen | Chỉ góc dưới-trái cột tên trên Model. Hiện đầu bảng và đường đất phía trên đường dóng nét đứt. Zoom chữ dọc cao độ / khoảng cách lẻ. Zoom hướng tuyến: `/\` hoặc `\/` với `A = …` căn giữa V. |
| 04-ket | Thẻ chữ, không quay Civil |

Không để hộp đăng nhập, email, hay đường dẫn thư mục cá nhân chiếm khung hình.
