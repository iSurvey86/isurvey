# -*- coding: utf-8 -*-
"""R02C readonly extractor — stable business_feature_id, XAML map, stub fix."""
from __future__ import annotations

import csv
import json
import re
from collections import defaultdict
from pathlib import Path

ROOT = Path(r"D:\AIProject\isurvey")
OUT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src"
R02B = ROOT / "docs/audits/2026-11-10_ISURVEY_VIDEO_FACTORY_SOURCE_AUDIT_R02B"
R02 = ROOT / "docs/audits/2026-11-10_ISURVEY_VIDEO_FACTORY_SOURCE_AUDIT_R02"
R01 = ROOT / "docs/audits/2026-11-10_ISURVEY_VIDEO_FACTORY_SOURCE_AUDIT"

CMD_ATTR = re.compile(r'\[CommandMethod\(\s*"([^"]+)"(?:\s*,\s*([^)\]]*))?\)\s*\]', re.M)
METH = re.compile(
    r"(?:public|internal|private)\s+(?:static\s+)?(?:async\s+)?(?:void|Task(?:<[^>]+>)?)\s+(\w+)\s*\("
)
ALIAS_CALL = re.compile(r"=>\s*_\w+\.(\w+)\s*\(")
BTN = re.compile(r'new\s+Btn\(\s*"([^"]*)"\s*,\s*"([^"]+)"\s*,\s*[^,]+,\s*(true|false)', re.S)
XAML_CLICK = re.compile(r'Click\s*=\s*"(\w+)"')
PALETTE_RUN = re.compile(r'PaletteCommands\.Run\(\s*"([^"]+)"\s*\)')
CALL_TYPES = re.compile(
    r"\b([A-Z][A-Za-z0-9_]*(?:Service|Dialog|Window|Store|Helper|Client|Logger|Manager|Builder|Flow|Checker|Module|Reader|Writer))\b"
)

PREFERRED_PRIMARY = {
    "Draw": "VEMCD",
    "New": "TAOTUYEN",
    "Stations": "PHATSINHCOC",
    "ListStations": "BANGCOC",
    "Build": "XAYMOHINH",
}

# Window / dialog type → likely command(s) that open it (from known code + search)
# Exact stem first (checked before fuzzy hints)
WINDOW_EXACT = {
    "ImportPointsWindow": ["NHAPDIEM"],
    "ExportPointsWindow": ["XUATDIEM"],
    "DgnImportWindow": ["ISURVEY_DGN_TO_CAD"],
    "DgnExportWindow": ["ISURVEY_CAD_TO_DGN"],
    "DrawProfileWindow": ["VEMCD"],
    "CreateRouteWindow": ["TAOTUYEN"],
    "CreateStationsWindow": ["PHATSINHCOC"],
    "StationStyleWindow": ["BANGCOC"],
    "CreateTerrainWindow": ["TAOMOHINH"],
    "UpdateWindow": ["ISURVEY_UPDATE"],
    "LoginWindow": ["ISURVEY_LOGIN"],
    "AboutWindow": ["ISURVEY_ABOUT"],
    "AiChatWindow": ["ISURVEY_AI_ASSIST"],
    "SequenceWindow": ["STT"],
    "Vn2000SetupWindow": [],  # helper CRS — NOT_APPLICABLE unless opener found
    "TaskProgressWindow": [],
    "DarkDialog": [],
}
WINDOW_HINTS = {
    "ImportPoints": ["NHAPDIEM"],
    "ExportPoints": ["XUATDIEM"],
    "DgnImport": ["ISURVEY_DGN_TO_CAD"],
    "DgnExport": ["ISURVEY_CAD_TO_DGN"],
    "DrawProfile": ["VEMCD"],
    "CreateRoute": ["TAOTUYEN"],
    "CreateStations": ["PHATSINHCOC"],
    "StationStyle": ["BANGCOC"],
    "CreateTerrain": ["TAOMOHINH"],
    "UpdateWindow": ["ISURVEY_UPDATE"],
    "AiChat": ["ISURVEY_AI_ASSIST"],
    "AiSettings": ["ISURVEY_AI_SETTINGS"],
    "Otp": ["ISURVEY_GENOTP"],
    "Sequence": ["STT"],
    "FrameEdit": ["ISURVEY_FRAME_EDIT"],
    "Khung": ["ISURVEY_KHUNG"],
    "Kml": ["ISURVEY_IMPORT_KML", "ISURVEY_EXPORT_KML"],
    "Palette": ["ISURVEY_PALETTE"],
}


def rel(p: Path) -> str:
    return p.relative_to(ROOT).as_posix()


def line_of(text: str, idx: int) -> int:
    return text.count("\n", 0, idx) + 1


def write_csv(name: str, rows: list[dict], fields: list[str] | None = None):
    if not rows:
        # still write header if fields given
        if not fields:
            return
        with (OUT / name).open("w", encoding="utf-8-sig", newline="") as f:
            csv.DictWriter(f, fieldnames=fields).writeheader()
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
        # expression body until semicolon at depth 0-ish
        end = text.find(";", arrow)
        if end < 0:
            end = min(len(text), arrow + 300)
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
    return start_line, line_of(text, i - 1), text[brace:i]


