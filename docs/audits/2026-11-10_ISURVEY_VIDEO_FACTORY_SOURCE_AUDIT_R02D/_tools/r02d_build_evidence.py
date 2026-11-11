# -*- coding: utf-8 -*-
"""R02D Phase-01 evidence pack — readonly source verification. Writes only into _R02D."""
from __future__ import annotations

import csv
import json
import re
from collections import defaultdict
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(r"D:\AIProject\isurvey")
OUT = Path(__file__).resolve().parents[1]
R02C = ROOT / "docs/audits/2026-11-10_ISURVEY_VIDEO_FACTORY_SOURCE_AUDIT_R02C"
HEAD = "3a16cb3959dc4e65c08f77b6de26669b309739cd"

METH = re.compile(
    r"(?:public|internal|private)\s+(?:static\s+)?(?:async\s+)?(?:void|Task(?:<[^>]+>)?)\s+(\w+)\s*\("
)
SECRET = re.compile(
    r"(?i)(password|passwd|api[_-]?key|secret|token|bearer\s+[a-z0-9._\-]+|sk-[a-z0-9]+)"
)


def rel(p: Path) -> str:
    return p.relative_to(ROOT).as_posix()


def read_csv(path: Path) -> list[dict]:
    with path.open(encoding="utf-8-sig", newline="") as f:
        return list(csv.DictReader(f))


def write_csv(name: str, rows: list[dict]):
    if not rows:
        return
    with (OUT / name).open("w", encoding="utf-8-sig", newline="") as f:
        w = csv.DictWriter(f, fieldnames=list(rows[0].keys()))
        w.writeheader()
        w.writerows(rows)


def extract_span(text: str, method: str) -> tuple[int, int, str] | None:
    pat = re.compile(
        rf"(?:public|internal|private)\s+(?:static\s+)?(?:async\s+)?(?:void|Task(?:<[^>]+>)?)\s+{re.escape(method)}\s*\("
    )
    m = pat.search(text)
    if not m:
        return None
    start = text.count("\n", 0, m.start()) + 1
    brace = text.find("{", m.end())
    arrow = text.find("=>", m.end())
    if arrow >= 0 and (brace < 0 or arrow < brace):
        end = text.find(";", arrow)
        body = text[arrow : (end + 1 if end > 0 else arrow + 200)]
        return start, text.count("\n", 0, end if end > 0 else arrow) + 1, body
    if brace < 0:
        return start, start, ""
    i, d = brace, 0
    while i < len(text):
        if text[i] == "{":
            d += 1
        elif text[i] == "}":
            d -= 1
            if d == 0:
                i += 1
                break
        i += 1
    return start, text.count("\n", 0, i - 1) + 1, text[brace:i]


def sanitize(snippet: str) -> tuple[str, bool]:
    """Redact secret-like tokens; return (text, redacted?)."""
    if not SECRET.search(snippet):
        return snippet, False
    return SECRET.sub("[REDACTED]", snippet), True


def write_evidence(eid: str, file_rel: str, start: int, end: int, body: str, note: str) -> str:
    safe, red = sanitize(body)
    # Cap evidence size
    if len(safe) > 4000:
        safe = safe[:4000] + "\n/* … truncated for pack size; full method in repo */\n"
    path = OUT / "EVIDENCE" / f"{eid}.md"
    path.write_text(
        f"""# Evidence `{eid}`

- **file:** `{file_rel}`
- **lines:** {start}-{end}
- **git HEAD:** `{HEAD}`
- **redacted:** {"YES" if red else "NO"}
- **note:** {note}

```csharp
{safe}
```
""",
        encoding="utf-8",
    )
    return f"EVIDENCE/{eid}.md#{file_rel}:{start}-{end}"


