# 04E — UAV/LiDAR, AI, Palette, Ribbon placeholder

## UAV & LiDAR (`UAV_LiDAR`) — `UavModule.cs`

### Đã implement (không stub)

| Lệnh | Alias | Method | Line |
|---|---|---|---:|
| ISURVEY_UAV_CLIP | UAVCLIP | ClipCommand | 10 |
| ISURVEY_UAV_FRAME_TOGGLE | AK | FrameToggleCommand | 19 |
| ISURVEY_UAV_SEND_BACK | AD | SendBackCommand | 28 |

Xử lý ảnh: `UavImageService` (clip/toggle/send back). Auth trước khi chạy.

### Stub — chỉ `ShowAlertDialog("Tính năng đang phát triển")`

`UavModule.cs:37–62` — ISURVEY_UAV_INSERT/DEM/CONTOUR/CRS, ISURVEY_PC_IMPORT/FILTER/GRID và alias AU/DEM/DM/HT/NP/LP/RL.

**Video:** `KHONG_DE_XUAT_VIDEO_STUB`.  
Ribbon vẫn có nút enabled trỏ vào một số stub (ví dụ DEM split) — bằng chứng UI cho thấy nút hiện nhưng runtime chỉ alert (`CÓ BẰNG CHỨNG TỪ MÃ NGUỒN`).

## Trợ lý AI (`Tro_ly_AI`)

| Lệnh | Alias | Source |
|---|---|---|
| ISURVEY_AI_ASSIST | AIA | `AiAssistantModule.cs:13` / aliases |
| ISURVEY_AI_SETTINGS | AIC | `AiAssistantModule.cs:26` |

- Panel Ribbon AI chỉ khi Admin (`RibbonBuilder` đồng bộ `_aiPanelPresent` / `IsAdmin`).
- **Không** đưa vào video công khai (Mục 04 audit + đối tượng ADMIN).

## Palette

| Lệnh | Alias | Source |
|---|---|---|
| ISURVEY_PALETTE | BANGIS | `PaletteModule.cs:9` / `:188` |

- PaletteSet khảo sát — UI phức tạp; video Tips/Overview nếu sau này được duyệt.

## Ribbon placeholder (25) — nhóm `Ribbon_Placeholder_Chua_Implement`

Tất cả `enabled=false` trong `RibbonBuilder.cs`; **không** có `[CommandMethod]` tương ứng; handler `RibbonPlaceholderHandler` in message yêu cầu đăng nhập.

Danh sách lệnh chuỗi (AUD-126…150 trong CSV):  
TAOKHUNG, TILEKHUNG, KHOAKHUNG, RAIMONG, BIENDUONGTAM, KETUYEN, BANGTOADO, KIEMTRACAODO, XUATEXCEL, GHISODM, NANGLCAO, TAODIEMCAD, HIENDIEMMA, LOCDIEMMA, CHENDIAVAT, TATDIEMBD, HIENDIEMTAT, XOADIEMVUNG, NOIDIEM, GANCAO, PHATSINHDIEM, DINHVIMIA, XOAYMIA, LOCDIEM, CHUYENFONT.

**Không** lập fixture / không quay video cho nhóm này cho đến khi có CommandMethod thật.
