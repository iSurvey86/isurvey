# 07 — Đánh giá tự động hóa Video Factory (chỉ trên mã nguồn)

Gán trạng thái: `HIỆN CÓ TRONG SOURCE` | `CÓ MỘT PHẦN` | `CHƯA TÌM THẤY` | `CHƯA ĐỦ BẰNG CHỨNG`.  
**Không** khẳng định runtime (4K, tắt màn hình, TTS thương mại, Drive, YouTube PASS).

| Thành phần (theo yêu cầu audit) | Trạng thái | Dẫn chứng | Ghi chú |
|---|---|---|---|
| Nhận diện phiên bản source / bundle template | `HIỆN CÓ TRONG SOURCE` | `iSurvey.csproj:11`, `PackageContents.xml:6` | Khớp máy đang nạp: chưa chứng thực |
| Bộ fixture tái tạo / baseline độc lập | `CHƯA TÌM THẤY` | Không có thư mục fixture chuẩn / test CAD tự động trong repo | Cần thiết kế mới |
| Điều khiển CAD thật / quan sát trước-sau | `CHƯA TÌM THẤY` trong product code | — | `tools/video/record-civil.ps1` chỉ ghi hình thủ công |
| Ghi hình UI / capture | `CÓ MỘT PHẦN` | `tools/video/record-civil.ps1` | Chưa chứng minh 4K/DPI/P3225QE |
| Giọng TTS + phụ đề | `CÓ MỘT PHẦN` | `requirements.txt` edge-tts; `narrate.ps1`; json `voice=vi-VN-HoaiMyNeural` | Giọng nữ cũ ≠ KT mới (nam miền Bắc); SRT/VTT pipeline: chưa thấy rõ trong script đã đọc |
| Dựng video / mux | `CÓ MỘT PHẦN` | `mux.ps1`, `build_lesson.py` | Master 4K + preview 1080: `CHƯA ĐỦ BẰNG CHỨNG` |
| Điều phối tác vụ / ca máy / bàn giao | `CHƯA TÌM THẤY` | — | Không có orchestrator tuần tự Drive |
| Bảo vệ source; Drive one-way | `CÓ MỘT PHẦN` (quy ước docs) | `YOUTUBE.md` secret ngoài git; QUY_TRINH thanh-pham ngoài git | Không có code chặn sync ngược |
| Phê duyệt G1–G4 / G5 / upload≠publish | `CHƯA TÌM THẤY` trong code | Upload script tồn tại | Cổng G* chưa mã hóa trong repo |
| Sao lưu/khôi phục công đoạn | `CHƯA TÌM THẤY` | — | |
| YouTube upload | `CÓ MỘT PHẦN` | `upload_youtube.py`, `upload-youtube.ps1`, `YOUTUBE.md` | `client_secret.json` MISSING trên máy audit snapshot |
| Catalog / kịch bản bài học | `HIỆN CÓ TRONG SOURCE` | `docs/video/CATALOG.md`, `kich-ban/*` | Chuẩn cũ |

## Rủi ro tự động hóa (source-based)

| Rủi ro | Mức | Vì sao |
|---|---|---|
| Modal WPF (VEMCD, nhiều dialog) | P1 | Khó “lệnh thuần” không chuột |
| Stub UAV enabled trên Ribbon | P1 | Quay nhầm ra alert “đang phát triển” |
| 25 placeholder Ribbon | P2 | Người xem nhầm tính năng có thật |
| Auth bắt buộc | P1 | Video lộ OTP nếu không chuẩn bị ngoài camera (QUY_TRINH đã cảnh báo) |
| Giọng TTS không khớp KT mới | P1 | Phải thay voice / từ điển trước pilot Factory |
| Không fixture chuẩn | P0 cho pilot có đo | Không baseline độc lập trong repo |

## Kết luận mục 07

Nền tảng **quay thủ công + TTS + mux + upload** có một phần trong `tools/video`. **Chưa** có Video Factory đủ KT-01–07/09 trong source. Mọi tuyên bố vận hành: `CHƯA KIỂM CHỨNG THỰC TẾ`.
