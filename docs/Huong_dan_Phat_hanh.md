# Hướng dẫn phát hành iSurvey (Admin)

Mục tiêu mỗi lần phát hành: **một file zip Setup** gửi người dùng + **update online** (file `version.json` trên Storage) + **nhật ký hoạt động** đã bật trên Supabase.

## 0. Một lần trên Supabase (nếu chưa làm)

1. Chạy SQL license: `deploy/supabase/schema.sql` (xem `Huong_dan_Supabase.md`).
2. Chạy SQL activity: `deploy/supabase/schema_activity.sql`.
3. Chạy SQL admin views: `deploy/supabase/schema_admin.sql` (tuỳ chọn; tab ksnpsc không bắt buộc view).
4. **Storage → New bucket** tên `isurvey-releases`, bật **Public bucket**.
5. Policy mặc định public read là đủ cho update (GET `version.json` + zip). Không cần cho client ghi.
6. Admin UI: cấu hình ksnpsc theo `docs/Huong_dan_Admin_ksnpsc.md`.

## 1. Đóng gói trên máy dev

1. Bump `<Version>` trong `iSurvey.csproj` và `AppVersion` / `ComponentEntry Version` trong `deploy/iSurvey.bundle/PackageContents.xml` (cùng số).
2. Cập nhật ghi chú trong `deploy/build-release.ps1` (biến `$notes`) nếu cần.
3. Chạy:

```powershell
.\deploy\build-release.ps1
```

4. Kết quả:
   - `deploy\output\iSurvey-Setup-<ver>.zip` — **gửi người dùng**
   - `deploy\output\release\version.json` + bản zip copy — **upload Storage**

## 2a. Phát hành một lệnh (khuyên dùng)

Khóa `service_role` đặt **một lần** trên máy dev (biến môi trường của user, không lưu vào file/repo):

```powershell
setx ISURVEY_SERVICE_ROLE_KEY "<service_role: Supabase → Project Settings → API>"
```

Mở lại terminal, rồi mỗi lần phát hành (sau khi bump version ở bước 1):

```powershell
.\deploy\publish-release.ps1 -Notes "Mô tả ngắn bản này"
```

Script tự build zip, upload zip, kiểm tra zip tải được và đúng dung lượng, rồi mới ghi đè `version.json` (cache 60 giây) và xác nhận lại trên server. Từ chối upload nếu version thấp hơn bản đang có trên server (cần `-Force` để rollback). Tùy chọn: `-SkipBuild` (chỉ upload bản đã build), `-DryRun` (chỉ kiểm tra). Chưa đặt biến môi trường thì script hỏi nhập khóa (ẩn ký tự, chỉ dùng lần đó).

## 2. Upload thủ công (dự phòng)

Storage bucket `isurvey-releases`, upload (ghi đè `version.json` mỗi lần):

| File | Vai trò |
|---|---|
| `iSurvey-Setup-<ver>.zip` | Bản tải về khi có update |
| `version.json` | Manifest (tên cố định) — app đọc file này |

Nội dung `version.json` mẫu:

```json
{
  "latest": "1.7.0",
  "url": "https://<project>.supabase.co/storage/v1/object/public/isurvey-releases/iSurvey-Setup-1.7.0.zip",
  "released_at": "2026-10-19",
  "notes": "...",
  "min_version": "1.0.0"
}
```

`url` phải trùng đúng object zip vừa upload. `min_version` (tuỳ chọn) đánh dấu bản quá cũ cần cập nhật bắt buộc (client hiện chỉ báo, chưa khóa cứng).

## 3. Người dùng cài / cập nhật

1. Đóng hết AutoCAD / Civil 3D.
2. Giải nén zip → chạy `Install-iSurvey.bat` (hoặc `.ps1`).
3. Mở Civil → Ribbon iSurvey.
4. Lần sau: nút **Cập nhật** / lệnh `UPD` → nếu có bản mới → **Cập nhật ngay** → thanh tiến trình tải zip → Civil tự thoát → helper cài (`Install-iSurvey.ps1`) → mở lại Civil. Nhật ký ghi `update_download` / `update_quit` / `update_result`. Nếu bản đang dùng còn helper cũ (treo), cài tay zip Setup **một lần** rồi các lần UPD sau mới chạy hết.

Gỡ: `Uninstall-iSurvey.bat`. Chi tiết trong `CAI_DAT.txt` trong zip.

## 4. Kiểm tra nhanh sau phát hành

- [ ] Cài zip trên máy sạch → mở Civil → Ribbon hiện đúng phiên bản (`ISURVEY_ABOUT`).
- [ ] `ISURVEY_UPDATE` khi đã upload `version.json` mới hơn → hiện hộp “Có bản mới”.
- [ ] Đăng nhập Online → Table `isurvey_activity` có dòng `login` / `command`.
- [ ] Gửi zip cho 1–2 user thử, không cần họ cấu hình gì.

## 5. Không làm

- Không đưa `service_role` vào DLL / zip / chat.
- Không commit `%AppData%\iSurvey\auth.config.json` hay `license.dat`.
