# HANDOFF — Phiên làm việc (mới nhất ở trên)

> **Máy khác:** `git pull` → đọc block **đầu tiên** dưới đây → tiếp tục chat.  
> **Cuối phiên:** `làm cuối phiên đầy đủ` (= HANDOFF → bump version + docs + changelog → commit + push). Chi tiết: [README](./README.md).

---

## Đã chốt — không hỏi lại

- Chuẩn hóa DGN theo TT 26/2024 và QCVN 72: user bỏ ngày 2026-10-23. Không làm bảng Level, màu, font theo quy chuẩn.
- Ô DỰ ÁN mặc định (thuộc tính `DU_AN`): `Đường dây và TBA 110KV KCN Minh Châu`.

---

## 2026-10-23 — Hộp F12 và nhật ký chi tiết (1.8.4)

**Máy / ngữ cảnh:** Cursor — máy văn phòng, Civil 3D 2026. Cuối phiên đầy đủ; phát hành **1.8.4**. DLL 2025 đã chép vào bundle máy này lúc Civil đóng, trước khi bump số phiên bản trong file; bản phát hành là 1.8.4.

### Đã chốt / đã làm

- **F12:** CAD 2025/2026 biến `[chữ]` thành mục chọn, nên `ISURVEY_MAP_SAT` chỉ hiện «1» và «iSurvey». Câu nhắc bỏ ngoặc vuông. Hộp F12 hiện **Xem lướt** / **Ghép ảnh HD** (gõ 1 hoặc 2 vẫn được), zoom 18/19/20, cắt UAV, xoay khung, lưới. CAD 2022 vốn in cả câu nên không lộ lỗi này. Chưa mở lại Civil sau khi chép DLL để nhìn hộp F12.
- **Nhật ký:** ghi một dòng khi lệnh xong, kèm tên file, bản vẽ, zoom, số tile hoặc số đối tượng. Xem lướt không có một file ảnh; ghép HD ghi tên `.jpg`. Trang quản trị (ksnpsc `4ce8b6f`) hiện phần `note`. Dòng cũ không có số liệu vì lúc đó chưa lưu.
- Máy này đã nhận DLL mới trong `%AppData%\Autodesk\ApplicationPlugins\iSurvey.bundle\Contents\Win64\2025` trước lúc đóng gói 1.8.4. Máy khác dùng **UPD**.

### File chính

| Khu vực | File |
|---------|------|
| Hộp F12 | `src/Modules/Map/MapModule.cs`, `HdMosaicWorkflow.cs`, `FrameService.cs`, `UavImageService.cs` |
| Nhật ký | `src/Auth/ActivityLogger.cs`, `ActivityPhrases.cs`, `AuthManager.cs` |
| Trang quản trị | repo `ksnpsc`, `src/lib/isurvey/activityLabels.js` (commit `4ce8b6f`) |

### Việc tiếp

- [ ] Mở Civil 2026, chạy `ISURVEY_MAP_SAT`, nhấn F12: thấy Xem lướt và Ghép ảnh HD. Chạy xong một lần rồi xem nhật ký có tên bản vẽ và số tile.
- [ ] Máy khác: **UPD** để nhận 1.8.4.
- [ ] Thử D2C trên bản sao file `.dgn`: ghép vào bản vẽ đang mở, rồi chế độ rời.
- [ ] Bản vẽ đã chèn khung cũ: `PURGE` block `KB_A*` rồi chèn lại để thấy tên dự án mới.

### Câu mở phiên sau

```text
git pull, đọc handoff và tiếp tục. iSurvey 1.8.4 — hộp F12 hiện đúng lựa chọn, nhật ký ghi tên file và số liệu khi lệnh xong. Tiếp: mở Civil kiểm tra F12 và một dòng nhật ký, hoặc thử nhập DGN thật trên bản sao.
```

**Lưu trữ ngày:** [2026-10-23-f12-nhat-ky.md](./2026-10-23-f12-nhat-ky.md)

---

## 2026-10-23 — CAD 2022, seed V8, khung tên, hộp thoại Nhập DGN (1.8.3)

**Máy / ngữ cảnh:** Cursor — máy văn phòng, Civil 3D 2026. Cuối phiên đầy đủ; phát hành **1.8.3**. Không có AutoCAD 2022 trên máy này.

### Đã chốt / đã làm

- **CAD 2022–2024** dùng một DLL `Contents/Win64/2023` build từ API 2022 (R24.1–R24.3). 2025–2027 giữ DLL riêng.
- **C2D:** bốn seed trong `src/Seeds` (V7 2D/3D và V8 2D/3D) đi theo bộ cài. Seed V8 là seed mét, không phải seed theo từng kinh tuyến.
- **D2C:** nhãn «Ghép thành một file .dwg» / «Rời từng file .dwg», tiền tố mặc định `iSurvey_`, bảng tệp có lưới mờ. Đã xem hộp thoại trên Civil 2026. Chưa nhập một file DGN thật.
- **KHUNG / KHUNGBV:** ô DỰ ÁN mặc định «Đường dây và TBA 110KV KCN Minh Châu». Bản vẽ đã có block `KB_A*` cũ cần `PURGE` rồi chèn lại.
- `CAI_DAT.txt`: thêm năm 2022; bỏ dòng admin giấy phép Supabase.

### File chính

| Khu vực | File |
|---------|------|
| CAD 2022 | `iSurvey.csproj`, `deploy/iSurvey.bundle/PackageContents.xml` |
| Seed V8 | `src/Seeds/VN2000_Seed2D_V8.dgn`, `VN2000_Seed3D_V8.dgn` |
| Nhập DGN | `src/Dgn/DgnImportWindow.xaml`, `DgnImportWindow.xaml.cs`, `DgnImportOptions.cs` |
| Khung tên | `src/Templates/KhungBanDo_Chuan.dwg`, `src/Modules/Frame/FrameService.cs` |
| Cài đặt | `deploy/CAI_DAT.txt` |

### Việc tiếp

- [ ] Máy khác: **UPD / Kiểm Tra Cập Nhật** để nhận 1.8.3 (đợi cache khoảng 60 giây). Máy AutoCAD 2022 cài bản này rồi mở CAD để xác nhận tab iSurvey.
- [ ] Thử D2C trên bản sao file `.dgn`: ghép vào bản vẽ đang mở, rồi chế độ rời.
- [ ] Bản vẽ đã chèn khung cũ: `PURGE` block `KB_A*` rồi chèn lại để thấy tên dự án mới.

### Câu mở phiên sau

```text
git pull, đọc handoff và tiếp tục. iSurvey 1.8.3 — CAD 2022, seed V8 đi kèm, ô DỰ ÁN mặc định, hộp thoại Nhập DGN (iSurvey_). Tiếp: thử nhập DGN thật trên bản sao, hoặc xác nhận tab iSurvey trên AutoCAD 2022.
```

**Lưu trữ ngày:** [2026-10-23-cad2022-seed-khung.md](./2026-10-23-cad2022-seed-khung.md)

---

## 2026-10-22 — Nhập TAB, GDAL dùng chung, hộp thoại Nhập DGN (1.8.2)

**Máy / ngữ cảnh:** Cursor — máy văn phòng, Civil 3D 2026. Cuối phiên đầy đủ; phát hành **1.8.2**. Bản **1.8.1** (TAB + GDAL dùng chung) đã có trên Supabase từ sớm hơn trong ngày; mã nguồn của phần đó vào git cùng commit 1.8.2.

### Đã chốt / đã làm

**1.8.1 (đã phát hành trước, mã nguồn commit cùng 1.8.2):**
- **T2C** nhập MapInfo TAB: hatch đặc theo màu MapInfo, layer `TAB_[MDSD]`, nhãn trên `TAB_LABELS`. Đổi TM ➔ TM bằng ProjNET khi kinh tuyến khác bản vẽ.
- Native GDAL một bản: `Contents/SharedDependencies/GdalNative`. Setup khoảng 40 MB. `TabGdal` nạp từ thư mục dùng chung.

