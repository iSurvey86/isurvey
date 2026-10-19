# 2026-10-19 — Cập nhật tự tải + thoát CAD + tự cài (1.7.2)

## Ngữ cảnh

Máy AutoCAD 2023. User hỏi UPD có tự cài không → chốt: tải về, bắt buộc thoát CAD, helper tự chạy Setup. Cuối phiên bump **1.7.2**.

## Đã làm

- `UpdateApplyService`: tải zip (http/https), giải nén an toàn, kiểm `Install-iSurvey.ps1` + `iSurvey.bundle`.
- `Apply-iSurveyUpdate.ps1`: chờ PID AutoCAD thoát → `Install-iSurvey.ps1` → MessageBox.
- `AuthModule` UPD: Cập nhật ngay → Thoát & cài → `Application.Quit()`.
- Docs: `CAI_DAT.txt`, `Huong_dan_Phat_hanh.md`. Bundle đã cài local.

## Việc mở

- Thử UPD từ máy đang 1.7.1 sau khi upload 1.7.2 lên `isurvey-releases`.
- Chuẩn hóa DGN (nhắc đầu phiên) vẫn hoãn.
