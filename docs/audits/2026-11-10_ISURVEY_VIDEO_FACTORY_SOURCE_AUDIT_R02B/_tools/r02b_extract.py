# -*- coding: utf-8 -*-
"""R02B readonly extractor — writes ONLY into parent _R02B folder."""
from __future__ import annotations

import csv
import json
import re
from collections import defaultdict
from pathlib import Path

ROOT = Path(r"D:\AIProject\isurvey")
OUT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src"
R02 = ROOT / "docs/audits/2026-11-10_ISURVEY_VIDEO_FACTORY_SOURCE_AUDIT_R02"
R01 = ROOT / "docs/audits/2026-11-10_ISURVEY_VIDEO_FACTORY_SOURCE_AUDIT"

CMD_ATTR = re.compile(r'\[CommandMethod\(\s*"([^"]+)"(?:\s*,\s*([^)\]]*))?\)\s*\]', re.M)
METH = re.compile(
    r"(?:public|internal|private)\s+(?:static\s+)?(?:async\s+)?(?:void|Task(?:<[^>]+>)?)\s+(\w+)\s*\("
)
ALIAS_CALL = re.compile(r"=>\s*_\w+\.(\w+)\s*\(")
BTN = re.compile(r'new\s+Btn\(\s*"([^"]*)"\s*,\s*"([^"]+)"\s*,\s*[^,]+,\s*(true|false)', re.S)
XAML_CLICK = re.compile(r'Click\s*=\s*"\{?([^}"]+)\}?"|Click\s*=\s*"(\w+)"')
PALETTE_RUN = re.compile(r'PaletteCommands\.Run\(\s*"([^"]+)"\s*\)')
STUB_HINT = re.compile(r"Stub|PLACEHOLDER|chưa làm|Chua lam|đang phát triển|NotImplemented", re.I)
THIN = re.compile(r"(?:=>\s*|{\s*)(?:this\.)?([A-Za-z_]\w*)\s*\(\s*\)\s*;?\s*}?\s*$", re.S)
CALL_TYPES = re.compile(
    r"\b([A-Z][A-Za-z0-9_]*(?:Service|Dialog|Window|Store|Helper|Client|Logger|Manager|Builder|Flow|Checker|Module))\b"
)

# Prefer short user-facing names as canonical when SAME_FEATURE
PREFERRED_PRIMARY = {
    "Draw": "VEMCD",
    "New": "TAOTUYEN",
    "Stations": "PHATSINHCOC",
    "ListStations": "BANGCOC",
    "Build": "XAYMOHINH",
}


def rel(p: Path) -> str:
    return p.relative_to(ROOT).as_posix()


def line_of(text: str, idx: int) -> int:
    return text.count("\n", 0, idx) + 1


def write_csv(name: str, rows: list[dict], fields: list[str] | None = None):
    if not rows and not fields:
        return
    fn = fields or list(rows[0].keys())
    with (OUT / name).open("w", encoding="utf-8-sig", newline="") as f:
        w = csv.DictWriter(f, fieldnames=fn, extrasaction="ignore")
        w.writeheader()
        for r in rows:
            w.writerow({k: r.get(k, "") for k in fn})


def extract_method_span(text: str, method: str) -> tuple[int, int, str] | None:
    pat = re.compile(
        rf"(?:public|internal|private)\s+(?:static\s+)?(?:async\s+)?(?:void|Task(?:<[^>]+>)?)\s+{re.escape(method)}\s*\("
    )
    m = pat.search(text)
    if not m:
        return None
    start_line = line_of(text, m.start())
    brace = text.find("{", m.end())
    arrow = text.find("=>", m.end())
    if arrow >= 0 and (brace < 0 or arrow < brace):
        end = text.find(";", arrow)
        if end < 0:
            end = arrow + 200
        body = text[arrow : end + 1]
        return start_line, line_of(text, end), body
    if brace < 0:
        return start_line, start_line, ""
    i, depth = brace, 0
    while i < len(text):
        if text[i] == "{":
            depth += 1
        elif text[i] == "}":
            depth -= 1
            if depth == 0:
                i += 1
                break
        i += 1
    body = text[brace:i]
    return start_line, line_of(text, i - 1), body


