# 2026-10-30 — Nghiên cứu mặt cắt dọc (chưa code)

Máy văn phòng. Cuối phiên đầy đủ. **Không bump.** Vẫn **1.8.14**, chưa phát hành.

## Đã chốt về mặt cắt dọc

Chuỗi đúng: tạo mô hình → thêm điểm / đường đồng mức / đường bao → xây dựng lưới tam giác → thêm tuyến → phát sinh cọc → **VEMCD** vẽ mặt cắt → hiệu chỉnh → **RAIMCD** rải khung in.

`RAIMCD` không vẽ mặt cắt. `VEMCD` là tên lệnh vẽ, chưa có `CommandMethod`.

Giao diện dự kiến: bảng neo tab dọc iSurvey (Khảo sát, Khung in, UAV, Bản đồ). Học bố cục cây trên / thuộc tính dưới / tab phải. Không copy tab hay icon của phần mềm khác.

Báo cáo Word: `docs/Bao_cao_phan_tich_module_ve_mat_cat_doc_iSurvey.docx`. Bản ngắn: `docs/nghien-cuu/ADSCivil-mat-cat-doc.md`.

## Catalog

Thêm dòng `VEMCD` / `ISURVEY_PROFILE_DRAW`, trạng thái **Đang làm**, phiên bản 1.8.14, ngày 30/10/2026. Dòng `RAIMCD` ghi rõ chỉ rải khung. `DGNCFG` đã ghi **Bỏ**.

## Cùng commit, vẫn 1.8.14

Các việc đã nằm cây làm việc từ trước trong phiên, chưa đẩy:

- Bỏ lệnh Cấu Hình DGN (`DGNCFG`). Cài đặt nằm trên hộp Nhập DGN và Xuất DGN.
- Mỗi nút ribbon một icon riêng.
- Heartbeat khi Civil còn mở; last seen và VER theo nhật ký. Script `deploy/supabase/schema_last_seen.sql`.

Không đưa vào git: bản vẽ thử, file DGN, video, thông tư trong `cad2dgn`, `dgn`, `cspl_dc`, `cspl_dh`, `prj_vn2000_63tinh`, `docs/Video_*.wmv`.

HDSD PDF và phân trang nhật ký thuộc repo ksnpsc, không có trong commit này.
