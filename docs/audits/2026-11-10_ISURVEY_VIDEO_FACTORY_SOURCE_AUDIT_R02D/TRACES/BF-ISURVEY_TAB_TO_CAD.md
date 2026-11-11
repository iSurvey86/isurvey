# BF-ISURVEY_TAB_TO_CAD — ISURVEY_TAB_TO_CAD

- **verdict_source:** `SOURCE_PARTIAL_VERIFIED`
- **priority:** HIGH_IMPACT
- **runtime:** NOT_TESTED

## Call chain
ISURVEY_TAB_TO_CAD @ src/Modules/Import/TabImportModule.cs:29 → TabToCadCommand → types=ActivityLogger;AuthManager;DocumentManager;MapModule;ShowAlertDialog;TabImportWindow;UserSettingsStore

## Inputs / Algorithm / Outputs / Errors / Destructive
- Inputs: xem evidence handler; CHƯA ĐỦ BẰNG CHỨNG nếu thiếu trong body
- Algorithm: {         if (!AuthManager.EnsureAuthenticated(track: false))             return;          var doc = AcadApp.DocumentManager.MdiActiveDocument;         if (doc is null)             return;          var ed = doc.Editor;         if (!MapModule.IsModelSpace(doc.Database))         {             ed.Write…
- Outputs: xem WriteMessage/Store/File trong evidence
- Errors: catch trong handler
- Destructive: không thấy trong đoạn đã đọc — CHƯA ĐỦ BẰNG CHỨNG toàn cục

## Gaps
không thấy file ShowAlertDialog.cs; không thấy file TabImportWindow.cs; CHƯA ĐỦ BẰNG CHỨNG body UserSettingsStore.['Load']

## Evidence
EVIDENCE/USER-BF-ISURVEY_TAB_TO_CAD.md#src/Modules/Import/TabImportModule.cs:30-82