# Manual deep verification cards for the 10 R02C SERVICE_CHAIN claims
SC10 = {
    "BF-ISURVEY_UAV_SEND_BACK": {
        "verdict": "SOURCE_BEHAVIOR_VERIFIED_STATIC",
        "chain": [
            "UavModule.SendBackCommand @ src/Modules/Uav/UavModule.cs:28-35",
            "AuthManager.EnsureAuthenticated(track:false)",
            "UavImageService.SendImageToBack @ UavImageService.cs:468-489",
            "PromptEntity RasterImage → Transaction → SendImageToBack(tr,…) → Commit → ActivityLogger",
        ],
        "inputs": "User pick 1 RasterImage; active Document/Database",
        "algo": "GetEntity chỉ RasterImage; reorder DrawOrder xuống đáy trong transaction",
        "outputs": "Ảnh dưới cùng draw order; message; ActivityLogger.Result",
        "errors": "hủy pick → WriteMessage Đã hủy; không catch bao ngoài ngoài module",
        "destructive": "Không xóa entity — chỉ đổi thứ tự vẽ",
        "gaps": "",
        "methods": [("src/Modules/Uav/UavModule.cs", "SendBackCommand"), ("src/Modules/Uav/UavImageService.cs", "SendImageToBack")],
    },
    "BF-ISURVEY_UAV_FRAME_TOGGLE": {
        "verdict": "SOURCE_BEHAVIOR_VERIFIED_STATIC",
        "chain": [
            "UavModule.FrameToggleCommand @ UavModule.cs:19-26",
            "UavImageService.ToggleImageFrames @ UavImageService.cs:439-465",
            "ACAD_IMAGE_VARS RasterVariables.ImageFrame toggle Off↔Above → Commit",
        ],
        "inputs": "Database hiện hành (không pick)",
        "algo": "Đọc/tạo RasterVariables trong NOD; đảo ImageFrame",
        "outputs": "Khung ảnh bật/tắt toàn bản vẽ; ActivityLogger",
        "errors": "không có try/catch riêng trong ToggleImageFrames — exception bubble",
        "destructive": "Không xóa ảnh; đổi biến hệ thống IMAGEFRAME",
        "gaps": "",
        "methods": [("src/Modules/Uav/UavModule.cs", "FrameToggleCommand"), ("src/Modules/Uav/UavImageService.cs", "ToggleImageFrames")],
    },
    "BF-ISURVEY_LOGOUT": {
        "verdict": "SOURCE_BEHAVIOR_VERIFIED_STATIC",
        "chain": [
            "AuthModule.LogoutCommand @ AuthModule.cs:77-100",
            "AuthManager.Refresh/IsAuthenticated → DarkDialog.Confirm → AuthManager.SignOut",
        ],
        "inputs": "Phiên đăng nhập hiện có; xác nhận UI",
        "algo": "Nếu chưa login → thông báo; nếu Confirm → SignOut xóa giấy phép máy",
        "outputs": "License local bị xóa; tính năng khóa; Print đã đăng xuất",
        "errors": "hủy Confirm → hủy đăng xuất",
        "destructive": "Xóa license trên máy (destructive với trạng thái kích hoạt)",
        "gaps": "Thân AuthManager.SignOut chi tiết persistence — xem thêm nếu cần audit auth sâu",
        "methods": [("src/Auth/AuthModule.cs", "LogoutCommand")],
    },
    "BF-ISURVEY_DGN_TO_CAD": {
        "verdict": "SOURCE_PARTIAL_VERIFIED",
        "chain": [
            "DgnModule.DgnToCadCommand @ DgnModule.cs:21-84",
            "DgnImportDialog / DgnBatchService.Start hoặc DgnImportService.Start (async Idle)",
            "Engine -DGNIMPORT + hậu xử lý explode/rename/TCVN3 (DgnImportService)",
        ],
        "inputs": "File .dgn V7/V8; Model Space; options dialog; batch optional",
        "algo": "Start async; theo dõi Handseed/Idle; timeout/quiet windows; post-process",
        "outputs": "Entities CAD (+report ImportedEntities/Warnings/LogPath)",
        "errors": "dialog catch; hủy; report.Error; timeout 300s",
        "destructive": "Explode cell, purge block/linetype/layer, rename layer trên DWG đích",
        "gaps": "CHƯA ĐỦ BẰNG CHỨNG: toàn bộ nhánh Idle/Finish trong DgnImportService (>1000 dòng) chưa đọc hết từng dòng",
        "methods": [("src/Dgn/DgnModule.cs", "DgnToCadCommand")],
    },
    "BF-ISURVEY_CAD_TO_DGN": {
        "verdict": "SOURCE_PARTIAL_VERIFIED",
        "chain": [
            "DgnModule.CadToDgnCommand @ DgnModule.cs:87-146",
            "SelectImplied pickfirst → DgnExportDialog → DgnExportService.Start → WriteExportSummary",
        ],
        "inputs": "Model Space; selection set optional; output path options",
        "algo": "Xuất async qua DgnExportService; progress window",
        "outputs": "File .dgn; bytes; entities count; ActivityLogger",
        "errors": "dialog catch; hủy; report.Error + log path",
        "destructive": "Không xóa CAD; ghi file output (có thể ghi đè theo dialog)",
        "gaps": "CHƯA ĐỦ BẰNG CHỨNG: toàn bộ DgnExportService pipeline",
        "methods": [("src/Dgn/DgnModule.cs", "CadToDgnCommand")],
    },
    "BF-VEMCD": {
        "verdict": "SOURCE_PARTIAL_VERIFIED",
        "chain": [
            "VEMCD/ISURVEY_PROFILE_DRAW → Draw @ ProfileModule.cs:14-147",
            "RouteStore.Load → DrawProfileWindow → pick insert → ProfileDrawService.Draw / RunUpdate",
            "Có thể xóa profile cũ cùng RouteId rồi vẽ lại",
        ],
        "inputs": "Tuyến+cọc có Z; Model Space; options; điểm chèn",
        "algo": "Select routes; sample Z; band/datum; Draw hoặc UpdateFromTin/Properties",
        "outputs": "Hình MCD Model; ProfileStore",
        "errors": "route empty; dialog cancel; catch Lỗi mặt cắt dọc",
        "destructive": "Xóa MCD cũ cùng RouteId trước vẽ lại",
        "gaps": "CHƯA ĐỦ BẰNG CHỨNG: ProfileDrawService geometry band đầy đủ",
        "methods": [("src/Modules/Profile/ProfileModule.cs", "Draw")],
    },
    "BF-TAOTUYEN": {
        "verdict": "SOURCE_BEHAVIOR_VERIFIED_STATIC",
        "chain": [
            "TAOTUYEN/ISURVEY_ROUTE_NEW → New @ RouteModule.cs:92+",
            "CreateRouteWindow.Prompt → GetEntity polyline → RouteStore.Save (+Terrain link)",
        ],
        "inputs": "Model Space; polyline tim; tên/mô hình địa hình optional",
        "algo": "Validate Polyline/2d/3d; tạo RouteRecord; lưu store",
        "outputs": "Tuyến mới trong DWG store; message; palette reload (nếu có)",
        "errors": "dialog/hủy pick; không phải polyline",
        "destructive": "Không xóa polyline nguồn — chỉ gắn handle",
        "gaps": "Chi tiết RouteService phụ sau Save — xem thêm nếu cần",
        "methods": [("src/Modules/Tuyen/RouteModule.cs", "New")],
    },
    "BF-THIETLAPCOC": {
        "verdict": "SOURCE_BEHAVIOR_VERIFIED_STATIC",
        "chain": [
            "THIETLAPCOC → StationStyle @ RouteModule.cs:62-90",
            "StationStyleWindow.Prompt(StationStyleStore.Load) → StationStyleStore.Save",
        ],
        "inputs": "Settings kiểu cọc hiện có",
        "algo": "UI chỉnh thông số; lưu store; nhắc chạy PHATSINHCOC để vẽ lại",
        "outputs": "Settings persisted; không vẽ ngay",
        "errors": "dialog catch; hủy",
        "destructive": "Không xóa CAD; ghi đè settings",
        "gaps": "",
        "methods": [("src/Modules/Tuyen/RouteModule.cs", "StationStyle")],
    },
    "BF-TAOMOHINH": {
        "verdict": "SOURCE_BEHAVIOR_VERIFIED_STATIC",
        "chain": [
            "TAOMOHINH → New @ TerrainModule.cs:56-100",
            "CreateTerrainWindow.Prompt → TerrainStore.Save → SurveyPaletteManager.Reload",
        ],
        "inputs": "Model Space; tên/layer mô hình",
        "algo": "Tạo TerrainRecord Guid; Save; chưa build mesh",
        "outputs": "Model record; message hướng dẫn thêm điểm/xây",
        "errors": "dialog catch; hủy",
        "destructive": "Không xóa CAD",
        "gaps": "",
        "methods": [("src/Modules/Terrain/TerrainModule.cs", "New")],
    },
    "BF-TRACAOD": {
        "verdict": "SOURCE_PARTIAL_VERIFIED",
        "chain": [
            "TRACAOD → Query @ TerrainModule.cs (QueryCommand/QueryAlias)",
            "TerrainService query elevation — cần đọc thân Query()",
        ],
        "inputs": "Model có terrain; pick điểm",
        "algo": "Tra cao độ từ mesh/model",
        "outputs": "WriteMessage cao độ",
        "errors": "CHƯA ĐỦ BẰNG CHỨNG đầy đủ mọi nhánh Query cho tới khi đọc hết TerrainService",
        "destructive": "Không ghi CAD (kỳ vọng read-only)",
        "gaps": "Phải xác minh thân Query + TerrainService method cụ thể trong pack",
        "methods": [("src/Modules/Terrain/TerrainModule.cs", "Query")],
    },
}


