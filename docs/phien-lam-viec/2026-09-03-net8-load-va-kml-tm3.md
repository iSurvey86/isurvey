# 2026-09-03 — net8 autoload + vá KML TM-3 + TM-6 TT973

## Ngữ cảnh

Máy văn phòng: Civil 3D 2026 (R25.1). Plugin build `net10.0-windows` **không nạp** (NETLOAD im lặng, lệnh Unknown). User xác nhận retarget **net8**. Sau khi cài bundle, tab **iSurvey** xuất hiện trên Ribbon. Test **EG** (Lào Cai, múi 3°, CM 104.75°) báo lỗi kinh tuyến — do validation copy nhầm logic TM-6.

## Trạng thái đã chốt

- **Retarget `net8.0-windows`:** `iSurvey.csproj`, `deploy/build-bundle.ps1`; bundle autoload + TRUSTEDPATHS hoạt động trên Civil 3D 2026.
- **TM-6 kinh tuyến chuẩn:** 105 / 111 / 117 (Thông tư 973/2001/TT-TCĐC) — `CoordinateService.Tm6CentralMeridians`, UI Map + Export.
- **63 tỉnh/thành:** `isurvey_province_crs_map.json` map trực tiếp từng tỉnh → `sourceProvinceKey`; `FindSavedGroup` giữ tương thích settings cũ (nhóm tỉnh).
- **Vá KML Export TM-3:** `IsValidCentralMeridian(cm, zone)` — TM-3 chấp nhận 102°–117° (vd Lào Cai 104.75°); TM-6 chỉ 105/111/117. Map Insert dùng chung helper.

## Bug đã vá (Mẫu_Vá_Lỗi)

| Lỗi | Nguyên nhân | Sửa |
|-----|-------------|-----|
| NETLOAD / autoload im lặng | AutoCAD 2026 host .NET 8, DLL build net10 | Retarget net8 + rebuild bundle |
| EG báo "Kinh tuyến trục không hợp lệ" dù Lào Cai TM-3 đúng | `KmlExportWindow` luôn check `Tm6CentralMeridians.Contains()` | Validation theo múi (TM-3 vs TM-6) |

## Việc mở

- User cài lại bundle sau khi thoát Civil 3D (`build-bundle.ps1 -Install`) rồi test EG Lào Cai.
- HDSD / workflow Map + Export.
- Mosaic Layout/in (chưa).
