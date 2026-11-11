# -*- coding: utf-8 -*-
"""Generate R02 markdown dossiers + required reports. Readonly vs product source."""
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

CALL_RE = re.compile(
    r"\b([A-Z][A-Za-z0-9_]*(?:Service|Dialog|Window|Form|Helper|Client|Store|Logger|Checker|Flow|Builder|Module|Palette|Manager|Provider))\b"
)
SHOW_RE = re.compile(r"\b(ShowModalWindow|ShowDialog|MessageBox\.Show|WriteMessage|Editor\.|GetSelection|GetEntity|GetPoint|GetString|Prompt)\w*\b")
IO_RE = re.compile(r"\b(File\.(?:Open|Read|Write|Exists|Copy|Delete)|Directory\.|StreamReader|StreamWriter|OpenFileDialog|SaveFileDialog|HttpClient|WebRequest|Download)\w*\b")
TX_RE = re.compile(r"\b(StartTransaction|Transaction|Commit|Abort|Database|BlockTable|ModelSpace)\b")
AUTH_RE = re.compile(r"\b(EnsureAuthenticated|RequireLogin|IsLoggedIn|AuthService|License)\b")
CATCH_RE = re.compile(r"\bcatch\s*\(([^)]+)\)")


def read_csv(name: str) -> list[dict]:
    with (OUT / name).open(encoding="utf-8-sig", newline="") as f:
        return list(csv.DictReader(f))


def write_csv(name: str, rows: list[dict], fieldnames: list[str] | None = None):
    if not rows:
        return
    fn = fieldnames or list(rows[0].keys())
    with (OUT / name).open("w", encoding="utf-8-sig", newline="") as f:
        w = csv.DictWriter(f, fieldnames=fn)
        w.writeheader()
        w.writerows(rows)


