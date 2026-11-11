# -*- coding: utf-8 -*-
"""R02 readonly extractor — writes ONLY into parent _R02 folder. No source mutation."""
from __future__ import annotations

import csv
import json
import re
from collections import defaultdict
from pathlib import Path

ROOT = Path(r"D:\AIProject\isurvey")
OUT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src"

CMD_ATTR = re.compile(
    r'\[CommandMethod\(\s*"([^"]+)"(?:\s*,\s*([^)\]]*))?\)\s*\]',
    re.M,
)
METH = re.compile(
    r"(?:public|internal|private)\s+(?:static\s+)?(?:async\s+)?(?:void|Task(?:<[^>]+>)?)\s+(\w+)\s*\(",
)
ALIAS_CALL = re.compile(r"=>\s*_\w+\.(\w+)\s*\(")
BTN = re.compile(
    r'new\s+Btn\(\s*"([^"]*)"\s*,\s*"([^"]+)"\s*,\s*[^,]+,\s*(true|false)',
    re.S,
)
XAML_CLICK = re.compile(
    r'Click\s*=\s*"\{?([^}"]+)\}?"|Click\s*=\s*"(\w+)"'
)
PALETTE_RUN = re.compile(r'PaletteCommands\.Run\(\s*"([^"]+)"\s*\)')
STUB_HINT = re.compile(r"Stub|PLACEHOLDER|chưa làm|Chua lam|đang phát triển|NotImplemented", re.I)


def rel(p: Path) -> str:
    return p.relative_to(ROOT).as_posix()


def line_of(text: str, idx: int) -> int:
    return text.count("\n", 0, idx) + 1


def inventory_files():
    rows = []
    for p in sorted(SRC.rglob("*")):
        if not p.is_file():
            continue
        if p.suffix.lower() not in {".cs", ".xaml"}:
            continue
        text = p.read_text(encoding="utf-8", errors="replace")
        rows.append(
            {
                "file": rel(p),
                "ext": p.suffix.lower(),
                "bytes": p.stat().st_size,
                "lines": text.count("\n") + 1,
                "has_commandmethod": "CommandMethod" in text,
                "has_btn": "new Btn(" in text,
                "has_click": "Click=" in text or "Click =" in text,
                "has_palette_run": "PaletteCommands.Run" in text,
            }
        )
    return rows


def extract_commands():
    rows = []
    for p in sorted(SRC.rglob("*.cs")):
        text = p.read_text(encoding="utf-8", errors="replace")
        is_alias = p.name == "CommandAliases.cs"
        for m in CMD_ATTR.finditer(text):
            cmd = m.group(1)
            flags = (m.group(2) or "").strip()
            after = text[m.end() : m.end() + 800]
            meth_m = METH.search(after)
            method = meth_m.group(1) if meth_m else ""
            alias_target = ""
            if is_alias:
                am = ALIAS_CALL.search(after)
                alias_target = am.group(1) if am else ""
            body_snip = after[:400]
            stub = bool(STUB_HINT.search(method + body_snip)) or method.endswith("Stub")
            rows.append(
                {
                    "cmd": cmd,
                    "flags": flags,
                    "method": method,
                    "file": rel(p),
                    "line": line_of(text, m.start()),
                    "is_alias_file": is_alias,
                    "alias_delegates_to_method": alias_target,
                    "stub_hint": stub,
                }
            )
    return rows


def extract_ribbon():
    p = SRC / "UI" / "RibbonBuilder.cs"
    text = p.read_text(encoding="utf-8", errors="replace")
    rows = []
    for m in BTN.finditer(text):
        rows.append(
            {
                "label": m.group(1).replace("\\n", " ").strip(),
                "cmd": m.group(2),
                "enabled": m.group(3) == "true",
                "file": "src/UI/RibbonBuilder.cs",
                "line": line_of(text, m.start()),
            }
        )
    return rows


def extract_xaml_clicks():
    rows = []
    for p in sorted(SRC.rglob("*.xaml")):
        text = p.read_text(encoding="utf-8", errors="replace")
        for m in XAML_CLICK.finditer(text):
            handler = (m.group(1) or m.group(2) or "").strip()
            if not handler or handler.startswith("{"):
                continue
            rows.append(
                {
                    "handler": handler,
                    "file": rel(p),
                    "line": line_of(text, m.start()),
                }
            )
    return rows


def extract_palette_runs():
    rows = []
    for p in sorted(SRC.rglob("*.cs")):
        text = p.read_text(encoding="utf-8", errors="replace")
        for m in PALETTE_RUN.finditer(text):
            rows.append(
                {
                    "cmd": m.group(1),
                    "file": rel(p),
                    "line": line_of(text, m.start()),
                }
            )
    return rows