def is_stub_body(method: str, body: str) -> tuple[bool, str]:
    """Strict stub: method name *Stub OR body only delegates to Stub(...)."""
    compact = re.sub(r"\s+", " ", (body or "").strip())
    if method.endswith("Stub"):
        return True, "method name ends with Stub"
    # => Stub("CMD") or { Stub("CMD"); }
    if re.search(r"(?:=>\s*|{\s*)Stub\s*\(\s*\"[^\"]+\"\s*\)\s*;?\s*}?\s*$", compact):
        return True, "body chỉ gọi Stub(\"CMD\")"
    if re.fullmatch(r"=>\s*Stub\s*\([^)]*\)\s*;?", compact):
        return True, "expression-body Stub(...)"
    # Do NOT treat mere presence of word Stub elsewhere / neighboring methods
    return False, "không phải stub theo thân method"


def resolve_effective(file_rel: str, method: str) -> dict:
    path = ROOT / file_rel
    text = path.read_text(encoding="utf-8", errors="replace") if path.exists() else ""
    span = extract_method_span(text, method) if text else None
    if not span:
        return {
            "effective_method": method,
            "body": "",
            "start": 0,
            "end": 0,
            "note": "CHƯA ĐỦ BẰNG CHỨNG: không mở method",
            "stub": False,
            "stub_reason": "",
            "types": [],
        }
    sl, el, body = span
    stub, stub_reason = is_stub_body(method, body)
    compact = body.strip()
    hop0 = re.search(r"=>\s*(?:this\.)?([A-Za-z_]\w*)\s*\(\s*\)\s*;?\s*$", compact)
    if not hop0:
        hop0 = re.search(r"{\s*(?:this\.)?([A-Za-z_]\w*)\s*\(\s*\)\s*;\s*}\s*$", compact)
    eff, note = method, ""
    if hop0 and not stub:
        target = hop0.group(1)
        if target != method and target.lower() != "stub":
            span2 = extract_method_span(text, target)
            if span2:
                sl, el, body = span2
                eff = target
                note = f"thin-wrapper {method}→{target}"
                stub, stub_reason = is_stub_body(target, body)
    types = sorted(set(CALL_TYPES.findall(body))) if body else []
    return {
        "effective_method": eff,
        "body": body,
        "start": sl,
        "end": el,
        "note": note,
        "stub": stub,
        "stub_reason": stub_reason,
        "types": types,
    }


def group_by_module(file_rel: str) -> str:
    f = file_rel.replace("\\", "/").lower()
    rules = [
        ("AUTH", "/auth/"),
        ("AI", "/modules/ai/"),
        ("DGN", "/dgn/"),
        ("MAP", "/modules/map/"),
        ("KML", "/modules/kml/"),
        ("TAB", "/modules/tab/"),
        ("UAV", "/modules/uav/"),
        ("FRAME", "/modules/frame/"),
        ("PLAN", "/modules/plan/"),
        ("DIEM", "/modules/diemdo/"),
        ("TERRAIN", "/modules/terrain/"),
        ("ROUTE", "/modules/tuyen/"),
        ("PROFILE", "/modules/profile/"),
        ("SCAN", "/modules/scan/"),
        ("PALETTE", "/ui/palette/"),
    ]
    for g, k in rules:
        if k in f:
            return g
    return "OTHER"


def bf_id(primary_cmd: str) -> str:
    """Stable business feature id from primary command name."""
    slug = re.sub(r"[^A-Z0-9]+", "_", primary_cmd.upper()).strip("_")
    return f"BF-{slug}"


def extract_commands():
    rows = []
    for p in sorted(SRC.rglob("*.cs")):
        text = p.read_text(encoding="utf-8", errors="replace")
        is_alias = p.name == "CommandAliases.cs"
        for m in CMD_ATTR.finditer(text):
            cmd = m.group(1)
            after = text[m.end() : m.end() + 900]
            meth_m = METH.search(after)
            method = meth_m.group(1) if meth_m else ""
            alias_target = ALIAS_CALL.search(after).group(1) if is_alias and ALIAS_CALL.search(after) else ""
            rows.append(
                {
                    "cmd": cmd,
                    "flags": (m.group(2) or "").strip(),
                    "method": method,
                    "file": rel(p),
                    "line": line_of(text, m.start()),
                    "is_alias_file": is_alias,
                    "alias_delegates_to_method": alias_target,
                }
            )
    return rows


def extract_ribbon():
    p = SRC / "UI" / "RibbonBuilder.cs"
    text = p.read_text(encoding="utf-8", errors="replace")
    return [
        {
            "label": m.group(1).replace("\\n", " ").strip(),
            "cmd": m.group(2),
            "enabled": m.group(3) == "true",
            "file": "src/UI/RibbonBuilder.cs",
            "line": line_of(text, m.start()),
        }
        for m in BTN.finditer(text)
    ]


def extract_xaml_clicks():
    rows = []
    for p in sorted(SRC.rglob("*.xaml")):
        text = p.read_text(encoding="utf-8", errors="replace")
        for m in XAML_CLICK.finditer(text):
            rows.append({"handler": m.group(1), "file": rel(p), "line": line_of(text, m.start())})
    return rows


