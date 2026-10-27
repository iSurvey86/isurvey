# 2026-10-27 — RAIKHUNG và RAIMCD

Máy văn phòng, Civil 3D 2026. Cuối phiên đầy đủ. **Không bump phiên bản, không phát hành.** Gói cài và UPD vẫn là 1.8.7. DLL bundle 2025 trên máy này đã là bản có lệnh mới.

## RAIKHUNG

`ISURVEY_ROUTE_SHEETS` đổi alias từ DANTRANG sang **RAIKHUNG**. `CATTUYEN` không còn.

Hộp thoại: khổ giấy, tỷ lệ, chồng gối và Hàng / Cột / Cách nằm một hàng. Bỏ dòng ghi chú tuyến dài. Mục 2 tiêu đề «2. THÔNG TIN KHUNG TÊN». Cột STT và các tiêu đề bảng căn giữa. Đầu cột là «Nội dung», không có dấu hai chấm. Không thanh cuộn.

Tick «Tự động gộp, xuất pdf sau khi tạo» màu xanh lá, bật sẵn khi mở hộp để tạo trang. Lệnh nhanh (`ISURVEY_ROUTE_SHEETS_QUICK`) vẫn không tự in PDF.

Chọn file chữ ký `.dwg` cho Họ tên 1–5 thì nút **Chèn chữ ký** đổi thành **Xóa chữ ký**.

Nhấp đúp block khung `KB_A0`…`KB_A4` mở hộp sửa thông tin, thay cho sửa thuộc tính của AutoCAD.

Đã xem hộp thoại trên bản vẽ trắng, rồi đóng không lưu.

## INTUYEN

Hỏi bằng số: `1` in khung đang chọn (chọn sẵn hoặc chọn thêm), `2` in mọi khung trên layout. Có cửa sổ tiến trình khi xuất PDF. Layout mặt cắt `MatCat_Doc` cũng nằm trong danh sách, không chỉ `BanDo_Tuyen`.

## RAIMCD

`ISURVEY_PROFILE_SHEETS` / **RAIMCD**. Một lần chọn: góc dưới-trái của hàng cuối cần in, giao của mép trái bảng và đáy hàng Khoảng cách cộng dồn. Không chọn đầu phải tuyến, không chọn mặt bằng.

Hộp thoại cùng kiểu RAIKHUNG. Tỷ lệ ghi trên khung là cặp in ấn, không phải tỷ lệ dựng trong phần mềm:

- Mặc định `1/500; 1/5000`, viewport khóa 1:5000.
- Ít gặp: `1/200; 1/2000`, viewport khóa 1:2000.

Không ghi `1/100; 1/1000` lên tờ. Không chọn tỷ lệ cho vừa trang. Dải cao hơn vùng vẽ thì tăng khổ A3 → A2 → A1 → A0 và giữ tỷ lệ.

Mỗi tờ một khung kỹ thuật `KB_A*` (không lưới tọa độ, không mũi tên Bắc), hai viewport cùng tỷ lệ và cùng cao độ: cột tên bên trái đứng yên, lý trình bên phải trượt. Chồng gối mặc định 100 m. Tờ cuối canh đúng cuối mặt cắt nên có thể chồng nhiều hơn 100 m. Các tờ xếp lưới trên một layout `MatCat_Doc`.

Mặt đất (trung vị đỉnh các tung độ) đặt khoảng 64% chiều cao khung, tính từ đáy, và vẫn giữ hàng cắt cùng lý trình trong khung. Góc trái cách mép trong khung mẫu khoảng 2 mm (mép trong ở 20 mm kể từ gốc block). Ô tỷ lệ rộng 168 px để thấy hết `1/500; 1/5000`.

Đã chạy trên bản sao `%TEMP%\isurvey-mcd2-test.dwg` (hai tờ A3) và `%TEMP%\isurvey-mcd1-test.dwg` (hai tờ A2, sau khi nâng mặt đất và kéo góc trái). Hộp thoại lần sau bấm Escape, không xuất PDF. Đóng bản sao không lưu, xóa file tạm. `docs\MCD\MCD1.dwg` và `MCD2.dwg` không đổi. Hai file này, file `.bak` và `desktop.ini` không đưa vào git.

## Việc chưa làm

- Phát hành (khi đó mới tăng số phiên bản) rồi các máy UPD.
- Alignment Civil làm đầu vào RAIKHUNG.
- Mẫu mặt cắt chỉ có trên máy này, trong `docs\MCD`.