**1.8.2 — Nhập DGN (D2C):**
- Hộp thoại nền sáng: bảng nhiều tệp (Thêm / Lên / Xuống / Xóa, cột STT, Tên tệp, Đường dẫn, Kích thước).
- Mặc định **ghép tọa độ** vào Model Space bản vẽ đang mở (không tạo bản vẽ mới). **Rời:** mỗi tệp một `.dwg` cùng thư mục.
- Layer: giữ Level gốc hoặc tiền tố `DGN_`. Bỏ mục ánh xạ JSON trên UI. TCVN3 bật, Explode Cell tắt. Dọn rác luôn chạy. Zoom Extents một lần ở tệp cuối khi ghép.
- Đã mở hộp thoại trên Civil 3D 2026 (`Drawing1.dwg` trống) và đối chiếu bố cục. Chưa chạy nhập một file DGN thật trong phiên này.

### File chính

| Khu vực | File |
|---------|------|
| Hộp thoại DGN | `src/Dgn/DgnImportWindow.xaml`, `DgnImportWindow.xaml.cs` |
| Ghép vào bản vẽ hiện hành | `src/Dgn/DgnBatchService.cs`, `DgnModule.cs`, `DgnImportOptions.cs` |
| TAB / GDAL | `src/Modules/Import/TabGdal.cs`, `deploy/build-bundle.ps1` |
| Phiên bản | `iSurvey.csproj`, `PackageContents.xml` |

### Việc tiếp

- [ ] Thử D2C trên bản sao file `.dgn`: ghép nhiều tệp vào một bản vẽ, rồi chế độ rời.
- [ ] Máy khác: **UPD / Kiểm Tra Cập Nhật** để nhận 1.8.2 (đợi cache khoảng 60 giây).
- [ ] Chuẩn hóa DGN theo quy chuẩn (khối NHẮC USER) vẫn hoãn. Bảng JSON không còn trên hộp thoại nhập.

### Câu mở phiên sau

```text
git pull, đọc handoff và tiếp tục. iSurvey 1.8.2 — hộp thoại Nhập DGN (ghép vào bản vẽ hiện hành hoặc tách .dwg). Tiếp: thử D2C trên bản sao file DGN thật.
```

**Lưu trữ ngày:** [2026-10-22-nhap-dgn.md](./2026-10-22-nhap-dgn.md)

---

## 2026-10-21 — Khung bản đồ / bản vẽ kỹ thuật, DUOITUYEN, trợ lý AI (1.8.0)

**Máy / ngữ cảnh:** Cursor — máy văn phòng, Civil 3D 2026. Cuối phiên đầy đủ; bump **1.8.0**. Mọi tính năng đã test thật trong Civil 3D 2026 (trên bản sao / bản vẽ mới).

### Đã chốt / đã làm

**Sản phẩm (AppVersion 1.8.0):**
- **`KHUNG` (`ISURVEY_KHUNG`)** khung bản đồ và **`KHUNGBV` (`ISURVEY_KHUNG_BV`)** khung bản vẽ kỹ thuật: A0–A4, 1/200…1/5000, TCVN 7285:2003; khung tên điền sẵn từ `INFOR.txt` + **logo EVN NPC vector** (chữ từ đường viền font Helvetica Black VU, không phụ thuộc font máy khác); KHUNG vẽ thêm lưới tọa độ CAD / mũi tên Bắc / thước tỷ lệ. 2 nút mới ở panel "Khung & Cắt Bình Đồ".
- **`DUOITUYEN`:** chọn tim tuyến bất kỳ + điểm đầu bắt buộc (DD, G1…Gn, DC), dải chữ nhật liền mạch, bảng góc `.txt` `P 43 34 53`.
- **Trợ lý AI (Admin-only):** `AIA` / `execute_cad` (đã code từ trước, nay đưa vào commit này).
- **FILEDIA/CMDDIA = 0** tự đặt lại 1 khi nạp add-in (lỗi do script test cũ của AI để lại).
- Đã `build-bundle.ps1 -Install` (3 target, 0 lỗi) và test trên Civil; **chưa** build Setup / upload Storage 1.8.0.

### File chính

| Khu vực | File |
|---------|------|
| Khung | `src/Modules/Frame/FrameService.cs`, `src/Templates/KhungBanDo_Chuan.dwg`, `docs/KhungBD/` (INFOR.txt, LogoEVN.png, mẫu) |
| Duỗi tuyến | `src/Modules/Tuyen/StripMapService.cs` |
| Trợ lý AI | `src/Modules/Ai/*`, `src/UI/Ai/*`, `docs/Huong_dan_Tro_ly_AI.md` |
| UI / đăng ký | `src/UI/RibbonBuilder.cs`, `RibbonIcons.cs`, `src/CommandAliases.cs`, `src/Main.cs`, `PackageContents.xml`, `iSurvey.csproj`, `build-bundle.ps1` |
| Chi tiết phiên | [2026-10-21-khung-ban-do-duoi-tuyen.md](./2026-10-21-khung-ban-do-duoi-tuyen.md) |

### Việc tiếp

- [ ] User mở lại Civil → kiểm 2 nút ribbon **Khung Bản Đồ** / **Khung Bản Vẽ Kỹ Thuật** (chưa nhìn thấy bằng mắt); thử `KHUNG` / `KHUNGBV` trên bản vẽ thật. Bản vẽ đã có block `KB_A*` cũ: `PURGE` trước.
- [x] Ô "DỰ ÁN" mặc định: `Đường dây và TBA 110KV KCN Minh Châu` (2026-10-23).
- [ ] Ý tưởng: lệnh nhập logo khác (ảnh/DWG → vector); form nhỏ chọn khổ/tỷ lệ/xoay cho KHUNG (user hiện chọn chỉ bật F12).
- [ ] Phát hành 1.8.0: `.\deploy\publish-release.ps1 -Notes "..."` (cần `ISURVEY_SERVICE_ROLE_KEY`); cập nhật `docs/Danh_sach_lenh_iSurvey.xlsx` (chưa thêm KHUNG, KHUNGBV, DUOITUYEN, AIA).
- [ ] **Nhắc user (đầu phiên):** chuẩn hóa DGN theo quy chuẩn (khối NHẮC USER) — vẫn hoãn. Từ nay mọi script test phải trả `FILEDIA`/`CMDDIA` về giá trị cũ.

### Câu mở phiên sau

```text
Đọc docs/phien-lam-viec/HANDOFF.md (block đầu và khối NHẮC USER). iSurvey 1.8.0 — KHUNG/KHUNGBV (khung chuẩn + logo vector), DUOITUYEN, trợ lý AI. Tiếp: kiểm nút ribbon khung trên Civil, chốt ô DỰ ÁN, hoặc phát hành 1.8.0 / nhắc chuẩn hóa DGN.
```

**Lưu trữ ngày:** [2026-10-21-khung-ban-do-duoi-tuyen.md](./2026-10-21-khung-ban-do-duoi-tuyen.md)

---

## 2026-10-19 (đêm) — Cập nhật tự tải + thoát CAD + tự cài (1.7.2)

**Máy / ngữ cảnh:** Cursor — máy có AutoCAD 2023. Cuối phiên đầy đủ; bump **1.7.2**. User đã cài thử build cục bộ (luồng UPD mới).

### Đã chốt / đã làm

