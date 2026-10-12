# 2026-10-12 — Ribbon 5 panel, CATBD, AUTOSCAN

## Ngữ cảnh

Máy văn phòng, Civil 3D 2026 host **.NET 10** (R25.1). User **XÁC NHẬN** 10 mục tư vấn rồi bổ sung 3 chi tiết: spline-fit bỏ ControlVertex, AUTOSCAN không bắt buộc EntGet, alias chống Telex.

## Trạng thái đã chốt

- Ribbon 5 panel, icon `DrawingVisual`. Nút: **Chèn GE**, **Cắt Bình Đồ** (không ghi tên lệnh trên nút).
- `CATBD` / alias **CBD**. `AUTOSCAN` / alias **SCN** (không dùng ASC vì Telex ra "ác").
- Nút chưa có lệnh: `RibbonPlaceholderHandler`, thông báo ASCII, không đăng ký lệnh giả.
- `iSurvey.csproj` + `build-bundle.ps1` chọn net8 hoặc net10 theo `acdbmgd.runtimeconfig.json`.
- Lõi cắt hình học: 18 ca thử độc lập (ngoài repo) đều đạt. Chưa có dự án test trong solution.
- User xem ribbon trên Civil và chốt nhãn nút. Chưa test cắt trên DWG có TIN / đồng mức.

## Việc mở

- Test `CBD` trên bản sao DWG có TIN và đồng mức Z>0.
- Test `SCN` trên vùng nhỏ, xem file `.txt`.
- Xác nhận EG Lào Cai TM-3 xuất KMZ (bundle đã cài lại trong phiên).
