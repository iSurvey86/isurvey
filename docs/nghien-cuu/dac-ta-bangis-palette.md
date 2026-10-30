# Đặc tả kỹ thuật: lệnh BANGIS (PaletteSet)

**Dự án:** iSurvey, AutoCAD / Civil 3D  
**Pha:** UI.1–UI.5, vỏ bảng dữ liệu  
**Khung mã:** `src/UI/Palette/`  
**Ngày:** 2026-10-30

Đặc tả này là bản đưa vào repo cho team. Khung đã dựng theo đây. Không copy giao diện phần mềm khác.

## 1. Lệnh

| | |
|--|--|
| CommandMethod | `ISURVEY_PALETTE` |
| Alias | `BANGIS` |
| Ribbon | Panel **Dữ liệu**, nút **Bảng dữ liệu** |
| Mục đích | Bảng neo trái. Chỗ làm việc cho chuỗi mô hình → tuyến → cọc → trắc dọc |

`RAIMCD` không nằm trong lệnh này. `RAIMCD` chỉ rải khung in. Vẽ mặt cắt sau này là `VEMCD`.

## 2. Kiến trúc

- AutoCAD `PaletteSet` + WPF `UserControl`.
- MVVM. File `.xaml.cs` chỉ gọi `InitializeComponent`.
- Không thêm NuGet MVVM. Dùng `ObservableObject` và `RelayCommand` trong thư mục Palette (ba target net48 / net8 / net10).
- Một `SurveyPaletteManager`, GUID cố định. AutoCAD nhớ neo và kích thước trong profile. Không tự ghi Registry thứ hai.
- `DocumentBecameCurrent` và `DocumentToBeDestroyed` nạp lại cây theo bản vẽ đang mở.
- Lệnh đã có (nhập điểm, khung, UAV, bản đồ) gửi bằng `SendStringToExecute`. Lệnh chưa làm chỉ báo trên dòng lệnh. Chưa mở `Transaction`. Khi pha sau sửa bản vẽ từ nút này, bọc `DocumentLock` và `Transaction`.

## 3. Giao diện

Nền xám `#E7EBF0`, không thẻ trắng quanh cây. Nút tab khác `CtaStyles`. `TabControl` dải tab bên phải, chữ xoay −90°. Tab đang chọn nền sáng, chữ xanh.

Bốn tab, không thêm tab khác:

1. **Khảo sát** (mặc định) — cây 70% và ô thuộc tính 30%, kéo được.
2. **Khung in**
3. **UAV**
4. **Bản đồ**

Cây bản vẽ trắng, bốn gốc, mỗi gốc một icon 16 px (`IconKind`):

- Điểm đo — chấm xanh. Chuột phải: Nhập điểm, Xuất điểm
- Mô hình địa hình — lớp xanh lá. Tạo mô hình… (chưa làm)
- Tuyến — nét cam. Tạo tuyến… (chưa làm)
- Trắc dọc — tờ tím. Vẽ mặt cắt (chưa làm)

Chưa có con: dòng «Phải chuột để tạo…» (điểm đo: «Phải chuột để nhập hoặc xuất.»).  
Chưa chọn mục: nhãn THUỘC TÍNH, ô viền đứt, chữ «Chọn một mục trên cây để xem và hiệu chỉnh».

## 4. File

| File | Vai trò |
|------|---------|
| `PaletteModule.cs` | Lệnh `ISURVEY_PALETTE` |
| `SurveyPaletteManager.cs` | Tạo, neo trái, đóng/mở, sự kiện bản vẽ |
| `SurveyPaletteView.xaml` | View |
| `PaletteViewModel.cs` | Tab, cây, mục chọn |
| `TreeNodeViewModel.cs` | Nút cây và menu |
| `TreeSelectionBehavior.cs` | Đưa mục chọn sang ViewModel |

Chưa có lớp `TerrainModel` / `RouteModel` / `ProfileModel`. Pha 1 sẽ thêm model thật trong namespace riêng, rồi cây đọc NOD. Không tạo model giả trùng tên ngay bây giờ.

## 5. Chưa làm trong vỏ này

- Đọc điểm, mô hình, tuyến từ bản vẽ
- Tạo mô hình, xây dựng lưới, phát sinh cọc, `VEMCD`
- Nhớ tab đang chọn giữa các lần mở Civil (neo và kích thước đã do PaletteSet nhớ)
