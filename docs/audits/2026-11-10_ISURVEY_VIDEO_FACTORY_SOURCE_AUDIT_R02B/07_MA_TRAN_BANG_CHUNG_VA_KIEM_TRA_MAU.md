# 07 — Ma trận bằng chứng và kiểm tra mẫu (R02B)

## Quy tắc mức kết luận

| Mức | Quy tắc gán (tái kiểm được) |
|---|---|
| DECLARATION_FOUND | Có `[CommandMethod("…")]` tại file:line |
| METHOD_BODY_SCANNED | Đã mở thân method (brace/=>), ghi nhận nội dung dù chưa theo service |
| HANDLER_TRACE_COMPLETE | Đã xác định chuỗi command→handler/effective + call-site UI/auth trong thân |
| SERVICE_CHAIN_VERIFIED_STATIC | Đã mở ≥1 file service/dialog liên quan và ghi I/O hoặc nhánh lỗi với file:line (hoặc mục MANUAL_DEEP) |
| STUB_ONLY | Method Stub / gọi Stub("CMD") |
| NOT_ENOUGH_EVIDENCE | Không mở được thân hoặc thiếu mắt xích |
| Runtime | luôn `NOT_TESTED` trong R02B |

## Phân bố evidence
```json
{
  "SERVICE_CHAIN_VERIFIED_STATIC": 31,
  "HANDLER_TRACE_COMPLETE": 18,
  "METHOD_BODY_SCANNED": 6,
  "STUB_ONLY": 8
}
```

## Kiểm thủ công độc lập (không chỉ has_cmd)

- `AUD-033` **VEMCD** @ `src/Modules/Profile/ProfileModule.cs:17` eff=`Draw` span=`20-147` → **VERIFIED_TRACE** (method=True, substr=True, manual_deep=True, evidence=`SERVICE_CHAIN_VERIFIED_STATIC`)
- `AUD-012` **NHAPDIEM** @ `src/Modules/DiemDo/PointFileModule.cs:13` eff=`ImportPointsCommand` span=`14-60` → **VERIFIED_TRACE** (method=True, substr=True, manual_deep=True, evidence=`SERVICE_CHAIN_VERIFIED_STATIC`)
- `AUD-013` **XUATDIEM** @ `src/Modules/DiemDo/PointFileModule.cs:62` eff=`ExportPointsCommand` span=`63-91` → **VERIFIED_TRACE** (method=True, substr=True, manual_deep=True, evidence=`SERVICE_CHAIN_VERIFIED_STATIC`)
- `AUD-011` **ISURVEY_DGN_TO_CAD** @ `src/Dgn/DgnModule.cs:21` eff=`DgnToCadCommand` span=`22-84` → **VERIFIED_TRACE** (method=True, substr=True, manual_deep=True, evidence=`SERVICE_CHAIN_VERIFIED_STATIC`)
- `AUD-057` **ISURVEY_UAV_CLIP** @ `src/Modules/Uav/UavModule.cs:10` eff=`ClipCommand` span=`11-17` → **VERIFIED_TRACE** (method=True, substr=True, manual_deep=True, evidence=`SERVICE_CHAIN_VERIFIED_STATIC`)
- `AUD-040` **TAOTUYEN** @ `src/Modules/Tuyen/RouteModule.cs:17` eff=`New` span=`92-192` → **VERIFIED_TRACE** (method=True, substr=True, manual_deep=False, evidence=`SERVICE_CHAIN_VERIFIED_STATIC`)
- `AUD-052` **XAYMOHINH** @ `src/Modules/Terrain/TerrainModule.cs:29` eff=`Build` span=`165-186` → **VERIFIED_TRACE** (method=True, substr=True, manual_deep=False, evidence=`SERVICE_CHAIN_VERIFIED_STATIC`)
- `AUD-030` **ISURVEY_UPDATE_GE** @ `src/Modules/Map/MapModule.cs:123` eff=`UpdateGeCommand` span=`124-167` → **VERIFIED_TRACE** (method=True, substr=True, manual_deep=False, evidence=`SERVICE_CHAIN_VERIFIED_STATIC`)
- `AUD-035` **DAODAUTUYEN** @ `src/Modules/Tuyen/RouteModule.cs:35` eff=`Reverse` span=`264-280` → **VERIFIED_TRACE** (method=True, substr=True, manual_deep=False, evidence=`SERVICE_CHAIN_VERIFIED_STATIC`)

## Script vs thủ công

- Features tổng: 63
- MANUAL_DEEP priority: 5 hành vi (NHAPDIEM/XUATDIEM/D2C/VEMCD/UAVCLIP)
- Còn lại: extractor + mở file type nếu tìm thấy — nhiều nhánh service **CHƯA ĐỦ BẰNG CHỨNG** 100%

## Tự khớp
- CommandMethod 125 = alias 46 + non-alias 79
- Canonical 63 + merged_away ~16
- Excluded rows: 33
- XAML classify: {'CLOSE_CANCEL': 47, 'SETTINGS': 7, 'INTERNAL': 29, 'DIALOG_OK': 8}
