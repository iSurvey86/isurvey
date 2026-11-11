# 05 — Open issues (R02D)

## SC10 còn partial
- BF-ISURVEY_DGN_TO_CAD
- BF-ISURVEY_CAD_TO_DGN
- BF-VEMCD

## USER còn partial / not enough
- BF-AUTOSCAN SOURCE_PARTIAL_VERIFIED — không thấy file ShowDialog.cs; không thấy file Win32SaveFileDialog.cs
- BF-CATBD SOURCE_PARTIAL_VERIFIED — (đã mở handler + ≥1 service call site)
- BF-ISURVEY_ABOUT SOURCE_PARTIAL_VERIFIED — (đã mở handler + ≥1 service call site)
- BF-ISURVEY_CAD_TO_DGN SOURCE_PARTIAL_VERIFIED — CHƯA ĐỦ BẰNG CHỨNG: toàn bộ DgnExportService pipeline
- BF-ISURVEY_DGN_TO_CAD SOURCE_PARTIAL_VERIFIED — CHƯA ĐỦ BẰNG CHỨNG: toàn bộ nhánh Idle/Finish trong DgnImportService (>1000 dòng) chưa đọc hết từng 
- BF-ISURVEY_EXPORT_KML SOURCE_PARTIAL_VERIFIED — thấy type KmlExportService nhưng chưa khớp method call; CHƯA ĐỦ BẰNG CHỨNG body Vn2000SetupDialog.['
- BF-ISURVEY_FIT_TITLE SOURCE_PARTIAL_VERIFIED — (đã mở handler + ≥1 service call site)
- BF-ISURVEY_FRAME_EDIT SOURCE_PARTIAL_VERIFIED — (đã mở handler + ≥1 service call site)
- BF-ISURVEY_HDSD SOURCE_PARTIAL_VERIFIED — (đã mở handler + ≥1 service call site)
- BF-ISURVEY_IMPORT_KML SOURCE_PARTIAL_VERIFIED — không thấy file AcadWindow.cs; thấy type CoordinateService nhưng chưa khớp method call; không thấy f
- BF-ISURVEY_KHUNG SOURCE_PARTIAL_VERIFIED — chỉ đọc được thân handler; service chưa theo
- BF-ISURVEY_KHUNG_BV SOURCE_PARTIAL_VERIFIED — chỉ đọc được thân handler; service chưa theo
- BF-ISURVEY_LOGIN SOURCE_PARTIAL_VERIFIED — (đã mở handler + ≥1 service call site)
- BF-ISURVEY_LOGO SOURCE_PARTIAL_VERIFIED — (đã mở handler + ≥1 service call site)
- BF-ISURVEY_LOGO1 SOURCE_PARTIAL_VERIFIED — chỉ đọc được thân handler; service chưa theo
- BF-ISURVEY_LOGO2 SOURCE_PARTIAL_VERIFIED — chỉ đọc được thân handler; service chưa theo
- BF-ISURVEY_LOGO_REPLACE SOURCE_PARTIAL_VERIFIED — (đã mở handler + ≥1 service call site)
- BF-ISURVEY_MAP SOURCE_PARTIAL_VERIFIED — không thấy file RunMapWorkflow.cs
- BF-ISURVEY_NUMBER_VERTICES SOURCE_PARTIAL_VERIFIED — (đã mở handler + ≥1 service call site)
- BF-ISURVEY_PALETTE SOURCE_PARTIAL_VERIFIED — (đã mở handler + ≥1 service call site)
- BF-VEMCD SOURCE_PARTIAL_VERIFIED — CHƯA ĐỦ BẰNG CHỨNG: ProfileDrawService geometry band đầy đủ
- BF-ISURVEY_PROFILE_SHEETS SOURCE_PARTIAL_VERIFIED — chỉ đọc được thân handler; service chưa theo
- BF-XOACOC SOURCE_PARTIAL_VERIFIED — CHƯA ĐỦ BẰNG CHỨNG body RouteService.['Choose', 'ClearStations']
- BF-ISURVEY_ROUTE_PLOT SOURCE_PARTIAL_VERIFIED — (đã mở handler + ≥1 service call site)
- BF-DAODAUTUYEN SOURCE_PARTIAL_VERIFIED — CHƯA ĐỦ BẰNG CHỨNG body RouteService.['Choose', 'Reverse']
- BF-ISURVEY_ROUTE_SHEETS SOURCE_PARTIAL_VERIFIED — chỉ đọc được thân handler; service chưa theo
- BF-PHATSINHCOC SOURCE_PARTIAL_VERIFIED — không thấy file CreateStationsWindow.cs; CHƯA ĐỦ BẰNG CHỨNG body RouteService.['Choose', 'TryLength'
- BF-THEMCOC SOURCE_PARTIAL_VERIFIED — CHƯA ĐỦ BẰNG CHỨNG body RouteService.['Choose', 'TryAddStationAtPoint', 'FormatChainage']
- BF-BANGCOC SOURCE_PARTIAL_VERIFIED — CHƯA ĐỦ BẰNG CHỨNG body RouteService.['Choose', 'TryAddStationAtPoint', 'FormatChainage']; không thấ
- BF-XOACOC1 SOURCE_PARTIAL_VERIFIED — CHƯA ĐỦ BẰNG CHỨNG body RouteService.['RemoveStationByEntity']
- BF-ISURVEY_STRIP_MAP SOURCE_PARTIAL_VERIFIED — (đã mở handler + ≥1 service call site)
- BF-ISURVEY_TAB_TO_CAD SOURCE_PARTIAL_VERIFIED — không thấy file ShowAlertDialog.cs; không thấy file TabImportWindow.cs; CHƯA ĐỦ BẰNG CHỨNG body User
- BF-THEMMOHINH SOURCE_PARTIAL_VERIFIED — không thấy file AddPointSourcesWindow.cs; CHƯA ĐỦ BẰNG CHỨNG body TerrainService.['Choose', 'AddPoin
- BF-XAYMOHINH SOURCE_PARTIAL_VERIFIED — CHƯA ĐỦ BẰNG CHỨNG body TerrainService.['Choose', 'Build']
- BF-XOATHANHPHAN SOURCE_PARTIAL_VERIFIED — không thấy file DarkDialog.cs; CHƯA ĐỦ BẰNG CHỨNG body TerrainService.['Choose', 'CountKind', 'KindL
- BF-GOMOHINH SOURCE_PARTIAL_VERIFIED — CHƯA ĐỦ BẰNG CHỨNG body TerrainService.['Choose', 'CountKind', 'KindLabel', 'Detach', 'KindLabel']
- BF-ANTIN SOURCE_PARTIAL_VERIFIED — CHƯA ĐỦ BẰNG CHỨNG body TerrainService.['Choose']; CHƯA ĐỦ BẰNG CHỨNG body TerrainStore.['Load']
- BF-ISURVEY_UAV_CLIP SOURCE_PARTIAL_VERIFIED — (đã mở handler + ≥1 service call site)
- BF-ISURVEY_UPDATE SOURCE_PARTIAL_VERIFIED — (đã mở handler + ≥1 service call site)
- BF-ISURVEY_UPDATE_GE SOURCE_PARTIAL_VERIFIED — CHƯA ĐỦ BẰNG CHỨNG body UserSettingsStore.['Load']
- BF-NHAPDIEM SOURCE_PARTIAL_VERIFIED — không thấy file ImportPointsWindow.cs; CHƯA ĐỦ BẰNG CHỨNG body PointDrawService.['Draw']
- BF-STT SOURCE_PARTIAL_VERIFIED — không thấy file SequenceWindow.cs
- BF-XUATDIEM SOURCE_PARTIAL_VERIFIED — không thấy file ExportPointsWindow.cs

## Runtime
Chưa thử CAD — không thuộc đợt này.

## Stop gate
DỪNG — chờ Chủ dự án / hội đồng quyết định nghiệm thu Phase 01.
