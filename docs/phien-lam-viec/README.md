# Phiên làm việc — iSurvey

Đồng bộ ngữ cảnh giữa các máy / chat Cursor qua git.

## Đầu phiên

1. `git pull`
2. Đọc **block đầu tiên** trong [HANDOFF.md](./HANDOFF.md)
3. (Tuỳ) mở file lưu trữ ngày được link trong block đó

## Cuối phiên — cụm lệnh bắt buộc

Khi user nói **`làm cuối phiên đầy đủ`**, AI phải làm đủ theo thứ tự:

1. **HANDOFF** — chèn block mới **lên đầu** `HANDOFF.md` (không xóa archive cũ).
2. **File ngày** — tạo `YYYY-MM-DD-slug.md` trong thư mục này; link từ block HANDOFF.
3. **Version** — bump `AppVersion` / `ComponentEntry Version` trong `deploy/iSurvey.bundle/PackageContents.xml` (và bản output nếu đang sync) khi phiên có thay đổi sản phẩm đáng kể; ghi số phiên bản trong block HANDOFF.
4. **Docs sản phẩm** — cập nhật `README.md` gốc (và `docs/` workflow / HDSD khi đã có) cho đúng hành vi mới.
5. **Changelog** — nếu đã có `docs/changelog/`, ghi mục phiên; nếu chưa thì ghi tóm tắt trong block HANDOFF là đủ.
6. **Commit + push** — chỉ khi user đã yêu cầu cụm「làm cuối phiên đầy đủ」(đồng nghĩa cho phép commit/push handoff + thay đổi phiên đó).

### Cấu trúc một block HANDOFF

- Tiêu đề: `## YYYY-MM-DD — Tóm tắt ngắn (x.y.z)`
- Máy / ngữ cảnh
- Đã chốt / đã làm
- File chính (bảng)
- Việc tiếp (checkbox)
- Câu mở phiên sau (fenced `text`)
- Link **Lưu trữ ngày**

## Quy tắc tư vấn / vá lỗi

- Ý tưởng mới: [`docs/Mẫu_Tư_Vấn.md`](../Mẫu_Tư_Vấn.md) — chờ **「XÁC NHẬN」** mới code full.
- Bug: [`docs/Mẫu_Vá_Lỗi.md`](../Mẫu_Vá_Lỗi.md) — chờ **「ĐỒNG Ý」** mới code full.

## FAQ dung lượng

- File handoff là text thuần, vài KB mỗi file — không ảnh hưởng build add-in.
- Không đưa vào bundle `iSurvey.bundle`.