def resolve_effective(file_rel: str, method: str) -> tuple[str, str, int, int, str]:
    """Return (effective_method, body, start, end, note). Follow one thin hop.

    Only merge-safe when hop is zero-arg: `=> Draw()`. Calls like `Stub("CMD")`
    stay as separate features (shared helper ≠ same business feature).
    """
    path = ROOT / file_rel
    text = path.read_text(encoding="utf-8", errors="replace") if path.exists() else ""
    span = extract_method_span(text, method) if text else None
    if not span:
        return method, "", 0, 0, "CHƯA ĐỦ BẰNG CHỨNG: không mở method"
    sl, el, body = span
    compact = body.strip()
    # Zero-arg thin wrapper only
    hop0 = re.search(r"=>\s*(?:this\.)?([A-Za-z_]\w*)\s*\(\s*\)\s*;?\s*$", compact)
    if not hop0:
        hop0 = re.search(r"{\s*(?:this\.)?([A-Za-z_]\w*)\s*\(\s*\)\s*;\s*}\s*$", compact)
    if hop0:
        target = hop0.group(1)
        if target != method and target.lower() != "stub":
            span2 = extract_method_span(text, target)
            if span2:
                sl2, el2, body2 = span2
                return target, body2, sl2, el2, f"thin-wrapper {method}→{target}"
    # Stub("CMD") or other arg calls: keep own identity; mark stub
    if re.search(r"\bStub\s*\(", compact) or method.endswith("Stub"):
        return method, body, sl, el, "stub-entry (không gộp qua helper Stub)"
    return method, body, sl, el, ""


def group_by_module(file_rel: str, cmd: str) -> str:
    f = file_rel.replace("\\", "/").lower()
    rules = [
        ("AUTH", ("/auth/",)),
        ("AI", ("/modules/ai/",)),
        ("DGN", ("/dgn/",)),
        ("MAP", ("/modules/map/",)),
        ("KML", ("/modules/kml/", "/kml/")),
        ("TAB", ("/modules/tab/", "/tab/")),
        ("UAV", ("/modules/uav/",)),
        ("FRAME", ("/modules/frame/",)),
        ("PLAN", ("/modules/plan/",)),
        ("DIEM", ("/modules/diemdo/", "/diem/",)),
        ("TERRAIN", ("/modules/terrain/",)),
        ("ROUTE", ("/modules/tuyen/", "/route/")),
        ("PROFILE", ("/modules/profile/",)),
        ("SCAN", ("/modules/scan/", "autoscan", "scanmodule")),
        ("PALETTE", ("/ui/palette/", "palette")),
    ]
    for g, keys in rules:
        if any(k in f for k in keys):
            return g
    # fallback by cmd tokens (ordered carefully — TERRAIN before UAV etc.)
    u = cmd.upper()
    ordered = [
        ("AUTH", ("LOGIN", "LOGOUT", "ABOUT", "HDSD", "GENOTP", "RIBBON", "MENUISURVEY", "ISURVEY_UPDATE")),
        ("MAP", ("UPDATE_GE", "MAP", "_GE", "IG", "SG", "XG")),
        ("DGN", ("DGN", "D2C", "C2D")),
        ("KML", ("KML", "INK", "EXK")),
        ("TAB", ("TAB", "T2C")),
        ("FRAME", ("KHUNG", "FRAME", "RAIKHUNG", "RAIMCD", "INTUYEN", "LOGO", "CODAN", "THAYLOGO")),
        ("PLAN", ("CATBD", "CBD", "PLAN")),
        ("DIEM", ("NHAPDIEM", "XUATDIEM")),
        ("TERRAIN", ("TERRAIN", "MOHINH", "TIN", "TRACAOD", "XOATHANHPHAN")),
        ("ROUTE", ("ROUTE", "TUYEN", "COC", "STATION", "TAOTUYEN", "PHATSINH", "BANGCOC", "DAODAU", "GOC", "STT", "DUOI", "STRIP", "NUMBER")),
        ("PROFILE", ("PROFILE", "VEMCD", "MCD")),
        ("UAV", ("UAV", "UAVCLIP", "DEM")),
        ("AI", ("AI_",)),
        ("SCAN", ("SCAN", "AUTOSCAN")),
        ("PALETTE", ("PALETTE", "BANGIS")),
    ]
    for g, keys in ordered:
        if any(k in u for k in keys):
            return g
    return "OTHER"


