# BF-STT — STT

- **verdict_source:** `SOURCE_PARTIAL_VERIFIED`
- **priority:** HIGH_IMPACT
- **runtime:** NOT_TESTED

## Call chain
STT @ src/Modules/Tuyen/SequenceNumberService.cs:28 → SequenceCommand → types=ActivityLogger;AuthManager;DocumentManager;SequenceWindow;TransactionManager

## Inputs / Algorithm / Outputs / Errors / Destructive
- Inputs: xem evidence handler; CHƯA ĐỦ BẰNG CHỨNG nếu thiếu trong body
- Algorithm: {         if (!AuthManager.EnsureAuthenticated("STT"))             return;          var doc = AcadApp.DocumentManager.MdiActiveDocument;         if (doc is null)             return;          var request = SequenceWindow.Prompt();         if (request is null)         {             doc.Editor.WriteMes…
- Outputs: xem WriteMessage/Store/File trong evidence
- Errors: catch trong handler
- Destructive: có dấu hiệu destructive

## Gaps
không thấy file SequenceWindow.cs

## Evidence
EVIDENCE/USER-BF-STT.md#src/Modules/Tuyen/SequenceNumberService.cs:29-116