def extract_palette():
    rows = []
    for p in sorted(SRC.rglob("*.cs")):
        text = p.read_text(encoding="utf-8", errors="replace")
        for m in PALETTE_RUN.finditer(text):
            rows.append({"cmd": m.group(1), "file": rel(p), "line": line_of(text, m.start())})
    return rows


def find_window_openers() -> dict[str, list[str]]:
    """Map type name → commands that reference it in module files."""
    openers: dict[str, list[str]] = defaultdict(list)
    # Build cmd list with bodies later — scan all cs for TypeName.Prompt/Show/new TypeName
    cmd_files = {}
    for p in SRC.rglob("*.cs"):
        text = p.read_text(encoding="utf-8", errors="replace")
        for m in CMD_ATTR.finditer(text):
            cmd = m.group(1)
            after = text[m.end() : m.end() + 2500]
            cmd_files[cmd] = (rel(p), after)
    type_names = set()
    for p in SRC.rglob("*.xaml"):
        # ImportPointsWindow.xaml → ImportPointsWindow
        type_names.add(p.stem)
    for t in type_names:
        for cmd, (f, after) in cmd_files.items():
            if t in after:
                openers[t].append(cmd)
    return openers


def classify_xaml_handler(handler: str, body: str) -> tuple[str, str, float]:
    """Return (classify, reason, confidence 0-1). Name-first, not close/cancel anywhere."""
    h = handler.lower()
    # Affirmative / business actions by NAME
    if any(k in h for k in ("confirm", "ok", "apply", "accept", "chapnhan", "xacnhan")):
        return "DIALOG_CONFIRM", "tên handler xác nhận/OK", 0.9
    if any(k in h for k in ("save", "export", "btnexport", "ghi", "xuat")):
        return "DIALOG_SAVE_EXPORT", "tên handler lưu/xuất", 0.9
    if any(k in h for k in ("create", "new", "add", "them", "tao")):
        return "DIALOG_CREATE", "tên handler tạo/thêm", 0.85
    if any(k in h for k in ("update", "refresh", "reload", "capnhat")):
        return "DIALOG_UPDATE", "tên handler cập nhật", 0.85
    if any(k in h for k in ("start", "run", "execute", "import", "open", "browse", "chon", "pick")):
        return "DIALOG_ACTION", "tên handler thao tác nghiệp vụ", 0.8
    if any(k in h for k in ("cancel", "close", "huy", "dong", "dismiss", "thoat")):
        return "CLOSE_CANCEL", "tên handler đóng/hủy", 0.9
    if any(k in h for k in ("setting", "config", "option", "cauhinh")):
        return "SETTINGS", "tên handler cấu hình", 0.8

    body_l = (body or "").lower()
    # Body effect — only if name inconclusive
    if "dialogresult" in body_l and ("true" in body_l or "ok" in body_l):
        return "DIALOG_CONFIRM", "DialogResult OK/true trong thân", 0.7
    if re.search(r"\bclose\s*\(\s*\)", body_l) and not any(
        k in body_l for k in ("write", "save", "export", "import", "start", "draw", "create")
    ):
        return "CLOSE_CANCEL", "Close() và không thấy side-effect ghi/chạy", 0.65
    if any(k in body_l for k in ("pointfilewriter", "file.write", "savefiledialog", "export")):
        return "DIALOG_SAVE_EXPORT", "thân có ghi file/export", 0.75
    if any(k in body_l for k in ("pointfilereader", "openfiledialog", "import", "dgnimport")):
        return "DIALOG_ACTION", "thân có đọc/import", 0.75
    return "INTERNAL", "công cụ UI nội bộ dialog (không suy thành feature độc lập)", 0.6


def map_parent_feature(xaml_file: str, handler: str, cmd_to_bf: dict, openers: dict) -> tuple[str, str, str]:
    """Return (parent_bf_id or '', mapping_status, reason)."""
    stem = Path(xaml_file).stem
    cmds: list[str] = []
    # 1) exact stem table wins
    if stem in WINDOW_EXACT:
        cmds = list(WINDOW_EXACT[stem])
    else:
        cmds = list(openers.get(stem, []))
        for hint, hc in WINDOW_HINTS.items():
            if hint.lower() in stem.lower():
                cmds = list(dict.fromkeys(cmds + hc))
                break
    # Prefer exact window opener from code search only if exact table empty
    if not cmds:
        cmds = list(openers.get(stem, []))
    for c in cmds:
        if c in cmd_to_bf:
            return cmd_to_bf[c], "MAPPED_VIA_WINDOW_OPENER", f"window `{stem}` liên kết lệnh `{c}`"
    if any(k in stem.lower() for k in ("darkdialog", "taskprogress", "busy", "message", "vn2000")):
        return "", "NOT_APPLICABLE_SHARED_OR_HELPER_UI", (
            f"`{stem}` là UI dùng chung/helper (CRS/progress/chrome), không phải feature độc lập; "
            f"handler `{handler}` không tạo business_feature mới"
        )
    if cmds:
        return "", "UNRESOLVED_OPENER_WITHOUT_BF", f"thấy opener cmds={cmds} nhưng chưa map BF — CHƯA ĐỦ BẰNG CHỨNG"
    return "", "NOT_APPLICABLE_DIALOG_TOOL", (
        f"handler `{handler}` thuộc dialog `{stem}`; không tạo feature mới; "
        f"opener CHƯA ĐỦ BẰNG CHỨNG hoặc công cụ nội bộ dialog"
    )


