using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using iSurvey.Auth;
using iSurvey.UI.Palette;
using iSurvey.UI.Terrain;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace iSurvey.Modules.Terrain;

public sealed class TerrainModule
{
    [CommandMethod("ISURVEY_TERRAIN_NEW")]
    public void NewCommand() => New();

    [CommandMethod("TAOMOHINH")]
    public void NewAlias() => New();

    [CommandMethod("ISURVEY_TERRAIN_ADD")]
    public void AddCommand() => Add();

    [CommandMethod("THEMMOHINH")]
    public void AddAlias() => Add();

    [CommandMethod("ISURVEY_TERRAIN_BUILD")]
    public void BuildCommand() => Build();

    [CommandMethod("XAYMOHINH")]
    public void BuildAlias() => Build();

    [CommandMethod("ISURVEY_TERRAIN_TOGGLE")]
    public void ToggleCommand() => Toggle();

    [CommandMethod("ANTIN")]
    public void ToggleAlias() => Toggle();

    [CommandMethod("ISURVEY_TERRAIN_Z")]
    public void QueryCommand() => Query();

    [CommandMethod("TRACAOD")]
    public void QueryAlias() => Query();

    private static void New()
    {
        if (!AuthManager.EnsureAuthenticated("TAOMOHINH"))
            return;
        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null || !ModelSpace(doc.Editor))
            return;

        var count = 1;
        try
        {
            count = TerrainStore.Load(doc.Database).Count + 1;
        }
        catch (System.Exception)
        {
            count = 1;
        }

        CreateTerrainRequest? request;
        try
        {
            request = CreateTerrainWindow.Prompt("Mô hình " + count);
        }
        catch (System.Exception ex)
        {
            doc.Editor.WriteMessage("\n[iSurvey] Không mở được hộp thoại: " + ex.Message);
            return;
        }

        if (request is null)
        {
            doc.Editor.WriteMessage("\n[iSurvey] Đã hủy tạo mô hình.");
            return;
        }

        var model = new TerrainRecord
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = request.Name,
            Layer = request.Layer
        };
        TerrainStore.Save(doc.Database, model);
        doc.Editor.WriteMessage("\n[iSurvey] Đã tạo mô hình \"" + model.Name + "\". Hãy thêm điểm, đứt gãy hoặc đồng mức, và đường bao, rồi xây dựng.");
        SurveyPaletteManager.Reload();
    }

    private static void Add()
    {
        if (!AuthManager.EnsureAuthenticated("THEMMOHINH"))
            return;
        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null || !ModelSpace(doc.Editor))
            return;
        var model = TerrainService.Choose(doc.Editor, doc.Database);
        if (model is null)
            return;
        var kind = Kind(doc.Editor);
        if (kind is null)
            return;
        var added = TerrainService.Add(doc, model, kind);
        var label = kind switch
        {
            "contour" => "đường đồng mức",
            "break" => "đường đứt gãy",
            "boundary" => "đường bao",
            _ => "nguồn điểm"
        };
        doc.Editor.WriteMessage("\n[iSurvey] Đã gắn " + added + " " + label + " vào \"" + model.Name + "\".");
        SurveyPaletteManager.Reload();
    }

    private static void Build()
    {
        if (!AuthManager.EnsureAuthenticated("XAYMOHINH"))
            return;
        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null || !ModelSpace(doc.Editor))
            return;
        var model = TerrainService.Choose(doc.Editor, doc.Database);
        if (model is null)
            return;
        try
        {
            var count = TerrainService.Build(doc, model);
            doc.Editor.WriteMessage("\n[iSurvey] Đã xây dựng " + count + " tam giác cho \"" + model.Name + "\".");
        }
        catch (System.Exception ex)
        {
            doc.Editor.WriteMessage("\n[iSurvey] Không xây dựng được: " + ex.Message);
        }

        SurveyPaletteManager.Reload();
    }

    private static void Toggle()
    {
        if (!AuthManager.EnsureAuthenticated("ANTIN"))
            return;
        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null)
            return;
        var pending = TerrainContext.ModelId;
        if (string.IsNullOrWhiteSpace(pending))
        {
            var models = TerrainStore.Load(doc.Database);
            if (models.Count == 0)
            {
                doc.Editor.WriteMessage("\n[iSurvey] Chưa có mô hình để ẩn hoặc hiện lưới.");
                return;
            }

            foreach (var model in models)
                Flip(doc, model);
            SurveyPaletteManager.Reload();
            return;
        }

        var one = TerrainService.Choose(doc.Editor, doc.Database);
        if (one is null)
            return;
        Flip(doc, one);
        SurveyPaletteManager.Reload();
    }

    private static void Query()
    {
        if (!AuthManager.EnsureAuthenticated("TRACAOD"))
            return;
        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null || !ModelSpace(doc.Editor))
            return;
        var model = TerrainService.Choose(doc.Editor, doc.Database);
        if (model is null)
            return;
        if (model.TriangleCount == 0)
        {
            doc.Editor.WriteMessage("\n[iSurvey] Mô hình chưa có lưới. Hãy xây dựng trước.");
            return;
        }

        var picked = doc.Editor.GetPoint("\nChọn vị trí cần tra cao độ: ");
        if (picked.Status != PromptStatus.OK)
            return;
        if (!TerrainService.TryQuery(model, picked.Value.X, picked.Value.Y, out var z))
        {
            doc.Editor.WriteMessage("\n[iSurvey] Điểm nằm ngoài lưới.");
            return;
        }

        doc.Editor.WriteMessage("\n[iSurvey] Cao độ Z = " + z.ToString("0.000") + " (" + model.Name + ").");
    }

    private static void Flip(Autodesk.AutoCAD.ApplicationServices.Document doc, TerrainRecord model)
    {
        var hadFaces = TerrainService.Toggle(doc, model);
        var state = model.MeshVisible ? "hiện" : "ẩn";
        if (!hadFaces)
            doc.Editor.WriteMessage("\n[iSurvey] \"" + model.Name + "\" chưa có lưới trên bản vẽ.");
        else
            doc.Editor.WriteMessage("\n[iSurvey] Đã " + state + " lưới \"" + model.Name + "\".");
    }

    private static string? Kind(Editor editor)
    {
        var pending = TerrainContext.TakeKind();
        if (pending is "point" or "contour" or "break" or "boundary")
            return pending;
        var options = new PromptKeywordOptions("\nThêm vào mô hình [Điểm/Đứt gãy/Đồng mức/Bao]")
        {
            AllowNone = false
        };
        options.Keywords.Add("Diem");
        options.Keywords.Add("Dutgay");
        options.Keywords.Add("Dongmuc");
        options.Keywords.Add("Bao");
        options.Keywords.Default = "Diem";
        var picked = editor.GetKeywords(options);
        if (picked.Status != PromptStatus.OK)
            return null;
        return picked.StringResult switch
        {
            "Dutgay" => "break",
            "Dongmuc" => "contour",
            "Bao" => "boundary",
            _ => "point"
        };
    }

    private static bool ModelSpace(Editor editor)
    {
        try
        {
            if (Convert.ToInt16(AcadApp.GetSystemVariable("TILEMODE")) == 1)
                return true;
            if (Convert.ToInt16(AcadApp.GetSystemVariable("CVPORT")) != 1)
                return true;
        }
        catch (System.Exception)
        {
            return true;
        }

        editor.WriteMessage("\n[iSurvey] Hãy mở Model Space trước khi làm mô hình.");
        return false;
    }
}