**Sản phẩm (AppVersion 1.7.2):**
- **UPD / `ISURVEY_UPDATE`:** có bản mới → tải zip Setup → bắt buộc thoát AutoCAD/Civil → helper `Apply-iSurveyUpdate.ps1` chạy `Install-iSurvey.ps1` khi CAD đã đóng → báo hoàn tất. Không cài đè khi DLL còn khóa.
- Lỗi tải: vẫn mở link thủ công nếu cần. Nhật ký `update_download` / `update_apply` (gửi trước khi Quit).
- Đã build Setup `deploy/output/iSurvey-Setup-1.7.2.zip` + `release/version.json`; đã cài bundle local. **Chưa upload Storage** (máy này thiếu `ISURVEY_SERVICE_ROLE_KEY`).

### File chính

| Khu vực | File |
|---------|------|
| Update | `src/Auth/UpdateApplyService.cs`, `AuthModule.cs` (`ISURVEY_UPDATE`) |
| Helper | `deploy/Apply-iSurveyUpdate.ps1`, `build-release.ps1`, `CAI_DAT.txt` |
| Docs | `docs/Huong_dan_Phat_hanh.md` |

### Việc tiếp

- [ ] Upload 1.7.2: `.\deploy\publish-release.ps1 -SkipBuild -Notes "UPD tu tai + thoat CAD + tu cai"` (cần `ISURVEY_SERVICE_ROLE_KEY`).
- [ ] **Nhắc user (đầu phiên):** chuẩn hóa DGN theo quy chuẩn (khối NHẮC USER) — vẫn hoãn.
- [ ] Thử UPD end-to-end trên máy có bản cũ hơn 1.7.2 (sau khi upload Storage).
- [ ] Seed V8 VN-2000; các mục test cũ (khóa thiết bị, CBD/SCN, EG Lào Cai…).

### Câu mở phiên sau

```text
Đọc docs/phien-lam-viec/HANDOFF.md (block đầu và khối NHẮC USER). iSurvey 1.7.2 — UPD tự tải zip, thoát CAD, tự cài. Tiếp: nhắc chuẩn hóa DGN, hoặc thử UPD từ máy bản cũ, hoặc việc mới theo Mẫu_Tư_Vấn / Mẫu_Vá_Lỗi.
```

**Lưu trữ ngày:** [2026-10-19-cap-nhat-tu-cai.md](./2026-10-19-cap-nhat-tu-cai.md)

---

## 2026-10-19 (tối) — Bản vá 1.7.1: refresh token / nhật ký + rà soát

> Đã commit/push `691fc4b`; Storage latest 1.7.1. Ghi chú “CHƯA commit” dưới đây là lịch sử phiên trước khi đóng.

- **Lỗi nghiêm trọng đã sửa:** `ActivityLogger` refresh token mỗi sự kiện mà không lưu token mới ➔ license hết hiệu lực sớm. Nay `AuthManager.AcquireAccessToken` + `TokenGate` chung với recheck, lưu token mới ngay; `logout` được gửi thật (cùng bước trả suất thiết bị); `TrackError` được gọi từ các lệnh. Chi tiết: CHANGELOG 1.7.1.
- Sửa thêm: GOC (đỉnh trùng, tuyến khép kín, Z≠0), UAVCLIP (layer khóa, biên suy biến, DrawOrder theo block), `version.json` (`min_version`, BOM), `build-release.ps1 -Notes`, `schema_activity.sql` (giới hạn `detail` — **chạy lại trên Supabase**).
- Đã build 3 target + `build-release.ps1` ➔ `deploy/output/iSurvey-Setup-1.7.1.zip` + `release/version.json`. **Chưa** upload Storage, chưa cài thử, chưa test trên Civil.
- Việc tiếp: user đóng Civil ➔ cài 1.7.1 ➔ đăng nhập, chạy vài lệnh, kiểm tab Nhật ký ksnpsc (thấy `command`, `heartbeat`, rồi `logout`); upload zip + `version.json` lên bucket `isurvey-releases`; chạy lại `schema_activity.sql`; commit + push.
- **Phát hành một lệnh:** `deploy/publish-release.ps1` (khóa `ISURVEY_SERVICE_ROLE_KEY` trong biến môi trường user, không vào repo). Đã `-DryRun` OK; 1.7.1 đã upload thủ công (server báo latest 1.7.1). Lần sau chỉ cần: bump version ➔ `.\deploy\publish-release.ps1 -Notes "..."`.
- Giới hạn còn lại: tên lệnh trong nhật ký là tên hàm C# (vd `ClipCommand`), chưa phải tên lệnh CAD; `ISURVEY_UPDATE` gọi mạng đồng bộ trên luồng UI (tối đa 12 giây).

---

## 2026-10-19 — Phát hành Setup / update / activity + UAVCLIP + GOC (1.7.0)

**Máy / ngữ cảnh:** Cursor — máy văn phòng, Civil 3D 2026. Cuối phiên đầy đủ; bump **1.7.0**. Repo ksnpsc đã push tab Admin iSurvey riêng (`e264362`).

### Đã chốt / đã làm

**UAVCLIP (user: Auto-Clip mỹ mãn):**
- Panel Ribbon **UAV & LiDAR**; `ISURVEY_UAV_CLIP` / `UAVCLIP`: chọn ảnh → (1) polyline + Smart Filter / auto-pair hoặc (2) Auto-Clip nền trắng/đen (downsample ≤2048, contour, Douglas-Peucker, `TryApplyClipPolygonPixels`).

**GOC (user: rất tốt):**
- `ISURVEY_NUMBER_VERTICES` / `GOC`: đánh số góc tuyến, layer `ISURVEY_GOC_TUYEN` ACI 1, cỡ theo tỷ lệ bản đồ (mặc định 1:1000).

**Phát hành / update / nhật ký:**
- `deploy/build-release.ps1` → `iSurvey-Setup-1.7.0.zip` (Install/Uninstall + bundle). Đã upload Storage bucket public `isurvey-releases` (`version.json` + zip) — kiểm tra HTTP 200 OK.
- `ISURVEY_UPDATE` đọc manifest công khai, so semver, mở link tải.
- `schema_activity.sql` + `ActivityLogger` (login/logout/command/update_check/heartbeat); `schema_admin.sql` (views tổng hợp).
- **Admin UI:** tab **iSurvey** trong ksnpsc → Quản lý hệ thống (API service role). Docs: `Huong_dan_Phat_hanh.md`, `Huong_dan_Admin_ksnpsc.md`.

### File chính

| Khu vực | File |
|---------|------|
| Setup | `deploy/Install-iSurvey.*`, `Uninstall-iSurvey.*`, `build-release.ps1`, `CAI_DAT.txt` |
| Update | `src/Auth/UpdateCheckService.cs`, `AuthModule` (`ISURVEY_UPDATE`) |
| Activity | `src/Auth/ActivityLogger.cs`, `deploy/supabase/schema_activity.sql`, `schema_admin.sql` |
| UAV | `src/Modules/Uav/*`, `RasterClipService.cs`, `RibbonIconsUav.cs` |
| GOC | `src/Modules/Tuyen/VertexNumberingService.cs` |
| Admin ksnpsc | `ksnpsc/.../IsurveyAdminPanel.js`, `api/isurvey/*` (repo riêng) |

### Việc tiếp

- [ ] **Nhắc user (đầu phiên):** chuẩn hóa DGN theo quy chuẩn (khối NHẮC USER ở đầu file) — vẫn hoãn.
- [ ] **Vercel ksnpsc:** thêm `ISURVEY_SUPABASE_URL` + `ISURVEY_SERVICE_ROLE_KEY` nếu chưa → tab iSurvey mới tải được dữ liệu.
- [ ] Chạy `schema_admin.sql` trên Supabase iSurvey nếu chưa.
- [ ] Cài bản Setup 1.7.0 trên máy dùng thật; thử `ISURVEY_UPDATE` trên máy bản cũ hơn.
- [ ] Seed V8 VN-2000; kiểm DGN xuất trong MicroStation (tồn từ 1.6.0).
- [ ] (Tuỳ) CI auto build+upload release khi push — chưa làm.

