# 2026-10-31 — Nhập điểm và mô hình địa hình

Máy văn phòng, Civil 3D 2026. Cuối phiên đầy đủ. Bump **1.8.16**, chưa phát hành. Gói UPD trên máy chủ vẫn là 1.8.14.

## Đã làm

- **NHAPDIEM**: sửa dòng, thêm và xóa điểm, cao chữ, kiểu điểm, cỡ điểm theo phần trăm. Bảng giữ Bắc/Đông. Bản vẽ nhận Đông trên X và Bắc trên Y.
- **TAOMOHINH**, **THEMMOHINH**, **XAYMOHINH**, **ANTIN**, **TRACAOD**. Không có cạnh dài nhất. Đứt gãy và đồng mức là cạnh bắt buộc. Đường bao cắt lưới. Không có bao thì lưới là bao lồi.
- **BANGIS** hiện mô hình đã lưu. Tuyến và trắc dọc chưa có đối tượng.
- Đã nhìn hộp tạo mô hình trên bản vẽ trắng Drawing1. Kiểm tra tính toán của lưới đúng với một cạnh bắt buộc và một điểm ngoài bao. Chưa vẽ lưới trên bản vẽ có điểm thật.

## Catalog

NHAPDIEM, BANGIS, TAOMOHINH, THEMMOHINH, XAYMOHINH, ANTIN, TRACAOD: phiên bản 1.8.16, ngày 31/10/2026. BANGIS chuyển Hoàn thành. VEMCD vẫn Đang làm.

## Chưa làm

Phát hành. Pha 2 tuyến và cọc. SQL last seen và force logout trên Supabase nếu chưa chạy. HDSD nằm repo ksnpsc.
