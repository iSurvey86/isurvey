# 02 — Kiểm kê lại Command / UI (R02B)

## Cải tiến so R02

| Hạng mục | R02 | R02B |
|---|---|---|
| Canonical | 79 = mọi non-`CommandAliases` declaration | **63** sau gộp SAME_FEATURE (thin zero-arg) |
| Stub helper | có thể gộp nhầm qua `Stub()` | **không gộp** `Stub("CMD")` — 8 stub tách |
| Nhóm | substring dễ sai | **ưu tiên đường dẫn module** |
| XAML/Palette | inventory thiếu ma trận | `03A` + `03D` đầy đủ |
| Evidence | `SOURCE_BEHAVIOR_REVIEWED` tự động | mức mới + quy tắc tái kiểm |

## Số liệu

| Metric | Giá trị |
|---|---:|
| CommandMethod | 125 |
| Alias CommandAliases.cs | 46 |
| Non-alias declarations | 79 |
| Canonical features | **63** |
| SOURCE_PRESENT / STUB | 55 / 8 |
| SAME_FEATURE decisions | 16 |
| Ribbon / XAML / Palette | 61 / 91 / 8 |
| Entry matrix rows | 285 |

## Không suy mỗi UI = feature mới

XAML Click phân loại: `{'CLOSE_CANCEL': 47, 'SETTINGS': 7, 'INTERNAL': 29, 'DIALOG_OK': 8}` — CLOSE/CANCEL/SETTINGS/INTERNAL không tạo feature mới.