def audience_of(cmd: str, group: str) -> str:
    if cmd in {"ISURVEY_GENOTP", "ISURVEY_AI_ASSIST", "ISURVEY_AI_SETTINGS"} or cmd.startswith("ISURVEY_AI"):
        return "ADMIN"
    if cmd in {"ISURVEY_RIBBON", "MENUISURVEY"}:
        return "SUPPORT"
    if "Stub" in cmd or group == "OTHER":
        return "UNKNOWN"
    return "USER"


def inventory_files():
    rows = []
    for p in sorted(SRC.rglob("*")):
        if not p.is_file() or p.suffix.lower() not in {".cs", ".xaml"}:
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
            after = text[m.end() : m.end() + 900]
            meth_m = METH.search(after)
            method = meth_m.group(1) if meth_m else ""
            alias_target = ""
            if is_alias:
                am = ALIAS_CALL.search(after)
                alias_target = am.group(1) if am else ""
            stub = bool(STUB_HINT.search(method + after[:400])) or method.endswith("Stub")
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
            rows.append({"handler": handler, "file": rel(p), "line": line_of(text, m.start())})
    return rows


def classify_xaml_handler(handler: str, xaml_file: str) -> dict:
    """Map XAML Click to code-behind classification."""
    cs = ROOT / xaml_file.replace(".xaml", ".xaml.cs")
    alt = ROOT / xaml_file.replace(".xaml", ".cs")
    path = cs if cs.exists() else (alt if alt.exists() else None)
    h = handler.lower()
    reason = ""
    kind = "UNRESOLVED"
    feature_hint = ""
    body_note = ""
    file_line = ""
    if path and path.exists():
        text = path.read_text(encoding="utf-8", errors="replace")
        span = extract_method_span(text, handler.split(".")[-1] if "." in handler else handler)
        # also try exact handler name
        if not span:
            simple = handler
            if "." in handler:
                simple = handler.split(".")[-1]
            span = extract_method_span(text, simple)
        if span:
            sl, el, body = span
            file_line = f"{rel(path)}:{sl}-{el}"
            body_l = body.lower()
            body_note = ", ".join(sorted(set(CALL_TYPES.findall(body)))[:8])
            if any(k in h or k in body_l for k in ("close", "cancel", "huy", "đóng", "dong")):
                kind = "CLOSE_CANCEL"
                reason = "handler đóng/hủy dialog"
            elif any(k in h or k in body_l for k in ("setting", "cauhinh", "option", "config")):
                kind = "SETTINGS"
                reason = "handler cấu hình trong dialog"
            elif "dialogresult" in body_l or "close()" in body_l:
                if "true" in body_l or "ok" in h:
                    kind = "DIALOG_OK"
                    reason = "xác nhận dialog"
                else:
                    kind = "CLOSE_CANCEL"
                    reason = "đóng dialog"
            elif any(k in body_l for k in ("sendstringtoexecute", "command(", "palettecommands")):
                kind = "COMMAND_DISPATCH"
                reason = "gọi lệnh CAD từ UI"
                m = re.search(r'"(NHAPDIEM|XUATDIEM|VEMCD|ISURVEY_[A-Z0-9_]+|[A-Z]{2,})"', body)
                if m:
                    feature_hint = m.group(1)
            else:
                kind = "INTERNAL"
                reason = "logic nội bộ dialog/window"
        else:
            kind = "UNRESOLVED"
            reason = "CHƯA ĐỦ BẰNG CHỨNG: không thấy method trong code-behind"
            file_line = rel(path) if path else ""
    else:
        kind = "UNRESOLVED"
        reason = "CHƯA ĐỦ BẰNG CHỨNG: không có file code-behind cạnh XAML"
    # name-based fallback
    if kind == "UNRESOLVED":
        if any(k in h for k in ("close", "cancel", "huy", "dong")):
            kind, reason = "CLOSE_CANCEL", "tên handler gợi ý đóng/hủy"
        elif any(k in h for k in ("ok", "apply", "chapnhan", "xacnhan", "accept")):
            kind, reason = "DIALOG_OK", "tên handler gợi ý xác nhận"
        elif any(k in h for k in ("browse", "chonfile", "pick", "browse")):
            kind, reason = "INTERNAL", "chọn file/đối tượng trong dialog"
    return {
        "classify": kind,
        "reason": reason,
        "codebehind": file_line,
        "types_in_body": body_note,
        "feature_hint": feature_hint,
    }


