# 07 — Ma trận bằng chứng và kiểm tra mẫu

## Quy ước mức kết luận

| Mức | Nghĩa |
|---|---|
| DECLARATION_FOUND | Có CommandMethod |
| CALL_CHAIN_TRACED | Alias→primary / handler xác định |
| SOURCE_BEHAVIOR_REVIEWED | Đã đọc thân method + ghi nhận hành vi |
| STUB_ONLY | Stub |
| UI_PLACEHOLDER | Ribbon disabled không CommandMethod |
| NOT_ENOUGH_EVIDENCE | Thiếu đọc sâu |
| RUNTIME | luôn NOT_TESTED trong R02 |

## Phân bố mức kết luận (canonical features)

```json
{
  "SOURCE_BEHAVIOR_REVIEWED": 70,
  "CALL_CHAIN_TRACED": 1,
  "STUB_ONLY": 8
}
```

## Mẫu mở file xác minh (mỗi nhóm đại diện)

- **AUD-012** `ISURVEY_DGN_TO_CAD` → `src/Dgn/DgnModule.cs:21` method `DgnToCadCommand` — **VERIFIED_OPEN** (cmd=True, method=True, expect `DgnToCad`=True, evidence=`SOURCE_BEHAVIOR_REVIEWED`)
- **AUD-038** `VEMCD` → `src/Modules/Profile/ProfileModule.cs:17` method `DrawAlias` — **VERIFIED_OPEN** (cmd=True, method=True, expect `DrawAlias`=True, evidence=`SOURCE_BEHAVIOR_REVIEWED`)
- **AUD-073** `ISURVEY_UAV_CLIP` → `src/Modules/Uav/UavModule.cs:10` method `ClipCommand` — **VERIFIED_OPEN** (cmd=True, method=True, expect `Uav`=True, evidence=`SOURCE_BEHAVIOR_REVIEWED`)
- **AUD-013** `NHAPDIEM` → `src/Modules/DiemDo/PointFileModule.cs:13` method `ImportPointsCommand` — **VERIFIED_OPEN** (cmd=True, method=True, expect `Import`=True, evidence=`SOURCE_BEHAVIOR_REVIEWED`)
- **AUD-014** `XUATDIEM` → `src/Modules/DiemDo/PointFileModule.cs:62` method `ExportPointsCommand` — **VERIFIED_OPEN** (cmd=True, method=True, expect `Export`=True, evidence=`SOURCE_BEHAVIOR_REVIEWED`)
- **AUD-006** `ISURVEY_LOGIN` → `src/Auth/AuthModule.cs:24` method `LoginCommand` — **VERIFIED_OPEN** (cmd=True, method=True, expect `Login`=True, evidence=`SOURCE_BEHAVIOR_REVIEWED`)
- **AUD-003** `ISURVEY_ABOUT` → `src/Auth/AuthModule.cs:67` method `AboutCommand` — **VERIFIED_OPEN** (cmd=True, method=True, expect `About`=True, evidence=`SOURCE_BEHAVIOR_REVIEWED`)
- **AUD-001** `ISURVEY_AI_ASSIST` → `src/Modules/Ai/AiAssistantModule.cs:13` method `AssistCommand` — **VERIFIED_OPEN** (cmd=True, method=True, expect `Assist`=True, evidence=`SOURCE_BEHAVIOR_REVIEWED`)
- **AUD-015** `ISURVEY_FIT_TITLE` → `src/Modules/Frame/FrameService.cs:296` method `FitTitleCommand` — **VERIFIED_OPEN** (cmd=True, method=True, expect `FitTit`=True, evidence=`SOURCE_BEHAVIOR_REVIEWED`)
- **AUD-026** `ISURVEY_EXPORT_KML` → `src/Modules/Export/ExportModule.cs:22` method `ExportKmlCommand` — **VERIFIED_OPEN** (cmd=True, method=True, expect `Export`=True, evidence=`SOURCE_BEHAVIOR_REVIEWED`)
- **AUD-028** `ISURVEY_DELETE_GE` → `src/Modules/Map/MapModule.cs:169` method `DeleteGeCommand` — **VERIFIED_OPEN** (cmd=True, method=True, expect `Delete`=True, evidence=`SOURCE_BEHAVIOR_REVIEWED`)
- **AUD-032** `ISURVEY_PC_FILTER` → `src/Modules/Uav/UavModule.cs:52` method `PcFilterStub` — **VERIFIED_OPEN** (cmd=True, method=True, expect `PcFilt`=True, evidence=`STUB_ONLY`)
- **AUD-035** `ISURVEY_PALETTE` → `src/UI/Palette/PaletteModule.cs:9` method `ShowCommand` — **VERIFIED_OPEN** (cmd=True, method=True, expect `ShowCo`=True, evidence=`SOURCE_BEHAVIOR_REVIEWED`)
- **AUD-036** `CATBD` → `src/Modules/Plan/PlanModule.cs:46` method `CutPlanCommand` — **VERIFIED_OPEN** (cmd=True, method=True, expect `CutPla`=True, evidence=`SOURCE_BEHAVIOR_REVIEWED`)
- **AUD-039** `BANGCOC` → `src/Modules/Tuyen/RouteModule.cs:59` method `ListAlias` — **VERIFIED_OPEN** (cmd=True, method=True, expect `ListAl`=True, evidence=`SOURCE_BEHAVIOR_REVIEWED`)
- **AUD-055** `ISURVEY_TAB_TO_CAD` → `src/Modules/Import/TabImportModule.cs:29` method `TabToCadCommand` — **VERIFIED_OPEN** (cmd=True, method=True, expect `TabToC`=True, evidence=`SOURCE_BEHAVIOR_REVIEWED`)
- **AUD-056** `ANTIN` → `src/Modules/Terrain/TerrainModule.cs:35` method `ToggleAlias` — **VERIFIED_OPEN** (cmd=True, method=True, expect `Toggle`=True, evidence=`SOURCE_BEHAVIOR_REVIEWED`)

