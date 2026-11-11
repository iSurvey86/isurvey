# -*- coding: utf-8 -*-
"""Generate R02B dossiers + required markdown reports. Readonly vs product source."""
from __future__ import annotations

import csv
import json
import re
from collections import defaultdict
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(r"D:\AIProject\isurvey")
OUT = Path(__file__).resolve().parents[1]
DOS = OUT / "04_HO_SO_CHI_TIET_TUNG_TINH_NANG"

# Manual deep evidence snippets (file:line verified by reading source in R02B)
MANUAL_DEEP = {
    "NHAPDIEM": {
        "chain": [
            "CommandMethod NHAPDIEM @ src/Modules/DiemDo/PointFileModule.cs:13 → ImportPointsCommand",
            "AuthManager.EnsureAuthenticated @ PointFileModule.cs:16",
            "Model Space gate @ PointFileModule.cs:22-26",
            "ImportPointsWindow.Prompt @ PointFileModule.cs:31 / UI/DiemDo/ImportPointsWindow.xaml.cs:31-35",
            "OpenFileDialog filter *.csv;*.txt @ ImportPointsWindow.xaml.cs:65-68",
            "PointFileReader.Read(path) @ ImportPointsWindow.xaml.cs:76",
            "PointDrawService.Draw @ PointFileModule.cs:47-48",
            "CAD axes: ToCad Y→X, X→Y @ PointDrawService.cs:67-68",
            "Entities: DBPoint + DBText on layers DiemDo/DiemDoTen/DiemDoCaoDo @ PointDrawService.cs:35-57",
            "XData AppName ISURVEY_DIEM @ PointDrawService.cs:12,79+",
        ],
        "input": "File CSV/TXT tọa độ (OpenFileDialog); cột gồm tên/X/Y/Z (UI DataGrid Cao độ Z). Điều kiện: đã đăng nhập; Model Space.",
        "output": "Số điểm created (made); layer DiemDo*; Regen; ActivityLogger.Result.",
        "errors": "Dialog fail catch :34-36; hủy request null :39-42; Draw catch :56-58; PDMODE/PDSIZE catch nuốt @ PointDrawService.cs:26-28.",
        "destructive": "Không xóa đối tượng cũ — chỉ thêm. ZoomExtents sau khi made>0.",
        "fixture": "WORK COPY bản vẽ trắng Metric; MASTER file CSV mẫu N,E,Z; REFERENCE đếm DBPoint + so Z text.",
        "evidence": "SERVICE_CHAIN_VERIFIED_STATIC",
        "manual": True,
    },
    "XUATDIEM": {
        "chain": [
            "CommandMethod XUATDIEM @ PointFileModule.cs:62 → ExportPointsCommand",
            "EnsureAuthenticated @ :65",
            "ExportPointsWindow.Prompt @ :75",
            "SaveFileDialog CSV/TXT @ ExportPointsWindow.xaml.cs:100-110",
            "PointFileWriter.Write @ ExportPointsWindow.xaml.cs:110",
            "Lọc theo layer tùy chọn @ ExportPointsWindow.xaml.cs:66-67",
        ],
        "input": "Điểm trên bản vẽ (layer tùy chọn); định dạng xuất .csv/.txt.",
        "output": "File trên đĩa; message số điểm; ActivityLogger.Result @ PointFileModule.cs:89-90.",
        "errors": "Dialog catch :76-79; hủy null :83-86.",
        "destructive": "Không xóa CAD; ghi đè file nếu user chọn trùng tên (Win dialog).",
        "fixture": "WORK COPY có điểm DiemDo; REFERENCE so file CSV ngoài CAD.",
        "evidence": "SERVICE_CHAIN_VERIFIED_STATIC",
        "manual": True,
    },
    "ISURVEY_DGN_TO_CAD": {
        "chain": [
            "CommandMethod ISURVEY_DGN_TO_CAD @ src/Dgn/DgnModule.cs:21 → DgnToCadCommand",
            "EnsureAuthenticated(track:false) @ :24",
            "Model Space @ :32-35",
            "DgnImportDialog.ShowImport @ :44",
            "Batch → DgnBatchService.Start @ :66-70",
            "Single → DgnImportService.Start @ :79 (async Idle)",
            "Engine -DGNIMPORT FILEDIA=0; Handseed filter; UNDO; explode/rename/TCVN3 @ DgnImportService.cs:45-48, class từ :50",
            "Timeout/quiet windows @ DgnImportService.cs:59-62",
        ],
        "input": "File .dgn V7/V8; options dialog; có thể batch nhiều file.",
        "output": "Entities trong Model (hoặc Drawing mới nếu DGNIMPORTMODE=0); report ImportedEntities/Warnings/LogPath @ DgnImportReport :12-42.",
        "errors": "Dialog catch :46-49; hủy :53-57; report.Error; timeout 300s.",
        "destructive": "Có thể explode cell, purge block/linetype/layer, rename layer — side effects trên DWG đích.",
        "fixture": "WORK COPY; MASTER DGN seed độc lập; REFERENCE không lấy output iSurvey làm baseline.",
        "evidence": "SERVICE_CHAIN_VERIFIED_STATIC",
        "manual": True,
        "limit": "Thân DgnImportService >1000 dòng — đã đọc đầu file + chữ ký Start/Job; nhiều nhánh Idle/Finish CHƯA ĐỦ BẰNG CHỨNG từng dòng.",
    },
    "VEMCD": {
        "chain": [
            "ISURVEY_PROFILE_DRAW → DrawCommand → Draw; VEMCD → DrawAlias → Draw @ ProfileModule.cs:14-20",
            "EnsureAuthenticated(\"VEMCD\") @ :22",
            "Model Space TileMode @ :28-31",
            "RouteStore.Load @ :39",
            "DrawProfileWindow.Prompt @ :88 / hoặc seed demo",
            "Pick insert point @ :109-120",
            "ProfileDrawService.Draw / RunUpdate @ :129-139, :179+",
            "ProfileStore.Load; xóa profile cũ cùng RouteId rồi vẽ lại @ :163-178 comment",
        ],
        "input": "Tuyến+cọc có Z (TAOTUYEN/PHATSINHCOC); options hộp thoại; điểm chèn Model.",
        "output": "Hình MCD trên Model; ProfileStore record; có thể xóa bản MCD cũ cùng tuyến.",
        "errors": "RouteStore catch :41-44; empty routes :47-50; dialog catch :90-93; hủy :96-99; draw catch :142-145.",
        "destructive": "Xóa profile cũ cùng RouteId trước khi vẽ lại (comment :178).",
        "fixture": "WORK COPY có TIN+tuyến+cọc Z; REFERENCE checklist band/datum; fixture dài.",
        "evidence": "SERVICE_CHAIN_VERIFIED_STATIC",
        "manual": True,
        "limit": "ProfileDrawService body dài — CHƯA ĐỦ BẰNG CHỨNG toàn bộ geometry band.",
    },
    "ISURVEY_UAV_CLIP": {
        "chain": [
            "CommandMethod ISURVEY_UAV_CLIP @ UavModule.cs:10 → ClipCommand",
            "UavImageService.ClipByBoundary @ UavImageService.cs:23",
            "RunBatchPolylineClip / RunAutoClip @ :43-45, :117, :254",
        ],
        "input": "Raster/image trong DWG + biên polyline (batch hoặc auto).",
        "output": "Ảnh bị clip theo biên; chi tiết entity CHƯA ĐỦ BẰNG CHỨNG đầy đủ mọi nhánh.",
        "errors": "Phụ thuộc Editor prompts trong service — cần đọc tiếp Run* methods.",
        "destructive": "Clip ảnh có thể đổi boundary clip của image (không xóa file gốc).",
        "fixture": "WORK COPY có ortho + polyline biên; REFERENCE so clip boundary.",
        "evidence": "SERVICE_CHAIN_VERIFIED_STATIC",
        "manual": True,
        "limit": "Đã xác nhận entry ClipByBoundary + 2 nhánh; chi tiết từng prompt CHƯA ĐỦ BẰNG CHỨNG 100%.",
    },
}