def extract_palette_runs():
    rows = []
    for p in sorted(SRC.rglob("*.cs")):
        text = p.read_text(encoding="utf-8", errors="replace")
        for m in PALETTE_RUN.finditer(text):
            rows.append({"cmd": m.group(1), "file": rel(p), "line": line_of(text, m.start())})
    return rows


def find_type_file(type_name: str) -> str | None:
    # Prefer *Service.cs exact
    candidates = list(SRC.rglob(f"{type_name}.cs"))
    if candidates:
        return rel(candidates[0])
    # partial
    for p in SRC.rglob("*.cs"):
        if p.stem == type_name or p.stem.startswith(type_name):
            return rel(p)
    return None


def scan_service_brief(type_name: str, max_chars: int = 8000) -> dict:
    f = find_type_file(type_name)
    if not f:
        return {"file": "", "note": f"CHƯA ĐỦ BẰNG CHỨNG: không tìm thấy file cho {type_name}", "io": [], "catch": 0, "lines": 0}
    text = (ROOT / f).read_text(encoding="utf-8", errors="replace")
    snippet = text[:max_chars]
    io = sorted(
        set(
            re.findall(
                r"\b(File\.\w+|Directory\.\w+|OpenFileDialog|SaveFileDialog|StreamReader|StreamWriter|HttpClient|DGNIMPORT|WriteMessage|StartTransaction)\b",
                snippet,
            )
        )
    )
    return {
        "file": f,
        "lines": text.count("\n") + 1,
        "io": io,
        "catch": len(re.findall(r"\bcatch\s*\(", snippet)),
        "note": f"đã mở {f} ({text.count(chr(10))+1} dòng), quét {min(len(text), max_chars)} ký tự đầu",
    }


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    files = inventory_files()
    cmds = extract_commands()
    ribbon = extract_ribbon()
    xaml = extract_xaml_clicks()
    palette = extract_palette_runs()

    # Resolve CommandAliases.cs
    non_alias = [c for c in cmds if not c["is_alias_file"]]
    by_method_file = defaultdict(list)
    for c in non_alias:
        by_method_file[(c["file"], c["method"])].append(c)

    # Effective target for each non-alias command
    for c in non_alias:
        eff, body, sl, el, note = resolve_effective(c["file"], c["method"])
        c["effective_method"] = eff
        c["eff_body"] = body
        c["eff_start"] = sl
        c["eff_end"] = el
        c["eff_note"] = note
        c["types"] = sorted(set(CALL_TYPES.findall(body))) if body else []

    # Group SAME_FEATURE by (file, effective_method)
    groups = defaultdict(list)
    for c in non_alias:
        groups[(c["file"], c["effective_method"])].append(c)

    merge_decisions = []
    # Explicit required pairs
    required_pairs = [
        ("ISURVEY_PROFILE_DRAW", "VEMCD"),
        ("BANGCOC", "ISURVEY_STATION_LIST"),
        ("ISURVEY_ROUTE_NEW", "TAOTUYEN"),
        ("ISURVEY_ROUTE_STATIONS", "PHATSINHCOC"),
        ("ISURVEY_TERRAIN_BUILD", "XAYMOHINH"),
    ]
    cmd_index = {c["cmd"]: c for c in non_alias}
    for a, b in required_pairs:
        ca, cb = cmd_index.get(a), cmd_index.get(b)
        if not ca or not cb:
            merge_decisions.append(
                {
                    "cmd_a": a,
                    "cmd_b": b,
                    "decision": "UNCERTAIN",
                    "reason": "CHƯA ĐỦ BẰNG CHỨNG: thiếu một trong hai CommandMethod",
                    "evidence": "",
                }
            )
            continue
        same = ca["file"] == cb["file"] and ca["effective_method"] == cb["effective_method"]
        merge_decisions.append(
            {
                "cmd_a": a,
                "cmd_b": b,
                "decision": "SAME_FEATURE" if same else "DIFFERENT_FEATURE",
                "reason": (
                    f"cùng file + effective method `{ca['effective_method']}`"
                    if same
                    else f"khác effective: {ca['effective_method']} vs {cb['effective_method']}"
                ),
                "evidence": f"{ca['file']}:{ca['line']}/{cb['line']}; eff {ca['eff_start']}-{ca['eff_end']}",
            }
        )

    # Auto decisions for other multi-cmd groups
    for (file, eff), lst in groups.items():
        if len(lst) < 2:
            continue
        cmds_sorted = sorted({x["cmd"] for x in lst})
        # skip if already covered as required pair subset
        for i in range(len(cmds_sorted)):
            for j in range(i + 1, len(cmds_sorted)):
                a, b = cmds_sorted[i], cmds_sorted[j]
                if any({a, b} == {d["cmd_a"], d["cmd_b"]} for d in merge_decisions):
                    continue
                merge_decisions.append(
                    {
                        "cmd_a": a,
                        "cmd_b": b,
                        "decision": "SAME_FEATURE",
                        "reason": f"cùng thin-target `{eff}` trong `{file}`",
                        "evidence": f"{file}; methods {[x['method'] for x in lst]}",
                    }
                )

    # Build canonical features from groups
    alias_file_cmds = [c for c in cmds if c["is_alias_file"]]
    # Map method name (on primary modules) for alias resolution
    primary_by_method_name = {}
    for (file, eff), lst in groups.items():
        # pick preferred primary cmd
        cmds_in = [x["cmd"] for x in lst]
        pref = PREFERRED_PRIMARY.get(eff)
        if pref and pref in cmds_in:
            primary_cmd = pref
        else:
            # prefer short non-ISURVEY if exists else ISURVEY
            shorts = [c for c in cmds_in if not c.startswith("ISURVEY_")]
            primary_cmd = sorted(shorts)[0] if shorts else sorted(cmds_in)[0]
        primary_by_method_name[eff] = next(x for x in lst if x["cmd"] == primary_cmd)

    # Also index for CommandAliases: resolve via alias_delegates_to_method → find feature
    features = []
    aud = 1
    feature_by_cmd = {}  # every cmd -> feature_id

    for (file, eff), lst in sorted(groups.items(), key=lambda kv: (group_by_module(kv[0][0], kv[1][0]["cmd"]), kv[1][0]["cmd"])):
        cmds_in = sorted({x["cmd"] for x in lst})
        pref = PREFERRED_PRIMARY.get(eff)
        if pref and pref in cmds_in:
            primary_cmd = pref
        else:
            shorts = [c for c in cmds_in if not c.startswith("ISURVEY_")]
            primary_cmd = sorted(shorts)[0] if shorts else sorted(cmds_in)[0]
        primary = next(x for x in lst if x["cmd"] == primary_cmd)
        module_aliases = [c for c in cmds_in if c != primary_cmd]
        stub = any(x["stub_hint"] or x["method"].endswith("Stub") for x in lst)
        group = group_by_module(file, primary_cmd)
        aud_id = f"AUD-{aud:03d}"
        aud += 1
        # evidence base
        types = primary.get("types") or []
        evidence = "STUB_ONLY" if stub else "METHOD_BODY_SCANNED"
        if primary.get("eff_body") and len(primary["eff_body"]) > 20:
            evidence = "HANDLER_TRACE_COMPLETE" if types or "EnsureAuthenticated" in primary["eff_body"] else "METHOD_BODY_SCANNED"
        # deep service later
        feat = {
            "feature_id": aud_id,
            "ten_hanh_vi": primary_cmd,
            "nhom": group,
            "audience": audience_of(primary_cmd, group),
            "source_status": "STUB" if stub else "SOURCE_PRESENT",
            "ly_do_status": "stub hint/method" if stub else "có CommandMethod + thân xử lý trong source (runtime NOT_TESTED)",
            "lenh_chinh": primary_cmd,
            "aliases_module": ";".join(module_aliases),
            "aliases_CommandAliases": "",
            "effective_method": eff,
            "handler_file": file,
            "handler_line": primary["line"],
            "eff_span": f"{primary['eff_start']}-{primary['eff_end']}",
            "types_in_handler": ";".join(types),
            "ribbon_entries": "",
            "palette_entries": "",
            "xaml_mapped": "",
            "muc_ket_luan_source": evidence,
            "runtime": "NOT_TESTED",
            "r01_refs": ";".join(cmds_in),
            "r02_refs": "",
            "video_candidate": "YES" if (not stub and audience_of(primary_cmd, group) == "USER") else "NO",
            "merge_note": primary.get("eff_note") or (f"gộp {cmds_in}" if len(cmds_in) > 1 else ""),
            "ghi_chu": "",
        }
        features.append(feat)
        for c in cmds_in:
            feature_by_cmd[c] = aud_id

    # Attach CommandAliases.cs aliases
    alias_rows_resolved = []
    for a in alias_file_cmds:
        # find feature by target method name among features' effective_method or method wrappers
        target = a["alias_delegates_to_method"]
        fid = ""
        primary_cmd = ""
        for f in features:
            # match if any non-alias command's method equals target OR effective equals
            # look up non_alias methods
            pass
        # map: find non_alias whose method == target
        owners = [c for c in non_alias if c["method"] == target]
        if owners:
            fid = feature_by_cmd.get(owners[0]["cmd"], "")
            primary_cmd = next((f["lenh_chinh"] for f in features if f["feature_id"] == fid), "")
        alias_rows_resolved.append(
            {
                "alias_cmd": a["cmd"],
                "alias_file": a["file"],
                "alias_line": a["line"],
                "delegates_method": target,
                "feature_id": fid,
                "primary_cmd": primary_cmd,
            }
        )
        if fid:
            for f in features:
                if f["feature_id"] == fid:
                    existing = [x for x in f["aliases_CommandAliases"].split(";") if x]
                    if a["cmd"] not in existing:
                        existing.append(a["cmd"])
                    f["aliases_CommandAliases"] = ";".join(sorted(existing))
                    feature_by_cmd[a["cmd"]] = fid

    # Ribbon / palette attach
    for b in ribbon:
        fid = feature_by_cmd.get(b["cmd"], "")
        if fid:
            for f in features:
                if f["feature_id"] == fid:
                    entry = f"{b['label']}|{b['cmd']}|en={b['enabled']}|L{b['line']}"
                    f["ribbon_entries"] = (f["ribbon_entries"] + ";" + entry).strip(";")
        elif not b["enabled"]:
            pass  # placeholder

    for p in palette:
        fid = feature_by_cmd.get(p["cmd"], "")
        if fid:
            for f in features:
                if f["feature_id"] == fid:
                    entry = f"{p['cmd']}|{p['file']}:L{p['line']}"
                    f["palette_entries"] = (f["palette_entries"] + ";" + entry).strip(";")

    # Map R02 feature ids
    r02_csv = R02 / "03_DANH_MUC_TINH_NANG_CHUAN_HOA.csv"
    r02_map = {}
    if r02_csv.exists():
        with r02_csv.open(encoding="utf-8-sig", newline="") as fh:
            for row in csv.DictReader(fh):
                r02_map[row["lenh_chinh"]] = row["feature_id"]
                for al in (row.get("aliases") or "").split(";"):
                    if al:
                        r02_map[al] = row["feature_id"]
    for f in features:
        refs = []
        for c in (f["r01_refs"] or "").split(";"):
            if c in r02_map:
                refs.append(r02_map[c])
        # also primary
        if f["lenh_chinh"] in r02_map:
            refs.append(r02_map[f["lenh_chinh"]])
        f["r02_refs"] = ";".join(sorted(set(refs)))

    # XAML classify
    xaml_map = []
    for x in xaml:
        cls = classify_xaml_handler(x["handler"], x["file"])
        fid = feature_by_cmd.get(cls.get("feature_hint") or "", "")
        xaml_map.append(
            {
                "entry_type": "XAML_EVENT",
                "handler": x["handler"],
                "xaml_file": x["file"],
                "xaml_line": x["line"],
                "classify": cls["classify"],
                "reason": cls["reason"],
                "codebehind": cls["codebehind"],
                "types_in_body": cls["types_in_body"],
                "feature_id": fid,
                "feature_hint": cls["feature_hint"],
            }
        )
        if fid:
            for f in features:
                if f["feature_id"] == fid:
                    f["xaml_mapped"] = (f["xaml_mapped"] + ";" + x["handler"]).strip(";")

    # Deep service for priority + all with types
    PRIORITY = {"VEMCD", "NHAPDIEM", "XUATDIEM", "ISURVEY_DGN_TO_CAD", "D2C", "ISURVEY_UAV_CLIP", "UAVCLIP", "TAOTUYEN", "PHATSINHCOC", "XAYMOHINH"}
    deep_notes = {}
    for f in features:
        types = [t for t in (f["types_in_handler"] or "").split(";") if t]
        # always try common services from types
        service_reports = []
        verified = 0
        for t in types[:6]:
            if t.endswith("Service") or t.endswith("Store") or t.endswith("Window") or t.endswith("Dialog"):
                br = scan_service_brief(t)
                service_reports.append(f"{t}@{br['file'] or '?'}: {br['note']}; io={br['io']}; catch~{br['catch']}")
                if br["file"]:
                    verified += 1
        deep_notes[f["feature_id"]] = service_reports
        if f["source_status"] == "STUB":
            f["muc_ket_luan_source"] = "STUB_ONLY"
        elif verified >= 1 and (f["lenh_chinh"] in PRIORITY or any(x in PRIORITY for x in (f["r01_refs"] or "").split(";"))):
            f["muc_ket_luan_source"] = "SERVICE_CHAIN_VERIFIED_STATIC"
        elif verified >= 1 and f["muc_ket_luan_source"] == "HANDLER_TRACE_COMPLETE":
            # upgraded if we opened service file
            f["muc_ket_luan_source"] = "SERVICE_CHAIN_VERIFIED_STATIC"
        elif not f["eff_span"] or f["eff_span"] == "0-0":
            f["muc_ket_luan_source"] = "NOT_ENOUGH_EVIDENCE"
        f["service_trace"] = " || ".join(service_reports)

    # Entry matrix 03A
    entries = []
    for c in cmds:
        fid = feature_by_cmd.get(c["cmd"], "")
        et = "ALIAS_FILE" if c["is_alias_file"] else ("MODULE_ALIAS_OR_PRIMARY")
        # refine
        if c["is_alias_file"]:
            et = "ALIAS_COMMANDALIASES"
        else:
            feat = next((f for f in features if f["feature_id"] == fid), None)
            if feat and c["cmd"] == feat["lenh_chinh"]:
                et = "PRIMARY_COMMAND"
            elif feat and c["cmd"] in (feat.get("aliases_module") or "").split(";"):
                et = "MODULE_WRAPPER_ALIAS"
            else:
                et = "PRIMARY_COMMAND"
        entries.append(
            {
                "entry_type": et,
                "name": c["cmd"],
                "feature_id": fid,
                "file": c["file"],
                "line": c["line"],
                "method": c["method"],
                "note": c.get("eff_note", "") if not c["is_alias_file"] else f"=> {c['alias_delegates_to_method']}",
            }
        )
    for b in ribbon:
        fid = feature_by_cmd.get(b["cmd"], "")
        et = "RIBBON_BTN" if b["enabled"] else ("RIBBON_PLACEHOLDER" if not fid else "RIBBON_DISABLED")
        if not b["enabled"] and b["cmd"] not in feature_by_cmd and b["cmd"] not in {c["cmd"] for c in cmds}:
            et = "RIBBON_PLACEHOLDER"
        entries.append(
            {
                "entry_type": et,
                "name": f"{b['label']}|{b['cmd']}",
                "feature_id": fid,
                "file": b["file"],
                "line": b["line"],
                "method": "",
                "note": f"enabled={b['enabled']}",
            }
        )
    for x in xaml_map:
        entries.append(
            {
                "entry_type": "XAML_EVENT",
                "name": x["handler"],
                "feature_id": x["feature_id"],
                "file": x["xaml_file"],
                "line": x["xaml_line"],
                "method": x["classify"],
                "note": x["reason"],
            }
        )
    for p in palette:
        entries.append(
            {
                "entry_type": "PALETTE_RUN",
                "name": p["cmd"],
                "feature_id": feature_by_cmd.get(p["cmd"], ""),
                "file": p["file"],
                "line": p["line"],
                "method": "",
                "note": "PaletteCommands.Run",
            }
        )

    # Stub/placeholder excluded
    excluded = []
    for f in features:
        if f["source_status"] == "STUB":
            excluded.append(
                {
                    "entry_type": "STUB_FEATURE",
                    "id_or_cmd": f["feature_id"],
                    "name": f["ten_hanh_vi"],
                    "reason": f["ly_do_status"],
                    "file": f["handler_file"],
                    "line": f["handler_line"],
                }
            )
    all_cmd_names = {c["cmd"] for c in cmds}
    for b in ribbon:
        if not b["enabled"] and b["cmd"] not in all_cmd_names:
            excluded.append(
                {
                    "entry_type": "RIBBON_PLACEHOLDER",
                    "id_or_cmd": b["cmd"],
                    "name": b["label"],
                    "reason": "Ribbon disabled, không có CommandMethod",
                    "file": b["file"],
                    "line": b["line"],
                }
            )

    # Renumber features sorted by group then name for stable AUD ids
    features_sorted = sorted(features, key=lambda f: (f["nhom"], f["lenh_chinh"]))
    # Keep IDs already assigned — reassign sequentially for cleanliness
    old_to_new = {}
    for i, f in enumerate(features_sorted, 1):
        new_id = f"AUD-{i:03d}"
        old_to_new[f["feature_id"]] = new_id
        f["feature_id"] = new_id
    # remap
    for e in entries:
        if e["feature_id"] in old_to_new:
            e["feature_id"] = old_to_new[e["feature_id"]]
    for x in xaml_map:
        if x["feature_id"] in old_to_new:
            x["feature_id"] = old_to_new[x["feature_id"]]
    for a in alias_rows_resolved:
        if a["feature_id"] in old_to_new:
            a["feature_id"] = old_to_new[a["feature_id"]]
    for ex in excluded:
        if ex["id_or_cmd"] in old_to_new:
            ex["id_or_cmd"] = old_to_new[ex["id_or_cmd"]]
    deep_notes = {old_to_new.get(k, k): v for k, v in deep_notes.items()}
    features = features_sorted

    # Write outputs
    write_csv("_inventory_files.csv", files)
    write_csv("_inventory_commands_r02b.csv", cmds)
    write_csv("_inventory_ribbon_r02b.csv", ribbon)
    write_csv("_inventory_xaml_clicks.csv", xaml)
    write_csv("_inventory_palette_runs.csv", palette)
    write_csv("03C_QUYET_DINH_GOP_TACH.csv", merge_decisions)
    write_csv(
        "03_DANH_MUC_TINH_NANG_CHUAN_HOA.csv",
        features,
        [
            "feature_id",
            "ten_hanh_vi",
            "nhom",
            "audience",
            "source_status",
            "ly_do_status",
            "lenh_chinh",
            "aliases_module",
            "aliases_CommandAliases",
            "effective_method",
            "handler_file",
            "handler_line",
            "eff_span",
            "types_in_handler",
            "ribbon_entries",
            "palette_entries",
            "xaml_mapped",
            "muc_ket_luan_source",
            "runtime",
            "r01_refs",
            "r02_refs",
            "video_candidate",
            "merge_note",
            "service_trace",
            "ghi_chu",
        ],
    )
    write_csv("03A_ANH_XA_DIEM_KICH_HOAT.csv", entries)
    write_csv("03B_STUB_PLACEHOLDER_EXCLUDED.csv", excluded)
    write_csv("03D_XAML_EVENT_MAP.csv", xaml_map)
    write_csv("03E_ALIAS_COMMANDALIASES.csv", alias_rows_resolved)

    summary = {
        "cs_xaml_files": len(files),
        "commandmethod_total": len(cmds),
        "alias_file": sum(1 for c in cmds if c["is_alias_file"]),
        "non_alias_declarations": len(non_alias),
        "canonical_features_r02b": len(features),
        "source_present": sum(1 for f in features if f["source_status"] == "SOURCE_PRESENT"),
        "stub_features": sum(1 for f in features if f["source_status"] == "STUB"),
        "same_feature_decisions": sum(1 for d in merge_decisions if d["decision"] == "SAME_FEATURE"),
        "ribbon": len(ribbon),
        "xaml_clicks": len(xaml),
        "palette": len(palette),
        "entries_total": len(entries),
        "evidence": {},
        "groups": {},
    }
    for f in features:
        summary["evidence"][f["muc_ket_luan_source"]] = summary["evidence"].get(f["muc_ket_luan_source"], 0) + 1
        summary["groups"][f["nhom"]] = summary["groups"].get(f["nhom"], 0) + 1

    # Compare R02 count
    summary["r02_canonical_was"] = 79
    summary["merged_away"] = 79 - len(features) if len(features) <= 79 else 0
    (OUT / "_extract_summary_r02b.json").write_text(json.dumps(summary, ensure_ascii=False, indent=2), encoding="utf-8")
    (OUT / "_deep_notes.json").write_text(json.dumps(deep_notes, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps(summary, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