### Câu mở phiên sau

```text
Đọc docs/phien-lam-viec/HANDOFF.md (block đầu và khối NHẮC USER). iSurvey 1.7.0 — Setup zip + update online (Storage OK) + activity/heartbeat; UAVCLIP + GOC đã OK trên Civil 2026; tab Admin iSurvey trên ksnpsc (cần env Vercel). Tiếp: nhắc chuẩn hóa DGN theo quy chuẩn, seed V8, hoặc việc mới.
```

**Lưu trữ ngày:** [2026-10-19-phat-hanh-uav-goc-admin.md](./2026-10-19-phat-hanh-uav-goc-admin.md)

---

## 2026-10-15 — DGN ➔ CAD (V7/V8, Batch) và CAD ➔ DGN (1.6.0)

**Máy / ngữ cảnh:** Cursor — máy văn phòng, Civil 3D 2026. Cuối phiên đầy đủ; **1.6.0** (HEAD trước đó là 1.5.2, nên 1.6.0 là bản phát hành duy nhất gồm D2C + Batch + C2D).

### Đã chốt / đã làm

**D2C (đã user kiểm chứng "rất tốt, đúng ý"):**
- Nhập V7/V8 bằng `-DGNIMPORT` + `DGNIMPORTMODE=2`; hoàn tất bất đồng bộ nên chờ `Handseed` ngừng đổi. `UNDO` 2 lần.
- V8: layer `<file> - Model$0$Level N` ➔ `Level N`; text V8 giải mã `N/64`, `∅`, `±` rồi TCVN3. Chữ hoa `.VnTimeH` ra chữ thường (giới hạn đã biết).
- **Batch**: Ghép (một bản vẽ mới chưa lưu, các mảnh liền nhau theo tọa độ) / Không ghép (mỗi file một `.dwg` cùng thư mục; trùng tên: đánh số / bỏ qua / ghi đè).
- Chế độ `DGNIMPORTMODE=0` lệch tọa độ +5×10⁸ / +10⁹ ➔ không dùng.

**C2D (`ISURVEY_CAD_TO_DGN`) — user đã xuất thành công trên DWG thật (Hải Dương, 1182 đối tượng, 3D):**
- Trình tự prompt `-DGNEXPORT`: `[V7/V8]` ➔ tên file ➔ `[Master/Sub]` ➔ mapping setup ➔ seed. Prompt seed chỉ nhận keyword hoặc `~` (hộp thoại modal) ➔ `SeedDialogDriver` luồng nền tự điền đường dẫn seed và bấm Open (ComboBox 13006 + IDOK).
- Seed: `Contents\Seeds\VN2000_Seed{2D|3D}_{V7|V8}.dgn` ➔ fallback seed metric của Civil. Bản vẽ có Z ≠ 0 bắt buộc seed 3D (`Detect3D`). Kẹt ở bước seed > 90 s ➔ tự hủy.
- Seed V7 2D tạo từ 5 element đầu của `DC2.dgn`; seed V7 3D = bản 2D vá byte TCB theo khác biệt 2D/3D của Civil. **Chưa có seed V8** chuẩn VN-2000 (V8 là OLE nén, không tạo được nếu không có MicroStation).
- `DgnExportService`: xuất từ bản sao tạm (Wblock) khi chọn "đối tượng đang chọn" hoặc bật Purge ➔ bản vẽ gốc không đổi. Có hộp thoại báo kết quả (nút mở thư mục).
- Headless test (Civil 2026): V7/V8 × chọn/toàn bộ × 2D/3D, nhập lại giữ đúng X/Y/Z.

**Build/cài:** `deploy\build-bundle.ps1 -Install` (Civil phải đóng). Bản 1.6.0 đã cài, kèm hộp thoại báo kết quả (user chưa xác nhận hộp thoại này).

### File chính

| Khu vực | File |
|---------|------|
| Lệnh | `src/Dgn/DgnModule.cs` |
| D2C | `DgnImportService.cs`, `DgnBatchService.cs`, `DgnImportWindow.xaml(.cs)`, `Tcvn3Converter.cs` |
| C2D | `DgnExportService.cs`, `DgnExportWindow.xaml(.cs)`, `DgnExportDialog.cs`, `DgnExportOptions.cs`, `DgnSeedLocator.cs`, `SeedDialogDriver.cs` |
| Seed | `src/Seeds/VN2000_Seed2D_V7.dgn`, `VN2000_Seed3D_V7.dgn` (+ build-bundle.ps1 copy vào `Contents\Seeds`) |
| Log | `%LocalAppData%\iSurvey\logs\dgn_*.log`, `c2d_*.log` |

### Việc tiếp