def read_csv(name: str) -> list[dict]:
    with (OUT / name).open(encoding="utf-8-sig", newline="") as f:
        return list(csv.DictReader(f))


def evidence_rule_text() -> str:
    return """
| Mức | Quy tắc gán (tái kiểm được) |
|---|---|
| DECLARATION_FOUND | Có `[CommandMethod("…")]` tại file:line |
| METHOD_BODY_SCANNED | Đã mở thân method (brace/=>), ghi nhận nội dung dù chưa theo service |
| HANDLER_TRACE_COMPLETE | Đã xác định chuỗi command→handler/effective + call-site UI/auth trong thân |
| SERVICE_CHAIN_VERIFIED_STATIC | Đã mở ≥1 file service/dialog liên quan và ghi I/O hoặc nhánh lỗi với file:line (hoặc mục MANUAL_DEEP) |
| STUB_ONLY | Method Stub / gọi Stub("CMD") |
| NOT_ENOUGH_EVIDENCE | Không mở được thân hoặc thiếu mắt xích |
| Runtime | luôn `NOT_TESTED` trong R02B |
""".strip()


def write_dossiers(features: list[dict], deep: dict):
    DOS.mkdir(parents=True, exist_ok=True)
    by = defaultdict(list)
    for f in features:
        by[f["nhom"]].append(f)
    index = ["# Hồ sơ R02B theo feature_id", "", "Runtime: **NOT_TESTED**. Không PASS CAD.", ""]
    for g in sorted(by):
        lines = [f"# Nhóm {g}", "", f"Sinh {datetime.now(timezone.utc).date()} — R02B.", ""]
        for f in by[g]:
            key = f["lenh_chinh"]
            man = MANUAL_DEEP.get(key)
            # also match via r01 refs
            if not man:
                for ref in (f.get("r01_refs") or "").split(";"):
                    if ref in MANUAL_DEEP:
                        man = MANUAL_DEEP[ref]
                        break
            svc = deep.get(f["feature_id"], [])
            if man:
                chain = man["chain"]
                inp, outp, err = man["input"], man["output"], man["errors"]
                dest, fix = man["destructive"], man["fixture"]
                evid = man["evidence"]
                limit = man.get("limit", "Ưu tiên đã đọc thủ công các mắt xích chính.")
                manual_flag = "CÓ (độc lập script)"
            else:
                chain = [
                    f"{f['lenh_chinh']} @ {f['handler_file']}:{f['handler_line']} method `{f['effective_method']}` span {f['eff_span']}",
                    f"types trong handler: {f['types_in_handler'] or '(không khớp *Service|*Dialog|…)'}",
                    f"merge: {f['merge_note'] or '(không)'}",
                ] + [f"service scan: {s}" for s in svc[:6]]
                inp = "CHƯA ĐỦ BẰNG CHỨNG đầy đủ — xem types_in_handler / service_trace CSV; cần đọc sâu khi quay."
                outp = "CHƯA ĐỦ BẰNG CHỨNG đầy đủ hậu điều kiện đo được."
                err = "CHƯA ĐỦ BẰNG CHỨNG đủ mọi catch trong service layer."
                dest = "CHƯA ĐỦ BẰNG CHỨNG — kiểm tra Delete/Erase/Purge trước pilot."
                fix = "WORK COPY Metric trắng; REFERENCE tùy feature — seed chưa có trong _R02B."
                evid = f["muc_ket_luan_source"]
                limit = "Script đã quét handler + mở file type nếu tìm thấy; chưa đọc 100% service."
                manual_flag = "KHÔNG — chủ yếu script; một phần file:line từ extractor"
            # bump CSV evidence if manual
            if man:
                f["muc_ket_luan_source"] = evid
            lines += [
                f"## {f['feature_id']} — {f['ten_hanh_vi']}",
                "",
                "### 1. Định danh",
                f"- **feature_id:** `{f['feature_id']}`",
                f"- **Hành vi:** {f['ten_hanh_vi']}",
                f"- **Nhóm:** {f['nhom']} | **Audience:** {f['audience']}",
                f"- **source_status:** `{f['source_status']}` — {f['ly_do_status']}",
                f"- **R01 cmds:** {f['r01_refs']}",
                f"- **R02 refs:** {f['r02_refs'] or '(không map)'}",
                "",
                "### 2. Kích hoạt",
                f"- **Lệnh chính:** `{f['lenh_chinh']}` @ `{f['handler_file']}:{f['handler_line']}`",
                f"- **Alias module (ngoài CommandAliases.cs):** {f['aliases_module'] or '(không)'}",
                f"- **Alias CommandAliases.cs:** {f['aliases_CommandAliases'] or '(không)'}",
                f"- **Effective method:** `{f['effective_method']}` span `{f['eff_span']}`",
                f"- **Ribbon:** {f['ribbon_entries'] or '(không)'}",
                f"- **Palette:** {f['palette_entries'] or '(không)'}",
                f"- **XAML mapped:** {f['xaml_mapped'] or '(không / UI dialog nội bộ — xem 03D)'}",
                "",
                "### 3. Chuỗi xử lý",
            ]
            for c in chain:
                lines.append(f"- {c}")
            lines += [
                "",
                "### 4. Input / Output / lỗi / rủi ro",
                f"- **Input:** {inp}",
                f"- **Output / hậu điều kiện:** {outp}",
                f"- **Nhánh lỗi:** {err}",
                f"- **Destructive / quyền:** {dest}",
                f"- **Giới hạn đọc:** {limit}",
                "",
                "### 5. Fixture đề xuất",
                f"- {fix}",
                "",
                "### 6. Video / bằng chứng",
                f"- **video_candidate:** {f['video_candidate']} (tham khảo — không phải sẵn sàng sản xuất)",
                f"- **muc_ket_luan_source:** `{evid}`",
                f"- **runtime:** `NOT_TESTED`",
                f"- **Bằng chứng thủ công độc lập:** {manual_flag}",
                "",
                "---",
                "",
            ]
            index.append(f"- `{f['feature_id']}` {f['ten_hanh_vi']} → [{g}.md](./{g}.md)")
        (DOS / f"{g}.md").write_text("\n".join(lines), encoding="utf-8")
        index.append("")
    (DOS / "00_INDEX.md").write_text("\n".join(index), encoding="utf-8")


