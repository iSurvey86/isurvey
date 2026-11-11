# 08 — Vấn đề chặn và hành động tiếp (R02B)

## Đã khắc phục so R02 (P0/P1)

1. Gộp SAME_FEATURE các wrapper ngoài CommandAliases (VEMCD/PROFILE_DRAW, BANGCOC/STATION_LIST, TAOTUYEN/ROUTE_NEW, PHATSINHCOC/ROUTE_STATIONS, XAYMOHINH/TERRAIN_BUILD, …).
2. Không gộp nhầm các Stub("CMD").
3. Sửa nhóm UPDATE_GE→MAP; DAODAUTUYEN/STATION_ADD/TERRAIN_ADD theo module path.
4. Ma trận XAML+Palette trong 03A/03D.
5. Evidence levels mới + mẫu thủ công priority.
6. Bảng diff Git R01→R02.

## P0 còn lại

1. Phase 01 **chưa** nghiệm thu — chờ hội đồng.
2. Nhiều service dài (FrameService.*, ProfileDrawService chi tiết band, DgnImportService Idle finish): **CHƯA ĐỦ BẰNG CHỨNG** 100% từng nhánh.
3. Context menu / dynamic macro: **CHƯA ĐỦ BẰNG CHỨNG**.

## P1

1. Fixture CAD seed chưa đóng gói trong audit.
2. Chính sách AI Cloud.
3. `source_status=SOURCE_PRESENT` ≠ đã chạy được.

## Phần chưa đọc đủ (rút gọn)

- AUD-001 ISURVEY_AI_ASSIST: service mở đầu file — CHƯA ĐỦ BẰNG CHỨNG toàn bộ nhánh
- AUD-002 ISURVEY_AI_SETTINGS: service mở đầu file — CHƯA ĐỦ BẰNG CHỨNG toàn bộ nhánh
- AUD-003 ISURVEY_ABOUT: HANDLER_TRACE_COMPLETE
- AUD-004 ISURVEY_GENOTP: service mở đầu file — CHƯA ĐỦ BẰNG CHỨNG toàn bộ nhánh
- AUD-005 ISURVEY_HDSD: HANDLER_TRACE_COMPLETE
- AUD-006 ISURVEY_LOGIN: HANDLER_TRACE_COMPLETE
- AUD-007 ISURVEY_LOGOUT: service mở đầu file — CHƯA ĐỦ BẰNG CHỨNG toàn bộ nhánh
- AUD-008 ISURVEY_RIBBON: HANDLER_TRACE_COMPLETE
- AUD-009 ISURVEY_UPDATE: HANDLER_TRACE_COMPLETE
- AUD-010 ISURVEY_CAD_TO_DGN: service mở đầu file — CHƯA ĐỦ BẰNG CHỨNG toàn bộ nhánh
- AUD-014 ISURVEY_FIT_TITLE: HANDLER_TRACE_COMPLETE
- AUD-015 ISURVEY_FRAME_EDIT: HANDLER_TRACE_COMPLETE
- AUD-016 ISURVEY_KHUNG: METHOD_BODY_SCANNED
- AUD-017 ISURVEY_KHUNG_BV: METHOD_BODY_SCANNED
- AUD-018 ISURVEY_LOGO: HANDLER_TRACE_COMPLETE
- AUD-019 ISURVEY_LOGO1: METHOD_BODY_SCANNED
- AUD-020 ISURVEY_LOGO2: METHOD_BODY_SCANNED
- AUD-021 ISURVEY_LOGO_REPLACE: HANDLER_TRACE_COMPLETE
- AUD-022 ISURVEY_PROFILE_SHEETS: METHOD_BODY_SCANNED
- AUD-023 ISURVEY_ROUTE_PLOT: HANDLER_TRACE_COMPLETE
- AUD-024 ISURVEY_ROUTE_SHEETS: METHOD_BODY_SCANNED
- AUD-025 ISURVEY_EXPORT_KML: service mở đầu file — CHƯA ĐỦ BẰNG CHỨNG toàn bộ nhánh
- AUD-026 ISURVEY_IMPORT_KML: service mở đầu file — CHƯA ĐỦ BẰNG CHỨNG toàn bộ nhánh
- AUD-027 ISURVEY_DELETE_GE: HANDLER_TRACE_COMPLETE
- AUD-028 ISURVEY_MAP: HANDLER_TRACE_COMPLETE
- AUD-029 ISURVEY_MAP_SAT: HANDLER_TRACE_COMPLETE
- AUD-030 ISURVEY_UPDATE_GE: service mở đầu file — CHƯA ĐỦ BẰNG CHỨNG toàn bộ nhánh
- AUD-031 ISURVEY_PALETTE: HANDLER_TRACE_COMPLETE
- AUD-032 CATBD: HANDLER_TRACE_COMPLETE
- AUD-034 BANGCOC: service mở đầu file — CHƯA ĐỦ BẰNG CHỨNG toàn bộ nhánh
- AUD-035 DAODAUTUYEN: service mở đầu file — CHƯA ĐỦ BẰNG CHỨNG toàn bộ nhánh
- AUD-036 ISURVEY_NUMBER_VERTICES: HANDLER_TRACE_COMPLETE
- AUD-037 ISURVEY_STRIP_MAP: HANDLER_TRACE_COMPLETE
- AUD-038 PHATSINHCOC: service mở đầu file — CHƯA ĐỦ BẰNG CHỨNG toàn bộ nhánh
- AUD-039 STT: service mở đầu file — CHƯA ĐỦ BẰNG CHỨNG toàn bộ nhánh
- AUD-040 TAOTUYEN: service mở đầu file — CHƯA ĐỦ BẰNG CHỨNG toàn bộ nhánh
- AUD-041 THEMCOC: service mở đầu file — CHƯA ĐỦ BẰNG CHỨNG toàn bộ nhánh
- AUD-042 THIETLAPCOC: service mở đầu file — CHƯA ĐỦ BẰNG CHỨNG toàn bộ nhánh
- AUD-043 XOACOC: service mở đầu file — CHƯA ĐỦ BẰNG CHỨNG toàn bộ nhánh
- AUD-044 XOACOC1: service mở đầu file — CHƯA ĐỦ BẰNG CHỨNG toàn bộ nhánh
…

## Hành động

Hội đồng thẩm định ZIP R02B. **Không** triển khai Video Factory / pilot cho đến chỉ đạo riêng.
