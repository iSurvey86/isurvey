# THEMMOHINH — Thêm điểm vào mô hình (1.8.16)

## Mục tiêu
Cho thấy hộp «Thêm điểm» và mục **Block + chữ cao độ cạnh**: XY lấy từ block, Z lấy từ chữ số gần block, không sửa block trên bản vẽ.

## Chuẩn bị
- Civil 3D Metric, bản vẽ mới.
- Đã có mô hình (`TAOMOHINH`).
- Vài block Z = 0 và chữ số cao độ cạnh block (dấu phẩy thập phân cũng được).

## Các bước quay
1. Mở BANGIS → chuột phải **Điểm** dưới mô hình → **Thêm** (hoặc gõ `THEMMOHINH` → Điểm).
2. Hộp thoại: tắt «Chữ là số» nếu chỉ muốn Z gắn block; bật **Block + chữ cao độ cạnh**.
3. Chọn cửa sổ gồm block và chữ → Enter.
4. Dòng lệnh báo đã gắn tạm Z.
5. `XAYMOHINH` rồi `TRACAOD` gần một block: Z trùng số trên chữ.

## Không làm
- Không ghi đè bản vẽ người dùng đang sửa.
- Không dùng video AI giả hộp thoại.