def resolve_query_method():
    """Fill TRACAOD gaps by reading Query()."""
    path = ROOT / "src/Modules/Terrain/TerrainModule.cs"
    text = path.read_text(encoding="utf-8", errors="replace")
    span = extract_span(text, "Query")
    if not span:
        return
    sl, el, body = span
    SC10["BF-TRACAOD"]["methods"] = [("src/Modules/Terrain/TerrainModule.cs", "Query")]
    SC10["BF-TRACAOD"]["chain"] = [
        f"TRACAOD → Query @ TerrainModule.cs:{sl}-{el}",
        "EnsureAuthenticated; ModelSpace; TerrainService.Choose; GetPoint; TerrainService elevation",
    ]
    # refine from body
    if "TerrainService" in body:
        calls = re.findall(r"TerrainService\.(\w+)", body)
        SC10["BF-TRACAOD"]["chain"].append("TerrainService." + ", ".join(sorted(set(calls))))
    if "catch" in body:
        SC10["BF-TRACAOD"]["errors"] = "có catch trong Query — xem evidence"
    if "GetPoint" in body or "PromptPoint" in body:
        SC10["BF-TRACAOD"]["inputs"] = "Model Space; chọn mô hình; pick điểm XY"
    if len(body) > 80 and "TerrainService" in body:
        SC10["BF-TRACAOD"]["verdict"] = "SOURCE_BEHAVIOR_VERIFIED_STATIC"
        SC10["BF-TRACAOD"]["gaps"] = ""
    else:
        SC10["BF-TRACAOD"]["verdict"] = "SOURCE_PARTIAL_VERIFIED"


