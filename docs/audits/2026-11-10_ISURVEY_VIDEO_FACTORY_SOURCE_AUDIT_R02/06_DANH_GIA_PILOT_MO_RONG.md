# 06 — Đánh giá pilot mở rộng (R02)

**Không chọn chạy pilot thay Chủ dự án.** Chỉ xếp hạng tư vấn.

## Tiêu chí (tĩnh)

Fixture dễ tạo | Đo độc lập | Modal/phức tạp UI | Phụ thuộc file ngoài | Rủi ro dữ liệu

## Ứng viên

| Ứng viên | Điểm tư vấn /40 | Ghi chú |
|---|---:|---|
| **XUATDIEM / NHAPDIEM** | **33** | Input CSV/TXT hoặc Point trên Drawing trắng; output file đo được ngoài CAD; UI hộp thoại vừa phải. **Ưu tiên đề xuất** nếu muốn fixture đơn giản. |
| D2C (`ISURVEY_DGN_TO_CAD`) | 30 | Cần DGN thật + seed; baseline không phải output iSurvey; phức tạp quyền/file. |
| VEMCD | 28 | Phụ thuộc mô hình→tuyến→cọc có Z; UI Zero-Clutter nhiều control; fixture dài. |
| UAVCLIP (`ISURVEY_UAV_CLIP`) | 27 | Cần raster + biên; reload/biên sơ họa; môi trường ảnh. |

## Kết luận tư vấn

1. Ưu tiên xem xét **NHAPDIEM hoặc XUATDIEM**.
2. Giữ D2C/VEMCD/UAVCLIP trong shortlist — **Chủ dự án quyết định**.
3. Không quay, không NETLOAD trong R02.
