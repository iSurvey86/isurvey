# BF-ISURVEY_LOGOUT — ISURVEY_LOGOUT

- **verdict_source:** `SOURCE_BEHAVIOR_VERIFIED_STATIC`
- **priority:** SC10_RECHECK
- **runtime:** NOT_TESTED

## Call chain
AuthModule.LogoutCommand @ AuthModule.cs:77-100 | AuthManager.Refresh/IsAuthenticated → DarkDialog.Confirm → AuthManager.SignOut

## Inputs / Algorithm / Outputs / Errors / Destructive
- Inputs: Phiên đăng nhập hiện có; xác nhận UI
- Algorithm: Nếu chưa login → thông báo; nếu Confirm → SignOut xóa giấy phép máy
- Outputs: License local bị xóa; tính năng khóa; Print đã đăng xuất
- Errors: hủy Confirm → hủy đăng xuất
- Destructive: Xóa license trên máy (destructive với trạng thái kích hoạt)

## Gaps
Thân AuthManager.SignOut chi tiết persistence — xem thêm nếu cần audit auth sâu

## Evidence

