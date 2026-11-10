# -*- coding: utf-8 -*-
import csv
from pathlib import Path

OUT = Path(__file__).resolve().parent
feat = list(csv.DictReader((OUT / "03_DANH_MUC_LENH_VA_TINH_NANG.csv").open(encoding="utf-8-sig")))
rib = list(csv.DictReader((OUT / "_inventory_ribbon.csv").open(encoding="utf-8-sig")))
cmds = {r["command_chinh"] for r in feat}
start = len(feat) + 1
extra = []
for r in rib:
    if r["enabled"] != "False":
        continue
    if r["cmd"] in cmds:
        continue
    idx = start + len(extra)
    line = r["line"]
    extra.append(
        {
            "ma_kiem_toan": f"AUD-{idx:03d}",
            "ten_tinh_nang": r["label"].replace("\n", " "),
            "nhom": "Ribbon_Placeholder_Chua_Implement",
            "doi_tuong_dung": "USER",
            "command_chinh": r["cmd"],
            "alias_lien_quan": "",
            "diem_vao_ui": f"Ribbon disabled @src/UI/RibbonBuilder.cs:{line}",
            "source_path_line": f"src/UI/RibbonBuilder.cs:{line}; handler=src/UI/RibbonPlaceholderHandler.cs",
            "class_method": "RibbonPlaceholderHandler",
            "flags": "",
            "trang_thai_source": "RIBBON_PLACEHOLDER_NO_COMMANDMETHOD",
            "cong_khai": "KHONG_DE_XUAT_VIDEO_CHUA_CO_LENH",
            "du_lieu_can": "N/A_CHUA_IMPLEMENT",
            "do_kho_pilot": "KHONG_AP_DUNG",
            "video_goi_y": "KHONG",
            "muc_xac_minh": "SOURCE_REVIEWED",
        }
    )

fields = list(feat[0].keys())
allrows = feat + extra
with (OUT / "03_DANH_MUC_LENH_VA_TINH_NANG.csv").open("w", encoding="utf-8-sig", newline="") as f:
    w = csv.DictWriter(f, fieldnames=fields)
    w.writeheader()
    w.writerows(allrows)
print("total_features", len(allrows), "added_placeholders", len(extra))
