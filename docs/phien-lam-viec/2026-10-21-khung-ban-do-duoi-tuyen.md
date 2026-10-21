# 2026-10-21 — Khung bản đồ / bản vẽ kỹ thuật, DUOITUYEN, trợ lý AI (1.8.0)

Máy văn phòng, Civil 3D 2026. User yêu cầu "làm thực chiến": mọi tính năng phải được test thật trong Civil, tuyệt đối không đụng file dwg/ảnh gốc (chưa có bản sao lưu).

## DUOITUYEN (`ISURVEY_STRIP_MAP`)
- Chọn tim tuyến ở vị trí bất kỳ (không cần gần đầu tuyến) + bắt buộc chọn điểm đầu → đầu gần nhất là `DD`, rồi `G1…Gn`, cuối là `DC`; nếu điểm đầu cách đều hai đầu thì báo lỗi.
- Dải cắt luôn là hình chữ nhật vuông góc cạnh (bỏ miter) → ảnh liền mạch, không còn khía hình V; số liệu giữ chính xác.
- Bảng góc `.txt`: `Tên Điểm,X,Y,Khoảng Cách Cạnh Trước,Góc Rẽ` — góc rẽ `T/P độ phút giây` trong cùng một cột (ví dụ `P 43 34 53`), X/Y 2 số lẻ.
- Đã test trên bản sao dữ liệu thật (tuyến 25 đỉnh, 2 TIFF).

## Khung chuẩn
- Chuẩn: TCVN 7285:2003 = ISO 5457:1999 — vùng vẽ A0 821×1159, A1 574×811, A2 400×564, A3 277×390, A4 180×277 mm; lề trái 20, còn lại 10; khung 0,7 mm; ô chia 50 mm; 4 dấu định tâm.
- Mẫu `docs/KhungBD/KhungBanDo_Chuan.dwg` (= `src/Templates/`, ship vào `Contents\Templates`). Sinh bằng script PowerShell/COM (không nằm trong repo) từ `INFOR.txt` + `LogoEVN.png`.
- Logo: ngôi sao/vòng tròn dựng vector từ ảnh (trace 4×); chữ EVN/NPC từ đường viền font `HelveticaBlackVU` / `HelveticaBlackOblicVU` (khớp logo gốc, tỷ lệ lệch ≤ 2%). Ghi chú: user viết ngược (EVN nghiêng) so với logo thật — làm theo logo gốc (EVN đứng, NPC nghiêng).
- Ô "DỰ ÁN" chưa có trong `INFOR.txt` → giữ `<TÊN DỰ ÁN>`.

## Lệnh `KHUNG` / `KHUNGBV` (`src/Modules/Frame/FrameService.cs`)
- Hỏi: khổ, tỷ lệ, kiểu xoay (Khong / Goc / Tuyen), (chỉ KHUNG) vẽ lưới + Bắc + thước; jig chọn tâm; Enter kết thúc. Hệ số chèn `s = N / (1000 · unitMeters)`.
- Test thật: UCS dời + xoay 20° đúng tâm; góc `Tuyen` 45°; INSUNITS mm ⇒ s = 1000, unitless ⇒ s = 1; mặc định (Enter) đúng; lưới đúng bội 500 m ở 1/5000; nhãn X/Y đúng.
- Nhãn `X=`/`Y=` là **tọa độ CAD (WCS)** — khi khung xoay, lưới vẫn song song trục WCS.
- Bản vẽ đã có block `KB_A*` cũ: lệnh dùng lại định nghĩa cũ → cần `PURGE` rồi chèn lại để lấy khung mới.

## FILEDIA/CMDDIA
- Script test của tôi từng đặt `FILEDIA=0` / `CMDDIA=0` và để lại → user thấy hộp thoại không hiện. `Main.OnFirstIdle` nay tự đặt lại 1 và báo. Từ nay script test phải trả biến hệ thống về giá trị cũ.
- User tự bật Dynamic Input (F12): `DYNPROMPT=1`, `DYNMODE=3` — các lệnh iSurvey dùng từ khóa chuẩn nên hiện cạnh con trỏ.

## Bài học thao tác
- COM `SendCommand` từng lần mỗi dòng KHÔNG dùng được khi lệnh đang chờ nhập (RPC_E_CALL_REJECTED); gửi cả chuỗi trả lời trong một lần.
- `-PNGOUT` sai; dùng `_.PNGOUT` với `FILEDIA=0` (và nhớ trả lại).
- Hatch qua COM: dùng `DispatchWrapper[]` cho `AppendOuterLoop/AppendInnerLoop`.
- `install` thất bại nếu còn tiến trình `acad` (DLL khóa) → bundle AppData bị thiếu một phần; chạy lại sau khi thoát hẳn Civil.
- Đặt `INSUNITS=0` làm Civil bật hộp thoại "Lighting – Insert Units" (chặn COM) — tránh trong test.
