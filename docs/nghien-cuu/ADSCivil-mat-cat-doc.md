# Nghiên cứu ý tưởng: mô hình địa hình → tuyến → trắc dọc / ngang

**Ngày:** 2026-10-30 (sửa thứ tự pha: mô hình trước, không nhảy thẳng tới vẽ TD)  
**Nguồn tham khảo (máy văn phòng, bản quyền người dùng):** `C:\Program Files\BZS\ADSCivil NW 2026 For Autocad`  
**Phạm vi:** học tư duy nghiệp vụ từ ribbon / cây dự án / hộp thoại XML. **Không** decompile ARX, không sao chép mã, icon, CUIX, hộp thoại pixel, lớp `KS-TUY-*` / `TK-ADS-*` / `KS-DHI-*`, object `.dbx`, hay mẫu `defaultprofile.td`.

iSurvey vẽ bằng entity AutoCAD/Civil của mình, lệnh riêng, mẫu riêng, icon tự vẽ.

---

## 1. ADS Civil là gì (cấu trúc, không phải mã)

Bộ plugin ARX/DBX, nạp theo `ads-modules.lst`. Ribbon tách tab:

| Tab | Việc chính |
|-----|------------|
| Survey | Điểm đo, **mô hình địa hình (DTM)**, point cloud, lưới/khung bản đồ, đường cũ |
| Road | **Tuyến + cọc + trắc dọc + trắc ngang** + thiết kế + khối lượng |
| Level | Lưới cao độ / san nền |
| Sewer | Mạng ống, trắc dọc cống |
| Intersection | Nút giao |
| Road marking / VPV | Vạch kẻ, hộ lan, VPV |

Cây dự án (`PANEL_TREEVIEW`) bốn nhánh: **Điểm đo** → **Bề mặt địa hình** → **Lưới tuyến** → Khung bản đồ. Đó chính là thứ tự phụ thuộc dữ liệu.

Object tùy biến (`.dbx`) giữ DTM, tuyến, cọc, mặt cắt. iSurvey **không** dùng object đó; dữ liệu của mình (JSON/XData/NOD) + Polyline / 3DFace / Text chuẩn.

---

## 2. Chuỗi nghiệp vụ (đối chiếu nguồn — **đúng**)

Người dùng nêu, và nguồn xác nhận:

```
Tạo mô hình
    → thêm điểm, đường đồng mức, đường bao
    → Xây dựng mô hình  =  lưới tam giác (TIN)
Thêm tuyến
    → phát sinh cọc  (lấy Z từ bề mặt)
Vẽ cắt dọc
    → Hiệu chỉnh trắc dọc
```

Một chỗ cần tách rõ tên lệnh: **Tạo** và **Xây dựng** không phải một bước. Thêm dữ liệu nằm **giữa** hai lệnh đó. “Tạo lưới tam giác” là **kết quả của Xây dựng**, còn “Vẽ lưới tam giác” chỉ là **hiện** các tam giác lên bản vẽ.

### 2.1 Survey — mô hình địa hình trước hết

Ribbon Survey panel **Mô hình địa hình** chỉ có một nút lớn: **Tạo mô hình địa hình**. Việc còn lại nằm trên cây **Bề mặt địa hình**.

| Bước | Chỗ trên UI | Việc thật |
|------|-------------|-----------|
| 1. Tạo mô hình | Menu gốc bề mặt: **Tạo mới mô hình địa hình**. Hộp `IDD_DTM_INFO_CREATENEW` | Tạo **vỏ** có tên: tên mô hình, mô tả, lớp, vẽ line / 3D, **cạnh dài nhất**, tự cập nhật đồng mức, điểm phát sinh có tham gia không. Chưa có tam giác. |
| 2. Thêm đối tượng | Menu thành phần: **Thêm mới các đối tượng**; **Thêm đường bao** | Đưa điểm đo, polyline đồng mức (breakline có Z), đường bao vào mô hình. |
| 3. Xây dựng | Menu trên mô hình đã tạo: **Xây dựng mô hình địa hình** (`IDR_MENU_IN_DTM` id 32799) | Tam giác hóa (cạnh ≤ cạnh dài nhất). Đây mới là TIN. |
| 4. Hiện kết quả | Cùng menu: **Vẽ lưới tam giác**, **Vẽ đường đồng mức** | Đồng mức ở đây là **đầu ra** từ TIN, khác đồng mức **đầu vào** bước 2. Còn: lật cạnh, tra cao độ, phát sinh điểm, mặt cắt địa hình. |

