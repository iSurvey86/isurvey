# 2026-10-27 — DANTRANG và sửa UPD

Máy văn phòng, Civil 3D 2026. Phát hành **1.8.6**.

## DANTRANG

Lệnh dàn trang in dọc tuyến: chọn polyline/line/arc/spline (Alignment chưa thử) → hộp thoại khổ, tỷ lệ (thêm 1/10000), chồng gối, lưới Hàng × Cột × Cách mm. Một layout `BanDo_Tuyen` (trùng thì hỏi ghi đè hoặc `BanDo_Tuyen-2`). Mỗi tờ: block `KB_A*` 1:1 giấy + viewport khóa, Twist để tuyến nằm ngang. Ô Tờ = `i/N`, tỷ lệ tự điền. Model chỉ có đường bao mảnh (layer `ISURVEY_MANH`, không in).

Kích đúp sửa ô khung tên. Bỏ khoảng trắng thừa. STT 1/5: Thay Logo / Ẩn logo. Tick in PDF gộp, cửa sổ in = cả tờ giấy (giữ lề 25 mm).

Đã in thử headless: tuyến 3921,9 m A3 1/2000 → 7 trang PDF; A4 1/500 → 54 trang. User hài lòng bản vẽ.

Không gộp với `DUOITUYEN` (cắt dải 100 m mỗi bên, nhân bản hình học trên Model để dựng mặt cắt dọc).

## UPD

Trước đây treo vì `Quit()` khi hộp thoại modal còn mở; helper cũ chờ 30 phút im lặng, không ghi kết quả. Nhật ký chỉ thấy `update_check`.

Bản này: đóng hộp thoại rồi mới thoát Civil. Helper đợi DLL nhả khóa (90 giây), thử cài lại, ghi `last-result.json` + `update.log`, gửi `update_result`, mở lại Civil. Đã mô phỏng helper: cài thành công 2,6 giây; thiếu Setup ghi `stage=setup`.

Server trước phát hành: 1.8.4. Máy này đã 1.8.5 (git, chưa lên Storage). Người dùng còn helper cũ phải cài tay zip 1.8.6 một lần.

## Việc chưa làm

- Alignment Civil làm đầu vào DANTRANG: chưa thử.
- Lệnh in PDF riêng (`INTUYEN`) khi layout đã có: chưa làm; in kèm lúc tạo hoặc chạy lại DANTRANG + ghi đè.
- Máy khác: UPD lên 1.8.6 sau khi có bản này trên Storage.
