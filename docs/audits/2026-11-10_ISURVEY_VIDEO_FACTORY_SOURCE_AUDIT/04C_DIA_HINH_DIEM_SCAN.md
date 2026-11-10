# 04C — Địa hình TIN / Điểm đo / Scan

## Địa hình (`Dia_hinh_TIN`) — `TerrainModule.cs`

| Full | Alias tắt | Line (TerrainModule) |
|---|---|---:|
| ISURVEY_TERRAIN_NEW | TAOMOHINH | 14 / 17 |
| ISURVEY_TERRAIN_ADD | THEMMOHINH | 20 / 23 |
| ISURVEY_TERRAIN_BUILD | XAYMOHINH | 26 / 29 |
| ISURVEY_TERRAIN_TOGGLE | ANTIN | 32 / 35 |
| ISURVEY_TERRAIN_Z | TRACAOD | 38 / 41 |
| ISURVEY_TERRAIN_DETACH | GOMOHINH | 44 / 47 |
| ISURVEY_TERRAIN_CLEAR | XOATHANHPHAN | 50 / 53 |

- Auth: 7× `EnsureAuthenticated` trong file module.
- Chuỗi nghiệp vụ theo docs/handoff: tạo mô hình → thêm điểm → build TIN → (tùy) ẩn TIN.
- I/O: điểm/block/đường bao trên DWG; Surface/TIN Civil — **API Civil riêng không có trong csproj**; cơ chế TIN cụ thể cần đọc sâu `Terrain*` services (`CHƯA ĐỦ BẰNG CHỨNG` chi tiết Civil object model).
- Phá hủy: `XOATHANHPHAN` / detach — `P1` fixture.
- Video: đã có kịch bản 1.8.16 cho THEMMOHINH/XAYMOHINH/GOMOHINH/TAOTUYEN liên quan chuỗi.

## Điểm đo (`Diem_do`)

| Lệnh | Source |
|---|---|
| NHAPDIEM / ND | `PointFileModule.cs:13` / aliases |
| XUATDIEM / XD | `PointFileModule.cs:62` / aliases |

- I/O: tệp tọa độ ↔ điểm trên bản vẽ.
- Nhiều nút Ribbon điểm đo khác là **placeholder** (04E), không có CommandMethod.

## Scan

| Lệnh | Alias | Source |
|---|---|---|
| AUTOSCAN | SCN | `ScanModule.cs:25` / `CommandAliases.cs:166` |

- Ribbon enabled: AutoScan.
- Chi tiết thuật toán quét: đọc điểm vào; body sâu `CHƯA ĐỦ BẰNG CHỨNG` nếu cần mô tả từng bước cho script quay.