Tham số mặc định trong `ads-config.xml` (`DTMSETTING`): cạnh dài nhất, tự cập nhật đồng mức, điểm phát sinh tham gia mô hình, lớp lưới tam giác.

**Đường đồng mức xuất hiện hai lần:**

- **Vào mô hình:** polyline 3D (hoặc đường có Z) làm cạnh bắt buộc khi tam giác hóa.
- **Ra khỏi mô hình:** vẽ lại đồng mức sau khi đã có TIN (và tùy chọn tự cập nhật khi rebuild).

iSurvey 1.8.16: nhập điểm, `GHISODM` ghi số đồng mức, mô hình đặt tên (`TAOMOHINH`), thêm điểm / đứt gãy / đồng mức / đường bao (`THEMMOHINH`), xây dựng lưới (`XAYMOHINH`). Không lọc cạnh dài. Đứt gãy và đồng mức là cạnh bắt buộc. Đường bao là chỗ cắt duy nhất. `ANTIN` ẩn hoặc hiện lưới của mô hình. `TRACAOD` lấy cao độ trên lưới. Chưa có tuyến, cọc, `VEMCD`.

### 2.2 Road — tuyến bám bề mặt, rồi mới cọc và TD

Cây **Lưới tuyến** / ribbon Road panel **Tuyến**:

| Bước | Chỗ trên UI | Việc thật |
|------|-------------|-----------|
| 5. Thêm tuyến | **Tạo tuyến** (polyline) hoặc từ tệp. Hộp tạo tuyến có ô **Bề mặt địa hình** | Tuyến 2D + lý trình gốc + lớp. **Gắn** vào DTM đã build. Tùy chọn: **Phát sinh cọc tự động**, **Trắc dọc tự động / Vẽ trắc dọc**, cả TN — đó là lối tắt, không bỏ bước 1–4. |
| 6. Phát sinh cọc | **Phát sinh cọc**. Hộp `IDD_ALIADDPOLE`: tuyến, **địa hình**, từ/đến lý trình, bước, cọc đặc biệt | Cọc lý trình trên tuyến; **Z lấy từ bề mặt**. Không có TIN thì không có cao độ tự nhiên. |
| 7. Vẽ cắt dọc | Panel Trắc dọc: **Vẽ trắc dọc tự nhiên**. Panel `IDD_PROFILEPANEL` tab **Vẽ TD tự nhiên** | Chiếu (lý trình, Z) ra view 2D hai tỷ lệ + đầu bảng. |
| 8. Hiệu chỉnh | **Hiệu chỉnh trắc dọc** | Sửa hình / số liệu TD đã vẽ; không thay cho bước vẽ. |

### 2.3 Phụ thuộc (không đảo được)

```
Điểm đo + đồng mức (Z) + đường bao
        ↓  Tạo mô hình (vỏ)
        ↓  Thêm đối tượng tham gia
        ↓  Xây dựng  →  TIN
Tuyến (XY, lý trình)  ──bám──►  bề mặt
        ↓  Phát sinh cọc  →  (tên, lý trình, Z tự nhiên)
        ↓  Vẽ trắc dọc tự nhiên
        ↓  Hiệu chỉnh trắc dọc
        ↓  RAIMCD / in  (iSurvey đã có phần rải khung)
```

Vẽ TD mà bỏ mô hình thì Z phải nhập tay hoặc lấy từng điểm — không phải chuỗi khảo sát đầy đủ, và không lấy được lát ngang sau này.

Hộp tạo tuyến cho phép tích “cọc tự động / TD tự động”: **gom bước 6–7**, vẫn đòi bề mặt ở bước 5.

---

## 3. Tư duy hình trắc dọc (sau khi đã có cọc + Z)

Mặt cắt dọc là **view 2D của tuyến**, không phải “vẽ polyline đẹp”:

```
Cọc (lý trình) + Z tự nhiên từ TIN
        ↓ chiếu
View: X = lý trình × tỷ lệ ngang, Y = (Z − mức so sánh) × tỷ lệ đứng
        ↓
Đầu bảng + đường đất + (sau) đường đỏ / địa chất
        ↓
Chia tờ in  ← iSurvey đã có RAIMCD
```

Đầu vào lúc vẽ: tuyến, cọc, Z, tỷ lệ H/V, mức so sánh.

