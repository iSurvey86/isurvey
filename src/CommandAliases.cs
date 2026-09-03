using Autodesk.AutoCAD.Runtime;
using iSurvey.Modules.Export;
using iSurvey.Modules.Map;

namespace iSurvey;

/// <summary>
/// Alias lệnh tắt iSurvey — tránh tổ hợp Telex tạo dấu tiếng Việt
/// (không dùng đuôi s/f/r/x/j sau nguyên âm; không dùng tiền tố IS = í).
/// </summary>
/// <remarks>
/// IG = Insert Google, SG = Satellite Google, XG = Xóa Google, EG = Export Google.
/// </remarks>
public sealed class CommandAliases
{
    private readonly MapModule _map = new();
    private readonly ExportModule _export = new();

    [CommandMethod("IG")]
    public void AliasInsertMap() => _map.InsertMapCommand();

    [CommandMethod("SG")]
    public void AliasInsertSatellite() => _map.InsertSatelliteCommand();

    [CommandMethod("XG")]
    public void AliasDeleteGe() => _map.DeleteGeCommand();

    [CommandMethod("EG")]
    public void AliasExportKml() => _export.ExportKmlCommand();
}
