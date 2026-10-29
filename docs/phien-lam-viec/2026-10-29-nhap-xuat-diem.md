# 2026-10-29 — Nhập xuất điểm đo, STT

Máy văn phòng, Civil 3D 2026. Cuối phiên đầy đủ. Đã phát hành **1.8.10**.

## Nhập điểm đo

**NHAPDIEM** (`ND`) đọc CSV hoặc TXT. Cột có 7 chữ số trước dấu thập phân là X (Bắc). Cột có 6 chữ số là Y (Đông). Hộp thoại có cột STT từ 1 đến hết, lưới ô mờ, và dòng tổng số điểm đo.

Khi tạo điểm, dấu điểm nằm trên `iSurvey_DiemDo`, tên điểm trên `iSurvey_DiemDo_Ten`, cao độ trên `iSurvey_DiemDo_CaoDo`. Ba layer có ba màu. Tạo xong thì phóng vừa vùng điểm.

Đã nhập 5 điểm trên bản vẽ mới, thấy đủ ba màu và khung nhìn vừa điểm, rồi đóng không lưu.

## Xuất điểm đo

**XUATDIEM** (`XD`) ghi cột cách nhau bằng chấm phẩy. Thập phân là dấu chấm, mặc định 3 số lẻ. Tên xếp từ số nhỏ đến lớn, rồi A–Z.

Đã mở file xuất bằng Excel. Tọa độ giữ phần thập phân. Tên số đứng trước tên chữ.

## Số thứ tự

**STT** vẽ chữ, vòng tròn và wipeout trên layer `iSurvey_STT`. Số đã có trên layer cũ `KS-STT` không tự chuyển.

## Danh sách lệnh

Ba lệnh đã có dòng trong `docs/Danh_sach_lenh_iSurvey.xlsx`. Không cần ghi chú thêm. Cột phiên bản của ba dòng là 1.8.10, vì gói 1.8.9 đã lên máy chủ trước khi có các lệnh này.

## Lệnh tắt

Thêm `UG`, `AK`, `AD`, `SK`, `AIC`, `ND`, `XD` cho lệnh đã có. Hai lệnh rải khung dùng để kiểm thử đã bỏ khỏi mã nguồn.