- [ ] **Nhắc user (đầu phiên):** chuẩn hóa theo quy chuẩn (xem khối nhắc ở đầu file) + user gửi thêm QCVN địa hình/địa chính.
- [ ] Seed V8 chuẩn VN-2000: user lưu từ MicroStation (`DC1.dgn` xóa hết đối tượng ➔ Save As `VN2000_Seed2D_V8.dgn`; thêm bản 3D) vào `src\Seeds\`, rồi build lại.
- [ ] User mở file DGN xuất trong MicroStation: kiểm Level (layer `0` ➔ level 63 theo mapping `Standard`), đơn vị, text tiếng Việt (font khi xuất chưa kiểm), view 3D của seed V7 3D (nếu ẩn đối tượng: Fit View).
- [ ] User xác nhận hộp thoại báo kết quả C2D hiện đúng.
- [ ] Batch xuất nhiều bản vẽ ➔ DGN (chưa có).
- [ ] Các mục test cũ: INK/EXK, lọc đỉnh, CAD 2024/2025/2027, khóa thiết bị Supabase, CBD/SCN, EG Lào Cai TM-3, MSI (khi được yêu cầu).

### Câu mở phiên sau

```text
Đọc docs/phien-lam-viec/HANDOFF.md (block đầu và khối NHẮC USER). iSurvey 1.6.0 — D2C (V7/V8, Batch) và C2D (xuất DGN V7/V8, seed tự động 2D/3D) đã chạy trên Civil 2026. Tiếp: nhắc user về chuẩn hóa theo quy chuẩn (Phụ lục 21/22, QCVN), seed V8 VN-2000, kiểm file DGN trong MicroStation, hoặc việc mới.
```

**Lưu trữ ngày:** [2026-10-15-dgn-d2c-c2d.md](./2026-10-15-dgn-d2c-c2d.md)

---

## 2026-10-13 — Ảnh HD ghép một raster (1.5.2)

**Máy / ngữ cảnh:** Cursor — máy văn phòng, Civil 3D 2026. Cuối phiên đầy đủ; bump **1.5.2**. User muốn hết rãnh ô vuông và ảnh mờ khi in, mà vẫn giữ xem lướt AutoRefresh.

### Đã chốt / đã làm

**Sản phẩm (AppVersion 1.5.2):**
- `ISURVEY_MAP_SAT` hỏi `[1. Xem luot (AutoRefresh) / 2. Ghep anh HD (Chon duong bao)]`. Enter = 1, luồng cũ.
- Chế độ 2: chọn Polyline đóng / Region / Circle. In số ô zoom 18, 19 (khuyên dùng), 20. Chặn mức nào vượt 400 ô hoặc cạnh ảnh vượt 32768 px.
- Tải `google-satellite` thêm `scale=2` (512×512), cache riêng `google-satellite-x2`. Ghép một JPEG chất lượng 93 vào `%LocalAppData%\iSurvey\mosaic\`.
- Gắn một `RasterImage` (`ISURVEY_HD_`), clip đường bao, DrawOrder xuống dưới cùng. Không bật AutoRefresh cho ảnh này.
- Refresh xem lướt không xóa ảnh HD. `ISURVEY_DELETE_GE` xóa cả tile xem lướt và ảnh HD.

**Đã kiểm:** build ba target. Bundle đã cài vào AppData sau khi user tắt Civil.

**Chưa test trên Civil:** user chưa chạy chế độ 2 trên đường bao thật.

### File chính

| Khu vực | File |
|---------|------|
| Lệnh | `src/Modules/Map/MapModule.cs` |
| Ghép ảnh | `src/Modules/Map/HdMosaicWorkflow.cs` |
| Tải scale=2 | `src/Modules/Map/TileDownloader.cs`, `TileCacheService.cs` |
| Giữ ảnh HD khi refresh | `src/Modules/Map/RasterClearService.cs`, `SurveyRasterHelper.cs` |

### Việc tiếp

- [ ] User test chế độ 2 trên Civil 2026: đường bao vừa, zoom 19, hết rãnh ô, nét hơn xem lướt, pan/zoom không đổi ảnh HD.
- [ ] User test: đóng Civil, mở lại, `INK` — hộp thoại hiện, danh sách là tên tỉnh.
- [ ] Test lọc đỉnh trên file địa giới dày (bật Douglas-Peucker, đối chiếu với lần tắt).
- [ ] Test `EXK`: chọn đối tượng rồi mới hộp lưu file; hủy chọn thì không hỏi lưu.
- [ ] Mở thử AutoCAD/Civil 2024, 2025, 2027 khi có bản cài.
- [ ] Test khóa thiết bị, hết suất 2 máy, và `LGO` trả suất trên Supabase.
- [ ] Test `CBD`, `SCN`, EG Lào Cai TM-3 trên DWG thật.
- [ ] MSI phát hành nội bộ — làm khi được yêu cầu.

### Câu mở phiên sau

```text
Đọc docs/phien-lam-viec/HANDOFF.md (block đầu). iSurvey 1.5.2 — ISURVEY_MAP_SAT có chế độ xem lướt và ghép ảnh HD (một raster, scale=2, clip đường bao). Bundle đã cài. Tiếp: test ảnh HD trên Civil 2026, hoặc INK / EXK, hoặc tính năng mới theo Mẫu_Tư_Vấn / vá lỗi theo Mẫu_Vá_Lỗi.
```

**Lưu trữ ngày:** [2026-10-13-anh-hd-ghep-raster.md](./2026-10-13-anh-hd-ghep-raster.md)

---

## 2026-10-13 — Vá hộp thoại kinh tuyến trục (1.5.1)

**Máy / ngữ cảnh:** Cursor — máy văn phòng, Civil 3D 2026. Cuối phiên đầy đủ; bump **1.5.1**. User mở `INK` sau 1.5.0 và hộp thoại không hiện.

### Đã chốt / đã làm

**Sản phẩm (AppVersion 1.5.1):**
- Ô chọn tỉnh chỉ còn `DisplayMemberPath="ProvinceName"`. Đã bỏ `ItemTemplate`. WPF không cho đặt cả hai, nên `InitializeComponent` ném `Set property 'ItemTemplate'` và lệnh in `Không mở được hộp thoại`.
- Nhãn vẫn **Chọn kinh tuyến trục**. Tên tỉnh vẫn hiện trong danh sách. `INK`, `EXK` và lần chèn ảnh đầu tiên dùng chung cửa sổ này.

**Đã kiểm:** build ba target và cài bundle. Chưa mở lại Civil để bấm `INK`.

**Chưa test:** user sẽ đóng Civil, mở lại, chạy `INK`. Các mục test của 1.5.0 vẫn mở.

### File chính

| Khu vực | File |
|---------|------|
| Hộp thoại | `src/UI/Vn2000/Vn2000SetupWindow.xaml` |
| Phiên bản | `iSurvey.csproj`, `deploy/iSurvey.bundle/PackageContents.xml` |

### Việc tiếp

- [ ] User test: đóng Civil, mở lại, `INK` — hộp thoại hiện, danh sách là tên tỉnh.
- [ ] Test lọc đỉnh trên file địa giới dày (bật Douglas-Peucker, đối chiếu với lần tắt).
- [ ] Test `EXK`: chọn đối tượng rồi mới hộp lưu file; hủy chọn thì không hỏi lưu.
- [ ] Mở thử AutoCAD/Civil 2024, 2025, 2027 khi có bản cài.
- [ ] Test khóa thiết bị, hết suất 2 máy, và `LGO` trả suất trên Supabase.
- [ ] Test `CBD`, `SCN`, EG Lào Cai TM-3 trên DWG thật.
- [ ] MSI phát hành nội bộ — làm khi được yêu cầu.

### Câu mở phiên sau

```text
Đọc docs/phien-lam-viec/HANDOFF.md (block đầu). iSurvey 1.5.1 — vá hộp thoại Chọn kinh tuyến trục (bỏ ItemTemplate trùng DisplayMemberPath). User sắp test INK trên Civil 2026. Tiếp theo kết quả test đó, hoặc lọc đỉnh / EXK, hoặc tính năng mới theo Mẫu_Tư_Vấn / vá lỗi theo Mẫu_Vá_Lỗi.
```

**Lưu trữ ngày:** [2026-10-13-sua-hop-thoai-tinh.md](./2026-10-13-sua-hop-thoai-tinh.md)

---

## 2026-10-13 — Nhập KML/KMZ + hộp thoại VN-2000 (1.5.0)

**Máy / ngữ cảnh:** Cursor — máy văn phòng, Civil 3D 2026. Cuối phiên đầy đủ; bump **1.5.0**. User đã nhập KML địa giới trên Civil và chốt luồng xuất.

### Đã chốt / đã làm

**Sản phẩm (AppVersion 1.5.0):**
- **Panel Bản đồ:** hai nút lớn Nhập (`INK`) và Xuất (`EXK`, giữ `EG`). Ba nút nhỏ: Chèn ảnh vệ tinh, Cập nhật ảnh (`ISURVEY_UPDATE_GE`), Xóa ảnh. Lệnh `IG` vẫn còn, không còn nút trên ribbon.
- **`Vn2000SetupWindow`:** chọn kinh tuyến trục (63 tỉnh, Độ/Phút), múi 3°/6°. Tùy chọn nhập hoặc xuất hiện theo lệnh. Nền tối. Ghi vào `UserSettingsStore`.
- **Nhập:** chọn file trước. KMZ lấy file `.kml` đầu tiên. Chiếu bằng `CoordinateService.ToVn2000`. Layer `KML_IMPORT`. Xong thì phóng vừa vùng vừa nhập.
- **Douglas-Peucker** tắt mặc định. Bật mới lọc đỉnh đường/vùng, sai số mét trên mặt phẳng. Điểm không lọc. Dưới 3 đỉnh thì giữ nguyên vòng.
- **Xuất:** thiết lập → chọn đối tượng (hoặc cả bản vẽ) → hỏi tên file → ghi → mở Google Earth nếu đã tick. Hủy chọn hoặc không có hình thì không hỏi lưu.

**Đã kiểm:** build ba target. Trên Civil 3D 2026: nhập KML chạy, user xác nhận. Ô tỉnh đã hiện tên tỉnh, không còn tên lớp C#.

**Chưa test:** lọc đỉnh trên file địa giới dày; xuất theo thứ tự mới trên bản vẽ thật; CAD 2024, 2025, 2027.

### File chính

| Khu vực | File |
|---------|------|
| Nhập | `src/Modules/Import/`, `src/Core/GeoMath.cs` |
| Hộp thoại | `src/UI/Vn2000/Vn2000SetupWindow.xaml` |
| Xuất | `src/Modules/Export/ExportModule.cs`, `KmlExportService.cs` |
| Ribbon | `src/UI/RibbonBuilder.cs`, `RibbonIcons.cs`, `CommandAliases.cs`, `PackageContents.xml` |

### Việc tiếp

- [ ] Test lọc đỉnh trên file địa giới dày (bật Douglas-Peucker, đối chiếu với lần tắt).
- [ ] Test `EXK`: chọn đối tượng rồi mới hộp lưu file; hủy chọn thì không hỏi lưu.
- [ ] Mở thử AutoCAD/Civil 2024, 2025, 2027 khi có bản cài.
- [ ] Test khóa thiết bị, hết suất 2 máy, và `LGO` trả suất trên Supabase.
- [ ] Test `CBD`, `SCN`, EG Lào Cai TM-3 trên DWG thật.
- [ ] MSI phát hành nội bộ — làm khi được yêu cầu.

### Câu mở phiên sau

```text
Đọc docs/phien-lam-viec/HANDOFF.md (block đầu). iSurvey 1.5.0 — nhập KML/KMZ (INK), xuất EXK theo thứ tự thiết lập → chọn đối tượng → lưu file, hộp thoại Chọn kinh tuyến trục. Đã nhập thử trên Civil 2026. Tiếp: test lọc đỉnh và EXK, hoặc CAD 2024–2027, hoặc tính năng mới theo Mẫu_Tư_Vấn / vá lỗi theo Mẫu_Vá_Lỗi.
```

**Lưu trữ ngày:** [2026-10-13-nhap-kml-vn2000.md](./2026-10-13-nhap-kml-vn2000.md)

---

## 2026-10-12 — Hỗ trợ CAD/Civil 2023–2027 (1.4.0)

**Máy / ngữ cảnh:** Cursor — máy có AutoCAD 2023 (R24.2, .NET Framework 4.8). Cuối phiên đầy đủ; bump **1.4.0**. Không đóng MSI (user hoãn).

### Đã chốt / đã làm

**Sản phẩm (AppVersion 1.4.0):**
- **Một bundle, ba DLL.** AutoCAD/Civil tự chọn theo series:
  - 2023–2024 (R24.2–R24.3): `Contents/Win64/2023`, .NET Framework 4.8, API 2023. 2024 tương thích nhị phân với plugin 2023.
  - 2025–2026 (R25.0–R25.1): `Contents/Win64/2025`, .NET 8, API 2025. 2026 nạp được plugin build cho 2025.
  - 2027 (R26.0): `Contents/Win64/2027`, .NET 10, build riêng. Không dùng DLL của 2025/2026.
- Cùng mã nguồn build được cả ba target. API lấy từ NuGet `AutoCAD.NET`, không copy DLL AutoCAD vào bundle.
- Bản 2023 nạp ProjNET và System.Text.Json từ thư mục plugin (`AssemblyResolve`). Trusted Location trỏ `Contents\Win64\...`.
- Catalog Excel bổ sung `CATBD`/`CBD`, `AUTOSCAN`/`SCN`, `LGI`/`UPD`/`ABT`/`LGO`, `ISURVEY_GENOTP`.

**Đã kiểm:** build Release cả `net48`, `net8.0-windows`, `net10.0-windows`. Trên AutoCAD 2023 máy này: bundle tự nạp, `ISURVEY_GENOTP` chạy (máy không có AdminSecret).

**Chưa chạy thử:** AutoCAD/Civil 2024, 2025, 2026, 2027 (không cài trên máy này).

### File chính

| Khu vực | File |
|---------|------|
| Target / API | `iSurvey.csproj` |
| Bundle | `deploy/iSurvey.bundle/PackageContents.xml`, `deploy/build-bundle.ps1`, `deploy/Set-iSurveyTrustedPath.ps1` |
| .NET Framework | `src/FrameworkAssemblyResolve.cs`, `src/Compat/FrameworkString.cs`, `src/Core/Digest.cs` |
| Catalog | `docs/Danh_sach_lenh_iSurvey.xlsx` |

### Việc tiếp

- [ ] Mở thử AutoCAD/Civil 2024, 2025, 2026, 2027 khi có bản cài (xác nhận tab iSurvey và một lệnh).
- [ ] Test khóa thiết bị, hết suất 2 máy, và `LGO` trả suất trên Supabase.
- [ ] Test `CBD` trên bản sao DWG có TIN và đồng mức Z>0.
- [ ] Test `SCN` trên vùng nhỏ, đọc file `.txt`.
- [ ] Xác nhận EG Lào Cai TM-3 xuất KMZ được.
- [ ] MSI phát hành nội bộ — user hoãn, làm khi được yêu cầu.

### Câu mở phiên sau

```text
Đọc docs/phien-lam-viec/HANDOFF.md (block đầu). iSurvey 1.4.0 — một bundle cho AutoCAD/Civil 2023–2027 (net48 / net8 / net10). Đã tự nạp trên AutoCAD 2023 máy này. Tiếp: chạy thử 2024–2027 khi có CAD, hoặc test thu hồi/hết suất/LGO, hoặc tính năng mới theo Mẫu_Tư_Vấn / vá lỗi theo Mẫu_Vá_Lỗi.
```

**Lưu trữ ngày:** [2026-10-12-ho-tro-cad-2023-2027.md](./2026-10-12-ho-tro-cad-2023-2027.md)

---

## 2026-10-12 — Đăng nhập OTP Supabase (1.3.0)

**Máy / ngữ cảnh:** Cursor — máy văn phòng (Civil 3D 2026 host .NET 10); cuối phiên đầy đủ; bump **1.3.0**. Project Supabase đã chạy; user nhận mail OTP và Ribbon hiện **Đã kích hoạt**.

### Đã chốt / đã làm

**Sản phẩm (AppVersion 1.3.0):**
- **Panel 6 Tài khoản & Hệ thống.** Panel 1–5 khóa khi chưa kích hoạt. Nút tài khoản: "Đăng Nhập / Kích Hoạt" hoặc email + "Đã kích hoạt".
- **Đăng nhập Gmail + OTP 6 số**, tự xác thực khi đủ 6 số. Sai: "Mã không đúng hoặc đã hết hạn. Còn N lần thử." Sai 5 lần thì khóa 5 phút.
- **Supabase** (HttpClient, không NuGet): gửi OTP, verify, RPC `isurvey_activate` / `isurvey_check` / `isurvey_release_device`. Tối đa **2 thiết bị / Gmail**.
- **Giấy phép cục bộ** `license.dat` (DPAPI + MachineGuid). Trực tuyến: kiểm tra ngầm mỗi 24 giờ (timeout 5 giây, không chặn lúc mở Civil); mất mạng vẫn dùng tới 7 ngày. Mã Admin / Debug: giấy phép ngoại tuyến 7 ngày.
- **Đăng xuất / Đổi máy** trả suất thiết bị (dòng đang active). Dòng Admin đã khóa không tự xóa được.
- Alias: **LGI**, **UPD**, **ABT**, **LGO**. Lệnh admin `ISURVEY_GENOTP` (cần `AdminSecret` trên máy đó, không nằm trong repo).
- URL + publishable key nằm trong `AuthConfig.cs` để máy khác pull về là kích hoạt được. SMTP App Password chỉ nằm trên Supabase.

**Đã kiểm:** test lõi offline (Debug 114, Release 118) và harness cửa sổ đăng nhập. Trên Civil thật: gửi mã, nhận mail, kích hoạt, Ribbon mở khóa.

**Chưa test trên Civil:** thu hồi (`is_active = false`), đủ 2 thiết bị, đăng xuất trả suất, kiểm tra lại sau 24 giờ.

### File chính

| Khu vực | File |
|---------|------|
| Auth | `src/Auth/` (`AuthManager`, `OtpAuthService`, `SupabaseAuthClient`, `LicenseStorage`, `LicenseRechecker`) |
| UI | `src/UI/Account/LoginWindow`, `DarkDialog`, `src/UI/RibbonBuilder.cs` |
| Supabase | `deploy/supabase/schema.sql`, `docs/Huong_dan_Supabase.md` |
| Lệnh / deploy | `src/Auth/AuthModule.cs`, `CommandAliases.cs`, `PackageContents.xml`, `iSurvey.csproj` |

### Việc tiếp

- [ ] Test khóa thiết bị, hết suất 2 máy, và `LGO` trả suất trên Supabase.
- [x] Bổ sung lệnh Auth, `CATBD`/`CBD` và `AUTOSCAN`/`SCN` vào `docs/Danh_sach_lenh_iSurvey.xlsx` (catalog trước đó dừng ở 1.1.0).
- [ ] Test `CBD` trên bản sao DWG có TIN và đồng mức Z>0.
- [ ] Test `SCN` trên vùng nhỏ, đọc file `.txt`.
- [ ] Xác nhận EG Lào Cai TM-3 xuất KMZ được.

### Câu mở phiên sau

```text
Đọc docs/phien-lam-viec/HANDOFF.md (block đầu). iSurvey 1.3.0 — đăng nhập Gmail OTP qua Supabase, tối đa 2 thiết bị, Ribbon panel Tài khoản. Đã kích hoạt thành công trên Civil máy văn phòng. Tiếp: test thu hồi / hết suất / đăng xuất, hoặc tính năng mới theo Mẫu_Tư_Vấn / vá lỗi theo Mẫu_Vá_Lỗi.
```

**Lưu trữ ngày:** [2026-10-12-dang-nhap-otp-supabase.md](./2026-10-12-dang-nhap-otp-supabase.md)

---

## 2026-10-12 — Ribbon 5 panel + CATBD + AUTOSCAN (1.2.0)

**Máy / ngữ cảnh:** Cursor — máy văn phòng (Civil 3D 2026 host .NET 10); cuối phiên đầy đủ; bump **1.2.0**.

### Đã chốt / đã làm

**Sản phẩm (AppVersion 1.2.0):**
- **Ribbon 5 panel:** Bản đồ, Khung & Cắt Bình Đồ, Biên tập Địa hình, Tuyến & Cột Điện, Hồ sơ & QA/QC. Icon vector (gradient, nét trắng). Nhãn đã chỉnh: **Chèn GE**, **Cắt Bình Đồ**. User đã xem trên Civil.
- **`CATBD` / alias `CBD`:** cắt bình đồ theo biên khép kín. Whitelist layer khung; CIRCLE ngoài whitelist xóa; TEXT dùng AlignmentPoint khi căn lề; INSERT dùng tâm GeometricExtents; LINE/polyline cắt từng đoạn, nội suy Z; cung LWPOLYLINE tách bulge; Polyline3d spline-fit bỏ ControlVertex rồi tạo SimplePoly. Không dùng TRIM. Một transaction — `UNDO` một lần.
- **`AUTOSCAN` / alias `SCN`:** quét vùng, xuất `CAD_AutoScan_Report.txt` (3 phần), mở Notepad. Mã DXF mô phỏng từ thuộc tính .NET (không EntGet). Alias không dùng `ASC` (Telex ra "ác").
- **Nút chưa có lệnh:** `RibbonPlaceholderHandler` in ASCII, không đăng ký lệnh giả.
- **Target tự chọn:** `iSurvey.csproj` và `build-bundle.ps1` đọc `acdbmgd.runtimeconfig.json` → net8 hoặc net10. Máy này host net10; máy host net8 vẫn build net8.

**Đã kiểm:** lõi cắt hình học 18 ca (dự án thử tạm, không nằm trong repo). Ribbon đã cài và user xác nhận giao diện.

**Chưa test trên DWG thật:** `CBD` với TIN/đồng mức; `SCN` file báo cáo; EG Lào Cai xuất KMZ sau bundle mới.

### File chính

| Khu vực | File |
|---------|------|
| Ribbon | `src/UI/RibbonBuilder.cs`, `RibbonIcons.cs`, `RibbonPlaceholderHandler.cs` |
| Cắt bình đồ | `src/Modules/Plan/PlanModule.cs`, `PlanGeometry.cs`, `PlanPaths.cs` |
| Quét cấu trúc | `src/Modules/Scan/ScanModule.cs`, `ScanReport.cs` |
| Lệnh / deploy | `src/Main.cs`, `CommandAliases.cs`, `PackageContents.xml`, `iSurvey.csproj`, `deploy/build-bundle.ps1` |

### Việc tiếp

- [ ] Test `CBD` trên bản sao DWG có TIN và đồng mức Z>0.
- [ ] Test `SCN` trên vùng nhỏ, đọc file `.txt`.
- [ ] Xác nhận EG Lào Cai TM-3 xuất KMZ được (bundle đã cài lại trong phiên).
- [ ] (Tuỳ) Mosaic lớn phục vụ Layout / in.
- [ ] (Tuỳ) Lưu settings trong DWG; toggle AutoRefresh trên UI.
- [ ] Bổ sung HDSD / workflow.

### Câu mở phiên sau

```text
Đọc docs/phien-lam-viec/HANDOFF.md (block đầu). iSurvey 1.2.0 — ribbon 5 panel, CATBD/CBD, AUTOSCAN/SCN, target net8 hoặc net10 theo host. Tiếp: test CBD và SCN trên DWG thật, hoặc tính năng mới theo Mẫu_Tư_Vấn / vá lỗi theo Mẫu_Vá_Lỗi.
```

**Lưu trữ ngày:** [2026-10-12-ribbon-catbd-autoscan.md](./2026-10-12-ribbon-catbd-autoscan.md)

---

## 2026-09-03 — net8 autoload + vá KML TM-3 + TM-6 TT973 (1.1.1)

**Máy / ngữ cảnh:** Cursor — máy văn phòng (Civil 3D 2026 / .NET 8); cuối phiên đầy đủ; bump **1.1.1**.

### Đã chốt / đã làm

**Sản phẩm (AppVersion 1.1.1):**
- **Retarget `net8.0-windows`:** plugin autoload trên Civil 3D 2026 (tab **iSurvey** trên Ribbon); `build-bundle.ps1` path net8.
- **TM-6 kinh tuyến chuẩn TT 973:** 105 / 111 / 117 — `CoordinateService`, Map + Export UI.
- **63 tỉnh/thành:** `isurvey_province_crs_map.json`; `FindSavedGroup` tương thích settings nhóm tỉnh cũ.
- **Vá Export KML TM-3:** `IsValidCentralMeridian(cm, zone)` — TM-3 102°–117° (vd Lào Cai 104.75°); TM-6 chỉ 105/111/117. Map Insert dùng chung.

**Debug phiên:** NETLOAD im lặng (evtx không có lỗi .NET) → xác định mismatch net10 vs host net8. Video + Event Viewer đã phân tích.

**Hoãn:** test EG Lào Cai sau cài lại bundle (Civil 3D phải thoát khi `-Install`).

### File chính

| Khu vực | File |
|---------|------|
| Deploy / runtime | `iSurvey.csproj`, `deploy/build-bundle.ps1`, `PackageContents.xml` |
| CRS / validation | `CoordinateService.cs` (`Tm6CentralMeridians`, `IsValidCentralMeridian`) |
| UI Map + Export | `MapInsertWindow.*`, `KmlExportWindow.xaml.cs` |
| Dữ liệu tỉnh | `src/Data/isurvey_province_crs_map.json` |
| Docs | `README.md`, `docs/changelog/CHANGELOG.md` |

### Việc tiếp

- [ ] Cài bundle 1.1.1 (`build-bundle.ps1 -Install`) và xác nhận EG Lào Cai TM-3 xuất được.
- [ ] (Tuỳ) Mosaic lớn phục vụ Layout / in.
- [ ] (Tuỳ) Palette dọc khi có thêm module.
- [ ] (Tuỳ) Lưu settings trong DWG; toggle AutoRefresh trên UI.
- [ ] Bổ sung HDSD / workflow Map + Export.

### Câu mở phiên sau

```text
Đọc docs/phien-lam-viec/HANDOFF.md (block đầu). iSurvey 1.1.1 — net8 autoload Civil 3D 2026, TM-6 105/111/117, 63 tỉnh, vá EG TM-3. Tiếp: test EG sau cài bundle, hoặc tính năng mới theo Mẫu_Tư_Vấn / vá lỗi theo Mẫu_Vá_Lỗi.
```

**Lưu trữ ngày:** [2026-09-03-net8-load-va-kml-tm3.md](./2026-09-03-net8-load-va-kml-tm3.md)

---

## 2026-09-03 — Export KML/KMZ + TM-6 + .NET 8 (1.1.0)

**Máy / ngữ cảnh:** Cursor — máy văn phòng (Civil 3D 2026 / .NET 8); cuối phiên đầy đủ; bump **1.1.0**.

### Đã chốt / đã làm

**Sản phẩm (AppVersion 1.1.0):**
- **Xuất Google Earth:** lệnh `ISURVEY_EXPORT_KML` (alias **EG**); module `src/Modules/Export/*` + UI `src/UI/KmlExport/*`.
  - Mặc định KMZ; phạm vi toàn bản vẽ / selection; group-by-layer; Use Z tắt mặc định; mở file sau xuất.
  - Không Dim / layer ẩn-đóng băng; block = điểm insert (không explode).
  - Style: màu CAD **1:1 RGB** → KML `aabbggrr` (không remap đen→trắng); outline không fill; strip MText; ẩn pin vàng; LineWeight → width.
- **Múi chiếu TM-3 / TM-6** (Map + Export): radio UI; mặc định TM-3; TM-6 snap kinh tuyến **105 / 111 / 117** theo Thông tư 973/2001/TT-TCĐC; cùng TOWGS84; `ZoneWidthDegrees` trong settings / session / pipeline CRS.
- **Alias lệnh:** `IG`=MAP, `SG`=MAP_SAT, `XG`=DELETE_GE, `EG`=EXPORT_KML (`CommandAliases.cs` + PackageContents).
- **Ribbon:** nút xuất KML/KMZ.
- **Target:** `net8.0-windows` (Civil R25.1 .NET 8); `deploy/build-bundle.ps1` path net8; `System.Drawing.Common` optional/framework.
- **Catalog lệnh:** `docs/Danh_sach_lenh_iSurvey.xlsx` (shortcut 2 ký tự ưu tiên, tránh xung đột acad.pgp / Telex).

**Nghiên cứu (không copy code):** HHMaps `C:\Hhmaps2019` — style KMZ tách loại, province/zone ini.

**Hoãn:** palette UI dọc (chờ thêm module).

### File chính

| Khu vực | File |
|---------|------|
| Export KML | `src/Modules/Export/*`, `src/UI/KmlExport/*`, `src/Models/KmlExportSettings.cs` |
| CRS / múi | `CoordinateService.cs`, `BasemapSession.cs`, Map + Export UI |
| Alias / ribbon | `src/CommandAliases.cs`, `RibbonBuilder.cs`, `PackageContents.xml` |
| Deploy | `iSurvey.csproj` (net8), `deploy/build-bundle.ps1` |
| Catalog | `docs/Danh_sach_lenh_iSurvey.xlsx` |

### Việc tiếp

- [ ] (Tuỳ) Mosaic lớn phục vụ Layout / in.
- [ ] (Tuỳ) Palette dọc khi có thêm module.
- [ ] (Tuỳ) Lưu settings trong DWG; toggle AutoRefresh trên UI.
- [ ] Bổ sung HDSD / workflow Map + Export.

### Câu mở phiên sau

```text
Đọc docs/phien-lam-viec/HANDOFF.md (block đầu). iSurvey 1.1.0 — Export KML/KMZ (EG), TM-3/TM-6 Map+Export, alias IG/SG/XG/EG. Tiếp: tính năng mới theo Mẫu_Tư_Vấn hoặc vá lỗi theo Mẫu_Vá_Lỗi.
```

**Lưu trữ ngày:** [2026-09-03-export-kml-tm6.md](./2026-09-03-export-kml-tm6.md)

---

## 2026-09-02 — Nền tảng Map tile GE + VN-2000 + handoff (1.0.0)

**Máy / ngữ cảnh:** Cursor — cuối phiên đầy đủ; dọn HANDOFF ksnpsc; khởi tạo handoff riêng repo **iSurvey**.

### Đã chốt / đã làm

**Sản phẩm (giữ AppVersion 1.0.0 — không bump, phiên chỉ docs):**
- Add-in **.NET 8 x64** cho AutoCAD / Civil 3D **2026**: chèn tile Google Earth (satellite / hybrid) georeference **VN-2000 TM-3**.
- Pipeline **per-tile 256×256**, disk cache, `ChooseZoom` / `FitZoom` (≤128 tiles), **AutoRefresh** theo pan/zoom, ẩn `IMAGEFRAME`.
- Lệnh: `ISURVEY_MAP`, `ISURVEY_MAP_SAT`, `ISURVEY_DELETE_GE`.
- Bundle autoload: `deploy/build-bundle.ps1` → `%AppData%\Autodesk\ApplicationPlugins\iSurvey.bundle` + TRUSTEDPATHS.
- **Clip theo biên** khi chèn (CAD clip, không mosaic/resize).
- **Xóa GE:** xác nhận Yes/No → xóa **toàn bộ** tile GE (không xóa theo trong/ngoài biên; không dùng Wipeout).

**Quy trình / docs:**
- `docs/phien-lam-viec/` — HANDOFF + README (cụm「làm cuối phiên đầy đủ」); mẫu tư vấn / vá lỗi.
- `docs/changelog/CHANGELOG.md` — khởi tạo.
- Root `README.md` — trỏ tới handoff.

### File chính

| Khu vực | File |
|---------|------|
| Lệnh / module | `src/Modules/Map/MapModule.cs`, `GeDeleteWorkflow.cs` |
| Tile / refresh | `TileAttachService.cs`, `BasemapRefreshService.cs`, `AutoRefreshController.cs`, `TileCacheService.cs` |
| Clip / xóa | `RasterClipService.cs`, `RasterDeleteService.cs`, `PolygonClipHelper.cs` |
| Deploy | `deploy/build-bundle.ps1`, `deploy/iSurvey.bundle/PackageContents.xml` |
| Docs quy trình | `docs/Mẫu_Tư_Vấn.md`, `docs/Mẫu_Vá_Lỗi.md`, `docs/phien-lam-viec/*`, `docs/changelog/CHANGELOG.md` |

### Việc tiếp

- [ ] (Tuỳ) Xuất một mosaic lớn phục vụ Layout / in (đã thảo luận, chưa làm).
- [ ] (Tuỳ) Lưu settings trong DWG; toggle AutoRefresh trên UI.
- [ ] Bổ sung HDSD / workflow module Map khi cần user cuối.

### Câu mở phiên sau

```text
Đọc docs/phien-lam-viec/HANDOFF.md (block đầu). iSurvey 1.0.0 — Map tile GE + VN-2000 + clip biên + xóa toàn bộ GE. Tiếp: tính năng mới theo Mẫu_Tư_Vấn hoặc vá lỗi theo Mẫu_Vá_Lỗi.
```

**Lưu trữ ngày:** [2026-09-02-nen-tang-map-tile.md](./2026-09-02-nen-tang-map-tile.md)

---
