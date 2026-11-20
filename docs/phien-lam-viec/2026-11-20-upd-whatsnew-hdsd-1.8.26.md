# 2026-11-20 — UPD What's New + HDSD + phát hành 1.8.22→1.8.26

Máy văn phòng (Civil 3D 2026). Phiên dài: sửa notes UPD, ASC relaunch, What's New HDSD, sắp xếp notes, UI danh sách lệnh web.

## Đã làm (sản phẩm iSurvey)

| Bản | Nội dung |
|---|---|
| **1.8.22** | Vá cwd khi relaunch sau UPD (tránh FATAL ASC). |
| **1.8.23** | Notes cắt theo máy: `[bản đang cài … latest]`. |
| **1.8.24** | Tối đa 5 khối notes; link HDSD `/tinh-nang-moi`. |
| **1.8.25** | Không tự mở lại Civil sau cài; hộp thoại VI có dấu. |
| **1.8.26** | Notes **mới nhất trước**. Đã `publish-release`. |

File chính: `UpdateWindow.xaml(.cs)`, `Apply-iSurveyUpdate.ps1`, `Install-iSurvey.ps1`, `UpdateApplyService.cs`, `deploy/sync-hdsd-tinh-nang-moi.ps1`, `Huong_dan_Phat_hanh.md`.

## HDSD (repo ksnpsc)

- Tab **Tính năng mới** (sync curated từ CHANGELOG, không lộ tech/admin).
- Bảng **Danh sách lệnh**: STT đậm; hover/focus gạch chân + `cursor: pointer`. Đã push `main` → Vercel.

## Catalog / video

- `Danh_sach_lenh_iSurvey.xlsx`: dòng UPD → **1.8.26**.
- Kịch bản + CATALOG: `upd-1.8.26` (Chưa quay).

## Việc tiếp

- [ ] Máy client: UPD lên 1.8.26; xác nhận notes mới nhất trước + không tự relaunch.
- [ ] Ctrl+F5 HDSD Giới thiệu / Tính năng mới sau Vercel.
- [ ] Quay video UPD 1.8.26 khi được yêu cầu.
