# 05 — Đối chiếu tài liệu và độ bao phủ

## 1. Tệp tham khảo — hiện diện trong repo

| Tệp | Trạng thái |
|---|---|
| `docs/Danh_sach_lenh_iSurvey.xlsx` | EXISTS |
| `docs/Danh_sach_lenh_iSurvey_De_xuat_quan_ly_video_KT04.xlsx` | **MISSING** |
| `docs/video/CATALOG.md` | EXISTS |
| `docs/video/QUY_TRINH.md` | EXISTS |
| `docs/video/YOUTUBE.md` | EXISTS |
| `docs/video/iSurvey_Video_Production_Standard_v1.0.md` | **MISSING** |
| `docs/video/iSurvey_Video_Prompt_Master_v1.0.md` | **MISSING** |
| `docs/video/iSurvey_Video_Schemas_v1.0.json` | **MISSING** |
| `docs/video/thanh-pham/` | **MISSING** trong workspace (QUY_TRINH: thành phẩm ngoài git) |
| `tools/video/*` | EXISTS (8 tệp) |
| `docs/changelog/CHANGELOG.md` | EXISTS |
| `deploy/iSurvey.bundle/PackageContents.xml` | EXISTS |

## 2. Ma trận sai khác (rút gọn)

| TÀI LIỆU CŨ | BẰNG CHỨNG SOURCE | QUYẾT ĐỊNH KT HIỆN HÀNH (tài liệu audit) | HƯỚNG XỬ LÝ |
|---|---|---|---|
| QUY_TRINH: giọng `vi-VN-HoaiMyNeural` | `kich-ban/*.json` field `voice` + `narrate.ps1`/`edge-tts` | Giọng NAM miền Bắc; 2 video thử giọng | Không áp dụng giọng cũ khi làm Factory mới; giữ edge-tts như công cụ cũ cần đánh giá lại |
| QUY_TRINH: mỗi chức năng một video; duyệt kịch bản cuối phiên | CATALOG nhiều dòng/lệnh; kịch bản md+json | Không mặc định 1 lệnh = 1 video; bỏ duyệt từng lời thoại | CATALOG cũ = đầu vào tham khảo, không phải chuẩn sản xuất mới |
| CATALOG D2C 1.8.15 “Đã ghép, chưa đăng” | Không có `thanh-pham` trong repo; không kiểm được MP4 | Upload/publication tách; G1–G4 | Xác minh file MP4 ngoài git nếu còn; không tuyên bố chất lượng |
| Excel danh sách lệnh | 125 CommandMethod + 25 placeholder | Kiểm kê source là nguồn sự thật | Cập nhật Excel khi triển khai Factory (ngoài phạm vi đợt này) |
| Standard/Prompt/Schema v1.0 | MISSING trong repo | Không dùng giả định từ ChatGPT upload | Chủ dự án bổ sung bản chính thức vào repo nếu cần đối chiếu tiếp |
| PackageContents liệt kê nhiều Command | Khớp một phần với CommandMethod; stub UAV vẫn khai báo | — | Giữ đồng bộ khi thêm/bớt lệnh |

## 3. Độ bao phủ kiểm toán ↔ tài liệu video

| Nguồn | Số mục (xấp xỉ) | Ghi chú |
|---|---:|---|
| CommandMethod source | 125 | Inventory đầy đủ |
| Ribbon placeholder không lệnh | 25 | CSV AUD-126+ |
| Dòng CATALOG.md | ~18 lệnh có dòng | Thiếu vs source |
| Kịch bản `kich-ban/*` | 39 tệp (md/json cặp) | Tập trung 1.8.15–1.8.17 |

**Kết luận bao phủ tài liệu video:** CATALOG/kich-ban **không** phủ hết CommandMethod; nhiều alias không có video riêng (đúng với nguyên tắc mới cho phép gộp).

## 4. Xung đột cần Chủ dự án quyết

1. Chuẩn giọng / độ dài video / cổng duyệt: tài liệu repo cũ ≠ KT-01–07/09.  
2. Thiếu bộ Standard/Prompt/Schema trong repo → đối chiếu “quy chuẩn chính thức” `BỊ CHẶN DO THIẾU DỮ LIỆU`.  
3. Trạng thái “đã ghép” D2C không kiểm chứng được file media trong git.
