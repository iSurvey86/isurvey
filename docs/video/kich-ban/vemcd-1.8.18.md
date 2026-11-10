# Vẽ mặt cắt dọc tự nhiên phiên bản 1.8.18

Lệnh tắt `VEMCD`. Lệnh đủ `ISURVEY_PROFILE_DRAW`. Hành vi Zero-Clutter đóng gói trong iSurvey **1.8.18** (UI chốt 2026-11-06 dưới 1.8.17 sau phát hành, bump 10/11/2026): hộp 2 cột — trái tỷ lệ V|H + Cao độ Min|Cách đầu bảng, checkbox, CTA 2×2; phải mẫu bảng STT/Nội dung/Cao chữ/Cao hàng (không Layer trên lưới). Z luôn 20 m; chữ thước/MSS=12; luôn MSS + đầu bảng + thước trái; ByLayer; batch `Y_min − 50`. KC 100m; hướng ADS; khoảng néo; chữ thập. RAIMCD chỉ rải khung.

File máy đọc: `vemcd-1.8.18.json`. Câu thoại nằm trong json. Trang này là việc phải làm trên Civil khi quay.

## Ngoài camera

Máy đã đăng nhập iSurvey. Bản vẽ mới đã có mô hình, tuyến và cọc có Z. Đứng ở Model Space. Không mở bản vẽ công trình. Không ghi đè file người dùng đang sửa.

## Trên camera

Raw ghi vào `docs/video/thanh-pham/vemcd-1.8.18/raw/<id>.mp4`.

| Cảnh | Việc trên màn hình |
|---|---|
| 01-mo-dau | Thẻ chữ, không quay Civil |
| 02-hop | Gõ `VEMCD`. Trái: tỷ lệ 2×2, checkbox, CTA 2×2 dưới **Vẽ nhiều tuyến**. Phải: tiêu đề + Nhập/Xuất/+/−; Rộng đầu bảng \| Mẫu; lưới không Layer. Footer Hủy/Lưu. |
| 03-chen | Chỉ góc dưới-trái cột tên. Hiện bảng, MSS, thước trái, đường đất. Zoom chữ dọc; KC 100m; hướng ADS. |
| 04-ket | Thẻ chữ, không quay Civil |

Không để hộp đăng nhập, email, hay đường dẫn thư mục cá nhân chiếm khung hình.
