using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using Autodesk.AutoCAD.Runtime;
using iSurvey.Auth;

namespace iSurvey.Modules.Uav;

/// <summary>Lệnh UAV &amp; LiDAR — clip ảnh thật; các lệnh còn lại giữ chỗ.</summary>
public sealed class UavModule
{
    [CommandMethod("ISURVEY_UAV_CLIP")]
    public void ClipCommand()
    {
        if (!AuthManager.EnsureAuthenticated(track: false)) return;
        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null) return;
        UavImageService.ClipByBoundary(doc.Editor, doc.Database);
    }

    [CommandMethod("ISURVEY_UAV_FRAME_TOGGLE")]
    public void FrameToggleCommand()
    {
        if (!AuthManager.EnsureAuthenticated(track: false)) return;
        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null) return;
        UavImageService.ToggleImageFrames(doc.Editor, doc.Database);
    }

    [CommandMethod("ISURVEY_UAV_SEND_BACK")]
    public void SendBackCommand()
    {
        if (!AuthManager.EnsureAuthenticated(track: false)) return;
        var doc = AcadApp.DocumentManager.MdiActiveDocument;
        if (doc is null) return;
        UavImageService.SendImageToBack(doc.Editor, doc.Database);
    }

    [CommandMethod("ISURVEY_UAV_INSERT")]
    public void InsertStub() => Stub("ISURVEY_UAV_INSERT");

    [CommandMethod("ISURVEY_UAV_DEM")]
    public void DemStub() => Stub("ISURVEY_UAV_DEM");

    [CommandMethod("ISURVEY_UAV_CONTOUR")]
    public void ContourStub() => Stub("ISURVEY_UAV_CONTOUR");

    [CommandMethod("ISURVEY_UAV_CRS")]
    public void CrsStub() => Stub("ISURVEY_UAV_CRS");

    [CommandMethod("ISURVEY_PC_IMPORT")]
    public void PcImportStub() => Stub("ISURVEY_PC_IMPORT");

    [CommandMethod("ISURVEY_PC_FILTER")]
    public void PcFilterStub() => Stub("ISURVEY_PC_FILTER");

    [CommandMethod("ISURVEY_PC_GRID")]
    public void PcGridStub() => Stub("ISURVEY_PC_GRID");

    private static void Stub(string command)
    {
        if (!AuthManager.EnsureAuthenticated(command)) return;
        AcadApp.ShowAlertDialog($"Tính năng đang phát triển\n({command})");
    }
}