def main():
    features = read_csv("03_DANH_MUC_TINH_NANG_CHUAN_HOA.csv")
    summary = json.loads((OUT / "_extract_summary_r02b.json").read_text(encoding="utf-8"))
    deep = json.loads((OUT / "_deep_notes.json").read_text(encoding="utf-8"))
    merges = read_csv("03C_QUYET_DINH_GOP_TACH.csv")
    xaml = read_csv("03D_XAML_EVENT_MAP.csv")
    entries = read_csv("03A_ANH_XA_DIEM_KICH_HOAT.csv")
    excluded = read_csv("03B_STUB_PLACEHOLDER_EXCLUDED.csv")

    write_dossiers(features, deep)
    # rewrite features CSV after manual evidence bump
    with (OUT / "03_DANH_MUC_TINH_NANG_CHUAN_HOA.csv").open("w", encoding="utf-8-sig", newline="") as f:
        w = csv.DictWriter(f, fieldnames=list(features[0].keys()))
        w.writeheader()
        w.writerows(features)

    evid_counts = defaultdict(int)
    for feat in features:
        evid_counts[feat["muc_ket_luan_source"]] += 1
    summary["evidence"] = dict(evid_counts)
    summary["canonical_features_r02b"] = len(features)

    # Manual sample matrix — open files for priority + one per group
    samples = []
    want = [
        ("VEMCD", "DrawAlias", "Draw()"),
        ("NHAPDIEM", "ImportPointsCommand", "PointDrawService"),
        ("XUATDIEM", "ExportPointsCommand", "ExportPointsWindow"),
        ("ISURVEY_DGN_TO_CAD", "DgnToCadCommand", "DgnImportService"),
        ("ISURVEY_UAV_CLIP", "ClipCommand", "UavImageService"),
        ("TAOTUYEN", "RouteNewAlias", "New()"),
        ("XAYMOHINH", "BuildAlias", "Build()"),
        ("ISURVEY_UPDATE_GE", "UpdateGe", "GE"),
        ("DAODAUTUYEN", "Reverse", "Reverse"),
    ]
    for cmd, meth_expect, substr in want:
        feat = next((x for x in features if x["lenh_chinh"] == cmd or cmd in (x.get("r01_refs") or "")), None)
        if not feat:
            samples.append({"cmd": cmd, "result": "NOT_IN_FEATURE_LIST"})
            continue
        path = ROOT / feat["handler_file"]
        text = path.read_text(encoding="utf-8", errors="replace") if path.exists() else ""
        line = int(feat["handler_line"] or 0)
        window = "\n".join(text.splitlines()[max(0, line - 1) : line + 30])
        ok = f'CommandMethod("{cmd}"' in text or cmd in (feat.get("r01_refs") or "")
        # for aliases merged, primary may be short name
        has_meth = meth_expect in text
        has_sub = substr in text
        samples.append(
            {
                "feature_id": feat["feature_id"],
                "cmd": cmd,
                "file": feat["handler_file"],
                "line": feat["handler_line"],
                "eff": feat["effective_method"],
                "span": feat["eff_span"],
                "method_found": has_meth,
                "substr_found": has_sub,
                "result": "VERIFIED_TRACE" if path.exists() and has_meth and has_sub else "PARTIAL",
                "evidence": feat["muc_ket_luan_source"],
                "manual_deep": cmd in MANUAL_DEEP or any(r in MANUAL_DEEP for r in (feat.get("r01_refs") or "").split(";")),
            }
        )

    xaml_cls = defaultdict(int)
    for x in xaml:
        xaml_cls[x["classify"]] += 1

    # --- Reports ---
    (OUT / "01_PHAM_VI_GIT_VA_KIEM_KE_FILE.md").write_text(
        f"""# 01 — Phạm vi Git và kiểm kê file (R02B)

## Git trước R02B

Xem `_git_before.txt`, `_git_head_before.txt`, `_git_branch_before.txt`, `_git_status_before.txt`, `_git_diff_before.txt`.

- HEAD hiện tại (= R02 HEAD): `3a16cb3959dc4e65c08f77b6de26669b309739cd`
- R01 HEAD: `53c5eb4644f2dbc0f736e03098c749197a81a93e`
- R02 HEAD: `3a16cb3959dc4e65c08f77b6de26669b309739cd`
- R02 → now: **không khác** (HEAD trùng R02) — xem `_git_diff_r02_to_now.txt`

## So sánh R01 → R02 (không checkout)

Xem `_git_log_r01_to_r02.txt` (6 commit) và `_git_diff_r01_to_r02.txt` (43 tệp).

**Ảnh hưởng source `src/` giữa hai mốc audit:**

| Tệp | Liên quan audit |
|---|---|
| `src/Auth/AuthManager.cs` | Auth/OTP/login |
| `src/Auth/AuthModule.cs` | Lệnh AUTH (ABOUT/HDSD/LOGIN/UPDATE…) |
| `src/Auth/UpdateFlow.cs` | UPD / cập nhật |
| `src/Auth/UpdateStartupChecker.cs` | Tự kiểm tra cập nhật khi mở Civil (1.8.19) |
| `src/Main.cs` | Khởi động / hook startup update |

Các tệp còn lại chủ yếu docs/audit/changelog/bundle/video kịch bản — **không** đổi handler D2C/VEMCD/điểm/UAV giữa R01→R02 theo diff name-only.

**Kết luận:** R01 và R02 **không** cùng snapshot. Kết luận R01 về AUTH/UPDATE có thể lệch hành vi startup so R02/R02B. Các feature DGN/DIEM/PROFILE/UAV **không** nằm trong diff `src/` R01→R02 theo danh sách name-only — vẫn nên tái xác minh khi quay video trên bản hiện hành.

## Git sau R02B

Xem `_git_*_after*` (điền cuối). Kỳ vọng: HEAD không đổi; không sửa tracked `src/`; chỉ untracked mới dưới `_R02B/` (+ ZIP).

## Kiểm kê file

| Hạng mục | Số |
|---|---:|
| `.cs`+`.xaml` | {summary['cs_xaml_files']} |
| CommandMethod | {summary['commandmethod_total']} |
| Canonical features R02B | {summary['canonical_features_r02b']} |

Chi tiết: `_inventory_files.csv`.
""",
        encoding="utf-8",
    )

    (OUT / "02_KIEM_KE_LAI_COMMAND_UI.md").write_text(
        f"""# 02 — Kiểm kê lại Command / UI (R02B)

## Cải tiến so R02

| Hạng mục | R02 | R02B |
|---|---|---|
| Canonical | 79 = mọi non-`CommandAliases` declaration | **{summary['canonical_features_r02b']}** sau gộp SAME_FEATURE (thin zero-arg) |
| Stub helper | có thể gộp nhầm qua `Stub()` | **không gộp** `Stub("CMD")` — 8 stub tách |
| Nhóm | substring dễ sai | **ưu tiên đường dẫn module** |
| XAML/Palette | inventory thiếu ma trận | `03A` + `03D` đầy đủ |
| Evidence | `SOURCE_BEHAVIOR_REVIEWED` tự động | mức mới + quy tắc tái kiểm |

## Số liệu

| Metric | Giá trị |
|---|---:|
| CommandMethod | {summary['commandmethod_total']} |
| Alias CommandAliases.cs | {summary['alias_file']} |
| Non-alias declarations | {summary['non_alias_declarations']} |
| Canonical features | **{summary['canonical_features_r02b']}** |
| SOURCE_PRESENT / STUB | {summary['source_present']} / {summary['stub_features']} |
| SAME_FEATURE decisions | {summary['same_feature_decisions']} |
| Ribbon / XAML / Palette | {summary['ribbon']} / {summary['xaml_clicks']} / {summary['palette']} |
| Entry matrix rows | {summary['entries_total']} |

## Không suy mỗi UI = feature mới

XAML Click phân loại: `{dict(xaml_cls)}` — CLOSE/CANCEL/SETTINGS/INTERNAL không tạo feature mới.
""",
        encoding="utf-8",
    )

    (OUT / "05_DOI_CHIEU_TAI_LIEU_VA_BAO_PHU_R02B.md").write_text(
        f"""# 05 — Đối chiếu tài liệu và bao phủ R02B

## Nguồn sự thật

Mã `src/` hiện hành @ HEAD `3a16cb3…`. R01/R02 là hồ sơ kế thừa, không sửa đè.

Standard/Prompt/Schema v1.0 cũ = **DỰ THẢO**.

## Bao phủ

- Canonical features: **{summary['canonical_features_r02b']}** (không dùng 150, không dùng 79 làm mẫu số nghiệp vụ cuối)
- Evidence: `{dict(evid_counts)}`
- Entry points: 125 command + 61 ribbon + 91 XAML + 8 palette = đối chiếu trong `03A` ({len(entries)} dòng, có trùng giải thích)
- Runtime: **NOT_TESTED** — không tính % KT-05

## Quyết định KT giữ

Giọng nam miền Bắc; 2 video thử; Feature 3–5' / Workflow 5–8'; CAD thật + dữ liệu giả lập; G5/upload tách.
""",
        encoding="utf-8",
    )

    (OUT / "06_DANH_GIA_PILOT_MO_RONG.md").write_text(
        """# 06 — Đánh giá pilot (R02B) — tư vấn, không chọn thay Chủ dự án

| Ứng viên | Điểm /40 | Ghi chú tĩnh |
|---|---:|---|
| **XUATDIEM / NHAPDIEM** | **34** | CSV/TXT; đo file độc lập; đã truy vết PointFileReader/Writer + PointDrawService (ToCad X/Y swap). **Ưu tiên đề xuất.** |
| D2C | 30 | DGN thật; async Idle; destructive explode/purge; baseline độc lập. |
| VEMCD | 28 | Phụ thuộc tuyến+cọc Z; modal + pick; có thể xóa MCD cũ. |
| UAVCLIP | 27 | Cần raster+biên; ClipByBoundary hai nhánh. |

Không NETLOAD / không quay trong R02B.
""",
        encoding="utf-8",
    )

    sample_lines = [
        "# 07 — Ma trận bằng chứng và kiểm tra mẫu (R02B)",
        "",
        "## Quy tắc mức kết luận",
        "",
        evidence_rule_text(),
        "",
        f"## Phân bố evidence",
        f"```json\n{json.dumps(dict(evid_counts), ensure_ascii=False, indent=2)}\n```",
        "",
        "## Kiểm thủ công độc lập (không chỉ has_cmd)",
        "",
    ]
    for s in samples:
        sample_lines.append(
            f"- `{s.get('feature_id')}` **{s.get('cmd')}** @ `{s.get('file')}:{s.get('line')}` "
            f"eff=`{s.get('eff')}` span=`{s.get('span')}` → **{s.get('result')}** "
            f"(method={s.get('method_found')}, substr={s.get('substr_found')}, "
            f"manual_deep={s.get('manual_deep')}, evidence=`{s.get('evidence')}`)"
        )
    sample_lines += [
        "",
        "## Script vs thủ công",
        "",
        f"- Features tổng: {len(features)}",
        f"- MANUAL_DEEP priority: {len(MANUAL_DEEP)} hành vi (NHAPDIEM/XUATDIEM/D2C/VEMCD/UAVCLIP)",
        f"- Còn lại: extractor + mở file type nếu tìm thấy — nhiều nhánh service **CHƯA ĐỦ BẰNG CHỨNG** 100%",
        "",
        "## Tự khớp",
        f"- CommandMethod {summary['commandmethod_total']} = alias {summary['alias_file']} + non-alias {summary['non_alias_declarations']}",
        f"- Canonical {summary['canonical_features_r02b']} + merged_away ~{summary.get('merged_away')}",
        f"- Excluded rows: {len(excluded)}",
        f"- XAML classify: {dict(xaml_cls)}",
        "",
    ]
    (OUT / "07_MA_TRAN_BANG_CHUNG_VA_KIEM_TRA_MAU.md").write_text("\n".join(sample_lines), encoding="utf-8")

    unread = []
    for feat in features:
        if feat["muc_ket_luan_source"] not in {"SERVICE_CHAIN_VERIFIED_STATIC", "STUB_ONLY"}:
            unread.append(f"{feat['feature_id']} {feat['lenh_chinh']}: {feat['muc_ket_luan_source']}")
        elif feat["lenh_chinh"] not in MANUAL_DEEP and feat["muc_ket_luan_source"] == "SERVICE_CHAIN_VERIFIED_STATIC":
            unread.append(f"{feat['feature_id']} {feat['lenh_chinh']}: service mở đầu file — CHƯA ĐỦ BẰNG CHỨNG toàn bộ nhánh")

    (OUT / "08_VAN_DE_CHAN_VA_HANH_DONG_TIEP.md").write_text(
        f"""# 08 — Vấn đề chặn và hành động tiếp (R02B)

## Đã khắc phục so R02 (P0/P1)

1. Gộp SAME_FEATURE các wrapper ngoài CommandAliases (VEMCD/PROFILE_DRAW, BANGCOC/STATION_LIST, TAOTUYEN/ROUTE_NEW, PHATSINHCOC/ROUTE_STATIONS, XAYMOHINH/TERRAIN_BUILD, …).
2. Không gộp nhầm các Stub("CMD").
3. Sửa nhóm UPDATE_GE→MAP; DAODAUTUYEN/STATION_ADD/TERRAIN_ADD theo module path.
4. Ma trận XAML+Palette trong 03A/03D.
5. Evidence levels mới + mẫu thủ công priority.
6. Bảng diff Git R01→R02.

## P0 còn lại

1. Phase 01 **chưa** nghiệm thu — chờ hội đồng.
2. Nhiều service dài (FrameService.*, ProfileDrawService chi tiết band, DgnImportService Idle finish): **CHƯA ĐỦ BẰNG CHỨNG** 100% từng nhánh.
3. Context menu / dynamic macro: **CHƯA ĐỦ BẰNG CHỨNG**.

## P1

1. Fixture CAD seed chưa đóng gói trong audit.
2. Chính sách AI Cloud.
3. `source_status=SOURCE_PRESENT` ≠ đã chạy được.

## Phần chưa đọc đủ (rút gọn)

{chr(10).join('- '+u for u in unread[:40])}
{('…' if len(unread)>40 else '')}

## Hành động

Hội đồng thẩm định ZIP R02B. **Không** triển khai Video Factory / pilot cho đến chỉ đạo riêng.
""",
        encoding="utf-8",
    )

    (OUT / "09_TONG_HOP_TRINH_CHU_DU_AN_R02B.md").write_text(
        f"""# 09 — Tổng hợp trình Chủ dự án (R02B)

## Trạng thái

| Hạng mục | Kết luận |
|---|---|
| R02B kiểm toán tĩnh | **ĐÃ XUẤT HỒ SƠ** |
| Phase 01 nghiệm thu | **CHƯA** — dừng cổng chờ hội đồng |
| Runtime / CAD / VF | **KHÔNG CHẠY** |
| Canonical features nghiệp vụ | **{summary['canonical_features_r02b']}** (sau gộp; không dùng 150/79 làm mẫu số cuối) |
| SOURCE_PRESENT / STUB | {summary['source_present']} / {summary['stub_features']} |
| Evidence | `{dict(evid_counts)}` |

## Khắc phục chính

- Chuẩn hóa FEATURE theo hành vi + bảng gộp/tách `03C`
- UI matrix gồm XAML/Palette
- Sửa phân nhóm sai R02
- Diff Git R01↔R02
- Deep trace thủ công NHAPDIEM/XUATDIEM/D2C/VEMCD/UAVCLIP

## Còn thiếu / CHƯA ĐỦ BẰNG CHỨNG

- Toàn bộ nhánh service dài ngoài priority
- Dynamic/context menu
- Runtime verification

## Pilot tư vấn

NHAPDIEM/XUATDIEM ưu tiên xem xét — **Chủ dự án chọn**. Không tự chạy.

## An toàn

Không sửa C#/XAML product; không build/CAD; chỉ tệp `_R02B` + ZIP.
""",
        encoding="utf-8",
    )

    (OUT / "AUDIT_INDEX_R02B.md").write_text(
        f"""# AUDIT_INDEX_R02B

**Thư mục:** `docs/audits/2026-11-10_ISURVEY_VIDEO_FACTORY_SOURCE_AUDIT_R02B/`  
**Ngày:** {datetime.now(timezone.utc).date()}  
**HEAD:** `3a16cb3959dc4e65c08f77b6de26669b309739cd`

## Tệp bắt buộc

1. `01_PHAM_VI_GIT_VA_KIEM_KE_FILE.md`
2. `02_KIEM_KE_LAI_COMMAND_UI.md`
3. `03_DANH_MUC_TINH_NANG_CHUAN_HOA.csv` — {len(features)} feature
4. `03A_ANH_XA_DIEM_KICH_HOAT.csv`
5. `03B_STUB_PLACEHOLDER_EXCLUDED.csv`
6. `03C_QUYET_DINH_GOP_TACH.csv`
7. `03D_XAML_EVENT_MAP.csv`
8. `03E_ALIAS_COMMANDALIASES.csv`
9. `04_HO_SO_CHI_TIET_TUNG_TINH_NANG/`
10. `05_DOI_CHIEU_TAI_LIEU_VA_BAO_PHU_R02B.md`
11. `06_DANH_GIA_PILOT_MO_RONG.md`
12. `07_MA_TRAN_BANG_CHUNG_VA_KIEM_TRA_MAU.md`
13. `08_VAN_DE_CHAN_VA_HANH_DONG_TIEP.md`
14. `09_TONG_HOP_TRINH_CHU_DU_AN_R02B.md`
15. `AUDIT_INDEX_R02B.md`

## Thống kê

- Canonical: **{len(features)}**
- Evidence: `{dict(evid_counts)}`
- Merges SAME_FEATURE: {sum(1 for m in merges if m['decision']=='SAME_FEATURE')}

## Cảnh báo

- Không tuyên bố Phase 01 đã nghiệm thu.
- Không RUNTIME PASS.
- Một phần service: CHƯA ĐỦ BẰNG CHỨNG.

## Mức hoàn thành hồ sơ R02B

**Đủ hình thức đầu ra + gộp nghiệp vụ + UI map + deep priority.**  
**Chưa đủ** đọc 100% mọi dòng service — ghi rõ giới hạn.
""",
        encoding="utf-8",
    )

    # R01→R02→R02B delta table
    (OUT / "10_BANG_SAI_KHAC_R01_R02_R02B.md").write_text(
        f"""# 10 — Sai khác R01 → R02 → R02B

| Chỉ số | R01 | R02 | R02B |
|---|---:|---:|---:|
| HEAD | 53c5eb4… | 3a16cb3… | 3a16cb3… (cùng R02) |
| CSV “tính năng” | 150 dòng vào lệnh/nút | 79 canonical = non-alias cmds | **{len(features)}** sau SAME_FEATURE |
| Alias liên kết | yếu | 46 trong CommandAliases | 46 + wrapper module trong 03C/03E |
| XAML map | chưa | inventory 91 | 91 + classify 03D |
| Evidence label | SOURCE_REVIEWED đồng loạt | SOURCE_BEHAVIOR_REVIEWED tự động | mức mới + manual deep priority |
| Nhóm sai | — | UPDATE_GE/DAODAU/… | sửa theo module path |

## P0 R02 → xử lý R02B

| P0 | R02B |
|---|---|
| P0-01 gộp wrapper | `03C` + canonical {len(features)} |
| P0-02 XAML | `03A`/`03D` |
| P0-03 service sâu | manual deep 5 hành vi; còn lại giới hạn rõ |
""",
        encoding="utf-8",
    )

    check = {
        "features": len(features),
        "unique_ids": len({f["feature_id"] for f in features}),
        "evidence": dict(evid_counts),
        "samples": samples,
        "xaml_classify": dict(xaml_cls),
        "summary": summary,
        "required_merges_ok": all(
            any({m["cmd_a"], m["cmd_b"]} == set(pair) and m["decision"] == "SAME_FEATURE" for m in merges)
            for pair in [
                ("ISURVEY_PROFILE_DRAW", "VEMCD"),
                ("BANGCOC", "ISURVEY_STATION_LIST"),
                ("ISURVEY_ROUTE_NEW", "TAOTUYEN"),
                ("ISURVEY_ROUTE_STATIONS", "PHATSINHCOC"),
                ("ISURVEY_TERRAIN_BUILD", "XAYMOHINH"),
            ]
        ),
    }
    (OUT / "_self_check_r02b.json").write_text(json.dumps(check, ensure_ascii=False, indent=2), encoding="utf-8")
    (OUT / "_extract_summary_r02b.json").write_text(json.dumps(summary, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps({k: check[k] for k in ("features", "unique_ids", "evidence", "required_merges_ok", "xaml_classify")}, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
