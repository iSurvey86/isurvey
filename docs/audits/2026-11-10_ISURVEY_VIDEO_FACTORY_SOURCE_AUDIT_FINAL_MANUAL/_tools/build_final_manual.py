# -*- coding: utf-8 -*-
"""Final manual Phase-01 pack — only writes into FINAL_MANUAL folder."""
from __future__ import annotations

import csv
import re
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(r"D:\AIProject\isurvey")
OUT = ROOT / "docs/audits/2026-11-10_ISURVEY_VIDEO_FACTORY_SOURCE_AUDIT_FINAL_MANUAL"
EV = OUT / "03_EVIDENCE"
R02C = ROOT / "docs/audits/2026-11-10_ISURVEY_VIDEO_FACTORY_SOURCE_AUDIT_R02C"
R02D = ROOT / "docs/audits/2026-11-10_ISURVEY_VIDEO_FACTORY_SOURCE_AUDIT_R02D"
HEAD = (OUT / "_git_head_before.txt").read_text(encoding="utf-8").strip()
SECRET = re.compile(r"(?i)(password|passwd|api[_-]?key|bearer\s+\S+|sk-\S+)")


def extract(path: str, a: int, b: int) -> tuple[str, bool]:
    text = (ROOT / path).read_text(encoding="utf-8", errors="replace").splitlines()
    chunk = "\n".join(text[a - 1 : b])
    red = bool(SECRET.search(chunk))
    if red:
        chunk = SECRET.sub("[REDACTED]", chunk)
    return chunk, red


def write_ev(code: str, path: str, a: int, b: int, title: str, note: str) -> str:
    chunk, red = extract(path, a, b)
    (EV / f"{code}.md").write_text(
        f"""# {code} — {title}

- **file:** `{path}`
- **lines:** {a}-{b}
- **git HEAD:** `{HEAD}`
- **redacted:** {"YES" if red else "NO"}
- **note:** {note}

```csharp
{chunk}
```
""",
        encoding="utf-8",
    )
    return f"03_EVIDENCE/{code}.md ({path}:{a}-{b})"


