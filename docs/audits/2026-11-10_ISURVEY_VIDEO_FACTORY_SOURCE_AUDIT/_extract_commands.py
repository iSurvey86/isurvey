# Readonly extractor for Phase-01 audit — outputs into this folder only.
# -*- coding: utf-8 -*-
from __future__ import annotations

import csv
import os
import re
from collections import defaultdict
from pathlib import Path

ROOT = Path(r"D:\AIProject\isurvey")
OUT = Path(__file__).resolve().parent

CMD_RE = re.compile(
    r'\[CommandMethod\(\s*"([^"]+)"(?:\s*,\s*([^\]]+))?\)\]'
)
METH_RE = re.compile(
    r"(?:public|internal|private)\s+(?:static\s+)?(?:void|Task)\s+(\w+)\s*\("
)

ALIAS_FILE = "src/CommandAliases.cs"


def rel(p: Path) -> str:
    return p.relative_to(ROOT).as_posix()


def scan_commands():
    rows = []
    for path in sorted((ROOT / "src").rglob("*.cs")):
        text = path.read_text(encoding="utf-8", errors="replace")
        lines = text.splitlines()
        for i, line in enumerate(lines, 1):
            m = CMD_RE.search(line)
            if not m:
                continue
            meth = ""
            for j in range(i, min(i + 10, len(lines) + 1)):
                mm = METH_RE.search(lines[j - 1])
                if mm:
                    meth = mm.group(1)
                    break
            rows.append(
                {
                    "cmd": m.group(1),
                    "flags": (m.group(2) or "").strip(),
                    "method": meth,
                    "file": rel(path),
                    "line": i,
                    "is_alias_file": path.as_posix().endswith("CommandAliases.cs"),
                }
            )
    return rows


def scan_ribbon_btns():
    path = ROOT / "src/UI/RibbonBuilder.cs"
    text = path.read_text(encoding="utf-8", errors="replace")
    # new Btn("label", "CMD", icon, enabled[, tooltip])
    btn_re = re.compile(
        r'new Btn\(\s*"([^"]*)"\s*,\s*"([^"]+)"\s*,\s*[^,]+,\s*(true|false)',
        re.M,
    )
    rows = []
    for i, line in enumerate(text.splitlines(), 1):
        for m in btn_re.finditer(line):
            rows.append(
                {
                    "label": m.group(1).replace("\\n", " "),
                    "cmd": m.group(2),
                    "enabled": m.group(3) == "true",
                    "file": "src/UI/RibbonBuilder.cs",
                    "line": i,
                }
            )
    return rows


def group_for_cmd(cmd: str) -> str:
    c = cmd.upper()
    if c.startswith("ISURVEY_LOGIN") or c in {
        "ISURVEY_LOGOUT",
        "ISURVEY_ABOUT",
        "ISURVEY_UPDATE",
        "ISURVEY_HDSD",
        "ISURVEY_RIBBON",
        "ISURVEY_GENOTP",
        "LGI",
        "LGO",
        "ABT",
        "UPD",
        "HD",
        "MENUISURVEY",
    }:
        return "He_thong_Auth"
    if "AI_" in c or c in {"AIA", "AIC"}:
        return "Tro_ly_AI"
    if "TERRAIN" in c or c in {
        "TAOMOHINH",
        "THEMMOHINH",
        "XAYMOHINH",
        "ANTIN",
        "TRACAOD",
        "GOMOHINH",
        "XOATHANHPHAN",
    }:
        return "Dia_hinh_TIN"
    if "ROUTE" in c or "STATION" in c or c in {
        "TAOTUYEN",
        "PHATSINHCOC",
        "XOACOC",
        "XOACOC1",
        "DAODAUTUYEN",
        "THIETLAPCOC",
        "THEMCOC",
        "BANGCOC",
        "GOC",
        "DUOITUYEN",
        "STT",
        "ISURVEY_NUMBER_VERTICES",
        "ISURVEY_STRIP_MAP",
    }:
        return "Tuyen_Coc"
    if "PROFILE" in c or c in {"VEMCD", "RAIMCD", "ISURVEY_PROFILE_SHEETS"}:
        return "Mat_cat_doc"
    if "UAV" in c or "PC_" in c or c in {
        "UAVCLIP",
        "AK",
        "AD",
        "AU",
        "DEM",
        "DM",
        "HT",
        "NP",
        "LP",
        "RL",
    }:
        return "UAV_LiDAR"
    if "MAP" in c or "GE" in c or c in {"IG", "SG", "XG", "UG"}:
        return "Ban_do_GE"
    if "KML" in c or c in {"INK", "EXK", "EG"}:
        return "KML_KMZ"
    if "DGN" in c or c in {"D2C", "C2D"}:
        return "DGN"
    if "TAB" in c or c == "T2C":
        return "TAB_MapInfo"
    if c in {"NHAPDIEM", "XUATDIEM", "ND", "XD"}:
        return "Diem_do"
    if "KHUNG" in c or "LOGO" in c or "FRAME" in c or c in {
        "KHUNG",
        "KHUNGBV",
        "ANLOGO",
        "ANLOGO1",
        "ANLOGO2",
        "RAIKHUNG",
        "INTUYEN",
        "THAYLOGO",
        "CODAN",
        "SK",
        "ISURVEY_FIT_TITLE",
        "ISURVEY_FRAME_EDIT",
        "ISURVEY_ROUTE_SHEETS",
        "ISURVEY_ROUTE_PLOT",
    }:
        return "Khung_Layout"
    if c in {"CATBD", "CBD"}:
        return "Cat_binh_do"
    if c in {"AUTOSCAN", "SCN"}:
        return "Scan"
    if c in {"ISURVEY_PALETTE", "BANGIS"}:
        return "Palette_UI"
    return "Khac"