def find_type_file(name: str) -> str | None:
    hits = list(SRC.rglob(f"{name}.cs"))
    return rel(hits[0]) if hits else None


def deep_trace_feature(feat: dict) -> dict:
    """Follow handler types; require side-effect evidence for SERVICE_CHAIN."""
    path = ROOT / feat["handler_file"]
    text = path.read_text(encoding="utf-8", errors="replace") if path.exists() else ""
    body = feat.get("_body") or ""
    types = feat.get("types_in_handler", "").split(";") if feat.get("types_in_handler") else []
    types = [t for t in types if t]

    chain = [
        f"{feat['lenh_chinh']} CommandMethod @ {feat['handler_file']}:{feat['handler_line']}",
        f"effective `{feat['effective_method']}` span {feat['eff_span']}",
    ]
    inputs, outputs, errors, destructive = [], [], [], []
    unresolved = []
    side_effect_hits = 0

    # Auth
    if "EnsureAuthenticated" in body or "AuthManager" in body:
        chain.append("Auth gate: AuthManager.EnsureAuthenticated (trong thân handler)")
    if "TileMode" in body or "Model Space" in body or "IsModelSpace" in body:
        inputs.append("Yêu cầu Model Space (handler)")
    if "WriteMessage" in body:
        outputs.append("Editor.WriteMessage phản hồi người dùng")
    if "catch" in body:
        errors.append(f"có catch trong handler span {feat['eff_span']}")

    for t in types[:8]:
        tf = find_type_file(t)
        if not tf:
            unresolved.append(f"không tìm thấy file cho type `{t}`")
            continue
        ttext = (ROOT / tf).read_text(encoding="utf-8", errors="replace")
        # Find key public methods referenced from body
        called = re.findall(rf"\b{re.escape(t)}\.(\w+)\s*\(", body)
        if not called:
            # instance style new T / T.Prompt
            called = re.findall(rf"\b{t}\b.*?\.(\w+)\s*\(", body[:2000])
        method_hits = []
        for cm in sorted(set(called))[:5]:
            span = extract_method_span(ttext, cm)
            if span:
                sl, el, b = span
                method_hits.append(f"{t}.{cm} @ {tf}:{sl}-{el}")
                # side effects
                if re.search(r"\b(File\.|StreamWriter|SaveFileDialog|Write\(|Commit\(|AppendEntity|StartTransaction|DGNIMPORT|SendStringToExecute|Delete|Erase|Purge|Clip|Draw)\b", b):
                    side_effect_hits += 1
                if re.search(r"\b(OpenFileDialog|Read\(|StreamReader|GetPoint|GetEntity|GetSelection|Load\()\b", b):
                    inputs.append(f"{t}.{cm}: có đọc/prompt (span {sl}-{el})")
                if re.search(r"\b(Write\(|SaveFileDialog|AppendEntity|Commit|WriteMessage|Result)\b", b):
                    outputs.append(f"{t}.{cm}: có ghi/xuất/CAD commit (span {sl}-{el})")
                if "catch" in b:
                    errors.append(f"{t}.{cm}: có catch @ {tf}:{sl}-{el}")
                if re.search(r"\b(Delete|Erase|Purge|Explode|Overwrite)\b", b, re.I):
                    destructive.append(f"{t}.{cm}: dấu hiệu destructive @ {tf}")
            else:
                unresolved.append(f"CHƯA ĐỦ BẰNG CHỨNG: không mở được {t}.{cm} trong {tf}")
        if method_hits:
            chain.extend(method_hits)
        else:
            # at least note type file exists
            chain.append(f"type `{t}` file `{tf}` ({ttext.count(chr(10))+1} dòng) — chưa khớp method call cụ thể")
            unresolved.append(f"CHƯA ĐỦ BẰNG CHỨNG: chưa khớp method call cụ thể cho `{t}` từ handler")

    # Evidence rule
    if feat["source_status"] == "STUB":
        evidence = "STUB_ONLY"
    elif side_effect_hits >= 1 and (inputs or outputs):
        evidence = "SERVICE_CHAIN_VERIFIED_STATIC"
    elif types and chain:
        evidence = "HANDLER_TRACE_COMPLETE"
    elif body:
        evidence = "METHOD_BODY_SCANNED"
    else:
        evidence = "NOT_ENOUGH_EVIDENCE"

    if unresolved and evidence == "SERVICE_CHAIN_VERIFIED_STATIC" and side_effect_hits < 1:
        evidence = "NOT_ENOUGH_EVIDENCE"

    return {
        "chain": " | ".join(chain),
        "inputs": "; ".join(inputs) or "CHƯA ĐỦ BẰNG CHỨNG đủ điều kiện tiên quyết",
        "outputs": "; ".join(outputs) or "CHƯA ĐỦ BẰNG CHỨNG hậu điều kiện đo được",
        "errors": "; ".join(errors) or "CHƯA ĐỦ BẰNG CHỨNG nhánh lỗi đầy đủ",
        "destructive": "; ".join(destructive) or "không thấy Delete/Erase/Purge trong đoạn đã đọc — CHƯA ĐỦ BẰNG CHỨNG toàn cục",
        "unresolved": "; ".join(unresolved),
        "evidence": evidence,
        "side_effect_hits": side_effect_hits,
    }