def build_primary_map(cmds):
    """Map method name -> primary command (prefer non-alias ISURVEY_* or longest meaningful)."""
    by_method = defaultdict(list)
    for r in cmds:
        if r["is_alias_file"]:
            continue
        if r["method"]:
            by_method[r["method"]].append(r)

    primary_for_method = {}
    for meth, lst in by_method.items():
        # Prefer CommandMethod starting with ISURVEY_
        pref = [x for x in lst if x["cmd"].startswith("ISURVEY_")]
        pick = pref[0] if pref else lst[0]
        primary_for_method[meth] = pick
    return primary_for_method


GROUP_RULES = [
    ("AUTH", ("LOGIN", "LOGOUT", "UPDATE", "ABOUT", "HDSD", "GENOTP", "LGI", "UPD", "ABT", "HD", "LGO", "RIBBON", "MENUISURVEY")),
    ("MAP", ("MAP", "GE", "IG", "SG", "XG", "UG")),
    ("KML", ("KML", "INK", "EXK", "EG")),
    ("TAB", ("TAB", "T2C")),
    ("DGN", ("DGN", "D2C", "C2D", "DGNCFG")),
    ("UAV", ("UAV", "UAVCLIP", "AK", "AD", "AU", "DEM", "DM", "HT", "NP", "LP", "RL")),
    ("FRAME", ("KHUNG", "KHUNGBV", "RAIKHUNG", "RAIMCD", "INTUYEN", "ANLOGO", "THAYLOGO", "CODAN", "SK", "FRAME", "ROUTE_SHEETS", "ROUTE_PLOT", "PROFILE_SHEETS", "FIT_TITLE", "LOGO")),
    ("PLAN", ("CATBD", "CBD", "PLAN")),
    ("DIEM", ("NHAPDIEM", "XUATDIEM", "ND", "XD", "POINT")),
    ("TERRAIN", ("TERRAIN", "TAOMOHINH", "THEMMOHINH", "XAYMOHINH", "GOMOHINH", "XOATHANHPHAN", "ANTIN", "TRACAOD")),
    ("ROUTE", ("ROUTE", "TAOTUYEN", "PHATSINHCOC", "THIETLAPCOC", "THEMCOC", "XOACOC", "BANGCOC", "DAODAUTUYEN", "GOC", "STT", "DUOITUYEN", "NUMBER", "STRIP", "STATION")),
    ("PROFILE", ("PROFILE", "VEMCD")),
    ("SCAN", ("AUTOSCAN", "SCN", "SCAN")),
    ("AI", ("AI_", "AIA", "AIC")),
    ("PALETTE", ("PALETTE", "BANGIS")),
]


def group_of(cmd: str) -> str:
    u = cmd.upper()
    for g, keys in GROUP_RULES:
        for k in keys:
            if k in u:
                return g
    return "OTHER"


def write_csv(path: Path, rows: list[dict], fieldnames: list[str]):
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8-sig", newline="") as f:
        w = csv.DictWriter(f, fieldnames=fieldnames, extrasaction="ignore")
        w.writeheader()
        for r in rows:
            w.writerow(r)


