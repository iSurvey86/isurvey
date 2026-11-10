# 06 — Dữ liệu CAD đầu vào và bằng chứng

**Phạm vi:** suy từ mã/tài liệu trong repo. Mọi đường dẫn fixture thực tế trên máy quay: `CHƯA KIỂM CHỨNG THỰC TẾ`.

## 1. Theo nhóm chức năng

| Nhóm | MASTER gợi ý | WORK COPY | REFERENCE / đối chứng | Rủi ro dữ liệu |
|---|---|---|---|---|
| Ban_do_GE | DWG + CRS VN2000 vùng nhỏ | Bản sao trước Update/Delete GE | Ảnh/tile trước-sau; tọa độ góc | Mạng; xóa GE |
| KML | KML/KMZ mẫu nhỏ | DWG scratch | Số entity / layer sau import | Ghi đè layer |
| TAB | File TAB + GDAL runtime | Scratch | So đếm đối tượng | Native GDAL thiếu |
| DGN | DGN V7/V8 + seed `src/Seeds` | Scratch | Mapping layer JSON | Seed/path |
| Diem_do | CSV/TXT tọa độ | Scratch | Số điểm | Sai CRS |
| Dia_hinh_TIN | Điểm có Z + bao | Scratch | TIN/mesh tồn tại | XOATHANHPHAN |
| Tuyen_Coc | Polyline tuyến | Scratch | Station list / handles | XOACOC |
| Mat_cat_doc | Tuyến+cọc Z (chuỗi TAOTUYEN→PHATSINHCOC) | Scratch `_test_*` | Entity handles ProfileStore; đo text H=12 | Modal UI; Model only |
| Khung_Layout | Template `src/Templates/*.dwg` | Layout scratch | Block khung trên paper | Layout phức tạp |
| UAV clip | Raster + biên polyline | Scratch | Boundary clip | Ảnh lớn |
| UAV stub | — | — | Alert dialog | Không đo output |
| Auth/Admin | — | — | Không quay | Bí mật |

## 2. Quy ước MASTER / WORK COPY / REFERENCE (đề xuất vận hành sau)

- **MASTER:** chỉ đọc; không mở bằng phiên quay.  
- **WORK COPY:** Civil thao tác + quay.  
- **REFERENCE:** ảnh/CSV độc lập hoặc bản DWG “golden” so khớp entity count / layer / text height.

## 3. Bắt buộc xác minh khi chạy CAD (ngoài đợt này)

1. Plugin nạp đúng `1.8.17` từ bundle AppData.  
2. Đăng nhập iSurvey trước lệnh gated.  
3. VEMCD: Model Space; có station Z.  
4. D2C/T2C: seed + GDAL có trên PATH/bundle.  
5. Không dùng bản vẽ khách hàng làm MASTER quay.

## 4. Nguồn trong repo có thể tái sử dụng (không phải fixture chuẩn hóa)

- Untracked `docs/MCD/` (có `Diemdo2k.dwg` theo lịch sử máy) — **không** mặc định dùng làm MASTER công khai; chứa dữ liệu công trình → rủi ro riêng tư `P1`.  
- `src/Templates`, `src/Seeds` — tài nguyên sản phẩm, phù hợp đóng gói fixture sạch hơn.