Trên Model: thước cao độ, lưới ô tùy chọn, đường đất, ghi Z tại cọc, đầu bảng (Z TN, khoảng lẻ, cộng dồn, tên cọc, lý trình — mẫu cấu hình, không hard-code).

Hai lệnh, không gộp:

| Lệnh | Việc | Trạng thái |
|------|------|------------|
| **VEMCD** | **Vẽ** mặt cắt dọc trên Model (đường đất + đầu bảng) | Chưa có. Lệnh mới |
| **RAIMCD** | **Rải khung in** lên mặt cắt **đã vẽ** | Giữ nguyên, không đổi vai trò |

`RAIMCD` không vẽ mặt cắt. Hình `VEMCD` phải vừa cách `RAIMCD` đang đo (góc hàng cuối, cột tên, tỷ lệ), để không phải sửa lệnh rải khung.

---

## 4. Trắc ngang (ghi nhớ, sau TD)

Cùng tuyến + cọc + **cùng TIN**. Mỗi cọc một lát vuông góc tim. Z trên lát cũng lấy từ bề mặt. Không có mô hình thì không có TN tự nhiên.

---

## 5. Việc khác (sau chuỗi trên)

1. Trắc ngang tự nhiên.
2. Cong đứng / đường đỏ (thiết kế).
3. Địa chất trên TD/TN.
4. Khối lượng, áo đường, 3D — Road đầy đủ, không phải mục tiêu gần.

---

## 6. iSurvey hôm nay vs chỗ trống

| Đã có | Chưa có |
|--------|---------|
| Nhập/xuất điểm, ghi số đồng mức, mô hình đặt tên, thêm điểm/đứt gãy/đồng mức/bao, xây dựng TIN, ẩn/hiện lưới, tra cao độ | Lỗ địa hình và đám mây điểm trong lưới |
| `DUOITUYEN` — dải 100 m trên bình đồ | Tuyến nội bộ + lý trình (chưa phải Alignment Civil) |
| | Phát sinh cọc, lấy Z từ TIN dọc tuyến |
| `RAIMCD` — rải layout lên mặt cắt **đã vẽ** | **Vẽ** đường đất + đầu bảng |
| `INTUYEN` in layout `MatCat_Doc` | Mẫu đầu bảng iSurvey (JSON) |

Pha đề xuất khi được lệnh code (độc lập, không copy ADS):

1. **Pha 1 — mô hình địa hình:** đã có trong 1.8.16. Tạo mô hình, thêm điểm / đứt gãy / đồng mức / đường bao, xây dựng lưới, hiện/ẩn, tra cao độ. Không lọc cạnh dài.
2. **Pha 2 — tuyến + cọc:** polyline → tuyến + lý trình gốc; phát sinh cọc; Z từ mô hình.
3. **Pha 3 — vẽ TD tự nhiên + hiệu chỉnh;** rồi `RAIMCD`.
4. Sau: TN tự nhiên, đường đỏ, địa chất.

Không nhảy thẳng pha 3 khi chưa có nguồn Z ổn định.

---

## 7. Mô hình dữ liệu độc lập (phác)

Tên chỉ là hướng; khi code đặt trong namespace iSurvey.

- `TerrainModel` — id, tên, lớp, điểm, đứt gãy, đồng mức, đường bao, lưới tam giác. Không có cạnh dài nhất.
- `Route` — id, tên, polyline Id, lý trình gốc, chiều, id mô hình bám.
- `Station` — tên, chainage, Z tự nhiên, ghi chú.
- `GroundSample` — chainage, offset, Z, nguồn (tin/point/manual).
- `ProfileViewStyle` / `ProfileBandRow` / `ProfileView` — như cũ (tỷ lệ, mức so sánh, đầu bảng).

Lưu: NOD hoặc JSON cạnh DWG. Vẽ: Polyline + 3DFace/Mesh + DBText. Không custom entity. Không đọc object ADS.

---

## 8. Nguyên tắc độc lập (bắt buộc khi làm tiếp)

1. Không decompile ARX/DLL, không copy icon/CUIX/mẫu `.td`.
2. Không bắt chước bố cục hộp thoại từng ô; hộp iSurvey theo `CtaStyles`.
3. Lệnh AutoCAD riêng. Không trùng alias ADS.
4. Layer/kiểu chữ iSurvey (`iSurvey-DTM-*`, `iSurvey-TD-*`), không `KS-DHI-*` / `KS-TUY-*`.
5. Được **đọc** điểm / polyline / 3DFace / Civil Surface người dùng đã có; không đọc object ADS.

