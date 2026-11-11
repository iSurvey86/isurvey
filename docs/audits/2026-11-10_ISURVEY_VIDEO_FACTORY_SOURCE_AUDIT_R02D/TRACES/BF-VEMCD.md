# BF-VEMCD — VEMCD

- **verdict_source:** `SOURCE_PARTIAL_VERIFIED`
- **priority:** SC10_RECHECK
- **runtime:** NOT_TESTED

## Call chain
VEMCD/ISURVEY_PROFILE_DRAW → Draw @ ProfileModule.cs:14-147 | RouteStore.Load → DrawProfileWindow → pick insert → ProfileDrawService.Draw / RunUpdate | Có thể xóa profile cũ cùng RouteId rồi vẽ lại

## Inputs / Algorithm / Outputs / Errors / Destructive
- Inputs: Tuyến+cọc có Z; Model Space; options; điểm chèn
- Algorithm: Select routes; sample Z; band/datum; Draw hoặc UpdateFromTin/Properties
- Outputs: Hình MCD Model; ProfileStore
- Errors: route empty; dialog cancel; catch Lỗi mặt cắt dọc
- Destructive: Xóa MCD cũ cùng RouteId trước vẽ lại

## Gaps
CHƯA ĐỦ BẰNG CHỨNG: ProfileDrawService geometry band đầy đủ

## Evidence