def audience(cmd: str, group: str) -> str:
    if cmd in {"ISURVEY_GENOTP"} or group == "Tro_ly_AI":
        return "ADMIN"
    if group == "He_thong_Auth":
        return "USER"
    return "USER"


def public_ok(aud: str, enabled_ribbon: bool | None, is_stub: bool) -> str:
    if aud == "ADMIN":
        return "KHONG_CONG_KHAI_ADMIN"
    if is_stub:
        return "KHONG_DE_XUAT_VIDEO_STUB"
    if enabled_ribbon is False:
        return "RIBBON_PLACEHOLDER_CHUA_LENH"
    return "CO_THE_XEM_XET_CONG_KHAI"


def main():
    cmds = scan_commands()
    ribbon = scan_ribbon_btns()
    ribbon_by_cmd = defaultdict(list)
    for r in ribbon:
        ribbon_by_cmd[r["cmd"]].append(r)

    # Map alias -> primary by reading alias method bodies is hard; use same-method targets from alias file comments + known pairs
    primary = [r for r in cmds if not r["is_alias_file"]]
    aliases = [r for r in cmds if r["is_alias_file"]]

    # Build feature rows: one per UNIQUE command string (each CommandMethod is a user-visible entry point)
    # Also merge primary+alias into features by pairing known duals later in markdown.
    stub_cmds = {
        "ISURVEY_UAV_INSERT",
        "ISURVEY_UAV_DEM",
        "ISURVEY_UAV_CONTOUR",
        "ISURVEY_UAV_CRS",
        "ISURVEY_PC_IMPORT",
        "ISURVEY_PC_FILTER",
        "ISURVEY_PC_GRID",
        "AU",
        "DEM",
        "DM",
        "HT",
        "NP",
        "LP",
        "RL",
    }

    # Inventory dump
    inv_path = OUT / "_inventory_commands.csv"
    with inv_path.open("w", encoding="utf-8-sig", newline="") as f:
        w = csv.DictWriter(
            f,
            fieldnames=["cmd", "flags", "method", "file", "line", "is_alias_file"],
        )
        w.writeheader()
        w.writerows(cmds)

    rib_path = OUT / "_inventory_ribbon.csv"
    with rib_path.open("w", encoding="utf-8-sig", newline="") as f:
        w = csv.DictWriter(f, fieldnames=["label", "cmd", "enabled", "file", "line"])
        w.writeheader()
        w.writerows(ribbon)

    # Feature CSV (03): one row per unique cmd string found as CommandMethod
    # Sort stable
    unique = {}
    for r in cmds:
        unique.setdefault(r["cmd"], r)

    features = []
    for idx, cmd in enumerate(sorted(unique.keys()), 1):
        r = unique[cmd]
        g = group_for_cmd(cmd)
        aud = audience(cmd, g)
        rib = ribbon_by_cmd.get(cmd, [])
        enabled = None
        if rib:
            enabled = any(x["enabled"] for x in rib)
        is_stub = cmd in stub_cmds
        aud_code = f"AUD-{idx:03d}"
        features.append(
            {
                "ma_kiem_toan": aud_code,
                "ten_tinh_nang": cmd,
                "nhom": g,
                "doi_tuong_dung": aud,
                "command_chinh": cmd,
                "alias_lien_quan": "",
                "diem_vao_ui": "; ".join(
                    f'{x["label"]}(enabled={x["enabled"]}) @{x["file"]}:{x["line"]}'
                    for x in rib
                )
                or ("CommandAliases" if r["is_alias_file"] else "Command line / CommandMethod"),
                "source_path_line": f'{r["file"]}:{r["line"]}',
                "class_method": r["method"],
                "flags": r["flags"],
                "trang_thai_source": (
                    "STUB_MESSAGE_ONLY"
                    if is_stub
                    else (
                        "RIBBON_DISABLED_PLACEHOLDER"
                        if enabled is False
                        else "SOURCE_REVIEWED"
                    )
                ),
                "cong_khai": public_ok(aud, enabled, is_stub),
                "du_lieu_can": "CHUA_DU_BANG_CHUNG_CHI_TIET_XEM_HO_SO",
                "do_kho_pilot": (
                    "CAO"
                    if is_stub or enabled is False
                    else ("TRUNG_BINH" if g in {"DGN", "Mat_cat_doc", "UAV_LiDAR"} else "THAP")
                ),
                "video_goi_y": (
                    "KHONG"
                    if aud == "ADMIN" or is_stub or enabled is False
                    else "UNG_VIEN"
                ),
                "muc_xac_minh": "SOURCE_REVIEWED",
            }
        )

    # Fill alias_lien_quan: if primary exists and alias maps same purpose via naming conventions
    # Simple reverse: for each alias row, note it; for primary leave blank and fill in post from known pairs in report.
    feat_path = OUT / "03_DANH_MUC_LENH_VA_TINH_NANG.csv"
    with feat_path.open("w", encoding="utf-8-sig", newline="") as f:
        fields = list(features[0].keys())
        w = csv.DictWriter(f, fieldnames=fields)
        w.writeheader()
        w.writerows(features)

    summary = OUT / "_extract_summary.txt"
    summary.write_text(
        "\n".join(
            [
                f"cs_under_src={(ROOT/'src').rglob('*.cs').__iter__().__class__}",
                f"commandmethod_total={len(cmds)}",
                f"unique_cmds={len(unique)}",
                f"primary={len(primary)}",
                f"alias_file_cmds={len(aliases)}",
                f"ribbon_btns={len(ribbon)}",
                f"ribbon_enabled={sum(1 for x in ribbon if x['enabled'])}",
                f"ribbon_disabled={sum(1 for x in ribbon if not x['enabled'])}",
                f"features_csv={len(features)}",
            ]
        ),
        encoding="utf-8",
    )
    # proper cs count
    cs_count = sum(1 for _ in (ROOT / "src").rglob("*.cs"))
    xaml_count = sum(1 for _ in (ROOT / "src").rglob("*.xaml"))
    summary.write_text(
        "\n".join(
            [
                f"cs_under_src={cs_count}",
                f"xaml_under_src={xaml_count}",
                f"commandmethod_total={len(cmds)}",
                f"unique_cmds={len(unique)}",
                f"primary_decl={len(primary)}",
                f"alias_file_cmds={len(aliases)}",
                f"ribbon_btns={len(ribbon)}",
                f"ribbon_enabled={sum(1 for x in ribbon if x['enabled'])}",
                f"ribbon_disabled={sum(1 for x in ribbon if not x['enabled'])}",
                f"features_csv={len(features)}",
            ]
        ),
        encoding="utf-8",
    )
    print(summary.read_text(encoding="utf-8"))


if __name__ == "__main__":
    main()
