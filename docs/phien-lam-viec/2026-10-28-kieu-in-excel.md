# 2026-10-28 — Kiểu in, Excel, lề mặt cắt

Máy văn phòng, Civil 3D 2026. Cuối phiên đầy đủ. Đã phát hành **1.8.9**.

## Kiểu in

Hộp thoại rải khung có **In màu** và **In đen trắng**. In màu gắn `acad.ctb`. In đen trắng gắn `monochrome.ctb`. Độ dày nét lấy theo file trên máy. Plugin không sửa hai file này.

RAIKHUNG và RAIMCD nhớ lựa chọn riêng. INTUYEN đọc bảng nét đang gắn trên layout rồi in lại đúng bảng đó. Layout cũ còn `iSurvey_005.ctb` thì lần in lại vẫn dùng bảng đó.

Đã tạo layout mặt cắt trên bản sao với In đen trắng. Thuộc tính layout đọc lại là `monochrome.ctb`. Đóng không lưu.

## Nhập Excel

Nút Nhập Excel/CSV đọc cột STT làm mã và cột Nội dung làm giá trị. File `.xlsx` và `.xls` đều đọc được, không cần Excel COM. Ngày lưu dạng số tháng/năm thành `10/2026`. Ô Tờ bỏ qua vì vẫn tự điền `i/N`.

Đã mở hộp thoại RAIKHUNG trên bản vẽ mới, nhập thử cả hai định dạng, thấy 24 ô và ngày `10/2026`, rồi bấm Hủy bỏ. Không tạo layout.

## Mặt cắt dọc

Trục đứng lùi 2,5 m về bên trái điểm chọn, nên thước cao độ và nét đứng nằm trong viewport. Từ góc dưới-trái điểm chọn, layout đặt mặt nạ che phần thừa bên trái và bên dưới. Đối tượng trên Model không bị xóa.

Đã xem layout `MatCat_Doc` trên bản sao `MCD2.dwg`: số cao độ, thước xanh và các nét đứng hiện lại. File gốc trong `docs\MCD` không mở để ghi và không đưa vào git.

## Hộp thoại

Một cửa sổ hai cột, cao vừa nội dung. Cột trái là khổ giấy, tỷ lệ, kiểu in, chồng gối và lưới. Cột phải là đủ 25 ô, không thanh cuộn. Ô nội dung dài có dấu `...`. Rê chuột vào ô bị khuất thì hiện đủ câu. Đã mở trên bản sao và hủy, không tạo layout.
