# 2026-10-30 — Hướng dẫn web, xuất điểm, mộc PDF

Máy văn phòng, Civil 3D 2026. Cuối phiên đầy đủ. Đã phát hành **1.8.14**.

## Hướng dẫn

**HD** (`ISURVEY_HDSD`) mở https://ksnpsc.vercel.app/huong-dan/isurvey. Nút nằm ở Tài khoản và Hệ thống. Không cần đăng nhập.

Trang hướng dẫn đã có trên ksnpsc trước bản này: danh sách lệnh hai cột, số La Mã, tên TAB và DGN viết rõ việc chuyển file, yêu cầu máy ghi AutoCAD hoặc Civil 3D từ 2022 đến 2026.

## Xuất điểm đo

**XUATDIEM** chọn Point, Block, Text hoặc MText. Chữ đọc được thành số thì thành cao độ và để trống tên. Block lấy tên và mã từ thuộc tính quen thuộc, không có thì lấy điểm chèn.

CSV dùng dấu phân cách danh sách của Windows. TXT dùng tab. Thập phân luôn là dấu chấm.

## In và mộc đỏ

Sửa khung chọn ảnh PNG. Khi xuất PDF, vòng mực đỏ 36 mm chỉ nằm trên file in, rồi được gỡ khỏi bản vẽ. Hộp báo in xong có nút Mở thư mục.

Nhắp đúp khung vẫn mở hộp sửa của iSurvey. Hộp thuộc tính của AutoCAD bị giữ thêm 700 ms.

Chưa mở lại một PDF tuyến thật trong phiên phát hành này để đo vòng mộc và xem hộp EATTEDIT.

## Danh sách lệnh

`docs/Danh_sach_lenh_iSurvey.xlsx`: dòng HD, XUATDIEM, INTUYEN và SK ghi phiên bản 1.8.14, ngày 30/10/2026.

## Không đưa vào git

Bản vẽ và seed trong `docs/MCD`, `docs/SEED 1000`, `docs/dau`, cùng file `.bak` mẫu khung.
