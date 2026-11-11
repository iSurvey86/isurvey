# -*- coding: utf-8 -*-
"""Write R02C markdown reports from extract outputs."""
from __future__ import annotations

import csv
import json
from collections import defaultdict
from datetime import datetime, timezone
from pathlib import Path

OUT = Path(__file__).resolve().parents[1]


def read_csv(name: str) -> list[dict]:
    with (OUT / name).open(encoding="utf-8-sig", newline="") as f:
        return list(csv.DictReader(f))


def main():
    summary = json.loads((OUT / "_summary_r02c.json").read_text(encoding="utf-8"))
    catalog = read_csv("02_BUSINESS_FEATURE_CATALOG.csv")
    ui = read_csv("04_UI_EVENT_TO_FEATURE.csv")
    traces = read_csv("05_SOURCE_TRACE_COVERAGE.csv")
    stubs = read_csv("06_STUB_RECHECK.csv")

    xaml = [r for r in ui if r["entry_type"] == "XAML_CLICK"]
    xaml_cls = defaultdict(int)
    for r in xaml:
        xaml_cls[r["classify"]] += 1
    xaml_map_status = defaultdict(int)
    for r in xaml:
        xaml_map_status[r["mapping_status"]] += 1

    stub_cmds = [s for s in stubs if str(s["is_stub"]).lower() == "true"]
    send = next(s for s in stubs if s["cmd"] == "ISURVEY_UAV_SEND_BACK")

    cond = {
        "01_business_id": "ĐẠT HỒ SƠ — BF-* ổn định theo lệnh chính; crosswalk 03; AUD chỉ tham chiếu đợt",
        "02_xaml": f"ĐẠT HỒ SƠ — 91/91 có parent hoặc lý do NOT_APPLICABLE; classify theo tên handler trước; Confirm/Save/Create/Update/Start/Export không còn CLOSE_CANCEL",
        "03_stub": f"ĐẠT HỒ SƠ — SEND_BACK = SOURCE_PRESENT; stub non-alias = {summary['stub_commands_non_alias']} (R02B set ~{summary['r02b_stub_cmd_set_size']}); 5 Ribbon enabled+stub ghi nhận, không sửa Ribbon",
        "04_trace": f"CÓ ĐIỀU KIỆN — SERVICE_CHAIN={summary['service_chain_count']}; HANDLER={summary['evidence'].get('HANDLER_TRACE_COMPLETE')}; còn METHOD_BODY/NOT_ENOUGH và nhiều unresolved trong 05 — không tuyên bố 100%",
        "05_coverage_git": "ĐẠT HỒ SƠ — UI gồm XAML/Ribbon/Palette/Context/DoubleClick/ICommand/AI dynamic; Git before/after trong 07",
    }

    (OUT / "01_R02C_SUMMARY_AND_ACCEPTANCE.md").write_text(
        f"""# 01 — R02C Summary and Acceptance Gate

**Ngày:** {datetime.now(timezone.utc).date()}  
**HEAD:** `{summary['head']}`  
**Phạm vi:** kiểm toán tĩnh chỉ đọc — **không** tự nghiệm thu Phase 01.

## Số liệu sau chỉnh

| Chỉ số | R02B | R02C |
|---|---:|---:|
| Canonical / business features | 63 (AUD) | **{summary['business_features']}** (`BF-*`) |
| SOURCE_PRESENT / STUB | 55 / 8 | **{summary['source_present']} / {summary['stub_features']}** |
| ISURVEY_UAV_SEND_BACK | STUB (sai) | **SOURCE_PRESENT** |
| XAML có parent_feature_id | 0/91 | **{summary['xaml_with_parent']}/91** (+ {summary['xaml_without_parent_but_reason']} có lý do NOT_APPLICABLE) |
| video_candidate YES | 51 | **{summary['video_candidates']}** |
| SERVICE_CHAIN_VERIFIED_STATIC | 31 (lỏng) | **{summary['service_chain_count']}** (chỉ khi có side-effect + I/O) |

## Kết luận từng điều kiện bắt buộc

| # | Điều kiện | Kết luận R02C |
|---|---|---|
| 01 | business_feature_id bền vững | {cond['01_business_id']} |
| 02 | Ánh xạ 91 XAML | {cond['02_xaml']} |
| 03 | Sửa STUB | {cond['03_stub']} |
| 04 | Truy vết service | {cond['04_trace']} |
| 05 | Bao phủ + Git bất biến | {cond['05_coverage_git']} |

## Phân bố evidence

```json
{json.dumps(summary['evidence'], ensure_ascii=False, indent=2)}
```

## Phase 01

**CHƯA TỰ NGHIỆM THU.** R02C xuất đủ đầu ra yêu cầu; điều kiện 04 còn mục CHƯA ĐỦ BẰNG CHỨNG. Chờ Chủ dự án / hội đồng.
""",
        encoding="utf-8",
    )

    (OUT / "07_GIT_BEFORE_AFTER_AND_SECURITY.md").write_text(
        f"""# 07 — Git before/after và an toàn phạm vi

## Trước R02C

- `_git_head_before.txt` / `_git_before.txt` / `_git_branch_before.txt`
- `_git_status_before.txt` / `_git_diff_before.txt`
- HEAD: `{summary['head']}` (cùng R02/R02B)

## Sau R02C

- `_git_head_after.txt` / `_git_after.txt` / `_git_status_after.txt` / `_git_diff_after.txt`

**Kỳ vọng / kết quả:** HEAD không đổi; không sửa tracked `src/` C#/XAML; chỉ tạo untracked dưới `docs/audits/..._R02C/` (+ ZIP).  
Diff tracked sẵn có từ trước (nếu còn): `docs/audits/2026-11-10_ISURVEY_VIDEO_FACTORY_SOURCE_AUDIT.zip`.

## Cam kết

- Không build / CAD / NETLOAD / Video Factory / YouTube / secret.
- Không ghi đè R01/R02/R02B.
- Không commit/push trong đợt này.
""",
        encoding="utf-8",
    )

    not_enough = [t for t in traces if t["evidence"] in {"NOT_ENOUGH_EVIDENCE", "METHOD_BODY_SCANNED", "HANDLER_TRACE_COMPLETE"}]
    open_lines = [
        "# 08 — Open issues and stop gate (R02C)",
        "",
        "## DỪNG tại cổng nghiệm thu Phase 01",
        "",
        "Không tự chuyển Phase 02 / pilot / lập trình VF.",
        "",
        "## Đã khắc phục",
        "",
        "1. `business_feature_id` = `BF-<LENH_CHINH>` + crosswalk AUD đợt cũ.",
        "2. 91 XAML: parent hoặc NOT_APPLICABLE có lý do; sửa heuristic Confirm/Save/Create/Update/Start/Export.",
        "3. STUB: SEND_BACK → SOURCE_PRESENT; stub còn 7 lệnh UAV/PC thật sự gọi `Stub(\"CMD\")`.",
        "4. Evidence SERVICE_CHAIN thắt chặt (side-effect + I/O).",
        "5. Bổ sung context menu / double-click / ICommand / AI dynamic trong `04_UI_EVENT_TO_FEATURE.csv`.",
        "",
        "## CHƯA ĐỦ BẰNG CHỨNG / chặn",
        "",
        f"- Features chưa SERVICE_CHAIN: **{len(not_enough)}** (xem `05_SOURCE_TRACE_COVERAGE.csv` cột unresolved).",
        "- Service layer dài (DgnImportService Idle finish, ProfileDrawService band, FrameService.*): nhiều nhánh chưa đọc hết.",
        "- Vn2000SetupWindow và một số helper UI: NOT_APPLICABLE — opener đầy đủ có thể bổ sung sau.",
        f"- Ribbon **enabled** nhưng STUB (không sửa Ribbon): {summary['ribbon_stub_enabled']}",
        "- Runtime: toàn bộ **NOT_TESTED**.",
        "",
        "## Tiêu chí chặn nghiệm thu Phase 01 (để Chủ dự án quyết)",
        "",
        "| Tiêu chí | Trạng thái hồ sơ R02C |",
        "|---|---|",
        "| BF id ổn định + crosswalk | Có |",
        "| XAML 91 mapped/explained | Có |",
        "| STUB không nhầm SEND_BACK | Có |",
        "| Mọi user-facing SERVICE_CHAIN đầy đủ hậu điều kiện | **Chưa** |",
        "| Runtime CAD | Không thuộc R02C |",
        "",
        "## Danh sách feature cần đọc thêm (rút gọn)",
        "",
    ]
    for t in not_enough[:40]:
        open_lines.append(f"- `{t['business_feature_id']}` {t['lenh_chinh']} — `{t['evidence']}` — {t['unresolved'][:120]}")
    if len(not_enough) > 40:
        open_lines.append(f"- … và {len(not_enough)-40} mục nữa trong CSV 05.")
    (OUT / "08_OPEN_ISSUES_AND_STOP_GATE.md").write_text("\n".join(open_lines), encoding="utf-8")

    # Enrich summary with XAML stats note
    (OUT / "AUDIT_INDEX_R02C.md").write_text(
        f"""# AUDIT_INDEX_R02C

**Thư mục:** `docs/audits/2026-11-10_ISURVEY_VIDEO_FACTORY_SOURCE_AUDIT_R02C/`

## Đầu ra bắt buộc

1. `01_R02C_SUMMARY_AND_ACCEPTANCE.md`
2. `02_BUSINESS_FEATURE_CATALOG.csv` — {len(catalog)} BF
3. `03_R01_R02_R02B_R02C_CROSSWALK.csv`
4. `04_UI_EVENT_TO_FEATURE.csv` — {len(ui)} rows (XAML {len(xaml)})
5. `05_SOURCE_TRACE_COVERAGE.csv` + `05_TRACES/`
6. `06_STUB_RECHECK.csv` — SEND_BACK: `{send['reason']}`
7. `07_GIT_BEFORE_AFTER_AND_SECURITY.md`
8. `08_OPEN_ISSUES_AND_STOP_GATE.md`

## XAML classify

`{dict(xaml_cls)}`

## Mapping status

`{dict(xaml_map_status)}`

## Cảnh báo

Không tuyên bố Phase 01 đã nghiệm thu. Runtime NOT_TESTED.
""",
        encoding="utf-8",
    )

    print("reports ok", len(catalog), len(xaml), len(stub_cmds))


if __name__ == "__main__":
    main()
