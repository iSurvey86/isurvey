using Autodesk.AutoCAD.Runtime;
using iSurvey.Auth;
using iSurvey.Dgn;
using iSurvey.Modules.Export;
using iSurvey.Modules.Import;
using iSurvey.Modules.Map;
using iSurvey.Modules.Plan;
using iSurvey.Modules.Scan;

namespace iSurvey;

/// <summary>
/// Alias lệnh tắt iSurvey — tránh tổ hợp Telex tạo dấu tiếng Việt
/// (không dùng đuôi s/f/r/x/j sau nguyên âm; không dùng tiền tố IS = í).
/// </summary>
/// <remarks>
/// IG = Insert Google, SG = Satellite Google, XG = Xóa Google, EG = Export Google,
/// INK = Import KML, EXK = Export KML (EG vẫn dùng được),
/// D2C = DGN to CAD, C2D = CAD to DGN, DGNCFG = Cấu hình DGN,
/// CBD = Cắt Bình Đồ (CATBD), SCN = Scan (AUTOSCAN; "ASC" bị Telex thành "ác" nên dùng SCN),
/// LGI = Login (ISURVEY_LOGIN), UPD = Update, ABT = About, LGO = Logout.
/// </remarks>
public sealed class CommandAliases
{
    private readonly MapModule _map = new();
    private readonly ExportModule _export = new();
    private readonly KmlImportModule _import = new();
    private readonly DgnModule _dgn = new();
    private readonly PlanModule _plan = new();
    private readonly ScanModule _scan = new();
    private readonly AuthModule _auth = new();

    [CommandMethod("IG")]
    public void AliasInsertMap() => _map.InsertMapCommand();

    [CommandMethod("SG")]
    public void AliasInsertSatellite() => _map.InsertSatelliteCommand();

    [CommandMethod("XG")]
    public void AliasDeleteGe() => _map.DeleteGeCommand();

    [CommandMethod("EG")]
    public void AliasExportKml() => _export.ExportKmlCommand();

    [CommandMethod("INK")]
    public void AliasImportKml() => _import.ImportKmlCommand();

    [CommandMethod("EXK")]
    public void AliasExportKmlShort() => _export.ExportKmlCommand();

    [CommandMethod("D2C")]
    public void AliasDgnToCad() => _dgn.DgnToCadCommand();

    [CommandMethod("C2D")]
    public void AliasCadToDgn() => _dgn.CadToDgnCommand();

    [CommandMethod("DGNCFG")]
    public void AliasDgnConfig() => _dgn.DgnConfigCommand();

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

    [CommandMethod("LGO")]
    public void AliasLogout() => _auth.LogoutCommand();
}