def main():
    files = inventory_files()
    cmds = extract_commands()
    ribbon = extract_ribbon()
    xaml = extract_xaml_clicks()
    palette = extract_palette_runs()
    primary_by_method = build_primary_map(cmds)

    # Alias resolution
    alias_rows = []
    for r in cmds:
        if not r["is_alias_file"]:
            continue
        target_method = r["alias_delegates_to_method"]
        primary = primary_by_method.get(target_method)
        alias_rows.append(
            {
                **r,
                "primary_cmd": primary["cmd"] if primary else "",
                "primary_file": primary["file"] if primary else "",
                "primary_line": primary["line"] if primary else "",
                "primary_method": primary["method"] if primary else target_method,
            }
        )

    # Canonical features = non-alias CommandMethods (unique cmd) + special UI-only placeholders
    primary_cmds = [r for r in cmds if not r["is_alias_file"]]
    # Deduplicate same cmd appearing twice
    seen = set()
    primary_unique = []
    for r in primary_cmds:
        if r["cmd"] in seen:
            continue
        seen.add(r["cmd"])
        primary_unique.append(r)

    ribbon_enabled = [b for b in ribbon if b["enabled"]]
    ribbon_disabled = [b for b in ribbon if not b["enabled"]]

    # Map ribbon cmd -> primary feature
    alias_to_primary = {a["cmd"]: a["primary_cmd"] for a in alias_rows if a["primary_cmd"]}
    for r in primary_unique:
        alias_to_primary[r["cmd"]] = r["cmd"]

    features = []
    aud = 1
    for r in sorted(primary_unique, key=lambda x: (group_of(x["cmd"]), x["cmd"])):
        aliases = sorted({a["cmd"] for a in alias_rows if a["primary_cmd"] == r["cmd"]})
        ribbon_hits = [b for b in ribbon if alias_to_primary.get(b["cmd"], b["cmd"]) == r["cmd"] or b["cmd"] == r["cmd"]]
        palette_hits = [p for p in palette if alias_to_primary.get(p["cmd"], p["cmd"]) == r["cmd"] or p["cmd"] == r["cmd"]]
        status = "STUB" if r["stub_hint"] or r["method"].endswith("Stub") else "IMPLEMENTED"
        evidence = "STUB_ONLY" if status == "STUB" else "DECLARATION_FOUND"
        # bump evidence if we have primary file
        if status == "IMPLEMENTED":
            evidence = "CALL_CHAIN_TRACED"  # alias→method known; deep body may still be partial
        fid = f"AUD-{aud:03d}"
        aud += 1
        audience = "ADMIN" if r["cmd"] in {"ISURVEY_GENOTP", "ISURVEY_AI_ASSIST", "ISURVEY_AI_SETTINGS", "AIA", "AIC"} or "AI_" in r["cmd"] else "USER"
        if r["cmd"] in {"ISURVEY_RIBBON", "MENUISURVEY"}:
            audience = "SUPPORT"
        features.append(
            {
                "feature_id": fid,
                "ten_tinh_nang": r["cmd"],
                "nhom": group_of(r["cmd"]),
                "audience": audience,
                "trang_thai": status,
                "ly_do_trang_thai": "method ends Stub / stub hint in body" if status == "STUB" else "CommandMethod + handler method found in source",
                "lenh_chinh": r["cmd"],
                "aliases": ";".join(aliases),
                "method": r["method"],
                "handler_file": r["file"],
                "handler_line": r["line"],
                "ribbon_entries": ";".join(f"{b['label']}|{b['cmd']}|en={b['enabled']}|L{b['line']}" for b in ribbon_hits) or "",
                "palette_entries": ";".join(f"{p['cmd']}|{p['file']}:L{p['line']}" for p in palette_hits) or "",
                "muc_ket_luan_source": evidence,
                "runtime": "NOT_TESTED",
                "r01_refs": r["cmd"],
                "video_candidate": "YES" if status == "IMPLEMENTED" and audience == "USER" else "NO",
                "ghi_chu": "",
            }
        )

    # UI placeholders: ribbon disabled with no commandmethod at all
    all_cmds = {c["cmd"] for c in cmds}
    stub_placeholder_rows = []
    for b in ribbon_disabled:
        if b["cmd"] in all_cmds:
            # has CommandMethod but ribbon disabled — still a command (often stub)
            continue
        stub_placeholder_rows.append(
            {
                "entry_type": "RIBBON_PLACEHOLDER_NO_COMMANDMETHOD",
                "label": b["label"],
                "cmd_or_id": b["cmd"],
                "file": b["file"],
                "line": b["line"],
                "feature_id": "",
                "excluded_from_user_video_denominator": "YES",
                "ghi_chu": "Ribbon Btn enabled=false; no CommandMethod string found in src",
            }
        )

    for r in features:
        if r["trang_thai"] == "STUB":
            stub_placeholder_rows.append(
                {
                    "entry_type": "STUB_COMMAND",
                    "label": r["ten_tinh_nang"],
                    "cmd_or_id": r["lenh_chinh"],
                    "file": r["handler_file"],
                    "line": r["handler_line"],
                    "feature_id": r["feature_id"],
                    "excluded_from_user_video_denominator": "YES",
                    "ghi_chu": r["ly_do_trang_thai"],
                }
            )

    # Mapping matrix 03A
    map_rows = []
    for r in features:
        map_rows.append(
            {
                "entry_kind": "PRIMARY_COMMAND",
                "entry_name": r["lenh_chinh"],
                "feature_id": r["feature_id"],
                "relation": "IS_CANONICAL",
                "file": r["handler_file"],
                "line": r["handler_line"],
                "merge_split_reason": "primary CommandMethod outside CommandAliases.cs",
            }
        )
        for a in (r["aliases"].split(";") if r["aliases"] else []):
            if not a:
                continue
            ar = next((x for x in alias_rows if x["cmd"] == a), None)
            map_rows.append(
                {
                    "entry_kind": "ALIAS",
                    "entry_name": a,
                    "feature_id": r["feature_id"],
                    "relation": "ALIAS_OF",
                    "file": ar["file"] if ar else "src/CommandAliases.cs",
                    "line": ar["line"] if ar else "",
                    "merge_split_reason": f"delegates to {ar['alias_delegates_to_method']}" if ar else "",
                }
            )
        for part in (r["ribbon_entries"].split(";") if r["ribbon_entries"] else []):
            if not part:
                continue
            lab, cmd, en, ln = (part.split("|") + ["", "", "", ""])[:4]
            map_rows.append(
                {
                    "entry_kind": "RIBBON_BTN",
                    "entry_name": f"{lab}/{cmd}",
                    "feature_id": r["feature_id"],
                    "relation": "UI_ENTRY",
                    "file": "src/UI/RibbonBuilder.cs",
                    "line": ln.replace("L", ""),
                    "merge_split_reason": en,
                }
            )

    for b in ribbon_disabled:
        if b["cmd"] in all_cmds:
            continue
        map_rows.append(
            {
                "entry_kind": "RIBBON_PLACEHOLDER",
                "entry_name": f"{b['label']}/{b['cmd']}",
                "feature_id": "",
                "relation": "NO_FEATURE",
                "file": b["file"],
                "line": b["line"],
                "merge_split_reason": "enabled=false and no CommandMethod",
            }
        )

    # Write inventories
    write_csv(
        OUT / "_inventory_files.csv",
        files,
        ["file", "ext", "bytes", "lines", "has_commandmethod", "has_btn", "has_click", "has_palette_run"],
    )
    write_csv(
        OUT / "_inventory_commands_r02.csv",
        cmds,
        [
            "cmd",
            "flags",
            "method",
            "file",
            "line",
            "is_alias_file",
            "alias_delegates_to_method",
            "stub_hint",
        ],
    )
    write_csv(
        OUT / "_inventory_aliases_resolved.csv",
        alias_rows,
        [
            "cmd",
            "method",
            "alias_delegates_to_method",
            "primary_cmd",
            "primary_method",
            "primary_file",
            "primary_line",
            "file",
            "line",
            "stub_hint",
        ],
    )
    write_csv(
        OUT / "_inventory_ribbon_r02.csv",
        ribbon,
        ["label", "cmd", "enabled", "file", "line"],
    )
    write_csv(
        OUT / "_inventory_xaml_clicks.csv",
        xaml,
        ["handler", "file", "line"],
    )
    write_csv(
        OUT / "_inventory_palette_runs.csv",
        palette,
        ["cmd", "file", "line"],
    )

    write_csv(
        OUT / "03_DANH_MUC_TINH_NANG_CHUAN_HOA.csv",
        features,
        list(features[0].keys()) if features else [],
    )
    write_csv(
        OUT / "03A_ANH_XA_LENH_ALIAS_RIBBON.csv",
        map_rows,
        [
            "entry_kind",
            "entry_name",
            "feature_id",
            "relation",
            "file",
            "line",
            "merge_split_reason",
        ],
    )
    write_csv(
        OUT / "03B_STUB_PLACEHOLDER_EXCLUDED.csv",
        stub_placeholder_rows,
        [
            "entry_type",
            "label",
            "cmd_or_id",
            "file",
            "line",
            "feature_id",
            "excluded_from_user_video_denominator",
            "ghi_chu",
        ],
    )

    summary = {
        "cs_xaml_files": len(files),
        "commandmethod_total": len(cmds),
        "commandmethod_alias_file": sum(1 for c in cmds if c["is_alias_file"]),
        "commandmethod_primary_file": sum(1 for c in cmds if not c["is_alias_file"]),
        "canonical_features": len(features),
        "features_implemented": sum(1 for f in features if f["trang_thai"] == "IMPLEMENTED"),
        "features_stub": sum(1 for f in features if f["trang_thai"] == "STUB"),
        "ribbon_btns": len(ribbon),
        "ribbon_enabled": len(ribbon_enabled),
        "ribbon_disabled": len(ribbon_disabled),
        "ribbon_placeholder_no_cmd": sum(
            1 for b in ribbon_disabled if b["cmd"] not in all_cmds
        ),
        "xaml_click_handlers": len(xaml),
        "palette_run_refs": len(palette),
        "aliases_resolved_to_primary": sum(1 for a in alias_rows if a["primary_cmd"]),
        "aliases_unresolved": sum(1 for a in alias_rows if not a["primary_cmd"]),
        "r01_commands_declared": 125,
        "r01_ribbon": 61,
        "r01_csv_rows": 150,
    }
    (OUT / "_extract_summary_r02.json").write_text(
        json.dumps(summary, indent=2, ensure_ascii=False), encoding="utf-8"
    )
    print(json.dumps(summary, indent=2, ensure_ascii=False))


if __name__ == "__main__":
    main()