## Phần chưa đọc đầy đủ (ảnh hưởng)

Các `*Service` / `*Dialog` xuất hiện trong thân command nhưng **chưa** mở hết file service:

- AUD-001:AiChatWindow — **CHƯA ĐỦ BẰNG CHỨNG** body service
- AUD-001:AuthManager — **CHƯA ĐỦ BẰNG CHỨNG** body service
- AUD-001:DocumentManager — **CHƯA ĐỦ BẰNG CHỨNG** body service
- AUD-002:AuthManager — **CHƯA ĐỦ BẰNG CHỨNG** body service
- AUD-002:DarkDialog — **CHƯA ĐỦ BẰNG CHỨNG** body service
- AUD-003:AuthManager — **CHƯA ĐỦ BẰNG CHỨNG** body service
- AUD-004:DocumentManager — **CHƯA ĐỦ BẰNG CHỨNG** body service
- AUD-004:OtpAuthService — **CHƯA ĐỦ BẰNG CHỨNG** body service
- AUD-005:ActivityLogger — **CHƯA ĐỦ BẰNG CHỨNG** body service
- AUD-005:AuthManager — **CHƯA ĐỦ BẰNG CHỨNG** body service
- AUD-006:AuthManager — **CHƯA ĐỦ BẰNG CHỨNG** body service
- AUD-007:AuthManager — **CHƯA ĐỦ BẰNG CHỨNG** body service
- AUD-007:DarkDialog — **CHƯA ĐỦ BẰNG CHỨNG** body service
- AUD-008:ActivityLogger — **CHƯA ĐỦ BẰNG CHỨNG** body service
- AUD-008:AuthManager — **CHƯA ĐỦ BẰNG CHỨNG** body service
- AUD-008:RibbonBuilder — **CHƯA ĐỦ BẰNG CHỨNG** body service
- AUD-009:UpdateFlow — **CHƯA ĐỦ BẰNG CHỨNG** body service
- AUD-010:ActivityLogger — **CHƯA ĐỦ BẰNG CHỨNG** body service
- AUD-010:AuthManager — **CHƯA ĐỦ BẰNG CHỨNG** body service
- AUD-010:DocumentManager — **CHƯA ĐỦ BẰNG CHỨNG** body service
- AUD-010:UserSettingsStore — **CHƯA ĐỦ BẰNG CHỨNG** body service
- AUD-011:ActivityLogger — **CHƯA ĐỦ BẰNG CHỨNG** body service
- AUD-011:AuthManager — **CHƯA ĐỦ BẰNG CHỨNG** body service
- AUD-011:DgnExportDialog — **CHƯA ĐỦ BẰNG CHỨNG** body service
- AUD-011:DgnExportService — **CHƯA ĐỦ BẰNG CHỨNG** body service
- AUD-011:DocumentManager — **CHƯA ĐỦ BẰNG CHỨNG** body service
- AUD-011:MapModule — **CHƯA ĐỦ BẰNG CHỨNG** body service
- AUD-011:TaskProgressWindow — **CHƯA ĐỦ BẰNG CHỨNG** body service
- AUD-012:ActivityLogger — **CHƯA ĐỦ BẰNG CHỨNG** body service
- AUD-012:AuthManager — **CHƯA ĐỦ BẰNG CHỨNG** body service
- AUD-012:DgnBatchService — **CHƯA ĐỦ BẰNG CHỨNG** body service
- AUD-012:DgnImportDialog — **CHƯA ĐỦ BẰNG CHỨNG** body service
- AUD-012:DgnImportService — **CHƯA ĐỦ BẰNG CHỨNG** body service
- AUD-012:DocumentManager — **CHƯA ĐỦ BẰNG CHỨNG** body service
- AUD-012:MapModule — **CHƯA ĐỦ BẰNG CHỨNG** body service
- AUD-012:TaskProgressWindow — **CHƯA ĐỦ BẰNG CHỨNG** body service
- AUD-013:ActivityLogger — **CHƯA ĐỦ BẰNG CHỨNG** body service
- AUD-013:AuthManager — **CHƯA ĐỦ BẰNG CHỨNG** body service
- AUD-013:DocumentManager — **CHƯA ĐỦ BẰNG CHỨNG** body service
- AUD-013:ImportPointsWindow — **CHƯA ĐỦ BẰNG CHỨNG** body service
- AUD-013:PointDrawService — **CHƯA ĐỦ BẰNG CHỨNG** body service
- AUD-014:ActivityLogger — **CHƯA ĐỦ BẰNG CHỨNG** body service
- AUD-014:AuthManager — **CHƯA ĐỦ BẰNG CHỨNG** body service
- AUD-014:DocumentManager — **CHƯA ĐỦ BẰNG CHỨNG** body service
- AUD-014:ExportPointsWindow — **CHƯA ĐỦ BẰNG CHỨNG** body service
- AUD-015:AuthManager — **CHƯA ĐỦ BẰNG CHỨNG** body service
- AUD-015:DocumentManager — **CHƯA ĐỦ BẰNG CHỨNG** body service
- AUD-016:ActivityLogger — **CHƯA ĐỦ BẰNG CHỨNG** body service
- AUD-016:AuthManager — **CHƯA ĐỦ BẰNG CHỨNG** body service
- AUD-016:DocumentManager — **CHƯA ĐỦ BẰNG CHỨNG** body service
- AUD-017:ActivityLogger — **CHƯA ĐỦ BẰNG CHỨNG** body service
- AUD-017:AuthManager — **CHƯA ĐỦ BẰNG CHỨNG** body service
- AUD-017:DocumentManager — **CHƯA ĐỦ BẰNG CHỨNG** body service
- AUD-018:ActivityLogger — **CHƯA ĐỦ BẰNG CHỨNG** body service
- AUD-018:AuthManager — **CHƯA ĐỦ BẰNG CHỨNG** body service
- AUD-018:DocumentManager — **CHƯA ĐỦ BẰNG CHỨNG** body service
- AUD-019:AuthManager — **CHƯA ĐỦ BẰNG CHỨNG** body service
- AUD-019:DocumentManager — **CHƯA ĐỦ BẰNG CHỨNG** body service
- AUD-020:AuthManager — **CHƯA ĐỦ BẰNG CHỨNG** body service
- AUD-020:DocumentManager — **CHƯA ĐỦ BẰNG CHỨNG** body service
- AUD-021:AuthManager — **CHƯA ĐỦ BẰNG CHỨNG** body service
- AUD-021:DocumentManager — **CHƯA ĐỦ BẰNG CHỨNG** body service
- AUD-022:ActivityLogger — **CHƯA ĐỦ BẰNG CHỨNG** body service
- AUD-022:AuthManager — **CHƯA ĐỦ BẰNG CHỨNG** body service
- AUD-022:DocumentManager — **CHƯA ĐỦ BẰNG CHỨNG** body service
- AUD-023:ActivityLogger — **CHƯA ĐỦ BẰNG CHỨNG** body service
- AUD-023:AuthManager — **CHƯA ĐỦ BẰNG CHỨNG** body service
- AUD-023:DocumentManager — **CHƯA ĐỦ BẰNG CHỨNG** body service
- AUD-024:ActivityLogger — **CHƯA ĐỦ BẰNG CHỨNG** body service
- AUD-024:AuthManager — **CHƯA ĐỦ BẰNG CHỨNG** body service
- AUD-024:DocumentManager — **CHƯA ĐỦ BẰNG CHỨNG** body service
- AUD-025:ActivityLogger — **CHƯA ĐỦ BẰNG CHỨNG** body service
- AUD-025:AuthManager — **CHƯA ĐỦ BẰNG CHỨNG** body service
- AUD-025:DocumentManager — **CHƯA ĐỦ BẰNG CHỨNG** body service
- AUD-026:ActivityLogger — **CHƯA ĐỦ BẰNG CHỨNG** body service
- AUD-026:AuthManager — **CHƯA ĐỦ BẰNG CHỨNG** body service
- AUD-026:DocumentManager — **CHƯA ĐỦ BẰNG CHỨNG** body service
- AUD-026:KmlExportService — **CHƯA ĐỦ BẰNG CHỨNG** body service
- AUD-026:MapModule — **CHƯA ĐỦ BẰNG CHỨNG** body service
- AUD-026:Vn2000SetupDialog — **CHƯA ĐỦ BẰNG CHỨNG** body service
- … và 225 mục nữa (xem hồ sơ 04/).

## Tự khớp số liệu

- CommandMethod = 125 (= alias 46 + primary 79)
- Canonical features = 79 (= primary)
- Alias resolved = 46 / unresolved = 0
- Ribbon = 61; placeholder = 25