---

## 9. Giao diện: bảng dữ liệu tab dọc (tùy biến iSurvey)

Nguồn tham khảo dùng palette neo cạnh màn hình: **cây đối tượng phía trên**, **ô thuộc tính phía dưới**, **tab chữ xoay 90° bên phải**. Đó là chỗ làm việc hàng ngày (Tạo mô hình, thêm đối tượng, build… nằm menu chuột phải trên cây), Ribbon chỉ là lối tắt.

iSurvey **chuyển sang cùng kiểu bố cục**, nhưng không copy tên tab, icon, cây «Thoát nước / Lưới san nền / Nút giao».

### 9.1 Học gì

- Một cửa sổ neo (AutoCAD `PaletteSet` + WPF bên trong), không phải hộp thoại modal cho cả chuỗi.
- Tab dọc = từng **mảng việc iSurvey đã có**, không phải tab của hãng khác.
- Cây = đối tượng **trong bản vẽ đang mở**, đúng thứ tự phụ thuộc: điểm → mô hình → tuyến → trắc dọc.
- Ô dưới = thuộc tính nút đang chọn (tên, cạnh dài nhất, số tam giác, Dirty, Z cọc…). Trống thì chữ gợi ý, không để xám im lặng.
- Không hiện tab/module chưa làm (không treo icon X «chưa có giấy phép» cho việc iSurvey không làm).

### 9.2 Tab dọc của iSurvey (tên riêng)

| Tab (chữ dọc) | Cây / nội dung | Việc iSurvey tương ứng |
|---------------|----------------|------------------------|
| **Khảo sát** | Điểm đo → Mô hình địa hình → Tuyến → Trắc dọc | Chuỗi TD; tab mặc định |
| **Khung in** | Khung bản đồ, rải theo tuyến, rải mặt cắt | `KHUNG`, `RAIKHUNG`, `RAIMCD` |
| **UAV** | Ảnh UAV / DEM / point cloud đã chèn | Panel UAV đã có |
| **Bản đồ** | KML, ảnh vệ tinh, DGN | Panel GIS đã có |

Không đưa vào palette: nút giao, thoát nước, san nền, «quản lý dự án», «chia sẻ dữ liệu». Tài khoản giữ trên Ribbon.

Cây tab **Khảo sát** (mở dần khi có dữ liệu):

```
Điểm đo
Mô hình địa hình
  └ [Tên mô hình]     ← phải chuột: Thêm đối tượng, Xây dựng, Ẩn/hiện lưới, Tra cao độ
       ├ Điểm
       ├ Đứt gãy
       ├ Đường đồng mức
       └ Đường bao
Tuyến
  └ [Tên tuyến]       ← Tạo từ polyline, Phát sinh cọc
       └ Cọc
Trắc dọc
  └ [Tên mặt cắt]     ← VEMCD vẽ, lệnh khác hiệu chỉnh; RAIMCD chỉ rải khung (tab Khung in)
```

Gốc chưa có con: phải chuột «Tạo…». Không dùng chữ «Lưới tuyến» — iSurvey gọi **Tuyến**. **Trắc dọc** là nút cây hạng nhất (nguồn tham khảo giấu trong ribbon Road).

### 9.3 Kỹ thuật (khi code)

- `PaletteSet` tên **iSurvey**, neo trái, rộng ~280–320 px (laptop 14").
- **Một** palette chứa WPF: `TabControl` `TabStripPlacement=Right`, chữ tab xoay, màu `CtaStyles` (nền sáng). Icon tab tự vẽ.
- Lệnh `ISURVEY_PALETTE` (alias đề xuất `BANGIS`) + nút Ribbon «Bảng dữ liệu». Nhớ đang mở/đóng theo máy.
- Ribbon **không bỏ**: nhập điểm, rải khung, UAV, GIS vẫn là lệnh một phát. Palette là nơi **thấy và sửa đối tượng**.
- Đổi bản vẽ / đóng DWG: cây nạp lại từ NOD bản vẽ hiện hành.

## 10. Việc tiếp theo (khi được yêu cầu code)

1. Pha UI và pha 1 mô hình đã có trong 1.8.16, chưa phát hành.
2. Pha 2: tuyến + cọc, Z lấy từ lưới. Không vẽ TD cho đến khi lấy Z được.
3. Pha 3: `VEMCD`. `RAIMCD` vẫn chỉ rải khung.
