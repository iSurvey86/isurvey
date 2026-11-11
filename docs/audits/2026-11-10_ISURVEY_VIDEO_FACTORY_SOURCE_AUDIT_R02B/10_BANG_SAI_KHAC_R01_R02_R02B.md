# 10 — Sai khác R01 → R02 → R02B

| Chỉ số | R01 | R02 | R02B |
|---|---:|---:|---:|
| HEAD | 53c5eb4… | 3a16cb3… | 3a16cb3… (cùng R02) |
| CSV “tính năng” | 150 dòng vào lệnh/nút | 79 canonical = non-alias cmds | **63** sau SAME_FEATURE |
| Alias liên kết | yếu | 46 trong CommandAliases | 46 + wrapper module trong 03C/03E |
| XAML map | chưa | inventory 91 | 91 + classify 03D |
| Evidence label | SOURCE_REVIEWED đồng loạt | SOURCE_BEHAVIOR_REVIEWED tự động | mức mới + manual deep priority |
| Nhóm sai | — | UPDATE_GE/DAODAU/… | sửa theo module path |

## P0 R02 → xử lý R02B

| P0 | R02B |
|---|---|
| P0-01 gộp wrapper | `03C` + canonical 63 |
| P0-02 XAML | `03A`/`03D` |
| P0-03 service sâu | manual deep 5 hành vi; còn lại giới hạn rõ |
