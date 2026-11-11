# BF-ISURVEY_HDSD — ISURVEY_HDSD

- **verdict_source:** `SOURCE_PARTIAL_VERIFIED`
- **priority:** HIGH_IMPACT
- **runtime:** NOT_TESTED

## Call chain
ISURVEY_HDSD @ src/Auth/AuthModule.cs:35 → HelpCommand → types=ActivityLogger;AuthManager

## Inputs / Algorithm / Outputs / Errors / Destructive
- Inputs: xem evidence handler; CHƯA ĐỦ BẰNG CHỨNG nếu thiếu trong body
- Algorithm: {         const string url = "https://ksnpsc.vercel.app/huong-dan/isurvey";         try         {             Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });             AuthManager.Print("\n[iSurvey] Da mo huong dan tren trinh duyet.");             ActivityLogger.Enq…
- Outputs: xem WriteMessage/Store/File trong evidence
- Errors: catch trong handler
- Destructive: không thấy trong đoạn đã đọc — CHƯA ĐỦ BẰNG CHỨNG toàn cục

## Gaps
(đã mở handler + ≥1 service call site)

## Evidence
EVIDENCE/USER-BF-ISURVEY_HDSD.md#src/Auth/AuthModule.cs:36-49
