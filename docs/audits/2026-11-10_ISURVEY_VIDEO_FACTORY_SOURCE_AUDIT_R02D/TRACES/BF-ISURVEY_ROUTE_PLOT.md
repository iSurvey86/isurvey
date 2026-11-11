# BF-ISURVEY_ROUTE_PLOT — ISURVEY_ROUTE_PLOT

- **verdict_source:** `SOURCE_PARTIAL_VERIFIED`
- **priority:** HIGH_IMPACT
- **runtime:** NOT_TESTED

## Call chain
ISURVEY_ROUTE_PLOT @ src/Modules/Frame/FrameService.RouteLayout.cs:33 → RoutePlotCommand → types=ActivityLogger;AuthManager;DocumentManager

## Inputs / Algorithm / Outputs / Errors / Destructive
- Inputs: xem evidence handler; CHƯA ĐỦ BẰNG CHỨNG nếu thiếu trong body
- Algorithm: {         if (!AuthManager.EnsureAuthenticated(track: false))             return;         var doc = AcadApp.DocumentManager.MdiActiveDocument;         if (doc is null)             return;         try         {             PlotExistingRoute(doc);         }         catch (System.Exception ex)         …
- Outputs: xem WriteMessage/Store/File trong evidence
- Errors: catch trong handler
- Destructive: không thấy trong đoạn đã đọc — CHƯA ĐỦ BẰNG CHỨNG toàn cục

## Gaps
(đã mở handler + ≥1 service call site)

## Evidence
EVIDENCE/USER-BF-ISURVEY_ROUTE_PLOT.md#src/Modules/Frame/FrameService.RouteLayout.cs:34-50