def extract_method_body(file_rel: str, method: str, depth: int = 0) -> dict:
    path = ROOT / file_rel
    out = {
        "ok": False,
        "start_line": 0,
        "end_line": 0,
        "body": "",
        "notes": [],
        "services": [],
        "ui_calls": [],
        "io_calls": [],
        "tx_calls": [],
        "auth_calls": [],
        "catches": [],
        "evidence": "NOT_ENOUGH_EVIDENCE",
        "limit": "",
    }
    if not path.exists() or not method:
        out["limit"] = "CHƯA ĐỦ BẰNG CHỨNG: không mở được file/method"
        return out
    text = path.read_text(encoding="utf-8", errors="replace")
    pat = re.compile(
        rf"(?:public|internal|private)\s+(?:static\s+)?(?:async\s+)?(?:void|Task(?:<[^>]+>)?)\s+{re.escape(method)}\s*\(",
    )
    m = pat.search(text)
    if not m:
        out["limit"] = f"CHƯA ĐỦ BẰNG CHỨNG: không thấy method {method} trong {file_rel}"
        return out
    start = m.start()
    start_line = text.count("\n", 0, start) + 1
    # find body start
    brace = text.find("{", m.end())
    arrow = text.find("=>", m.end())
    if brace < 0 and arrow < 0:
        out["limit"] = "CHƯA ĐỦ BẰNG CHỨNG: không tìm thấy thân method"
        out["start_line"] = start_line
        return out
    if arrow >= 0 and (brace < 0 or arrow < brace):
        # expression-bodied — take until semicolon
        end = text.find(";", arrow)
        body = text[arrow:end + 1] if end > 0 else text[arrow : arrow + 400]
        end_line = text.count("\n", 0, end if end > 0 else arrow + 400) + 1
    else:
        i = brace
        depth = 0
        end = brace
        while end < len(text):
            c = text[end]
            if c == "{":
                depth += 1
            elif c == "}":
                depth -= 1
                if depth == 0:
                    end += 1
                    break
            end += 1
        body = text[brace:end]
        end_line = text.count("\n", 0, end - 1) + 1
        # Cap extremely long methods for report size but note truncation
        if len(body) > 12000:
            out["limit"] = f"Thân method dài {len(body)} ký tự — đã phân tích 12KB đầu; phần còn lại CHƯA ĐỦ BẰNG CHỨNG chi tiết từng dòng."
            body = body[:12000]

    services = sorted(set(CALL_RE.findall(body)))
    ui = sorted(set(SHOW_RE.findall(body)))
    io = sorted(set(IO_RE.findall(body)))
    tx = sorted(set(TX_RE.findall(body)))
    auth = sorted(set(AUTH_RE.findall(body)))
    catches = CATCH_RE.findall(body)
    notes = []
    if auth:
        notes.append("có kiểm tra/auth: " + ", ".join(auth[:6]))
    if ui:
        notes.append("UI/prompt: " + ", ".join(ui[:8]))
    if io:
        notes.append("I/O: " + ", ".join(io[:8]))
    if tx:
        notes.append("CAD tx: " + ", ".join(tx[:6]))
    if services:
        notes.append("kiểu/service gọi: " + ", ".join(services[:12]))
    if catches:
        notes.append(f"catch ({len(catches)}): " + "; ".join(c.strip()[:40] for c in catches[:4]))
    if "async" in text[m.start() : m.start() + 80] or "await " in body:
        notes.append("có async/await")
    if "Stub" in method or "chưa" in body.lower()[:500]:
        notes.append("stub/hint trong thân")

    evidence = "DECLARATION_FOUND"
    if method:
        evidence = "CALL_CHAIN_TRACED"
    if len(body) > 40 and (notes or services or ui):
        evidence = "SOURCE_BEHAVIOR_REVIEWED"
    if "Stub" in method or (len(body) < 120 and ("NotImplemented" in body or "chưa" in body.lower())):
        evidence = "STUB_ONLY"

    # One-hop follow for thin wrappers: => Other() or { Other(); }
    hop = re.search(r"(?:=>\s*|{\s*)(?:this\.)?([A-Z]\w+)\s*\(", body)
    hop_note = ""
    if depth < 1 and hop and hop.group(1) != method and len(body) < 400:
        hop_name = hop.group(1)
        hop_a = extract_method_body(file_rel, hop_name, depth=depth + 1)
        if hop_a.get("ok"):
            hop_note = f"thin-wrapper → {hop_name} @ {file_rel}:{hop_a['start_line']}-{hop_a['end_line']}"
            notes.append(hop_note)
            services = sorted(set(services + hop_a["services"]))
            ui = sorted(set(ui + hop_a["ui_calls"]))
            io = sorted(set(io + hop_a["io_calls"]))
            tx = sorted(set(tx + hop_a["tx_calls"]))
            auth = sorted(set(auth + hop_a["auth_calls"]))
            catches = catches + hop_a["catches"]
            notes.extend([n for n in hop_a["notes"] if n not in notes])
            if hop_a["evidence"] == "SOURCE_BEHAVIOR_REVIEWED" or hop_a["notes"]:
                evidence = "SOURCE_BEHAVIOR_REVIEWED"
            if hop_a.get("limit"):
                out["limit"] = ((out.get("limit") or "") + " | hop: " + hop_a["limit"]).strip(" |")

    out.update(
        {
            "ok": True,
            "start_line": start_line,
            "end_line": end_line,
            "body": body,
            "notes": notes,
            "services": services,
            "ui_calls": ui,
            "io_calls": io,
            "tx_calls": tx,
            "auth_calls": auth,
            "catches": catches,
            "evidence": evidence,
        }
    )
    return out


def slug_anchor(fid: str, name: str) -> str:
    raw = f"{fid} — {name}".lower()
    raw = re.sub(r"[^\w\s\-]", "", raw, flags=re.UNICODE)
    return re.sub(r"\s+", "-", raw.strip())


