# 2026-10-19 — Phát hành 1.7.0 (Setup, update, activity, UAVCLIP, GOC, Admin ksnpsc)

## Ngữ cảnh
Cursor — máy văn phòng. Civil 3D 2026. Cuối phiên đầy đủ sau khi làm Setup/update/activity + gắn tab Admin vào ksnpsc.

## Sản phẩm CAD
- **UAV & LiDAR** panel; `UAVCLIP`: clip polyline (Smart Filter) hoặc Auto-Clip (downsample, threshold, contour, Douglas-Peucker, CAD Y-axis). User: Auto-Clip mỹ mãn.
- **GOC** đánh số góc tuyến, layer `ISURVEY_GOC_TUYEN`, kích thước theo tỷ lệ (mặc định 1:1000). User: rất tốt.

## Vận hành
- `build-release.ps1` → zip Setup; Install/Uninstall bat/ps1; `CAI_DAT.txt`.
- Update: `UpdateCheckService` + `ISURVEY_UPDATE`; Storage bucket public `isurvey-releases` + `version.json` + zip đã upload và kiểm HTTP 200.
- Activity: `schema_activity.sql`, `ActivityLogger`; heartbeat/login/recheck; `schema_admin.sql` views.
- Admin: tab iSurvey trong ksnpsc (push `e264362`); cần Vercel env `ISURVEY_*`.

## Việc Admin còn lại (ngoài code)
- [ ] Vercel: thêm `ISURVEY_SUPABASE_URL` + `ISURVEY_SERVICE_ROLE_KEY`, chờ deploy tab iSurvey.
- [ ] SQL Editor: chạy `schema_admin.sql` nếu chưa (activity đã chạy trước đó).
- [ ] Cài zip 1.7.0 trên máy user / máy dev (đóng Civil trước).
