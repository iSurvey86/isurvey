# 2026-10-27 — DANTRANG hộp thoại và đăng xuất từ xa

Máy văn phòng, Civil 3D 2026. Phát hành **1.8.7**.

## DANTRANG

Hộp thoại chia hai cột: khổ giấy / tỷ lệ / chồng gối, và Hàng / Cột / Cách.

Nhập TXT/CSV: mỗi dòng `MÃ;nội dung` (tab hoặc dấu phẩy đầu tiên cũng được). Bỏ dòng `#` và `//`. Không nhập ô Tờ và Tỷ lệ. Đã thử hai ô trên bản vẽ trắng, rồi khôi phục `route_layout.json` — chữ thử không còn trong thiết lập đã lưu.

Chèn chữ ký: chọn Họ tên 1–5, chọn file DWG. Lần đầu nét nằm đè tên và tràn ra bản đồ. Đã sửa để nằm trong ô trống bên phải tên (A3: vạch đứng x=390 tách tên và chữ ký). Đã nhìn lại trên tờ A3 1/1000.

Sau Tạo Layout: hộp “Dàn trang in đã xong”, kèm tên layout, số tờ, và dòng chưa in PDF thì gõ INTUYEN.

Tên PDF gợi ý `tên bản vẽ-yyyyMMdd-HHmm` (không dấu hai chấm). Bản vẽ chưa lưu thì gốc tên là `BanVe`. Đã thấy `BanVe-20261027-0919` trong hộp lưu, bấm Cancel.

Tick in PDF tắt sẵn. Hỏi ghi đè layout và chọn layout khi in lại dùng số `1`, `2`, vì chữ có dấu cách làm hỏng bấm chọn trên F12.

Đã thử trên `Drawing1` (acadiso), đóng không lưu. Không mở bản vẽ tuyến.

## INTUYEN

`ISURVEY_ROUTE_PLOT` / **INTUYEN** in lại layout `BanDo_Tuyen*` đã có, không dựng trang mới. Một layout thì không hỏi. Nhiều layout thì `1`, `2`, … tối đa 9.

## Đăng xuất từ xa

Khóa (`is_active`) vẫn chặn cho đến khi Mở. Đăng xuất chỉ bật `force_logout`. Máy vẫn Active. Đăng nhập lại thì `isurvey_activate` xóa cờ.

Plugin từ 1.8.7 hỏi `isurvey_check` khoảng mỗi 2 phút (`p_touch=false`, không đẩy “lần thấy”). Thấy `logout` thì xóa giấy phép trên máy, hiện hộp đăng nhập lại, không gọi `isurvey_release_device`. Thấy `blocked` hoặc `not_registered` thì khóa như kiểm tra 24 giờ. Bản 1.8.6 không đọc cờ.

SQL `deploy/supabase/schema_force_logout.sql`: lần chạy đầu lỗi `cannot change name of view column activated_at to force_logout`. Sửa bằng `DROP VIEW` rồi tạo lại. Lần sau: Success. No rows returned.

Nút **Đăng xuất** và nhãn “Chờ thoát” ở repo ksnpsc, tab Thiết bị. Chưa bấm đăng xuất trên máy này.

## Việc chưa làm

- Alignment Civil làm đầu vào DANTRANG.
- Các máy UPD lên 1.8.7.
- Thử nút Đăng xuất trên trang quản trị sau khi ksnpsc lên site, với một máy đã cài 1.8.7.