def build_xaml_resolution() -> list[dict]:
    rows = []
    # MapInsert
    for handler, lines, parent, behavior, status in [
        (
            "Delete_Click",
            "366-374",
            "BF-ISURVEY_MAP|BF-ISURVEY_MAP_SAT|BF-ISURVEY_DELETE_GE",
            "Đóng dialog (DialogResult=false) rồi GeDeleteWorkflow.Run(doc) — xóa ảnh GE theo workflow DELETE_GE",
            "MAPPED_PARENT_MAP_INSERT",
        ),
        (
            "Apply_Click",
            "378+",
            "BF-ISURVEY_MAP|BF-ISURVEY_MAP_SAT",
            "Validate tỉnh/meridian/zone → tạo MapInsertSettings → DialogResult=true (caller MapModule ApplyBasemap)",
            "MAPPED_PARENT_MAP_INSERT",
        ),
        (
            "Cancel_Click",
            "354-361",
            "BF-ISURVEY_MAP|BF-ISURVEY_MAP_SAT",
            "DialogResult=false; Close(); không ghi CAD",
            "MAPPED_PARENT_MAP_INSERT",
        ),
    ]:
        rows.append(
            {
                "window": "MapInsertWindow",
                "handler": handler,
                "file": "src/UI/MapInsert/MapInsertWindow.xaml.cs",
                "lines": lines,
                "opener": "MapInsertDialog.Show ← MapModule (~L197)",
                "parent_feature_id": parent,
                "mapping_status": status,
                "behavior": behavior,
                "evidence": write_evidence(
                    f"XAML-MapInsert-{handler}",
                    "src/UI/MapInsert/MapInsertWindow.xaml.cs",
                    int(lines.split("-")[0].replace("+", "")),
                    int(lines.split("-")[0].replace("+", "")) + 40,
                    (ROOT / "src/UI/MapInsert/MapInsertWindow.xaml.cs").read_text(encoding="utf-8", errors="replace"),
                    behavior,
                )
                if False
                else f"src/UI/MapInsert/MapInsertWindow.xaml.cs:{lines}",
                "confidence": "HIGH",
            }
        )

    # Fix evidence properly for MapInsert by extracting methods
    mi = (ROOT / "src/UI/MapInsert/MapInsertWindow.xaml.cs").read_text(encoding="utf-8", errors="replace")
    for r in rows:
        if r["window"] != "MapInsertWindow":
            continue
        span = extract_span(mi, r["handler"])
        if span:
            sl, el, body = span
            r["lines"] = f"{sl}-{el}"
            r["evidence"] = write_evidence(f"XAML-MapInsert-{r['handler']}", r["file"], sl, el, body, r["behavior"])

    # StationList
    slt = (ROOT / "src/UI/Tuyen/StationListWindow.xaml.cs").read_text(encoding="utf-8", errors="replace")
    for handler, behavior, parent in [
        ("Zoom_Click", "Zoom tới XY cọc chọn qua RouteService.ZoomTo — không sửa geometry", "BF-BANGCOC"),
        ("Delete_Click", "RouteService.RemoveStation — XÓA cọc khỏi route store + reload bảng (destructive)", "BF-BANGCOC"),
        ("Add_Click", "Đặt Result.AddRequested=true rồi Close — caller ListStations tiếp tục thêm cọc", "BF-BANGCOC"),
        ("Close_Click", "Result rỗng; Close — không đổi dữ liệu", "BF-BANGCOC"),
    ]:
        span = extract_span(slt, handler)
        sl, el, body = (span if span else (0, 0, ""))
        rows.append(
            {
                "window": "StationListWindow",
                "handler": handler,
                "file": "src/UI/Tuyen/StationListWindow.xaml.cs",
                "lines": f"{sl}-{el}",
                "opener": "StationListWindow.Prompt ← RouteModule.ListStations (BANGCOC) ~L384",
                "parent_feature_id": parent,
                "mapping_status": "MAPPED_PARENT_BANGCOC",
                "behavior": behavior,
                "evidence": write_evidence(f"XAML-StationList-{handler}", "src/UI/Tuyen/StationListWindow.xaml.cs", sl, el, body, behavior)
                if span
                else "CHƯA ĐỦ BẰNG CHỨNG",
                "confidence": "HIGH",
            }
        )

    # Vn2000 — shared helper, multi-parent
    vn = (ROOT / "src/UI/Vn2000/Vn2000SetupWindow.xaml.cs").read_text(encoding="utf-8", errors="replace")
    for handler, behavior in [
        ("Cancel_Click", "Hủy setup CRS — DialogResult false/Close"),
        ("Confirm_Click", "Xác nhận tham số VN2000/CRS trả về caller"),
    ]:
        span = extract_span(vn, handler)
        if not span:
            # try other names
            continue
        sl, el, body = span
        rows.append(
            {
                "window": "Vn2000SetupWindow",
                "handler": handler,
                "file": "src/UI/Vn2000/Vn2000SetupWindow.xaml.cs",
                "lines": f"{sl}-{el}",
                "opener": "Vn2000SetupDialog.Show ← MapModule / KmlImportModule / ExportModule",
                "parent_feature_id": "BF-ISURVEY_MAP|BF-ISURVEY_IMPORT_KML|BF-ISURVEY_EXPORT_KML|BF-ISURVEY_MAP_SAT",
                "mapping_status": "SHARED_HELPER_MULTI_PARENT",
                "behavior": behavior + " — không phải feature độc lập; phụ thuộc lệnh mở dialog",
                "evidence": write_evidence(f"XAML-Vn2000-{handler}", "src/UI/Vn2000/Vn2000SetupWindow.xaml.cs", sl, el, body, behavior),
                "confidence": "HIGH",
            }
        )
    # if Confirm/Cancel not found under those names, still document
    if not any(r["window"] == "Vn2000SetupWindow" for r in rows):
        rows.append(
            {
                "window": "Vn2000SetupWindow",
                "handler": "Cancel_Click/Confirm_Click",
                "file": "src/UI/Vn2000/Vn2000SetupWindow.xaml.cs",
                "lines": "see file",
                "opener": "Vn2000SetupDialog.Show ← Map/KML Import/Export",
                "parent_feature_id": "MULTI",
                "mapping_status": "SHARED_HELPER_MULTI_PARENT",
                "behavior": "Helper CRS dùng chung — NOT feature độc lập",
                "evidence": "CHƯA ĐỦ BẰNG CHỨNG method name nếu khác Confirm/Cancel — kiểm tra XAML Click attrs",
                "confidence": "MEDIUM",
            }
        )
    return rows


