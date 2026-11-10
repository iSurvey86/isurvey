# 04D — Tuyến / cọc / mặt cắt dọc / khung layout

## Tuyến & cọc (`Tuyen_Coc`) — `RouteModule.cs` + services

| Full | Alias | RouteModule lines |
|---|---|---|
| ISURVEY_ROUTE_NEW | TAOTUYEN | 14 / 17 |
| ISURVEY_ROUTE_STATIONS | PHATSINHCOC | 20 / 23 |
| ISURVEY_ROUTE_CLEAR_STATIONS | XOACOC | 26 / 29 |
| ISURVEY_ROUTE_REVERSE | DAODAUTUYEN | 32 / 35 |
| ISURVEY_STATION_STYLE | THIETLAPCOC | 38 / 41 |
| ISURVEY_STATION_REMOVE_ONE | XOACOC1 | 44 / 47 |
| ISURVEY_STATION_ADD_ONE | THEMCOC | 50 / 53 |
| ISURVEY_STATION_LIST | BANGCOC | 56 / 59 |

Bổ sung:

| Lệnh | Source |
|---|---|
| ISURVEY_NUMBER_VERTICES / GOC | `VertexNumberingService.cs:37` / aliases `:112` |
| ISURVEY_STRIP_MAP / DUOITUYEN | `StripMapService.cs:27` / `:115` |
| STT | `SequenceNumberService.cs:28` |

- Tiền điều kiện VEMCD (source): có tuyến + cọc có Z — `ProfileModule.cs:47–50` thông báo chạy TAOTUYEN rồi PHATSINHCOC.
- Lưu trữ: `RouteStore` trên Database DWG (tham chiếu từ ProfileModule Load).
- Video: nhiều kịch bản 1.8.16/1.8.17 trong `docs/video/kich-ban/`.

## Mặt cắt dọc (`Mat_cat_doc`)

| Lệnh | Source |
|---|---|
| ISURVEY_PROFILE_DRAW | `ProfileModule.cs:14` → `Draw()` |
| VEMCD | `ProfileModule.cs:17` → cùng `Draw()` |
| ISURVEY_PROFILE_SHEETS / RAIMCD | `FrameService.ProfileLayout.cs:28` / alias `:136` |

### Truy vết VEMCD (trọng điểm)

1. Auth: `AuthManager.EnsureAuthenticated("VEMCD")` — `ProfileModule.cs:22`.
2. Chỉ Model Space — `:28–32`.
3. Load routes — `:39`; empty → message `:47–50`.
4. UI: `DrawProfileWindow.Prompt` — `:88` (WPF Zero-Clutter theo HEAD).
5. Vẽ: `ProfileDrawService` + `ProfileStore` handles (redraw/erase) — file `ProfileDrawService.cs`, `ProfileStore.cs`.
6. Hard-code UI: `NormalizeHardCodedOptions` — SampleStep 20, ProfileTextHeight 12, ScaleBarWidth 1 — `ProfileDrawService.cs` (Normalize*).
7. Kết quả thiết kế: entity đầu bảng + đường đất + thước trên Model; layer `iSurvey_TD_*` từ mẫu hàng.

**Modal WPF:** có — độ khó quay trung bình–cao (cần thao tác dialog + pick điểm chèn).  
**Mức xác minh đợt này:** `SOURCE_REVIEWED`. Runtime trên Civil: ngoài phạm vi (dù lịch sử handoff ghi đã nhìn — **không** dùng làm bằng chứng đợt kiểm toán tĩnh này trừ khi coi là tài liệu phụ: đánh dấu `SUY LUẬN CẦN XÁC MINH` nếu lấy từ HANDOFF).

RAIMCD: chỉ rải khung in theo mô tả docs/README — điểm vào ProfileLayout; không đổi mức so sánh theo changelog.

## Khung & layout (`Khung_Layout`)

Điểm vào chính trong `FrameService.cs` / partials:

| Lệnh | Ghi chú |
|---|---|
| ISURVEY_KHUNG / KHUNG | Chèn khung bản đồ |
| ISURVEY_KHUNG_BV / KHUNGBV | Khung kỹ thuật |
| ISURVEY_LOGO* / ANLOGO* | Bật/tắt logo |
| ISURVEY_LOGO_REPLACE / THAYLOGO | Thay logo |
| ISURVEY_FIT_TITLE / CODAN | Fit chữ khung tên |
| ISURVEY_FRAME_EDIT / SK | Sửa thông tin khung |
| ISURVEY_ROUTE_SHEETS / RAIKHUNG | Rải khung theo tuyến |
| ISURVEY_ROUTE_PLOT / INTUYEN | Plot tuyến (`UsePickSet`) |

- Template DWG: `src/Templates/*.dwg`.
- Tác động layout/paper space — fixture layout phức tạp hơn Model-only.

## Gộp bài học (đề xuất, không sản xuất)

- **Workflow khảo sát MCD:** TAOTUYEN → PHATSINHCOC → VEMCD → (tuỳ) RAIMCD.
- **Cọc phụ:** THEMCOC / XOACOC1 / BANGCOC / THIETLAPCOC có thể gộp Tips sau bài PHATSINHCOC.