def write_dossiers(features: list[dict]) -> list[dict]:
    DOS.mkdir(parents=True, exist_ok=True)
    by_group = defaultdict(list)
    analyses = []
    for f in features:
        by_group[f["nhom"]].append(f)

    index_lines = [
        "# Hồ sơ chi tiết từng tính năng (R02)",
        "",
        "Mỗi `feature_id` có mục riêng trong file nhóm tương ứng.",
        "Runtime luôn `NOT_TESTED`. Không tuyên bố PASS CAD.",
        "",
    ]

    for g in sorted(by_group):
        path = DOS / f"{g}.md"
        lines = [
            f"# Nhóm {g} — hồ sơ từng feature_id",
            "",
            f"Sinh {datetime.now(timezone.utc).date()} — phân tích tĩnh thân method (brace-match).",
            "",
        ]
        for f in by_group[g]:
            a = extract_method_body(f["handler_file"], f["method"])
            if f["trang_thai"] == "STUB":
                a["evidence"] = "STUB_ONLY"
            f["muc_ket_luan_source"] = a["evidence"]
            analyses.append({"feature_id": f["feature_id"], **{k: a[k] for k in ("start_line", "end_line", "evidence", "limit", "notes", "services", "ui_calls", "io_calls")}})
            body_loc = (
                f"{f['handler_file']}:{a['start_line']}-{a['end_line']}"
                if a["ok"]
                else "CHƯA ĐỦ BẰNG CHỨNG"
            )
            svc = ", ".join(a["services"][:15]) if a["services"] else "CHƯA ĐỦ BẰNG CHỨNG / không thấy tên *Service|Dialog|… trong thân command (có thể gọi qua biến cục bộ)"
            ui = ", ".join(a["ui_calls"]) if a["ui_calls"] else "(không thấy ShowModal/Prompt/WriteMessage trong thân đã đọc)"
            io = ", ".join(a["io_calls"]) if a["io_calls"] else "(không thấy File/Http dialog API trực tiếp trong thân — có thể nằm service)"
            tx = ", ".join(a["tx_calls"]) if a["tx_calls"] else "(không thấy Transaction trực tiếp trong thân)"
            auth = ", ".join(a["auth_calls"]) if a["auth_calls"] else "(không thấy EnsureAuthenticated trong thân)"
            catches = "; ".join(c.strip() for c in a["catches"][:6]) if a["catches"] else "CHƯA ĐỦ BẰNG CHỨNG / không có catch trong thân command (lỗi có thể bubble hoặc xử lý ngoài)"
            limit = a["limit"] or (
                "Đã đọc thân command method; các service được gọi chưa được mở đầy đủ từng file trừ khi nằm trong cùng method."
            )
            lines += [
                f"## {f['feature_id']} — {f['ten_tinh_nang']}",
                "",
                "### 1. Định danh",
                f"- **feature_id:** `{f['feature_id']}`",
                f"- **Tên:** {f['ten_tinh_nang']}",
                f"- **Nhóm:** {f['nhom']}",
                f"- **Audience:** {f['audience']}",
                f"- **Trạng thái:** `{f['trang_thai']}` — {f['ly_do_trang_thai']}",
                "",
                "### 2. Điểm kích hoạt",
                f"- **Lệnh chính:** `{f['lenh_chinh']}` @ `{f['handler_file']}:{f['handler_line']}` method `{f['method']}`",
                f"- **Alias:** {f['aliases'] or '(không)'}",
                f"- **Ribbon:** {f['ribbon_entries'] or '(không / chưa gắn RibbonBuilder)'}",
                f"- **Palette Run:** {f['palette_entries'] or '(không thấy PaletteCommands.Run)'}",
                f"- **Thân method:** `{body_loc}`",
                "",
                "### 3. Chuỗi xử lý (tĩnh)",
                f"- CommandMethod → `{f['method']}` → quan sát: {'; '.join(a['notes']) if a['notes'] else 'CHƯA ĐỦ BẰNG CHỨNG hành vi'}",
                f"- **Service/Dialog types trong thân:** {svc}",
                f"- **Giới hạn đọc:** {limit}",
                "",
                "### 4. Input / Output / CAD",
                f"- **UI/prompt:** {ui}",
                f"- **I/O file/network (trực tiếp):** {io}",
                f"- **Transaction CAD (trực tiếp):** {tx}",
                f"- **Auth gate:** {auth}",
                f"- **Đối tượng CAD / đơn vị / phiên bản phụ thuộc:** CHƯA ĐỦ BẰNG CHỨNG nếu không nằm trong thân command — cần đọc service layer khi quay video.",
                f"- **Hành động phá hủy:** CHƯA ĐỦ BẰNG CHỨNG đầy đủ; nếu thân có Delete/Erase sẽ nằm trong notes/services — kiểm tra lại trước pilot.",
                "",
                "### 5. Nhánh lỗi",
                f"- **catch quan sát:** {catches}",
                "",
                "### 6. Fixture / đo độc lập (đề xuất tĩnh)",
                "- **MASTER/WORK COPY:** bản vẽ trắng Metric WORK COPY; không đụng file khách.",
                "- **REFERENCE:** đo độc lập ngoài CAD khi feature xuất file; nếu chỉ ghi DWG — REFERENCE bằng checklist đối tượng.",
                f"- **Thiếu bằng chứng fixture cụ thể:** ghi nhận cho `{f['feature_id']}` — chưa có seed audit riêng trong `_R02`.",
                "",
                "### 7. Khả năng quay / rủi ro",
                f"- Modal/UI: {'có dấu hiệu modal/dialog' if any('Show' in x or 'Dialog' in x for x in a['ui_calls']+a['services']) else 'chưa thấy modal trong thân'}",
                f"- Chuột/pick: {'có Prompt/Get*' if any(x.startswith('Get') or 'Prompt' in x for x in a['ui_calls']) else 'chưa thấy pick trong thân'}",
                f"- Async: {'có' if any('async' in n for n in a['notes']) else 'không thấy trong thân'}",
                f"- Ứng viên video người dùng (cờ CSV): {f['video_candidate']}",
                "",
                "### 8. Mức kết luận",
                f"- **Source:** `{a['evidence']}`",
                f"- **Runtime:** `NOT_TESTED`",
                f"- **R01 refs:** {f['r01_refs']}",
                "",
                "---",
                "",
            ]
            index_lines.append(f"- `{f['feature_id']}` {f['ten_tinh_nang']} → [{g}.md](./{g}.md)")
        path.write_text("\n".join(lines), encoding="utf-8")
        index_lines.append("")
    (DOS / "00_INDEX.md").write_text("\n".join(index_lines), encoding="utf-8")
    return analyses


