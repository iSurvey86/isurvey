# Đăng nhập YouTube một lần

Script đăng video bằng YouTube Data API. Khóa và token không để trong repo. Chúng nằm tại:

`%AppData%\iSurvey\youtube\`

- `client_secret.json` — tải từ Google Cloud
- `token.json` — script ghi sau lần đăng nhập đầu

Kênh YouTube của đúng tài khoản Google đó phải đã tồn tại. Vào [youtube.com](https://www.youtube.com) và tạo kênh nếu Google báo chưa có kênh.

## Tạo OAuth trên Google Cloud

1. Mở [Google Cloud Console](https://console.cloud.google.com/) bằng Gmail của kênh.
2. Tạo project, tên gợi ý `iSurvey`.
3. **APIs & Services → Library**, tìm **YouTube Data API v3**, bấm **Enable**.
4. **APIs & Services → OAuth consent screen**. Chọn External. Tên ứng dụng `iSurvey`, điền email hỗ trợ.
5. Thêm scope `https://www.googleapis.com/auth/youtube`. Scope này vừa tải video lên vừa đổi video không công khai sang công khai sau khi bạn xem.
6. Thêm đúng Gmail của kênh vào **Test users**.
7. **Credentials → Create credentials → OAuth client ID → Desktop app**. Tải JSON.
8. Đổi tên file thành `client_secret.json` và chép vào `%AppData%\iSurvey\youtube\`.

Ứng dụng để ở chế độ Testing là đủ cho kênh của chính bạn. Token loại này Google cho hết hạn sau khoảng 7 ngày. Hết hạn thì chạy lại lệnh đăng nhập bên dưới. Không commit file JSON.

## Đăng nhập và đăng video

Trong thư mục repo:

```powershell
.\tools\video\setup.ps1
.\tools\video\upload-youtube.ps1 -Login
```

Trình duyệt mở ra. Chọn đúng Gmail, chấp nhận quyền tải video lên. Script ghi `token.json` cạnh `client_secret.json`.

Đăng bài học (mặc định không công khai, theo trường `privacy` trong json):

```powershell
.\tools\video\upload-youtube.ps1 -Lesson docs\video\kich-ban\d2c-1.8.15.json
```

Đưa một video đã đăng ra công khai:

```powershell
.\tools\video\upload-youtube.ps1 -SetPublic -VideoId <id>
```

Hạn mức mặc định của YouTube khoảng sáu video một ngày.
