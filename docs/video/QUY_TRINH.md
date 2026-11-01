# Quy trình video hướng dẫn iSurvey

Mỗi chức năng đã chạy được trên Civil 3D có một video. Tiêu đề mang số phiên bản của **hành vi**, ví dụ **Nhập file DGN phiên bản 1.8.15**: cách làm trong video có từ iSurvey 1.8.15. Bản sau không đổi Nhập DGN thì vẫn dùng video này. Khi chức năng đổi, làm video mới với số mới. Video cũ giữ trên kênh.

Video là màn hình Civil 3D thật, trên bản vẽ mới. Không quay bản vẽ đang làm. Không dùng video AI để vẽ hộp thoại.

## Khi nào viết kịch bản

Cuối phiên, nếu một lệnh người dùng thấy đã đổi và đã nhìn trên Civil:

1. Lấy số phiên bản là mục changelog cuối cùng đụng chức năng đó.
2. Viết `docs/video/kich-ban/<lenh>-<phien-ban>.md` và file `.json` cùng tên.
3. Thêm một dòng vào `docs/video/CATALOG.md`, trạng thái **Chưa quay**.

Chưa quay ở bước này.

## Khi nào quay và đăng

Chỉ khi được yêu cầu **làm video** kèm tên chức năng.

1. Cài công cụ một lần: `.\tools\video\setup.ps1`
2. Đăng nhập YouTube một lần nếu chưa có token: `docs/video/YOUTUBE.md`, rồi `.\tools\video\upload-youtube.ps1 -Login`
3. Mở Civil 3D. Đăng nhập iSurvey trước khi quay, để video không lộ mã OTP.
4. Chuẩn bị file mẫu trên bản vẽ mới, ngoài camera nếu bước đó không thuộc bài học.
5. Với từng cảnh `screen` trong json: `.\tools\video\record-civil.ps1 -OutFile <raw>\<id>.mp4` rồi thao tác, sau đó `-Stop`.
6. Ghép tiếng: `.\tools\video\mux.ps1 -Lesson docs\video\kich-ban\<id>.json`
7. Đăng không công khai: `.\tools\video\upload-youtube.ps1 -Lesson docs\video\kich-ban\<id>.json`
8. Ghi link vào sổ video, trạng thái **Không công khai**. Khi được yêu cầu công khai, chạy upload với `-Privacy public` không áp vào video đã đăng; dùng lại token và API `videos.update`, hoặc đổi trên YouTube Studio. Script `-SetPublic -VideoId` làm việc đó.

Thành phẩm nằm ở `docs/video/thanh-pham/` và không vào git.

## Giọng và hình

- Giọng máy `vi-VN-HoaiMyNeural`. Không thu micro.
- Mỗi cảnh một đoạn hình, khớp với câu thoại của cảnh đó.
- Thẻ đầu và thẻ cuối do script vẽ, không quay màn hình.