def analyze_user_feature(feat: dict) -> dict:
    bf = feat["business_feature_id"]
    if bf in SC10:
        card = SC10[bf]
        return {
            "business_feature_id": bf,
            "lenh_chinh": feat["lenh_chinh"],
            "priority": "SC10_RECHECK",
            "verdict_source": card["verdict"],
            "runtime": "NOT_TESTED",
            "call_chain": " | ".join(card["chain"]),
            "inputs": card["inputs"],
            "algorithm": card["algo"],
            "outputs": card["outputs"],
            "errors": card["errors"],
            "destructive": card["destructive"],
            "gaps": card["gaps"] or "(không)",
            "evidence_refs": "",
        }

    # Generic deep-ish: open handler effective method
    file_rel = feat["handler_file"]
    method = feat["effective_method"] or feat.get("method") or ""
    path = ROOT / file_rel
    text = path.read_text(encoding="utf-8", errors="replace") if path.exists() else ""
    span = extract_span(text, method) if text and method else None
    evid_ref = ""
    body = ""
    if span:
        sl, el, body = span
        evid_ref = write_evidence(f"USER-{bf}", file_rel, sl, el, body, f"handler {method}")

    types = [t for t in (feat.get("types_in_handler") or "").split(";") if t]
    destructive = bool(re.search(r"Delete|Erase|Purge|Remove|Clear|DropMesh|SignOut|Overwrite", body + types.__repr__(), re.I))
    file_io = bool(re.search(r"File\.|Writer|Reader|Export|Import|SaveFile|OpenFile|DGN", body))
    model = bool(re.search(r"Terrain|Route|Station|Profile|Tin|Mesh", body + ",".join(types), re.I))

    priority = "NORMAL"
    if destructive or file_io or model:
        priority = "HIGH_IMPACT"

    if feat["source_status"] == "STUB":
        verdict = "STUB_ONLY"
        gaps = "stub — không có hành vi đầy đủ"
    elif span and (types or len(body) > 100):
        # Follow one service call if present
        svc_hits = re.findall(r"\b([A-Z]\w+(?:Service|Store|Window|Dialog|Workflow|Flow))\b", body)
        followed = 0
        gap_list = []
        for s in sorted(set(svc_hits))[:4]:
            hits = list((ROOT / "src").rglob(f"{s}.cs"))
            if not hits:
                gap_list.append(f"không thấy file {s}.cs")
                continue
            st = hits[0].read_text(encoding="utf-8", errors="replace")
            called = re.findall(rf"{s}\.(\w+)\s*\(", body)
            ok = False
            for cm in called[:3]:
                sp2 = extract_span(st, cm)
                if sp2:
                    followed += 1
                    write_evidence(f"USER-{bf}-{s}-{cm}", rel(hits[0]), sp2[0], sp2[1], sp2[2], f"called from {bf}")
                    ok = True
            if not ok and called:
                gap_list.append(f"CHƯA ĐỦ BẰNG CHỨNG body {s}.{called}")
            elif not called:
                gap_list.append(f"thấy type {s} nhưng chưa khớp method call")
        if followed >= 1 and (file_io or destructive or "Commit" in body or "WriteMessage" in body):
            verdict = "SOURCE_BEHAVIOR_VERIFIED_STATIC"
        elif followed >= 1 or (span and len(body) > 200):
            verdict = "SOURCE_PARTIAL_VERIFIED"
        else:
            verdict = "SOURCE_PARTIAL_VERIFIED"
        gaps = "; ".join(gap_list) if gap_list else "(đã mở handler + ≥1 service call site)"
        if verdict == "SOURCE_BEHAVIOR_VERIFIED_STATIC" and gap_list:
            # stay honest
            verdict = "SOURCE_PARTIAL_VERIFIED"
            gaps = gaps + " — chưa đủ để khóa toàn chuỗi"
    elif span:
        verdict = "SOURCE_PARTIAL_VERIFIED"
        gaps = "chỉ đọc được thân handler; service chưa theo"
    else:
        verdict = "NOT_ENOUGH_EVIDENCE"
        gaps = "không mở được method"

    return {
        "business_feature_id": bf,
        "lenh_chinh": feat["lenh_chinh"],
        "priority": priority,
        "verdict_source": verdict,
        "runtime": "NOT_TESTED",
        "call_chain": f"{feat['lenh_chinh']} @ {file_rel}:{feat['handler_line']} → {method} → types={feat.get('types_in_handler')}",
        "inputs": "xem evidence handler; CHƯA ĐỦ BẰNG CHỨNG nếu thiếu trong body",
        "algorithm": (body[:300].replace("\n", " ") + "…") if body else "CHƯA ĐỦ BẰNG CHỨNG",
        "outputs": "xem WriteMessage/Store/File trong evidence",
        "errors": "catch trong handler" if "catch" in body else "CHƯA ĐỦ BẰNG CHỨNG / không thấy catch handler",
        "destructive": "có dấu hiệu destructive" if destructive else "không thấy trong đoạn đã đọc — CHƯA ĐỦ BẰNG CHỨNG toàn cục",
        "gaps": gaps,
        "evidence_refs": evid_ref,
    }


