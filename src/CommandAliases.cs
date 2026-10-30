using Autodesk.AutoCAD.Runtime;
using iSurvey.Auth;
using iSurvey.Dgn;
using iSurvey.Modules.Export;
using iSurvey.Modules.Import;
using iSurvey.Modules.Map;
using iSurvey.Modules.Plan;
using iSurvey.Modules.Scan;
using iSurvey.Modules.Tuyen;
using iSurvey.Modules.Ai;
using iSurvey.Modules.Frame;
using iSurvey.Modules.Uav;
using iSurvey.Modules.DiemDo;

namespace iSurvey;

/// <summary>
/// Alias lệnh tắt iSurvey — tránh tổ hợp Telex tạo dấu tiếng Việt
/// (không dùng đuôi s/f/r/x/j sau nguyên âm; không dùng tiền tố IS = í).
/// </summary>
/// <remarks>
/// IG = Insert Google, SG = Satellite Google, XG = Xóa Google, EG = Export Google,
/// INK = Import KML, EXK = Export KML (EG vẫn dùng được),
/// D2C = DGN to CAD, C2D = CAD to DGN, T2C = TAB to CAD,
/// UAVCLIP = Cắt ảnh UAV theo biên, AK = bật tắt khung ảnh, AD = đưa ảnh xuống dưới,
/// GOC = Đánh số góc tuyến, STT = Đánh số thứ tự tại điểm click, DUOITUYEN = Duỗi thẳng tuyến, KHUNG = Chèn khung bản đồ A0–A4 (1/200…1/5000),
/// RAIKHUNG = Rải khung theo tuyến, SK = sửa thông tin khung,
/// UG = cập nhật ảnh Google, ND = nhập điểm đo, XD = xuất điểm đo,
/// AIA = Trợ lý AI (Admin-Only), AIC = cấu hình AI,
/// CBD = Cắt Bình Đồ (CATBD), SCN = Scan (AUTOSCAN; "ASC" bị Telex thành "ác" nên dùng SCN),
/// LGI = Login (ISURVEY_LOGIN), UPD = Update, ABT = About, LGO = Logout.
/// </remarks>
public sealed class CommandAliases
{
    private readonly MapModule _map = new();
    private readonly ExportModule _export = new();
    private readonly KmlImportModule _import = new();
    private readonly TabImportModule _tab = new();
    private readonly DgnModule _dgn = new();
    private readonly UavModule _uav = new();
    private readonly PointFileModule _points = new();
    private readonly AiAssistantModule _ai = new();
    private readonly VertexNumberingService _goc = new();
    private readonly StripMapService _strip = new();
    private readonly FrameService _frame = new();
    private readonly PlanModule _plan = new();
    private readonly ScanModule _scan = new();
    private readonly AuthModule _auth = new();

    [CommandMethod("IG")]
    public void AliasInsertMap() => _map.InsertMapCommand();

    [CommandMethod("SG")]
    public void AliasInsertSatellite() => _map.InsertSatelliteCommand();

    [CommandMethod("XG")]
    public void AliasDeleteGe() => _map.DeleteGeCommand();

    [CommandMethod("UG")]
    public void AliasUpdateGe() => _map.UpdateGeCommand();

    [CommandMethod("EG")]
    public void AliasExportKml() => _export.ExportKmlCommand();

    [CommandMethod("INK")]
    public void AliasImportKml() => _import.ImportKmlCommand();

    [CommandMethod("EXK")]
    public void AliasExportKmlShort() => _export.ExportKmlCommand();

    [CommandMethod("D2C")]
    public void AliasDgnToCad() => _dgn.DgnToCadCommand();

    [CommandMethod("T2C")]
    public void AliasTabToCad() => _tab.TabToCadCommand();

    [CommandMethod("C2D")]
    public void AliasCadToDgn() => _dgn.CadToDgnCommand();

    [CommandMethod("UAVCLIP")]
    public void AliasUavClip() => _uav.ClipCommand();

    [CommandMethod("AK")]
    public void AliasUavFrame() => _uav.FrameToggleCommand();

    [CommandMethod("AD")]
    public void AliasUavSendBack() => _uav.SendBackCommand();

    [CommandMethod("AU")]
    public void AliasUavInsert() => _uav.InsertStub();

    [CommandMethod("DEM")]
    public void AliasUavDem() => _uav.DemStub();

    [CommandMethod("DM")]
    public void AliasUavContour() => _uav.ContourStub();

    [CommandMethod("HT")]
    public void AliasUavCrs() => _uav.CrsStub();

    [CommandMethod("NP")]
    public void AliasPcImport() => _uav.PcImportStub();

    [CommandMethod("LP")]
    public void AliasPcFilter() => _uav.PcFilterStub();

    [CommandMethod("RL")]
    public void AliasPcGrid() => _uav.PcGridStub();

    [CommandMethod("GOC")]
    public void AliasGocTuyen() => _goc.NumberVerticesCommand();

    [CommandMethod("DUOITUYEN")]
    public void AliasStripMap() => _strip.StripMapCommand();

    [CommandMethod("KHUNG")]
    public void AliasFrame() => _frame.FrameCommand();

    [CommandMethod("KHUNGBV")]
    public void AliasTechFrame() => _frame.TechFrameCommand();

    [CommandMethod("ANLOGO")]
    public void AliasToggleLogo() => _frame.ToggleLogoCommand();

    [CommandMethod("ANLOGO1")]
    public void AliasToggleLogoOwner() => _frame.ToggleLogoOwnerCommand();

    [CommandMethod("ANLOGO2")]
    public void AliasToggleLogoDesign() => _frame.ToggleLogoDesignCommand();

    [CommandMethod("RAIKHUNG")]
    public void AliasRouteLayout() => _frame.RouteLayoutCommand();

    [CommandMethod("RAIMCD")]
    public void AliasProfileLayout() => _frame.ProfileLayoutCommand();

    [CommandMethod("INTUYEN", CommandFlags.UsePickSet | CommandFlags.Redraw)]
    public void AliasRoutePlot() => _frame.RoutePlotCommand();

    [CommandMethod("THAYLOGO")]
    public void AliasReplaceLogo() => _frame.ReplaceLogoCommand();

    [CommandMethod("CODAN")]
    public void AliasFitTitle() => _frame.FitTitleCommand();

    [CommandMethod("SK")]
    public void AliasEditFrame() => _frame.EditFrameInfoCommand();

    [CommandMethod("AIA")]
    public void AliasAiAssist() => _ai.AssistCommand();

    [CommandMethod("AIC")]
    public void AliasAiSettings() => _ai.SettingsCommand();

    [CommandMethod("ND")]
    public void AliasImportPoints() => _points.ImportPointsCommand();

    [CommandMethod("XD")]
    public void AliasExportPoints() => _points.ExportPointsCommand();

    [CommandMethod("CBD")]
    public void AliasCutPlan() => _plan.CutPlanCommand();

    [CommandMethod("SCN")]
    public void AliasAutoScan() => _scan.AutoScanCommand();

    // Tài khoản & hệ thống — tránh Telex: không dùng tiền tố IS (í) và đuôi s/f/r/x/j sau nguyên âm.
    [CommandMethod("LGI")]
    public void AliasLogin() => _auth.LoginCommand();

    [CommandMethod("UPD")]
    public void AliasUpdate() => _auth.UpdateCommand();

    [CommandMethod("ABT")]
    public void AliasAbout() => _auth.AboutCommand();

    [CommandMethod("HD")]
    public void AliasHelp() => _auth.HelpCommand();

    [CommandMethod("LGO")]
    public void AliasLogout() => _auth.LogoutCommand();
}

