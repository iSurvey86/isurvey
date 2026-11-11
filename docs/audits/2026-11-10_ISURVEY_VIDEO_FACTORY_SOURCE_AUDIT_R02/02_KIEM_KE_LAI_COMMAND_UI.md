# 02 — Kiểm kê lại Command / UI (R02)

## Phương pháp cải tiến so với R01

| Hạng mục | R01 | R02 |
|---|---|---|
| CommandMethod | regex từng dòng | regex toàn file (đa dòng / flags) |
| Alias | 46, liên quan rỗng | resolve `=> _x.Method()` → primary |
| Ribbon `new Btn` | cùng dòng | `re.S` RibbonBuilder |
| XAML Click | chưa | `_inventory_xaml_clicks.csv` (91) |
| Palette | chưa đủ | `_inventory_palette_runs.csv` (8) |

## Số liệu R02

| Metric | Giá trị |
|---|---:|
| CommandMethod tổng | 125 |
| Trong CommandAliases.cs | 46 |
| Primary declarations | 79 |
| Canonical features | 79 |
| IMPLEMENTED | 71 |
| STUB | 8 |
| Ribbon Btn | 61 (en 36 / dis 25) |
| Ribbon placeholder | 25 |
| Alias → primary | 46/46 |

## Đối chiếu R01

- R01: 125 CommandMethod, 61 Ribbon, 150 dòng CSV (125+25 placeholder).
- R02: **cùng 125** CommandMethod → không phát hiện thêm khai báo literal bị bỏ sót bởi parser đa dòng trong corpus hiện tại.
- **Không** dùng 150 làm mẫu số feature. Canonical = **79**; placeholder Ribbon = **25** (loại khỏi mẫu số video user).

## Giới hạn còn lại

- Chưa Roslyn AST.
- Context menu / dynamic macro: **CHƯA ĐỦ BẰNG CHỨNG**.
- XAML Click handlers chưa map 1-1 hết sang feature_id (một phần là nút trong dialog dùng chung).
