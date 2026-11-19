# 2026-11-19 — D2C batch V7 Phú Cường + MenuBar (không bump)

Máy văn phòng, AutoCAD 2023. Cuối phiên đầy đủ. **Không bump.** Vẫn **1.8.20** (chưa phát hành UPD).

## Đã làm

- **D2C** (`DgnImportService`): stage dưới `C:\Temp\isurvey-dgn-stage` **tách reference** mặc định (DC09 Phú Cường = 268; trước đây chép sibling → 0). Khi DGNIMPORT = 0: chèn `*-Default*.dwg` (stage hoặc thư mục nguồn); nếu chưa có thì retry kèm sibling reference rồi fallback DWG (DC24/DC25). Token prompt V7: `Default` / `_Master` / `Standard`. Ẩn hộp tiến trình Topmost lúc gửi lệnh.
- Nghiệm thu batch `C:\Temp\phucuong` (30 DGN từ `Downloads\phu cuong`): **30/30 OK**, Model Space ~68 164, ZOOM E ổn. Không kẹt Master/Sub.
- **MenuBuilder**: menu cổ điển **iSurvey** khi Ribbon tắt; `Main` / `MENUISURVEY` dựng lại Ribbon + menu.
- Catalog Excel D2C; README; CHANGELOG (mục 1.8.20); kịch bản `docs/video/kich-ban/d2c-1.8.20` + dòng CATALOG (Chưa quay). Gói `Microsoft.NETFramework.ReferenceAssemblies.net48` để build net48 khi máy chỉ có Targeting Pack 4.8.1.
- Không quay video, không đăng YouTube. Không bump, không `publish-release.ps1`.

## Catalog

Sửa **D2C** / `ISURVEY_DGN_TO_CAD`. Phiên bản 1.8.20, ngày 19/11/2026, Hoàn thành.

## Chưa làm

Phát hành UPD 1.8.20 khi được bảo. Quay video D2C 1.8.20 khi được yêu cầu. HDSD web (ksnpsc) sau phát hành.