def load_csv(path: Path) -> list[dict]:
    if not path.exists():
        return []
    with path.open(encoding="utf-8-sig", newline="") as f:
        return list(csv.DictReader(f))


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    cmds = extract_commands()
    ribbon = extract_ribbon()
    xaml = extract_xaml_clicks()
    palette = extract_palette()
    openers = find_window_openers()

    non_alias = [c for c in cmds if not c["is_alias_file"]]
    for c in non_alias:
        info = resolve_effective(c["file"], c["method"])
        c.update(
            {
                "effective_method": info["effective_method"],
                "eff_start": info["start"],
                "eff_end": info["end"],
                "eff_note": info["note"],
                "stub": info["stub"],
                "stub_reason": info["stub_reason"],
                "types": info["types"],
                "_body": info["body"],
            }
        )

    # Stub recheck for ALL 125 (alias file: not stub by themselves)
    stub_rows = []
    for c in cmds:
        if c["is_alias_file"]:
            stub_rows.append(
                {
                    "cmd": c["cmd"],
                    "file": c["file"],
                    "line": c["line"],
                    "method": c["method"],
                    "is_stub": False,
                    "reason": "alias trong CommandAliases.cs — không phân stub tại đây",
                    "body_excerpt": f"=> {c['alias_delegates_to_method']}",
                    "r02b_was_stub": "",
                }
            )
            continue
        stub_rows.append(
            {
                "cmd": c["cmd"],
                "file": c["file"],
                "line": c["line"],
                "method": c["method"],
                "is_stub": c["stub"],
                "reason": c["stub_reason"] or "SOURCE_PRESENT",
                "body_excerpt": (c.get("_body") or "")[:180].replace("\n", " "),
                "r02b_was_stub": "",
            }
        )

    # Compare R02B stubs
    r02b_feats = load_csv(R02B / "03_DANH_MUC_TINH_NANG_CHUAN_HOA.csv")
    r02b_stub_cmds = set()
    for f in r02b_feats:
        if f.get("source_status") == "STUB":
            r02b_stub_cmds.add(f["lenh_chinh"])
            for a in (f.get("aliases_module") or "").split(";"):
                if a:
                    r02b_stub_cmds.add(a)
            for a in (f.get("r01_refs") or "").split(";"):
                if a:
                    r02b_stub_cmds.add(a)
    for s in stub_rows:
        s["r02b_was_stub"] = "YES" if s["cmd"] in r02b_stub_cmds else "NO"

    # Force-check SEND_BACK
    sb = next((c for c in non_alias if c["cmd"] == "ISURVEY_UAV_SEND_BACK"), None)
    if sb:
        assert not sb["stub"], "SEND_BACK must not be stub"
        # update stub row
        for s in stub_rows:
            if s["cmd"] == "ISURVEY_UAV_SEND_BACK":
                s["is_stub"] = False
                s["reason"] = "thân gọi UavImageService.SendImageToBack — không Stub()"
                s["body_excerpt"] = (sb.get("_body") or "")[:180].replace("\n", " ")

    # Group SAME_FEATURE
    groups = defaultdict(list)
    for c in non_alias:
        if c["stub"]:
            groups[(c["file"], c["method"], c["cmd"])].append(c)  # unique per stub cmd
        else:
            groups[(c["file"], c["effective_method"], "")].append(c)

    features = []
    for (file, eff, stubcmd), lst in sorted(groups.items(), key=lambda kv: kv[1][0]["cmd"]):
        cmds_in = sorted({x["cmd"] for x in lst})
        if stubcmd:
            primary_cmd = stubcmd
        else:
            pref = PREFERRED_PRIMARY.get(eff)
            if pref and pref in cmds_in:
                primary_cmd = pref
            else:
                shorts = [c for c in cmds_in if not c.startswith("ISURVEY_")]
                primary_cmd = sorted(shorts)[0] if shorts else sorted(cmds_in)[0]
        primary = next(x for x in lst if x["cmd"] == primary_cmd)
        module_aliases = [c for c in cmds_in if c != primary_cmd]
        stub = primary["stub"]
        bid = bf_id(primary_cmd)
        group = group_by_module(file)
        audience = "ADMIN" if primary_cmd in {"ISURVEY_GENOTP", "ISURVEY_AI_ASSIST", "ISURVEY_AI_SETTINGS"} else (
            "SUPPORT" if primary_cmd in {"ISURVEY_RIBBON", "MENUISURVEY"} else ("INTERNAL" if stub else "USER")
        )
        features.append(
            {
                "business_feature_id": bid,
                "ten_hanh_vi": primary_cmd,
                "nhom": group,
                "audience": audience,
                "source_status": "STUB" if stub else "SOURCE_PRESENT",
                "ly_do_status": primary["stub_reason"] if stub else "CommandMethod + thân method (runtime NOT_TESTED)",
                "lenh_chinh": primary_cmd,
                "aliases_module": ";".join(module_aliases),
                "aliases_CommandAliases": "",
                "effective_method": primary["effective_method"],
                "handler_file": file,
                "handler_line": primary["line"],
                "eff_span": f"{primary['eff_start']}-{primary['eff_end']}",
                "types_in_handler": ";".join(primary["types"]),
                "ribbon_entries": "",
                "palette_entries": "",
                "xaml_parent_events": "",
                "muc_ket_luan_source": "STUB_ONLY" if stub else "METHOD_BODY_SCANNED",
                "runtime": "NOT_TESTED",
                "video_candidate": "YES" if (not stub and audience == "USER") else "NO",
                "merge_note": primary.get("eff_note") or (f"gộp {cmds_in}" if len(cmds_in) > 1 else ""),
                "_body": primary.get("_body") or "",
                "_all_cmds": cmds_in,
            }
        )

    # Attach CommandAliases
    cmd_to_bf = {}
    for f in features:
        for c in f["_all_cmds"]:
            cmd_to_bf[c] = f["business_feature_id"]

    for a in cmds:
        if not a["is_alias_file"]:
            continue
        owners = [c for c in non_alias if c["method"] == a["alias_delegates_to_method"]]
        if owners:
            bid = cmd_to_bf.get(owners[0]["cmd"], "")
            cmd_to_bf[a["cmd"]] = bid
            for f in features:
                if f["business_feature_id"] == bid:
                    als = [x for x in f["aliases_CommandAliases"].split(";") if x]
                    if a["cmd"] not in als:
                        als.append(a["cmd"])
                    f["aliases_CommandAliases"] = ";".join(sorted(als))

    # Ribbon / palette
    ribbon_stub_enabled = []
    for b in ribbon:
        bid = cmd_to_bf.get(b["cmd"], "")
        if bid:
            for f in features:
                if f["business_feature_id"] == bid:
                    entry = f"{b['label']}|{b['cmd']}|en={b['enabled']}|L{b['line']}"
                    f["ribbon_entries"] = (f["ribbon_entries"] + ";" + entry).strip(";")
                    if f["source_status"] == "STUB" and b["enabled"]:
                        ribbon_stub_enabled.append({"cmd": b["cmd"], "label": b["label"], "line": b["line"], "bf": bid})
    for p in palette:
        bid = cmd_to_bf.get(p["cmd"], "")
        if bid:
            for f in features:
                if f["business_feature_id"] == bid:
                    f["palette_entries"] = (f["palette_entries"] + ";" + f"{p['cmd']}|{p['file']}:L{p['line']}").strip(";")

    # Deep traces
    traces = []
    for f in features:
        tr = deep_trace_feature(f)
        f["muc_ket_luan_source"] = tr["evidence"]
        traces.append(
            {
                "business_feature_id": f["business_feature_id"],
                "lenh_chinh": f["lenh_chinh"],
                "source_status": f["source_status"],
                "evidence": tr["evidence"],
                "call_chain": tr["chain"],
                "inputs": tr["inputs"],
                "outputs": tr["outputs"],
                "errors": tr["errors"],
                "destructive": tr["destructive"],
                "unresolved": tr["unresolved"],
                "side_effect_hits": tr["side_effect_hits"],
                "runtime": "NOT_TESTED",
            }
        )
        # write per-feature short trace
        (OUT / "05_TRACES" / f"{f['business_feature_id']}.md").write_text(
            f"""# {f['business_feature_id']} — {f['lenh_chinh']}

- **source_status:** {f['source_status']}
- **evidence:** `{tr['evidence']}`
- **runtime:** NOT_TESTED

## Call chain
{tr['chain']}

## Inputs
{tr['inputs']}

## Outputs / hậu điều kiện
{tr['outputs']}

## Errors
{tr['errors']}

## Destructive
{tr['destructive']}

## Unresolved
{tr['unresolved'] or '(không)'}
""",
            encoding="utf-8",
        )

    # XAML mapping
    ui_rows = []
    xaml_mapped_count = 0
    for x in xaml:
        cs = ROOT / x["file"].replace(".xaml", ".xaml.cs")
        if not cs.exists():
            cs = ROOT / x["file"].replace(".xaml", ".cs")
        body = ""
        cb_span = ""
        if cs.exists():
            cst = cs.read_text(encoding="utf-8", errors="replace")
            span = extract_method_span(cst, x["handler"])
            if span:
                sl, el, body = span
                cb_span = f"{rel(cs)}:{sl}-{el}"
        classify, reason, conf = classify_xaml_handler(x["handler"], body)
        parent, map_status, map_reason = map_parent_feature(x["file"], x["handler"], cmd_to_bf, openers)
        if parent:
            xaml_mapped_count += 1
            for f in features:
                if f["business_feature_id"] == parent:
                    f["xaml_parent_events"] = (f["xaml_parent_events"] + ";" + x["handler"]).strip(";")
        # Every row MUST have parent OR reason
        assert parent or map_status.startswith("NOT_APPLICABLE") or map_status.startswith("UNRESOLVED"), map_status
        ui_rows.append(
            {
                "entry_type": "XAML_CLICK",
                "name": x["handler"],
                "file": x["file"],
                "line": x["line"],
                "codebehind_span": cb_span,
                "classify": classify,
                "classify_reason": reason,
                "confidence": conf,
                "parent_feature_id": parent,
                "mapping_status": map_status,
                "mapping_reason": map_reason,
                "side_effect_summary": (body[:160].replace("\n", " ") if body else "CHƯA ĐỦ BẰNG CHỨNG thân handler"),
            }
        )

    # Add ribbon/palette/context to UI map
    for b in ribbon:
        ui_rows.append(
            {
                "entry_type": "RIBBON_BTN",
                "name": f"{b['label']}|{b['cmd']}",
                "file": b["file"],
                "line": b["line"],
                "codebehind_span": "src/UI/RibbonCommandHandler.cs (ICommand→SendStringToExecute)",
                "classify": "COMMAND_DISPATCH" if b["enabled"] else "PLACEHOLDER",
                "classify_reason": f"enabled={b['enabled']}",
                "confidence": 0.95,
                "parent_feature_id": cmd_to_bf.get(b["cmd"], ""),
                "mapping_status": "MAPPED" if b["cmd"] in cmd_to_bf else (
                    "NOT_APPLICABLE_PLACEHOLDER" if not b["enabled"] else "UNRESOLVED"
                ),
                "mapping_reason": "Ribbon Btn → command",
                "side_effect_summary": "gửi lệnh CAD",
            }
        )
    for p in palette:
        ui_rows.append(
            {
                "entry_type": "PALETTE_RUN",
                "name": p["cmd"],
                "file": p["file"],
                "line": p["line"],
                "codebehind_span": f"{p['file']}:{p['line']}",
                "classify": "COMMAND_DISPATCH",
                "classify_reason": "PaletteCommands.Run",
                "confidence": 0.95,
                "parent_feature_id": cmd_to_bf.get(p["cmd"], ""),
                "mapping_status": "MAPPED" if p["cmd"] in cmd_to_bf else "UNRESOLVED",
                "mapping_reason": "Palette run → command",
                "side_effect_summary": "SendStringToExecute",
            }
        )

    # Context menu / dynamic
    extra_ui = [
        {
            "entry_type": "CONTEXT_MENU",
            "name": "NodeMenu (SurveyPaletteView)",
            "file": "src/UI/Palette/SurveyPaletteView.xaml",
            "line": 15,
            "codebehind_span": "src/UI/Palette/ (RelayCommand/ICommand)",
            "classify": "CONTEXT_MENU",
            "classify_reason": "ContextMenu trong Palette XAML",
            "confidence": 0.8,
            "parent_feature_id": cmd_to_bf.get("ISURVEY_PALETTE", cmd_to_bf.get("BANGIS", "")),
            "mapping_status": "MAPPED_OR_SHARED",
            "mapping_reason": "Menu ngữ cảnh palette — không tạo feature mới",
            "side_effect_summary": "ICommand / node actions",
        },
        {
            "entry_type": "DOUBLE_CLICK",
            "name": "FrameDoubleClick→ISURVEY_FRAME_EDIT",
            "file": "src/Modules/Frame/FrameDoubleClick.cs",
            "line": 324,
            "codebehind_span": "src/Modules/Frame/FrameDoubleClick.cs:324",
            "classify": "COMMAND_DISPATCH",
            "classify_reason": "SendStringToExecute _.ISURVEY_FRAME_EDIT",
            "confidence": 0.9,
            "parent_feature_id": cmd_to_bf.get("ISURVEY_FRAME_EDIT", ""),
            "mapping_status": "MAPPED" if "ISURVEY_FRAME_EDIT" in cmd_to_bf else "UNRESOLVED",
            "mapping_reason": "Double-click khung → lệnh sửa khung",
            "side_effect_summary": "gửi lệnh FRAME_EDIT",
        },
        {
            "entry_type": "ICOMMAND",
            "name": "RibbonCommandHandler",
            "file": "src/UI/RibbonCommandHandler.cs",
            "line": 7,
            "codebehind_span": "src/UI/RibbonCommandHandler.cs",
            "classify": "COMMAND_DISPATCH",
            "classify_reason": "ICommand SendStringToExecute",
            "confidence": 0.95,
            "parent_feature_id": "",
            "mapping_status": "NOT_APPLICABLE_INFRA",
            "mapping_reason": "Hạ tầng Ribbon — ánh xạ qua từng Btn",
            "side_effect_summary": "dispatch lệnh",
        },
        {
            "entry_type": "ICOMMAND",
            "name": "RibbonPlaceholderHandler",
            "file": "src/UI/RibbonPlaceholderHandler.cs",
            "line": 8,
            "codebehind_span": "src/UI/RibbonPlaceholderHandler.cs",
            "classify": "PLACEHOLDER",
            "classify_reason": "ICommand cho nút Ribbon disabled/placeholder",
            "confidence": 0.9,
            "parent_feature_id": "",
            "mapping_status": "NOT_APPLICABLE_PLACEHOLDER",
            "mapping_reason": "Không phải feature độc lập",
            "side_effect_summary": "placeholder",
        },
        {
            "entry_type": "DYNAMIC_AI",
            "name": "AiToolRegistry SendStringToExecute",
            "file": "src/Modules/Ai/AiToolRegistry.cs",
            "line": 252,
            "codebehind_span": "src/Modules/Ai/AiToolRegistry.cs:252,397",
            "classify": "DYNAMIC_DISPATCH",
            "classify_reason": "AI tool có thể gửi lệnh CAD động",
            "confidence": 0.7,
            "parent_feature_id": cmd_to_bf.get("ISURVEY_AI_ASSIST", ""),
            "mapping_status": "MAPPED_PARTIAL",
            "mapping_reason": "Thuộc AI assist; lệnh đích động — CHƯA ĐỦ BẰNG CHỨNG mọi tool",
            "side_effect_summary": "SendStringToExecute(script)",
        },
    ]
    ui_rows.extend(extra_ui)

    # Crosswalk R01/R02/R02B
    r02_feats = load_csv(R02 / "03_DANH_MUC_TINH_NANG_CHUAN_HOA.csv")
    r01_csv = load_csv(R01 / "03_DANH_MUC_LENH_VA_TINH_NANG.csv")
    cross = []
    for f in features:
        r02b_ids = []
        for rf in r02b_feats:
            refs = set((rf.get("r01_refs") or "").split(";")) | {rf.get("lenh_chinh")}
            if set(f["_all_cmds"]) & refs or f["lenh_chinh"] == rf.get("lenh_chinh"):
                r02b_ids.append(rf["feature_id"])
        r02_ids = []
        for rf in r02_feats:
            refs = set((rf.get("aliases") or "").split(";")) | {rf.get("lenh_chinh")}
            if set(f["_all_cmds"]) & refs or f["lenh_chinh"] == rf.get("lenh_chinh"):
                r02_ids.append(rf["feature_id"])
        r01_rows = []
        for rf in r01_csv:
            name = rf.get("ten_lenh") or rf.get("lenh") or rf.get("command") or ""
            # try common columns
            for k, v in rf.items():
                if v in f["_all_cmds"]:
                    r01_rows.append(rf.get("id") or rf.get("feature_id") or v)
                    break
        cross.append(
            {
                "business_feature_id": f["business_feature_id"],
                "lenh_chinh": f["lenh_chinh"],
                "all_commands": ";".join(f["_all_cmds"]),
                "r02b_aud_ids": ";".join(sorted(set(r02b_ids))),
                "r02_aud_ids": ";".join(sorted(set(r02_ids))),
                "r01_refs": ";".join(sorted(set(r01_rows))) if r01_rows else ";".join(f["_all_cmds"]),
                "note": "AUD chỉ là ID đợt; BF-* là khóa nghiệp vụ ổn định",
            }
        )

    # Validate XAML: no empty parent without reason
    bad = [u for u in ui_rows if u["entry_type"] == "XAML_CLICK" and not u["parent_feature_id"] and not u["mapping_status"]]
    assert not bad

    # Strip private fields for catalog
    catalog = []
    for f in features:
        catalog.append({k: v for k, v in f.items() if not k.startswith("_")})

    write_csv("02_BUSINESS_FEATURE_CATALOG.csv", catalog)
    write_csv("03_R01_R02_R02B_R02C_CROSSWALK.csv", cross)
    write_csv("04_UI_EVENT_TO_FEATURE.csv", ui_rows)
    write_csv("05_SOURCE_TRACE_COVERAGE.csv", traces)
    write_csv("06_STUB_RECHECK.csv", stub_rows)

    evid = defaultdict(int)
    for t in traces:
        evid[t["evidence"]] += 1

    stub_now = sum(1 for c in non_alias if c["stub"])
    stub_r02b = len(r02b_stub_cmds)
    summary = {
        "head": "3a16cb3959dc4e65c08f77b6de26669b309739cd",
        "commandmethod_total": len(cmds),
        "business_features": len(features),
        "source_present": sum(1 for f in features if f["source_status"] == "SOURCE_PRESENT"),
        "stub_features": sum(1 for f in features if f["source_status"] == "STUB"),
        "stub_commands_non_alias": stub_now,
        "r02b_stub_cmd_set_size": stub_r02b,
        "send_back_is_stub": bool(sb and sb["stub"]) if sb else None,
        "send_back_status": "SOURCE_PRESENT" if sb and not sb["stub"] else "MISSING/STUB",
        "xaml_clicks": len(xaml),
        "xaml_with_parent": xaml_mapped_count,
        "xaml_without_parent_but_reason": sum(
            1 for u in ui_rows if u["entry_type"] == "XAML_CLICK" and not u["parent_feature_id"]
        ),
        "video_candidates": sum(1 for f in features if f["video_candidate"] == "YES"),
        "evidence": dict(evid),
        "ribbon_stub_enabled": ribbon_stub_enabled,
        "service_chain_count": evid.get("SERVICE_CHAIN_VERIFIED_STATIC", 0),
        "not_enough": evid.get("NOT_ENOUGH_EVIDENCE", 0) + evid.get("METHOD_BODY_SCANNED", 0),
    }
    (OUT / "_summary_r02c.json").write_text(json.dumps(summary, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps(summary, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