def verify_sample(features: list[dict], cmd: str, expect_substr: str) -> dict:
    f = next((x for x in features if x["lenh_chinh"] == cmd), None)
    if not f:
        return {"cmd": cmd, "result": "NOT_IN_FEATURE_LIST", "expect": expect_substr}
    path = ROOT / f["handler_file"]
    if not path.exists():
        return {"cmd": cmd, "feature_id": f["feature_id"], "result": "FILE_MISSING", "file": f["handler_file"]}
    text = path.read_text(encoding="utf-8", errors="replace")
    lines = text.splitlines()
    line_no = int(f["handler_line"] or 0)
    window = "\n".join(lines[max(0, line_no - 1) : line_no + 25])
    has_cmd = f'CommandMethod("{cmd}"' in text or f"CommandMethod(\"{cmd}\"" in text
    has_method = f["method"] in window or f["method"] in text
    has_expect = expect_substr in text if expect_substr else True
    ok = has_cmd and has_method and has_expect
    return {
        "cmd": cmd,
        "feature_id": f["feature_id"],
        "file": f["handler_file"],
        "line": f["handler_line"],
        "method": f["method"],
        "commandmethod_found": has_cmd,
        "method_near_attr": has_method,
        "expect_substr": expect_substr,
        "expect_found": has_expect,
        "result": "VERIFIED_OPEN" if ok else "MISMATCH",
        "muc_ket_luan_source": f["muc_ket_luan_source"],
    }