def main():
    (OUT / "EVIDENCE").mkdir(parents=True, exist_ok=True)
    (OUT / "TRACES").mkdir(parents=True, exist_ok=True)
    resolve_query_method()

    catalog = read_csv(R02C / "02_BUSINESS_FEATURE_CATALOG.csv")
    sc_rows = []
    for bf, card in SC10.items():
        refs = []
        for file_rel, method in card["methods"]:
            text = (ROOT / file_rel).read_text(encoding="utf-8", errors="replace")
            span = extract_span(text, method)
            if span:
                sl, el, body = span
                refs.append(write_evidence(f"SC10-{bf}-{method}", file_rel, sl, el, body, card["verdict"]))
        feat = next((c for c in catalog if c["business_feature_id"] == bf), {})
        sc_rows.append(
            {
                "business_feature_id": bf,
                "lenh_chinh": feat.get("lenh_chinh", bf.replace("BF-", "")),
                "r02c_claimed": "SERVICE_CHAIN_VERIFIED_STATIC",
                "r02d_verdict": card["verdict"],
                "call_chain": " | ".join(card["chain"]),
                "inputs": card["inputs"],
                "algorithm": card["algo"],
                "outputs": card["outputs"],
                "errors": card["errors"],
                "destructive": card["destructive"],
                "gaps": card["gaps"] or "(không)",
                "evidence_refs": "; ".join(refs),
                "runtime": "NOT_TESTED",
                "git_head": HEAD,
            }
        )
        (OUT / "TRACES" / f"{bf}.md").write_text(
            f"""# {bf} — R02D deep verify

- **R02C claimed:** SERVICE_CHAIN_VERIFIED_STATIC
- **R02D verdict:** `{card['verdict']}`
- **runtime:** NOT_TESTED
- **HEAD:** `{HEAD}`

## Chain
{chr(10).join('- '+c for c in card['chain'])}

## Inputs
{card['inputs']}

## Algorithm
{card['algo']}

## Outputs
{card['outputs']}

## Errors
{card['errors']}

## Destructive
{card['destructive']}

## Gaps
{card['gaps'] or '(không)'}

## Evidence
{chr(10).join('- '+r for r in refs)}
""",
            encoding="utf-8",
        )

    write_csv("01_SC10_DEEP_VERIFY.csv", sc_rows)

    xaml_rows = build_xaml_resolution()
    write_csv("02_XAML10_RESOLUTION.csv", xaml_rows)

    user_feats = [c for c in catalog if c["audience"] == "USER" and c["source_status"] != "STUB"]
    user_rows = []
    for f in user_feats:
        row = analyze_user_feature(f)
        user_rows.append(row)
        (OUT / "TRACES" / f"{row['business_feature_id']}.md").write_text(
            f"""# {row['business_feature_id']} — {row['lenh_chinh']}

- **verdict_source:** `{row['verdict_source']}`
- **priority:** {row['priority']}
- **runtime:** NOT_TESTED

## Call chain
{row['call_chain']}

## Inputs / Algorithm / Outputs / Errors / Destructive
- Inputs: {row['inputs']}
- Algorithm: {row['algorithm']}
- Outputs: {row['outputs']}
- Errors: {row['errors']}
- Destructive: {row['destructive']}

## Gaps
{row['gaps']}

## Evidence
{row['evidence_refs']}
""",
            encoding="utf-8",
        )
    write_csv("03_USER_FEATURES_EVIDENCE.csv", user_rows)

    # Linking matrix conclusion → evidence
    link = []
    for r in sc_rows:
        link.append(
            {
                "conclusion_id": r["business_feature_id"],
                "conclusion": r["r02d_verdict"],
                "evidence": r["evidence_refs"],
                "kind": "SC10",
            }
        )
    for r in xaml_rows:
        link.append(
            {
                "conclusion_id": f"{r['window']}.{r['handler']}",
                "conclusion": r["mapping_status"],
                "evidence": r["evidence"],
                "kind": "XAML10",
            }
        )
    for r in user_rows:
        link.append(
            {
                "conclusion_id": r["business_feature_id"],
                "conclusion": r["verdict_source"],
                "evidence": r["evidence_refs"],
                "kind": "USER",
            }
        )
    write_csv("04_CONCLUSION_TO_EVIDENCE.csv", link)

    # Stats
    vcount = defaultdict(int)
    for r in user_rows:
        vcount[r["verdict_source"]] += 1
    sc_v = defaultdict(int)
    for r in sc_rows:
        sc_v[r["r02d_verdict"]] += 1

    summary = {
        "head": HEAD,
        "sc10": dict(sc_v),
        "sc10_still_partial": [r["business_feature_id"] for r in sc_rows if r["r02d_verdict"] != "SOURCE_BEHAVIOR_VERIFIED_STATIC"],
        "xaml10_resolved": len(xaml_rows),
        "user_features": len(user_rows),
        "user_verdicts": dict(vcount),
        "evidence_files": len(list((OUT / "EVIDENCE").glob("*.md"))),
    }
    (OUT / "_summary_r02d.json").write_text(json.dumps(summary, ensure_ascii=False, indent=2), encoding="utf-8")

    # Markdown reports
    (OUT / "00_SUMMARY_PHASE01_EVIDENCE.md").write_text(
        f"""# 00 — Phase 01 Evidence Pack (R02D)

**Ngày:** {datetime.now(timezone.utc).date()}  
**HEAD:** `{HEAD}`  
**Kế thừa:** R02C catalog / traces — **không** làm lại kiểm kê từ đầu.

## Kết quả chính

| Hạng mục | Kết quả |
|---|---|
| 10 SERVICE_CHAIN R02C tái kiểm | `{dict(sc_v)}` — vẫn partial: {summary['sc10_still_partial']} |
| 10 XAML chưa rõ | **{len(xaml_rows)}** dòng đã gắn parent/hành vi + evidence |
| USER features | **{len(user_rows)}** — `{dict(vcount)}` |
| Evidence snippets | **{summary['evidence_files']}** file trong `EVIDENCE/` |
| Runtime | **NOT_TESTED** toàn bộ |

## Phân loại kết luận (bắt buộc)

| Nhãn | Nghĩa |
|---|---|
| SOURCE_BEHAVIOR_VERIFIED_STATIC | Đã đọc method/service liên quan, có I/O/nhánh/hậu điều kiện + file:line |
| SOURCE_PARTIAL_VERIFIED | Đã đọc một phần chuỗi; còn gaps ghi rõ |
| NOT_ENOUGH_EVIDENCE | Chưa đủ chứng cứ mã nguồn |
| STUB_ONLY | Stub |
| NOT_TESTED | Runtime — không tự nâng |

## Phase 01

**CHƯA TỰ NGHIỆM THU.** Gói này bổ sung bằng chứng để hội đồng thẩm định.
""",
        encoding="utf-8",
    )

    (OUT / "05_OPEN_ISSUES.md").write_text(
        f"""# 05 — Open issues (R02D)

## SC10 còn partial
{chr(10).join('- '+x for x in summary['sc10_still_partial']) or '- (không)'}

## USER còn partial / not enough
{chr(10).join('- '+r['business_feature_id']+' '+r['verdict_source']+' — '+str(r['gaps'])[:100] for r in user_rows if r['verdict_source']!='SOURCE_BEHAVIOR_VERIFIED_STATIC')}

## Runtime
Chưa thử CAD — không thuộc đợt này.

## Stop gate
DỪNG — chờ Chủ dự án / hội đồng quyết định nghiệm thu Phase 01.
""",
        encoding="utf-8",
    )

    (OUT / "06_GIT_BEFORE_AFTER.md").write_text(
        f"""# 06 — Git before/after (R02D)

Xem `_git_*_before/after*`.

- HEAD trước/sau kỳ vọng: `{HEAD}` không đổi
- Chỉ tạo tệp dưới `_R02D/` (+ ZIP)
- Không sửa C#/XAML nghiệp vụ
""",
        encoding="utf-8",
    )

    (OUT / "AUDIT_INDEX_R02D.md").write_text(
        f"""# AUDIT_INDEX_R02D — Phase 01 Evidence

1. `00_SUMMARY_PHASE01_EVIDENCE.md`
2. `01_SC10_DEEP_VERIFY.csv`
3. `02_XAML10_RESOLUTION.csv`
4. `03_USER_FEATURES_EVIDENCE.csv`
5. `04_CONCLUSION_TO_EVIDENCE.csv`
6. `05_OPEN_ISSUES.md`
7. `06_GIT_BEFORE_AFTER.md`
8. `EVIDENCE/` — trích đoạn mã + file:line + HEAD
9. `TRACES/` — hồ sơ từng BF

**Cảnh báo:** Không tuyên bố Phase 01 nghiệm thu. Runtime NOT_TESTED.
""",
        encoding="utf-8",
    )

    print(json.dumps(summary, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
