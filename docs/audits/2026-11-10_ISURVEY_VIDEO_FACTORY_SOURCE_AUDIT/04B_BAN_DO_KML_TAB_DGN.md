# 04B — Bản đồ GE / KML / TAB / DGN

## Bản đồ Google Earth (`Ban_do_GE`)

| Full | Alias | Source chính |
|---|---|---|
| ISURVEY_MAP | (Ribbon có thể khác) | `MapModule.cs:25` |
| ISURVEY_MAP_SAT | SG | `MapModule.cs:32` / `CommandAliases.cs:55` |
| ISURVEY_UPDATE_GE | UG | `MapModule.cs:123` / `:61` |
| ISURVEY_DELETE_GE | XG | `MapModule.cs:169` / `:58` |
| IG | IG | Alias InsertMap → `InsertMapCommand` `:52` |

- Auth: `EnsureAuthenticated` trong MapModule (4 tham chiếu file).
- I/O: tải/chèn ảnh vệ tinh, layer GE — chi tiết HTTP/tile trong `src/Modules/Map/*` (22 file `.cs`). Đã xác nhận điểm vào; **không** đọc hết mọi helper tile trong đợt này → một phần `CHƯA ĐỦ BẰNG CHỨNG` ở mức thuật toán tải.
- Tác động: entity raster/image trên DWG; xóa GE có thể phá hủy đối tượng GE (`P1` quy trình video cần WORK COPY).
- Video: ứng viên tốt nếu có vùng tọa độ VN2000 mẫu; GUI/tiến trình có thể xuất hiện (`TaskProgress` dùng chung theo changelog).

## KML/KMZ

| Full | Alias | Source |
|---|---|---|
| ISURVEY_IMPORT_KML | INK | `KmlImportModule.cs:21` / aliases `:67` |
| ISURVEY_EXPORT_KML | EG, EXK | `ExportModule.cs:22` / `:64`, `:70` |

- I/O: file KML/KMZ ↔ entity CAD.
- Video: Feature ngắn; fixture KML nhỏ.

## TAB MapInfo

| Full | Alias | Source |
|---|---|---|
| ISURVEY_TAB_TO_CAD | T2C | `TabImportModule.cs:29` / `:76` |

- Phụ thuộc GDAL (`iSurvey.csproj` MaxRev.Gdal.*).
- Runtime native GDAL trên máy: `CHƯA KIỂM CHỨNG THỰC TẾ`.

## DGN

| Full | Alias | Source |
|---|---|---|
| ISURVEY_DGN_TO_CAD | D2C | `DgnModule.cs:21` / `:73` |
| ISURVEY_CAD_TO_DGN | C2D | `DgnModule.cs:87` (`UsePickSet`) / `:79` |

- Seed DGN: `src/Seeds/*.dgn` đóng gói.
- Mapping: `src/Data/dgn_layer_mapping.json`.
- Đã có kịch bản video `docs/video/kich-ban/d2c-1.8.15.*` và `d2c-1.8.17.*`; CATALOG ghi 1.8.15 “Đã ghép, chưa đăng” — thư mục `docs/video/thanh-pham` **không tồn tại trong repo** → file MP4 thành phẩm `CHƯA TÌM THẤY` trong phạm vi git; có thể nằm ngoài git theo QUY_TRINH.

## Thuận lợi tự động hóa (nhóm)

| Tiêu chí | Đánh giá source |
|---|---|
| Lệnh thuần / có dialog | D2C/T2C thường có tiến trình/dialog — cần quan sát UI |
| Phá hủy | XG, C2D ghi file — cần WORK COPY |
| Mạng | GE tiles cần mạng |
