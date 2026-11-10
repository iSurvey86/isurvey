# 09 — Ba ứng viên video thử nghiệm (đề xuất, không chạy)

Thang điểm mỗi tiêu chí 0–5; tổng tối đa 40. Chỉ dựa trên bằng chứng source + docs trong repo.

## Bảng chấm

| Tiêu chí | A: D2C (ISURVEY_DGN_TO_CAD) | B: VEMCD | C: UAVCLIP |
|---|---:|---:|---:|
| Bằng chứng source rõ | 5 (`DgnModule.cs:21`) | 5 (`ProfileModule.cs:14–18`) | 5 (`UavModule.cs:10`) |
| Cài đặt / phụ thuộc | 3 (seed DGN + mapping) | 4 (cần tuyến+cọc trong DWG) | 3 (raster + biên) |
| Fixture giả lập được | 4 | 3 (chuỗi TAOTUYEN→PHATSINHCOC) | 3 |
| Output đo độc lập | 4 (layer/entity sau import) | 4 (handles, text H=12, MSS) | 4 (boundary clip) |
| GUI dễ quan sát | 3 (tiến trình) | 2 (modal WPF phức tạp) | 3 |
| Ít rủi ro riêng tư | 4 (file mẫu) | 3 (dễ dính DWG công trình) | 3 |
| Chi phí/thời gian vừa | 4 (đã có kịch bản 1.8.15/17) | 3 | 3 |
| Mở rộng về sau | 4 | 5 (workflow MCD) | 3 |
| **Tổng** | **31** | **29** | **28** |

### Điểm trừ nổi bật

- **A D2C:** phụ thuộc môi trường DGN/seed; cần xác minh GDAL không liên quan nhưng DGN pipeline riêng.  
- **B VEMCD:** dialog Zero-Clutter nhiều control; quay cần chuột; tiền điều kiện dài.  
- **C UAVCLIP:** dữ liệu ảnh lớn; chưa có kịch bản “đo sai số” chuẩn trong repo bằng nhau.

## Đề xuất 01 ưu tiên: **A — D2C (Nhập DGN)**

**Lý do:** điểm cao nhất; đã có kịch bản `d2c-1.8.15` / `d2c-1.8.17`; CommandMethod ổn định; phù hợp Feature video; ít phụ thuộc Civil Surface; CATALOG đã theo dõi trạng thái ghép/đăng.

**Điều kiện tiên quyết (chưa làm trong đợt này):**

1. Fixture DGN MASTER sạch + WORK COPY.  
2. Bundle 1.8.17 nạp được — `CHƯA KIỂM CHỨNG THỰC TẾ`.  
3. Quyết định giọng NAM miền Bắc thay `HoaiMyNeural`.  
4. Không dùng dữ liệu khách trong `docs/MCD/`.  
5. Chủ dự án cho phép bước pilot sau khi thẩm định audit.

## Ứng viên dự bị

2. **VEMCD** — sau khi có fixture tuyến+cọc chuẩn (workflow).  
3. **UAVCLIP** — sau khi có ảnh mẫu nhẹ và biên chuẩn.

## Không chọn

- UAV stub, Ribbon placeholder, GENOTP, AIA/AIC, lệnh Auth thuần.
