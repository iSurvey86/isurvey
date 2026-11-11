# BF-ISURVEY_LOGOUT — ISURVEY_LOGOUT

- **source_status:** SOURCE_PRESENT
- **evidence:** `SERVICE_CHAIN_VERIFIED_STATIC`
- **runtime:** NOT_TESTED

## Call chain
ISURVEY_LOGOUT CommandMethod @ src/Auth/AuthModule.cs:77 | effective `LogoutCommand` span 78-100 | Auth gate: AuthManager.EnsureAuthenticated (trong thân handler) | AuthManager.Print @ src/Auth/AuthManager.cs:625-636 | AuthManager.Refresh @ src/Auth/AuthManager.cs:125-147 | AuthManager.SignOut @ src/Auth/AuthManager.cs:204-215

## Inputs
CHƯA ĐỦ BẰNG CHỨNG đủ điều kiện tiên quyết

## Outputs / hậu điều kiện
AuthManager.Print: có ghi/xuất/CAD commit (span 625-636)

## Errors
AuthManager.Print: có catch @ src/Auth/AuthManager.cs:625-636

## Destructive
AuthManager.SignOut: dấu hiệu destructive @ src/Auth/AuthManager.cs

## Unresolved
không tìm thấy file cho type `DarkDialog`
