namespace iSurvey.Auth;

/// <summary>
/// Câu ghi vào nhật ký để admin đọc được mà không cần tra tên hàm.
/// Giữ ngắn hơn 80 ký tự (cột command_name trên Supabase cắt ở 80).
/// </summary>
internal static class ActivityPhrases
{
    private static readonly Dictionary<string, string> Commands = new(StringComparer.Ordinal)
    {
        ["DgnToCadCommand"] = "D2C (chuyển file DGN sang CAD)",
        ["ISURVEY_DGN_TO_CAD"] = "D2C (chuyển file DGN sang CAD)",
        ["D2C"] = "D2C (chuyển file DGN sang CAD)",
        ["AliasDgnToCad"] = "D2C (chuyển file DGN sang CAD)",

        ["CadToDgnCommand"] = "C2D (xuất CAD sang file DGN)",
        ["ISURVEY_CAD_TO_DGN"] = "C2D (xuất CAD sang file DGN)",
        ["C2D"] = "C2D (xuất CAD sang file DGN)",

        ["DgnConfigCommand"] = "DGNCFG (lưu cấu hình nhập DGN)",
        ["ISURVEY_DGN_CONFIG"] = "DGNCFG (lưu cấu hình nhập DGN)",
        ["DGNCFG"] = "DGNCFG (lưu cấu hình nhập DGN)",

        ["TabToCadCommand"] = "T2C (nhập file TAB MapInfo vào CAD)",
        ["ISURVEY_TAB_TO_CAD"] = "T2C (nhập file TAB MapInfo vào CAD)",
        ["T2C"] = "T2C (nhập file TAB MapInfo vào CAD)",

        ["InsertMapCommand"] = "IG (chèn bản đồ Google Earth)",
        ["ISURVEY_MAP"] = "IG (chèn bản đồ Google Earth)",
        ["IG"] = "IG (chèn bản đồ Google Earth)",

        ["InsertSatelliteCommand"] = "SG (chèn ảnh vệ tinh)",
        ["ISURVEY_MAP_SAT"] = "SG (chèn ảnh vệ tinh)",
        ["SG"] = "SG (chèn ảnh vệ tinh)",

        ["UpdateGeCommand"] = "Cập nhật ảnh vệ tinh theo khung nhìn",
        ["ISURVEY_UPDATE_GE"] = "Cập nhật ảnh vệ tinh theo khung nhìn",

        ["DeleteGeCommand"] = "XG (xóa ảnh vệ tinh trên bản vẽ)",
        ["ISURVEY_DELETE_GE"] = "XG (xóa ảnh vệ tinh trên bản vẽ)",
        ["XG"] = "XG (xóa ảnh vệ tinh trên bản vẽ)",

        ["ImportKmlCommand"] = "INK (nhập file KML/KMZ vào CAD)",
        ["ISURVEY_IMPORT_KML"] = "INK (nhập file KML/KMZ vào CAD)",
        ["INK"] = "INK (nhập file KML/KMZ vào CAD)",

        ["ExportKmlCommand"] = "EXK (xuất bản vẽ CAD sang KML/KMZ)",
        ["ISURVEY_EXPORT_KML"] = "EXK (xuất bản vẽ CAD sang KML/KMZ)",
        ["EG"] = "EXK (xuất bản vẽ CAD sang KML/KMZ)",
        ["EXK"] = "EXK (xuất bản vẽ CAD sang KML/KMZ)",

        ["AutoScanCommand"] = "SCN (quét cấu trúc bản vẽ, xuất báo cáo)",
        ["AUTOSCAN"] = "SCN (quét cấu trúc bản vẽ, xuất báo cáo)",
        ["SCN"] = "SCN (quét cấu trúc bản vẽ, xuất báo cáo)",

        ["CutPlanCommand"] = "CBD (cắt bình đồ theo đường biên)",
        ["CATBD"] = "CBD (cắt bình đồ theo đường biên)",
        ["CBD"] = "CBD (cắt bình đồ theo đường biên)",

        ["StripMapCommand"] = "DUOITUYEN (duỗi thẳng tuyến, xuất bảng góc)",
        ["ISURVEY_STRIP_MAP"] = "DUOITUYEN (duỗi thẳng tuyến, xuất bảng góc)",
        ["DUOITUYEN"] = "DUOITUYEN (duỗi thẳng tuyến, xuất bảng góc)",

        ["FrameCommand"] = "KHUNG (chèn khung bản đồ)",
        ["ISURVEY_KHUNG"] = "KHUNG (chèn khung bản đồ)",
        ["KHUNG"] = "KHUNG (chèn khung bản đồ)",

        ["TechFrameCommand"] = "KHUNGBV (chèn khung bản vẽ kỹ thuật)",
        ["ISURVEY_KHUNG_BV"] = "KHUNGBV (chèn khung bản vẽ kỹ thuật)",
        ["KHUNGBV"] = "KHUNGBV (chèn khung bản vẽ kỹ thuật)",

        ["ToggleLogoCommand"] = "ANLOGO (ẩn hoặc hiện logo EVN)",
        ["ISURVEY_LOGO"] = "ANLOGO (ẩn hoặc hiện logo EVN)",
        ["ANLOGO"] = "ANLOGO (ẩn hoặc hiện logo EVN)",
        ["ToggleLogoOwnerCommand"] = "ANLOGO1 (ẩn hoặc hiện logo chủ đầu tư)",
        ["ISURVEY_LOGO1"] = "ANLOGO1 (ẩn hoặc hiện logo chủ đầu tư)",
        ["ANLOGO1"] = "ANLOGO1 (ẩn hoặc hiện logo chủ đầu tư)",
        ["ToggleLogoDesignCommand"] = "ANLOGO2 (ẩn hoặc hiện logo đơn vị thiết kế)",
        ["ISURVEY_LOGO2"] = "ANLOGO2 (ẩn hoặc hiện logo đơn vị thiết kế)",
        ["ANLOGO2"] = "ANLOGO2 (ẩn hoặc hiện logo đơn vị thiết kế)",

        ["RouteLayoutCommand"] = "DANTRANG (dàn trang in dọc tuyến)",
        ["ISURVEY_ROUTE_SHEETS"] = "DANTRANG (dàn trang in dọc tuyến)",
        ["DANTRANG"] = "DANTRANG (dàn trang in dọc tuyến)",
        ["CATTUYEN"] = "DANTRANG (dàn trang in dọc tuyến)",
        ["RouteLayoutQuickCommand"] = "DANTRANG (không hộp thoại)",
        ["ISURVEY_ROUTE_SHEETS_QUICK"] = "DANTRANG (không hộp thoại)",
        ["RoutePlotCommand"] = "INTUYEN (in lại layout dàn trang)",
        ["ISURVEY_ROUTE_PLOT"] = "INTUYEN (in lại layout dàn trang)",
        ["INTUYEN"] = "INTUYEN (in lại layout dàn trang)",

        ["ReplaceLogoCommand"] = "THAYLOGO (thay logo khung tên bằng ảnh)",
        ["ISURVEY_LOGO_REPLACE"] = "THAYLOGO (thay logo khung tên bằng ảnh)",
        ["THAYLOGO"] = "THAYLOGO (thay logo khung tên bằng ảnh)",

        ["FitTitleCommand"] = "CODAN (co chữ chủ đầu tư về một dòng)",
        ["ISURVEY_FIT_TITLE"] = "CODAN (co chữ chủ đầu tư về một dòng)",
        ["CODAN"] = "CODAN (co chữ chủ đầu tư về một dòng)",

        ["NumberVerticesCommand"] = "GOC (đánh số góc tuyến)",
        ["ISURVEY_NUMBER_VERTICES"] = "GOC (đánh số góc tuyến)",
        ["GOC"] = "GOC (đánh số góc tuyến)",

        ["ClipCommand"] = "UAVCLIP (cắt ảnh UAV theo đường biên)",
        ["ISURVEY_UAV_CLIP"] = "UAVCLIP (cắt ảnh UAV theo đường biên)",
        ["UAVCLIP"] = "UAVCLIP (cắt ảnh UAV theo đường biên)",

        ["FrameToggleCommand"] = "Bật hoặc tắt khung ảnh",
        ["ISURVEY_UAV_FRAME_TOGGLE"] = "Bật hoặc tắt khung ảnh",
        ["SendBackCommand"] = "Đưa ảnh xuống dưới các đối tượng khác",
        ["ISURVEY_UAV_SEND_BACK"] = "Đưa ảnh xuống dưới các đối tượng khác",

        ["ISURVEY_UAV_INSERT"] = "Chèn ảnh UAV (đang phát triển)",
        ["ISURVEY_UAV_DEM"] = "Tạo DEM từ ảnh UAV (đang phát triển)",
        ["ISURVEY_UAV_CONTOUR"] = "Vẽ đường đồng mức (đang phát triển)",
        ["ISURVEY_UAV_CRS"] = "Đặt hệ tọa độ ảnh UAV (đang phát triển)",
        ["ISURVEY_PC_IMPORT"] = "Nhập đám mây điểm (đang phát triển)",
        ["ISURVEY_PC_FILTER"] = "Lọc đám mây điểm (đang phát triển)",
        ["ISURVEY_PC_GRID"] = "Lưới đám mây điểm (đang phát triển)",
        ["Stub"] = "Lệnh đang phát triển",

        ["LoginCommand"] = "LGI (đăng nhập)",
        ["ISURVEY_LOGIN"] = "LGI (đăng nhập)",
        ["LGI"] = "LGI (đăng nhập)",
        ["LogoutCommand"] = "LGO (đăng xuất, trả suất máy)",
        ["ISURVEY_LOGOUT"] = "LGO (đăng xuất, trả suất máy)",
        ["LGO"] = "LGO (đăng xuất, trả suất máy)",
        ["UpdateCommand"] = "UPD (kiểm tra và tải bản cập nhật)",
        ["ISURVEY_UPDATE"] = "UPD (kiểm tra và tải bản cập nhật)",
        ["UPD"] = "UPD (kiểm tra và tải bản cập nhật)",
        ["AboutCommand"] = "ABT (xem tài khoản và giấy phép)",
        ["ISURVEY_ABOUT"] = "ABT (xem tài khoản và giấy phép)",
        ["ABT"] = "ABT (xem tài khoản và giấy phép)",
        ["GenerateOtpCommand"] = "Tạo mã OTP trong ngày (máy admin)",
        ["ISURVEY_GENOTP"] = "Tạo mã OTP trong ngày (máy admin)",

        ["AssistCommand"] = "AIA (trợ lý AI)",
        ["ISURVEY_AI_ASSIST"] = "AIA (trợ lý AI)",
        ["AIA"] = "AIA (trợ lý AI)",
        ["SettingsCommand"] = "Mở cài đặt trợ lý AI",
        ["ISURVEY_AI_SETTINGS"] = "Mở cài đặt trợ lý AI",
        ["run_isurvey_command"] = "Trợ lý AI chạy một lệnh iSurvey",
        ["execute_cad"] = "Trợ lý AI chạy chuỗi lệnh CAD",
    };

    public static string? Command(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return name;
        return Commands.TryGetValue(name.Trim(), out var phrase) ? phrase : name.Trim();
    }
}