def main() -> None:
    EV.mkdir(parents=True, exist_ok=True)
    ev_index: list[dict] = []
    specs = [
        ("E-D2C-START", "src/Dgn/DgnImportService.cs", 108, 165, "D2C Start → DGNIMPORT async", "Gửi -DGNIMPORT rồi trả về; hậu xử lý Idle"),
        ("E-D2C-MODULE", "src/Dgn/DgnModule.cs", 21, 84, "D2C command entry", "Auth, Model Space, dialog, Start/Batch"),
        ("E-C2D-OVERWRITE", "src/Dgn/DgnExportService.cs", 106, 148, "C2D file overwrite + Start path", "File.Exists/Delete trước xuất"),
        ("E-C2D-RUNEXPORT", "src/Dgn/DgnExportService.cs", 346, 435, "C2D -DGNEXPORT + poll File.Exists", "Command -DGNEXPORT; OnIdle File.Exists"),
        ("E-C2D-MODULE", "src/Dgn/DgnModule.cs", 87, 146, "C2D command entry", "Pickfirst + dialog + Start"),
        ("E-VEMCD-DRAW", "src/Modules/Profile/ProfileModule.cs", 20, 147, "VEMCD Draw() handler", "Auth, RouteStore, dialog, pick, RunDraw/Update"),
        ("E-VEMCD-REDRAW", "src/Modules/Profile/ProfileModule.cs", 163, 208, "VEMCD erase old + Draw + Save", "eraseHandles → Draw → ProfileStore.Save"),
        ("E-VEMCD-SERVICE", "src/Modules/Profile/ProfileDrawService.cs", 142, 231, "EraseHandles + Draw strip loop", "Xóa handle; sample; entity ModelSpace; Commit"),
        ("E-TRACAOD-QUERY", "src/Modules/Terrain/TerrainModule.cs", 303, 344, "TRACAOD Query loop", "ShowElevationMark loop; EraseElevationMark end"),
        ("E-TRACAOD-MARK", "src/Modules/Terrain/TerrainService.cs", 664, 710, "Show/Erase elevation mark", "DBText create/erase"),
        ("E-LOGOUT-CMD", "src/Auth/AuthModule.cs", 77, 100, "LOGOUT command", "Confirm → SignOut"),
        ("E-LOGOUT-SIGNOUT", "src/Auth/AuthManager.cs", 204, 215, "AuthManager.SignOut", "Storage.Delete; clear license"),
        ("E-LOGOUT-STORAGE", "src/Auth/LicenseStorage.cs", 196, 207, "LicenseStorage.Delete", "File.Delete license.dat"),
        ("E-TAOTUYEN-SAVE", "src/Modules/Tuyen/RouteStore.cs", 60, 87, "RouteStore.Save", "Xrecord JSON NOD"),
        ("E-TAOTUYEN-NEW", "src/Modules/Tuyen/RouteModule.cs", 92, 160, "TAOTUYEN New() start", "Dialog + pick polyline"),
        ("E-TAOMOHINH-SAVE", "src/Modules/Terrain/TerrainStore.cs", 73, 100, "TerrainStore.Save", "Xrecord JSON"),
        ("E-TAOMOHINH-NEW", "src/Modules/Terrain/TerrainModule.cs", 56, 100, "TAOMOHINH New()", "Dialog → Save"),
        ("E-THIETLAPCOC", "src/Modules/Tuyen/RouteModule.cs", 62, 90, "THIETLAPCOC StationStyle", "Window → Store.Save"),
        ("E-THIETLAPCOC-STORE", "src/Modules/Tuyen/StationStyleStore.cs", 30, 50, "StationStyleStore.Save", "AppData JSON WriteAllText"),
        ("E-DELETE-GE", "src/Modules/Map/GeDeleteWorkflow.cs", 11, 57, "DELETE_GE workflow", "Confirm → DeleteAll"),
        ("E-MAPSAT-HD", "src/Modules/Map/HdMosaicWorkflow.cs", 30, 100, "MAP_SAT HdMosaicWorkflow.Run", "Biên/zoom — phần đầu"),
        ("E-NUMBER-VERTICES", "src/Modules/Tuyen/VertexNumberingService.cs", 37, 125, "NUMBER_VERTICES CAD", "Pick polyline; tạo số góc"),
        ("E-NUMBER-TXT", "src/Modules/Tuyen/VertexNumberingService.cs", 225, 237, "NUMBER_VERTICES txt", "AngleTableExport file"),
        ("E-UAV-SENDBACK", "src/Modules/Uav/UavImageService.cs", 468, 489, "SEND_BACK", "RasterImage DrawOrder"),
        ("E-UAV-FRAME", "src/Modules/Uav/UavImageService.cs", 439, 465, "FRAME_TOGGLE", "IMAGEFRAME vars"),
        ("E-XAML-MAP-DELETE", "src/UI/MapInsert/MapInsertWindow.xaml.cs", 366, 374, "MapInsert Delete_Click", "Close + GeDeleteWorkflow.Run"),
        ("E-XAML-MAP-APPLY", "src/UI/MapInsert/MapInsertWindow.xaml.cs", 378, 430, "MapInsert Apply_Click", "Build MapInsertSettings"),
        ("E-XAML-STN-DEL", "src/UI/Tuyen/StationListWindow.xaml.cs", 78, 94, "StationList Delete_Click", "RemoveStation"),
        ("E-XAML-STN-ZOOM", "src/UI/Tuyen/StationListWindow.xaml.cs", 67, 76, "StationList Zoom_Click", "ZoomTo"),
        ("E-XAML-STN-ADD", "src/UI/Tuyen/StationListWindow.xaml.cs", 96, 100, "StationList Add_Click", "AddRequested"),
        ("E-XAML-VN-CONFIRM", "src/UI/Vn2000/Vn2000SetupWindow.xaml.cs", 197, 230, "Vn2000 Confirm_Click", "CRS helper"),
    ]
    for s in specs:
        ref = write_ev(*s)
        ev_index.append({"code": s[0], "ref": ref, "title": s[4]})

    (EV / "INDEX.md").write_text(
        "# Evidence INDEX\n\n" + "\n".join(f"- `{e['code']}` — {e['title']} → `{e['ref']}`" for e in ev_index),
        encoding="utf-8",
    )

    cat = list(csv.DictReader((R02C / "02_BUSINESS_FEATURE_CATALOG.csv").open(encoding="utf-8-sig")))
    user = [c for c in cat if c["audience"] == "USER" and c["source_status"] != "STUB"]

    manual = {
        "BF-ISURVEY_DGN_TO_CAD": (
            "PERSISTENT_CAD_MUTATION",
            "PARTIAL_BLOCKED",
            "Đã chứng minh Start/-DGNIMPORT; chưa đọc hết Idle/Finish/explode → chặn video đến checklist riêng",
            "E-D2C-MODULE;E-D2C-START",
        ),
        "BF-ISURVEY_CAD_TO_DGN": (
            "FILE_WRITE",
            "SOURCE_SUPPORTED",
            "File.Delete ghi đè + -DGNEXPORT + OnIdle File.Exists(size)>0 — không dựa WriteMessage",
            "E-C2D-MODULE;E-C2D-OVERWRITE;E-C2D-RUNEXPORT",
        ),
        "BF-VEMCD": (
            "PERSISTENT_CAD_MUTATION",
            "PARTIAL_BLOCKED",
            "eraseHandles+Draw+ProfileStore đã chứng minh; band geometry còn dài → chặn video",
            "E-VEMCD-DRAW;E-VEMCD-REDRAW;E-VEMCD-SERVICE",
        ),
        "BF-TRACAOD": (
            "TEMPORARY_CAD_MUTATION",
            "SOURCE_SUPPORTED",
            "Không read-only: DBText mark; Erase cuối vòng; crash giữa vòng có thể sót nhãn",
            "E-TRACAOD-QUERY;E-TRACAOD-MARK",
        ),
        "BF-ISURVEY_LOGOUT": (
            "AUTH_STATE",
            "SOURCE_SUPPORTED",
            "SignOut→LicenseStorage.Delete(license.dat); online release nền",
            "E-LOGOUT-CMD;E-LOGOUT-SIGNOUT;E-LOGOUT-STORAGE",
        ),
        "BF-TAOTUYEN": (
            "PERSISTENT_CAD_MUTATION",
            "SOURCE_SUPPORTED",
            "RouteStore.Save Xrecord DWG",
            "E-TAOTUYEN-NEW;E-TAOTUYEN-SAVE",
        ),
        "BF-TAOMOHINH": (
            "PERSISTENT_CAD_MUTATION",
            "SOURCE_SUPPORTED",
            "TerrainStore.Save Xrecord; chưa build mesh",
            "E-TAOMOHINH-NEW;E-TAOMOHINH-SAVE",
        ),
        "BF-THIETLAPCOC": (
            "FILE_WRITE",
            "SOURCE_SUPPORTED",
            "station-style.json AppData",
            "E-THIETLAPCOC;E-THIETLAPCOC-STORE",
        ),
        "BF-ISURVEY_DELETE_GE": (
            "PERSISTENT_CAD_MUTATION",
            "SOURCE_SUPPORTED",
            "GeDeleteWorkflow→RasterDeleteService.DeleteAll",
            "E-DELETE-GE",
        ),
        "BF-ISURVEY_MAP_SAT": (
            "PERSISTENT_CAD_MUTATION",
            "PARTIAL_BLOCKED",
            "HdMosaicWorkflow biên/zoom đã đọc; ghép tile/SaveJpeg sau L100 chưa hết lượt này",
            "E-MAPSAT-HD",
        ),
        "BF-ISURVEY_NUMBER_VERTICES": (
            "PERSISTENT_CAD_MUTATION",
            "SOURCE_SUPPORTED",
            "Entity số góc + AngleTableExport txt",
            "E-NUMBER-VERTICES;E-NUMBER-TXT",
        ),
        "BF-ISURVEY_UAV_SEND_BACK": (
            "PERSISTENT_CAD_MUTATION",
            "SOURCE_SUPPORTED",
            "DrawOrder persistent",
            "E-UAV-SENDBACK",
        ),
        "BF-ISURVEY_UAV_FRAME_TOGGLE": (
            "PERSISTENT_CAD_MUTATION",
            "SOURCE_SUPPORTED",
            "IMAGEFRAME NOD vars",
            "E-UAV-FRAME",
        ),
        "BF-BANGCOC": (
            "PERSISTENT_CAD_MUTATION",
            "PARTIAL_BLOCKED",
            "StationList Delete/Add/Zoom đã chứng minh; ListStations đầy đủ chưa hết",
            "E-XAML-STN-DEL;E-XAML-STN-ADD;E-XAML-STN-ZOOM",
        ),
        "BF-ISURVEY_MAP": (
            "PERSISTENT_CAD_MUTATION",
            "PARTIAL_BLOCKED",
            "MapInsert Apply/Delete gắn MAP/DELETE_GE; ApplyBasemap đầy đủ chưa hết",
            "E-XAML-MAP-APPLY;E-XAML-MAP-DELETE;E-DELETE-GE",
        ),
    }

    rows = []
    for f in user:
        bf = f["business_feature_id"]
        handler = f"{f['handler_file']}:{f['handler_line']} / {f['effective_method']}"
        services = f.get("types_in_handler") or ""
        if bf in manual:
            impact, disp, reason, evid = manual[bf]
            level = "STATIC_SOURCE_MANUAL"
            block = reason if disp != "SOURCE_SUPPORTED" else ""
            if disp == "SOURCE_SUPPORTED":
                block = ""
                # keep residual risk notes in evidence_codes note via reason only when blocked
            evid_out = evid if disp == "SOURCE_SUPPORTED" else evid
            # For SOURCE_SUPPORTED store short note in block_reason empty; put residual in evidence note field via reason appended for TRACAOD
            if bf == "BF-TRACAOD":
                block = "Residual: có thể sót DBText nếu crash giữa vòng Query (không chặn SOURCE_SUPPORTED)"
            elif disp == "SOURCE_SUPPORTED":
                block = ""
            else:
                block = reason
            note_reason = reason
        else:
            impact = "UNKNOWN"
            t = (services + f["lenh_chinh"]).upper()
            if any(k in t for k in ("EXPORT", "WRITE", "SAVE", "KML", "DGN", "CSV", "SCAN")):
                impact = "FILE_WRITE"
            elif any(
                k in t
                for k in (
                    "DELETE",
                    "DRAW",
                    "ROUTE",
                    "TERRAIN",
                    "FRAME",
                    "PROFILE",
                    "STATION",
                    "CLIP",
                    "MAP",
                    "DIEM",
                    "POINT",
                    "KHUNG",
                    "UAV",
                )
            ):
                impact = "PERSISTENT_CAD_MUTATION"
            elif any(k in t for k in ("LOGIN", "LOGOUT", "UPDATE", "ABOUT", "HDSD")):
                impact = "AUTH_STATE"
            disp = "PARTIAL_BLOCKED"
            level = "STATIC_INHERITED_PARTIAL"
            note_reason = "Chưa đối soát thủ công P0 trong lượt cuối; kế thừa R02C/R02D — chặn video"
            block = note_reason
            evid_out = f"R02D/TRACES/{bf}.md (tham chiếu; không nâng SOURCE_SUPPORTED)"
            if (R02D / "EVIDENCE" / f"USER-{bf}.md").exists():
                evid_out = f"R02D/EVIDENCE/USER-{bf}.md; {evid_out}"

        rows.append(
            {
                "business_feature_id": bf,
                "lenh_chinh": f["lenh_chinh"],
                "handler_service": f"{handler} | {services}",
                "impact_type": impact,
                "evidence_level": level,
                "disposition": disp,
                "block_reason": block,
                "evidence_codes": evid_out,
                "runtime": "NOT_TESTED",
                "aliases": ";".join(
                    x for x in [f.get("aliases_module"), f.get("aliases_CommandAliases")] if x
                ),
                "manual_note": note_reason if bf in manual else "",
            }
        )

    assert len(rows) == 52, len(rows)
    with (OUT / "02_FEATURE_DISPOSITION_52_USER.csv").open("w", encoding="utf-8-sig", newline="") as fh:
        w = csv.DictWriter(fh, fieldnames=list(rows[0].keys()))
        w.writeheader()
        w.writerows(rows)

    supported = [r for r in rows if r["disposition"] == "SOURCE_SUPPORTED"]
    blocked = [r for r in rows if r["disposition"] == "PARTIAL_BLOCKED"]
    blocked_ids = [r["business_feature_id"] for r in blocked]

    (OUT / "01_KET_LUAN_DOI_SOAT_CUOI.md").write_text(
        f"""# 01 — Kết luận đối soát thủ công cuối Phase 01

**Ngày:** {datetime.now(timezone.utc).date()}  
**HEAD:** `{HEAD}`  
**Kế thừa:** R01–R02D (không sao chép lại).  
**Runtime:** NOT_TESTED toàn bộ.

## P0

### 1. ISURVEY_DGN_TO_CAD — PARTIAL_BLOCKED
Chuỗi: `DgnModule` → `DgnImportService.Start` → `-DGNIMPORT` async → hậu xử lý.  
Tác động: PERSISTENT_CAD_MUTATION.  
Đủ: entry/Start/lỗi file. Thiếu: toàn bộ Idle/Finish/explode.  
Chứng cứ: `E-D2C-MODULE`, `E-D2C-START`.

### 2. ISURVEY_CAD_TO_DGN — SOURCE_SUPPORTED (FILE_WRITE)
**Không** lấy WriteMessage làm bằng chứng tạo file.  
Đã thấy: `File.Exists`/`File.Delete` ghi đè → `Editor.Command("_.-DGNEXPORT", …, OutputPath)` → `OnIdle` chờ `File.Exists && size>0`.  
Chứng cứ: `E-C2D-MODULE`, `E-C2D-OVERWRITE`, `E-C2D-RUNEXPORT`.

### 3. VEMCD / ISURVEY_PROFILE_DRAW — PARTIAL_BLOCKED
Đã chứng minh: eraseHandles cũ → `ProfileDrawService.Draw` tạo entity + Commit → `ProfileStore.Save`.  
Thiếu: geometry band còn lại. Chứng cứ: `E-VEMCD-DRAW`, `E-VEMCD-REDRAW`, `E-VEMCD-SERVICE`.

### 4. TRACAOD — SOURCE_SUPPORTED (TEMPORARY_CAD_MUTATION)
**Không read-only.** `ShowElevationMark` tạo DBText; `EraseElevationMark` cuối vòng. Crash giữa vòng có thể sót nhãn.  
Chứng cứ: `E-TRACAOD-QUERY`, `E-TRACAOD-MARK`.

### 5. ISURVEY_LOGOUT — SOURCE_SUPPORTED (AUTH_STATE)
`SignOut` → `LicenseStorage.Delete()` xóa `license.dat`; online release nền. Không trích token.  
Chứng cứ: `E-LOGOUT-CMD`, `E-LOGOUT-SIGNOUT`, `E-LOGOUT-STORAGE`.

### 6. Store/Delete
| Lệnh | Impact | Disposition | Evidence |
|---|---|---|---|
| TAOTUYEN | PERSISTENT_CAD_MUTATION | SOURCE_SUPPORTED | E-TAOTUYEN-* |
| TAOMOHINH | PERSISTENT_CAD_MUTATION | SOURCE_SUPPORTED | E-TAOMOHINH-* |
| THIETLAPCOC | FILE_WRITE | SOURCE_SUPPORTED | E-THIETLAPCOC-* |
| DELETE_GE | PERSISTENT_CAD_MUTATION | SOURCE_SUPPORTED | E-DELETE-GE |

### 7. Bằng chứng từng bị cắt
- VEMCD Draw: khôi phục `E-VEMCD-*` (không cắt giữa EraseHandles→Commit).
- MAP_SAT `HdMosaicWorkflow.Run`: `E-MAPSAT-HD` — PARTIAL_BLOCKED phần sau L100.
- NUMBER_VERTICES: `E-NUMBER-VERTICES`, `E-NUMBER-TXT` — SOURCE_SUPPORTED.

## P1

### 8. XAML 10 binding / 9 handler
Hai `Close_Click` StationList = một handler. MapInsert Delete gọi `GeDeleteWorkflow` (không CLOSE thuần). StationList Delete = `RemoveStation`. Vn2000 = helper đa lệnh. Evidence: `E-XAML-*`.

### 9. 10 USER thiếu evidence_refs R02D
Chính SC10 — đã gắn `E-*` trong CSV disposition.

### 10. Ma trận 52 USER
`02_FEATURE_DISPOSITION_52_USER.csv`: SOURCE_SUPPORTED={len(supported)}; PARTIAL_BLOCKED={len(blocked)}. Không nâng hồ sơ chưa đối soát thủ công.

## Đề xuất
**GO_FOR_PROMPT_MASTER_DESIGN_WITH_BLOCKED_FEATURES**  
Tính năng `PARTIAL_BLOCKED` cấm đưa vào kế hoạch quay đến cổng xác minh riêng. Đóng kiểm toán nguồn ≠ runtime CAD.
""",
        encoding="utf-8",
    )

    (OUT / "04_STOP_GATE_GIT.md").write_text(
        f"""# 04 — Stop gate & Git

## Thiếu sót còn tồn tại / BLOCKED cho video
{chr(10).join('- `'+i+'`' for i in blocked_ids)}

## Không được phép đưa vào video tự động
Mọi `disposition=PARTIAL_BLOCKED` trong `02_FEATURE_DISPOSITION_52_USER.csv`.

## Git
- HEAD trước: xem `_git_head_before.txt` (`{HEAD}`)
- HEAD sau: xem `_git_head_after.txt` (kỳ vọng trùng)
- Không sửa tracked `src/`
- Chỉ tạo `docs/audits/..._FINAL_MANUAL/` (+ ZIP)

## Đề xuất kết luận
**GO_FOR_PROMPT_MASTER_DESIGN_WITH_BLOCKED_FEATURES**

Lý do: P0 đã có bằng chứng hoặc BLOCKED rõ; 52 USER có trạng thái riêng; gap liệt kê; runtime NOT_TESTED. Chủ dự án quyết định chấp nhận ngoại lệ để thiết kế Prompt Master.

**DỪNG — không mở R02E/R02F.**
""",
        encoding="utf-8",
    )

    print("user", len(rows), "supported", len(supported), "blocked", len(blocked), "evidence", len(list(EV.glob("E-*.md"))))


if __name__ == "__main__":
    main()
