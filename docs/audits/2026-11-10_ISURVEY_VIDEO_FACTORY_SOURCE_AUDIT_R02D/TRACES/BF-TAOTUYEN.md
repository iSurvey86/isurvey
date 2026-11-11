# BF-TAOTUYEN — TAOTUYEN

- **verdict_source:** `SOURCE_BEHAVIOR_VERIFIED_STATIC`
- **priority:** SC10_RECHECK
- **runtime:** NOT_TESTED

## Call chain
TAOTUYEN/ISURVEY_ROUTE_NEW → New @ RouteModule.cs:92+ | CreateRouteWindow.Prompt → GetEntity polyline → RouteStore.Save (+Terrain link)

## Inputs / Algorithm / Outputs / Errors / Destructive
- Inputs: Model Space; polyline tim; tên/mô hình địa hình optional
- Algorithm: Validate Polyline/2d/3d; tạo RouteRecord; lưu store
- Outputs: Tuyến mới trong DWG store; message; palette reload (nếu có)
- Errors: dialog/hủy pick; không phải polyline
- Destructive: Không xóa polyline nguồn — chỉ gắn handle

## Gaps
Chi tiết RouteService phụ sau Save — xem thêm nếu cần

## Evidence