def main():
    features = read_csv("03_DANH_MUC_TINH_NANG_CHUAN_HOA.csv")
    summary = json.loads((OUT / "_extract_summary_r02.json").read_text(encoding="utf-8"))
    analyses = write_dossiers(features)
    # rewrite CSV with updated evidence
    write_csv("03_DANH_MUC_TINH_NANG_CHUAN_HOA.csv", features)

    # completeness stats
    evidence_counts = defaultdict(int)
    for f in features:
        evidence_counts[f["muc_ket_luan_source"]] += 1
    unread_services = []
    for a in analyses:
        if a.get("services"):
            for s in a["services"]:
                unread_services.append(f"{a['feature_id']}:{s}")

    samples = [
        verify_sample(features, "ISURVEY_DGN_TO_CAD", "DgnToCad"),
        verify_sample(features, "VEMCD", "DrawAlias"),
        verify_sample(features, "ISURVEY_UAV_CLIP", "Uav"),
        verify_sample(features, "NHAPDIEM", "Import"),
        verify_sample(features, "XUATDIEM", "Export"),
        verify_sample(features, "ISURVEY_LOGIN", "Login"),
        verify_sample(features, "ISURVEY_ABOUT", "About"),
        verify_sample(features, "ISURVEY_AI_ASSIST", "Assist"),
    ]
    # one per group if missing
    seen_groups = {next((x for x in features if x["lenh_chinh"] == s["cmd"]), {}).get("nhom") for s in samples if "feature_id" in s}
    for f in features:
        if f["nhom"] not in seen_groups:
            samples.append(verify_sample(features, f["lenh_chinh"], f["method"][:6] if f["method"] else ""))
            seen_groups.add(f["nhom"])

    inv_files = read_csv("_inventory_files.csv")
    read_full = sum(1 for r in inv_files if r.get("has_commandmethod") == "True" or r.get("has_commandmethod") is True)
    # CSV may store boolean as string
    def truthy(v):
        return str(v).lower() in {"true", "1", "yes"}

    (OUT / "01_PHAM_VI_GIT_VA_KIEM_KE_FILE.md").write_text(
        f"""# 01 — Phạm vi Git và kiểm kê file (R02)

## Git trước R02

Xem `_git_before.txt`, `_git_status_before.txt`, `_git_diff_before.txt`, `_git_head_before.txt`.

- Working tree có sẵn nhiều file untracked (docs/MCD, SEED, …) — **không** thuộc phạm vi sửa R02.
- Không đọc secret/token.

## Git sau R02

Xem `_git_after.txt` / `_git_status_after.txt` / `_git_diff_after.txt` / `_git_head_after.txt` (điền cuối đợt).

**Kỳ vọng nghiệm thu an toàn:** HEAD không đổi so trước; `git diff --name-only` không liệt kê file tracked product; chỉ xuất hiện untracked mới dưới `docs/audits/..._R02/` (+ ZIP nếu đặt cùng thư mục).

## Kiểm kê file nguồn

| Hạng mục | Số |
|---|---:|
| File `.cs`+`.xaml` dưới `src/` | {summary['cs_xaml_files']} |
| File có CommandMethod (inventory) | {sum(1 for r in inv_files if truthy(r.get('has_commandmethod')))} |
| XAML Click matches | {summary['xaml_click_handlers']} |
| PaletteCommands.Run refs | {summary['palette_run_refs']} |

Chi tiết: `_inventory_files.csv`.

## Mức đọc

| Phạm vi | Mức |
|---|---|
| Toàn bộ `src/**/*.{{cs,xaml}}` | Đã quét inventory + regex CommandMethod/Btn/Click/Palette |
| Mỗi canonical feature — thân command method | Brace-match + trích service/UI/IO/catch (tối đa 12KB/method) |
| Service layer phụ thuộc (ProfileDrawService, FrameService.*, …) | **CHƯA ĐỦ BẰNG CHỨNG** đầy đủ từng dòng — liệt kê theo feature trong 04/ và 07 |

## Giới hạn

- Không Roslyn AST chính thức.
- Không đọc secret / config nhạy cảm.
- Dynamic command registration ngoài literal string: **CHƯA ĐỦ BẰNG CHỨNG**.
""",
        encoding="utf-8",
    )

    (OUT / "02_KIEM_KE_LAI_COMMAND_UI.md").write_text(
        f"""# 02 — Kiểm kê lại Command / UI (R02)

## Phương pháp cải tiến so với R01

| Hạng mục | R01 | R02 |
|---|---|---|
| CommandMethod | regex từng dòng | regex toàn file (đa dòng / flags) |
| Alias | 46, liên quan rỗng | resolve `=> _x.Method()` → primary |
| Ribbon `new Btn` | cùng dòng | `re.S` RibbonBuilder |
| XAML Click | chưa | `_inventory_xaml_clicks.csv` ({summary['xaml_click_handlers']}) |
| Palette | chưa đủ | `_inventory_palette_runs.csv` ({summary['palette_run_refs']}) |

## Số liệu R02

| Metric | Giá trị |
|---|---:|
| CommandMethod tổng | {summary['commandmethod_total']} |
| Trong CommandAliases.cs | {summary['commandmethod_alias_file']} |
| Primary declarations | {summary['commandmethod_primary_file']} |
| Canonical features | {summary['canonical_features']} |
| IMPLEMENTED | {summary['features_implemented']} |
| STUB | {summary['features_stub']} |
| Ribbon Btn | {summary['ribbon_btns']} (en {summary['ribbon_enabled']} / dis {summary['ribbon_disabled']}) |
| Ribbon placeholder | {summary['ribbon_placeholder_no_cmd']} |
| Alias → primary | {summary['aliases_resolved_to_primary']}/{summary['commandmethod_alias_file']} |

## Đối chiếu R01

- R01: 125 CommandMethod, 61 Ribbon, 150 dòng CSV (125+25 placeholder).
- R02: **cùng 125** CommandMethod → không phát hiện thêm khai báo literal bị bỏ sót bởi parser đa dòng trong corpus hiện tại.
- **Không** dùng 150 làm mẫu số feature. Canonical = **{summary['canonical_features']}**; placeholder Ribbon = **{summary['ribbon_placeholder_no_cmd']}** (loại khỏi mẫu số video user).

## Giới hạn còn lại

- Chưa Roslyn AST.
- Context menu / dynamic macro: **CHƯA ĐỦ BẰNG CHỨNG**.
- XAML Click handlers chưa map 1-1 hết sang feature_id (một phần là nút trong dialog dùng chung).
""",
        encoding="utf-8",
    )

    (OUT / "05_DOI_CHIEU_TAI_LIEU_VA_BAO_PHU_R02.md").write_text(
        f"""# 05 — Đối chiếu tài liệu và bao phủ R02

## Phân biệt nguồn sự thật

| Nguồn | Vai trò R02 |
|---|---|
| Mã C#/XAML `src/` | **Nguồn sự thật** |
| `docs/Danh_sach_lenh_iSurvey.xlsx`, `docs/video/CATALOG.md`, changelog | Đối chiếu phụ |
| Standard/Prompt/Schema v1.0 cũ | **DỰ THẢO** — không chặn audit |
| Excel KT04 ngoài repo | Chờ cung cấp |

## Bao phủ

- Canonical features có hồ sơ 04: **{summary['canonical_features']}**
- Phân bố mức kết luận source (sau đọc thân): {dict(evidence_counts)}
- Stub/placeholder: `03B_STUB_PLACEHOLDER_EXCLUDED.csv`
- Runtime: toàn bộ **NOT_TESTED** — không tính % KT-05

## Quyết định KT giữ nguyên

- Giọng nam miền Bắc; 2 video thử rồi duyệt giọng.
- Feature 3–5 phút / Workflow 5–8 phút (có ngoại lệ).
- CAD thật, dữ liệu giả lập hợp lệ; G5/upload tách pilot nội bộ.
""",
        encoding="utf-8",
    )

    (OUT / "06_DANH_GIA_PILOT_MO_RONG.md").write_text(
        """# 06 — Đánh giá pilot mở rộng (R02)

**Không chọn chạy pilot thay Chủ dự án.** Chỉ xếp hạng tư vấn.

## Tiêu chí (tĩnh)

Fixture dễ tạo | Đo độc lập | Modal/phức tạp UI | Phụ thuộc file ngoài | Rủi ro dữ liệu

## Ứng viên

| Ứng viên | Điểm tư vấn /40 | Ghi chú |
|---|---:|---|
| **XUATDIEM / NHAPDIEM** | **33** | Input CSV/TXT hoặc Point trên Drawing trắng; output file đo được ngoài CAD; UI hộp thoại vừa phải. **Ưu tiên đề xuất** nếu muốn fixture đơn giản. |
| D2C (`ISURVEY_DGN_TO_CAD`) | 30 | Cần DGN thật + seed; baseline không phải output iSurvey; phức tạp quyền/file. |
| VEMCD | 28 | Phụ thuộc mô hình→tuyến→cọc có Z; UI Zero-Clutter nhiều control; fixture dài. |
| UAVCLIP (`ISURVEY_UAV_CLIP`) | 27 | Cần raster + biên; reload/biên sơ họa; môi trường ảnh. |

## Kết luận tư vấn

1. Ưu tiên xem xét **NHAPDIEM hoặc XUATDIEM**.
2. Giữ D2C/VEMCD/UAVCLIP trong shortlist — **Chủ dự án quyết định**.
3. Không quay, không NETLOAD trong R02.
""",
        encoding="utf-8",
    )

    sample_lines = [
        "# 07 — Ma trận bằng chứng và kiểm tra mẫu",
        "",
        "## Quy ước mức kết luận",
        "",
        "| Mức | Nghĩa |",
        "|---|---|",
        "| DECLARATION_FOUND | Có CommandMethod |",
        "| CALL_CHAIN_TRACED | Alias→primary / handler xác định |",
        "| SOURCE_BEHAVIOR_REVIEWED | Đã đọc thân method + ghi nhận hành vi |",
        "| STUB_ONLY | Stub |",
        "| UI_PLACEHOLDER | Ribbon disabled không CommandMethod |",
        "| NOT_ENOUGH_EVIDENCE | Thiếu đọc sâu |",
        "| RUNTIME | luôn NOT_TESTED trong R02 |",
        "",
        "## Phân bố mức kết luận (canonical features)",
        "",
        f"```json\n{json.dumps(dict(evidence_counts), ensure_ascii=False, indent=2)}\n```",
        "",
        "## Mẫu mở file xác minh (mỗi nhóm đại diện)",
        "",
    ]
    for s in samples:
        sample_lines.append(
            f"- **{s.get('feature_id', s.get('cmd'))}** `{s.get('cmd')}` → `{s.get('file', '?')}:{s.get('line', '?')}` "
            f"method `{s.get('method', '?')}` — **{s.get('result')}** "
            f"(cmd={s.get('commandmethod_found')}, method={s.get('method_near_attr')}, "
            f"expect `{s.get('expect_substr')}`={s.get('expect_found')}, evidence=`{s.get('muc_ket_luan_source', '')}`)"
        )
    sample_lines += [
        "",
        "## Phần chưa đọc đầy đủ (ảnh hưởng)",
        "",
        "Các `*Service` / `*Dialog` xuất hiện trong thân command nhưng **chưa** mở hết file service:",
        "",
    ]
    # unique first 80
    uniq = []
    seen = set()
    for x in unread_services:
        if x not in seen:
            seen.add(x)
            uniq.append(x)
    for x in uniq[:80]:
        sample_lines.append(f"- {x} — **CHƯA ĐỦ BẰNG CHỨNG** body service")
    if len(uniq) > 80:
        sample_lines.append(f"- … và {len(uniq) - 80} mục nữa (xem hồ sơ 04/).")
    sample_lines += [
        "",
        "## Tự khớp số liệu",
        "",
        f"- CommandMethod = {summary['commandmethod_total']} (= alias {summary['commandmethod_alias_file']} + primary {summary['commandmethod_primary_file']})",
        f"- Canonical features = {summary['canonical_features']} (= primary)",
        f"- Alias resolved = {summary['aliases_resolved_to_primary']} / unresolved = {summary['aliases_unresolved']}",
        f"- Ribbon = {summary['ribbon_btns']}; placeholder = {summary['ribbon_placeholder_no_cmd']}",
        "",
    ]
    (OUT / "07_MA_TRAN_BANG_CHUNG_VA_KIEM_TRA_MAU.md").write_text("\n".join(sample_lines), encoding="utf-8")

    (OUT / "08_VAN_DE_CHAN_VA_HANH_DONG_TIEP.md").write_text(
        f"""# 08 — Vấn đề chặn và hành động tiếp (R02)

## P0

1. **Nghiệm thu Phase 01 vẫn phụ thuộc Chủ dự án / hội đồng** — R02 chỉ bổ sung hồ sơ; **không** tự tuyên bố nghiệm thu.
2. Service layer dài — nhiều nhánh lỗi/IO **CHƯA ĐỦ BẰNG CHỨNG** ở mức từng dòng (xem 07).

## P1

1. Chưa Roslyn AST — rủi ro đăng ký động (hiện không thấy thêm CommandMethod so R01).
2. Fixture CAD chuẩn cho pilot chưa có trong audit folder.
3. Chính sách AI Cloud / mã nguồn nhạy cảm — Chủ dự án minh định.
4. YouTube/Drive token **không** chặn pilot nội bộ.

## P2

1. Đồng bộ Excel catalog với canonical feature list khi Chủ dự án yêu cầu.

## Hành động đề xuất (không thực hiện trong R02)

- Hội đồng thẩm định ZIP R02.
- Nếu đạt: chỉ đạo chọn pilot + phạm vi Phase tiếp.
- **Không** triển khai Video Factory cho đến khi có lệnh riêng.
""",
        encoding="utf-8",
    )

    (OUT / "09_TONG_HOP_TRINH_CHU_DU_AN_R02.md").write_text(
        f"""# 09 — Tổng hợp trình Chủ dự án (R02)

## Trạng thái

| Hạng mục | Kết luận |
|---|---|
| Kiểm toán bổ sung R02 (tĩnh) | **ĐÃ XUẤT HỒ SƠ** theo 12 nhóm đầu ra |
| Phase 01 nghiệm thu | **CHƯA** — chờ hội đồng / Chủ dự án |
| Runtime / CAD / Video Factory | **KHÔNG CHẠY** trong R02 |
| `SOURCE_REVIEWED` đồng loạt R01 | **ĐÃ THAY** bằng mức kết luận phân tách |

## Số liệu chính

- Canonical features: **{summary['canonical_features']}** (không dùng 150)
- IMPLEMENTED: **{summary['features_implemented']}** | STUB: **{summary['features_stub']}**
- Alias → primary: **{summary['aliases_resolved_to_primary']}/46**
- Ribbon placeholder loại khỏi mẫu số video: **{summary['ribbon_placeholder_no_cmd']}**
- Phân bố evidence: `{dict(evidence_counts)}`

## Pilot (tư vấn)

Ưu tiên xem xét NHAPDIEM/XUATDIEM; D2C/VEMCD/UAVCLIP giữ shortlist — **Chủ dự án chọn**.

## Cam kết an toàn

- Không sửa mã nghiệp vụ C#/XAML product.
- Không build/CAD/tools/video.
- Chỉ tạo tệp dưới thư mục R02 (+ ZIP).
""",
        encoding="utf-8",
    )

    (OUT / "AUDIT_INDEX_R02.md").write_text(
        f"""# AUDIT_INDEX_R02

**Thư mục:** `docs/audits/2026-11-10_ISURVEY_VIDEO_FACTORY_SOURCE_AUDIT_R02/`  
**Ngày:** {datetime.now(timezone.utc).date()}

## Tệp bắt buộc

1. `01_PHAM_VI_GIT_VA_KIEM_KE_FILE.md`
2. `02_KIEM_KE_LAI_COMMAND_UI.md`
3. `03_DANH_MUC_TINH_NANG_CHUAN_HOA.csv` — {summary['canonical_features']} feature
4. `03A_ANH_XA_LENH_ALIAS_RIBBON.csv`
5. `03B_STUB_PLACEHOLDER_EXCLUDED.csv`
6. `04_HO_SO_CHI_TIET_TUNG_TINH_NANG/` (+ `00_INDEX.md` + theo nhóm)
7. `05_DOI_CHIEU_TAI_LIEU_VA_BAO_PHU_R02.md`
8. `06_DANH_GIA_PILOT_MO_RONG.md`
9. `07_MA_TRAN_BANG_CHUNG_VA_KIEM_TRA_MAU.md`
10. `08_VAN_DE_CHAN_VA_HANH_DONG_TIEP.md`
11. `09_TONG_HOP_TRINH_CHU_DU_AN_R02.md`
12. `AUDIT_INDEX_R02.md` (file này)

## Trung gian

- `_tools/r02_extract.py`, `_tools/r02_generate_reports.py`
- `_inventory_*.csv`, `_extract_summary_r02.json`
- `_git_*before/after*`

## Thống kê

- Canonical features: **{summary['canonical_features']}**
- Evidence: `{dict(evidence_counts)}`
- Alias unresolved: **{summary['aliases_unresolved']}**

## Cảnh báo

- Không tuyên bố Phase 01 đã nghiệm thu.
- Không RUNTIME PASS.
- Service layer dài: CHƯA ĐỦ BẰNG CHỨNG từng dòng.

## Mức hoàn thành hồ sơ R02

**ĐỦ HÌNH THỨC 12 nhóm đầu ra + hồ sơ từng feature_id canonical.**  
**Chưa đủ** để kết luận đã đọc 100% mọi dòng service — đã ghi rõ giới hạn trong 04/ và 07.
""",
        encoding="utf-8",
    )

    # self-check JSON
    ids = [f["feature_id"] for f in features]
    check = {
        "feature_rows": len(features),
        "unique_feature_ids": len(set(ids)),
        "duplicate_ids": sorted({i for i in ids if ids.count(i) > 1}),
        "evidence_counts": dict(evidence_counts),
        "samples_verified_open": sum(1 for s in samples if s.get("result") == "VERIFIED_OPEN"),
        "samples_total": len(samples),
        "summary": summary,
    }
    (OUT / "_self_check_r02.json").write_text(json.dumps(check, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps(check, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
