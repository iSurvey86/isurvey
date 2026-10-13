# 2026-10-13 — Vá hộp thoại kinh tuyến trục

## Ngữ cảnh

Máy văn phòng, Civil 3D 2026. Sau khi cài 1.5.0, user chạy nhập KML. Dòng lệnh báo:

`[iSurvey] Không mở được hộp thoại: Set property 'System.Windows.Controls.ItemsControl.ItemTemplate' threw an exception.`

## Trạng thái đã chốt

- Nguyên nhân: `ProvinceCombo` vừa có `DisplayMemberPath="ProvinceName"` vừa có `ItemTemplate`. WPF từ chối và hộp thoại không tạo được.
- Sửa: giữ `DisplayMemberPath`, bỏ `ItemTemplate`. Nhãn **Chọn kinh tuyến trục** không đổi.
- Bump **1.5.1**. Bundle đã build và cài. User sẽ đóng Civil rồi test `INK`.

## Việc mở

- Xác nhận hộp thoại mở và danh sách là tên tỉnh.
- Test lọc đỉnh và luồng `EXK`.
- CAD 2024, 2025, 2027 chưa chạy.
- Thu hồi giấy phép, hết 2 thiết bị, `LGO`.
- `CBD`, `SCN`, EG Lào Cai. MSI khi được yêu cầu.
