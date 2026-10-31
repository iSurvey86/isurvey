using System.Globalization;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using AcadColor = Autodesk.AutoCAD.Colors.Color;
using ColorMethod = Autodesk.AutoCAD.Colors.ColorMethod;

namespace iSurvey.Modules.Terrain;

/// <summary>Thêm thành phần, dựng lưới và tra cao độ.</summary>
public static class TerrainService
{
    public const string AppName = "ISURVEY_DTM";

    public static TerrainRecord? Choose(Editor editor, Database db)
    {
        var models = TerrainStore.Load(db);
        if (models.Count == 0)
        {
            editor.WriteMessage("\n[iSurvey] Chưa có mô hình. Hãy tạo mô hình trước.");
            return null;
        }

        var pending = TerrainContext.TakeModel();
        if (!string.IsNullOrWhiteSpace(pending))
        {
            foreach (var model in models)
            {
                if (string.Equals(model.Id, pending, StringComparison.OrdinalIgnoreCase))
                    return model;
            }
        }

        if (models.Count == 1)
            return models[0];

        editor.WriteMessage("\n[iSurvey] Các mô hình trong bản vẽ:");
        for (var i = 0; i < models.Count; i++)
            editor.WriteMessage("\n  " + (i + 1) + ". " + models[i].Name);
        var options = new PromptIntegerOptions("\nSố thứ tự mô hình")
        {
            LowerLimit = 1,
            UpperLimit = models.Count
        };
        var picked = editor.GetInteger(options);
        if (picked.Status != PromptStatus.OK)
            return null;
        return models[picked.Value - 1];
    }

    public static int Add(Autodesk.AutoCAD.ApplicationServices.Document doc, TerrainRecord model, string kind)
    {
        var editor = doc.Editor;
        model.Breaklines ??= new List<string>();
        var dxf = kind == "point" ? "POINT,LWPOLYLINE,POLYLINE" : "LWPOLYLINE,POLYLINE";
        var message = kind switch
        {
            "contour" => "\nChọn đường đồng mức: ",
            "break" => "\nChọn đường đứt gãy: ",
            "boundary" => "\nChọn đường bao kín: ",
            _ => "\nChọn điểm đo hoặc đường nối điểm: "
        };
        var selection = editor.GetSelection(
            new PromptSelectionOptions { MessageForAdding = message },
            new SelectionFilter(new[] { new TypedValue((int)DxfCode.Start, dxf) }));
        if (selection.Status != PromptStatus.OK || selection.Value is null)
            return 0;

        var added = 0;
        using (doc.LockDocument())
        using (var tr = doc.Database.TransactionManager.StartTransaction())
        {
            foreach (SelectedObject item in selection.Value)
            {
                if (item is null)
                    continue;
                if (tr.GetObject(item.ObjectId, OpenMode.ForRead) is not Entity entity || entity.IsErased)
                    continue;
                var handle = entity.Handle.ToString();
                if (kind == "boundary")
                {
                    if (!IsClosed(tr, entity))
                    {
                        editor.WriteMessage("\n[iSurvey] Bỏ qua đường chưa kín.");
                        continue;
                    }

                    model.Boundary = handle;
                    added = 1;
                }
                else
                {
                    var list = kind switch
                    {
                        "contour" => model.Contours,
                        "break" => model.Breaklines,
                        _ => model.Points
                    };
                    if (list.Exists(itemHandle => string.Equals(itemHandle, handle, StringComparison.OrdinalIgnoreCase)))
                        continue;
                    list.Add(handle);
                    added++;
                }
            }

            tr.Commit();
        }

        if (added > 0)
            TerrainStore.Save(doc.Database, model);
        return added;
    }

    public static int Build(Autodesk.AutoCAD.ApplicationServices.Document doc, TerrainRecord model)
    {
        var points = new List<TinVertex>();
        var constraints = new List<TinSegment>();
        List<TinVertex>? boundary = null;
        var missing = 0;
        model.Breaklines ??= new List<string>();
        using (var tr = doc.Database.TransactionManager.StartTransaction())
        {
            foreach (var handle in model.Points)
                missing += Collect(tr, doc.Database, handle, points, null);
            foreach (var handle in model.Breaklines)
                missing += Collect(tr, doc.Database, handle, points, constraints);
            foreach (var handle in model.Contours)
                missing += Collect(tr, doc.Database, handle, points, constraints);
            var boundaryHandle = model.Boundary;
            if (!string.IsNullOrEmpty(boundaryHandle))
            {
                var ring = new List<TinVertex>();
                missing += Collect(tr, doc.Database, boundaryHandle, ring, null);
                if (ring.Count >= 3)
                    boundary = ring;
            }

            tr.Commit();
        }

        if (missing > 0)
            doc.Editor.WriteMessage("\n[iSurvey] " + missing + " đối tượng không còn trong bản vẽ.");
        if (points.Count < 3)
        {
            doc.Editor.WriteMessage("\n[iSurvey] Cần ít nhất 3 điểm để xây dựng lưới.");
            return 0;
        }

        var triangles = TinBuilder.Build(points, constraints, boundary, out var unforced);
        if (boundary is null)
            doc.Editor.WriteMessage("\n[iSurvey] Chưa có đường bao. Lưới giữ bao lồi, kể cả tam giác dài giữa các cụm điểm.");
        if (unforced > 0)
            doc.Editor.WriteMessage("\n[iSurvey] " + unforced + " cạnh đứt gãy hoặc đồng mức chưa ép được vào lưới.");
        model.SetTriangles(triangles);
        using (doc.LockDocument())
        using (var tr = doc.Database.TransactionManager.StartTransaction())
        {
            EnsureApp(tr, doc.Database);
            EnsureLayer(tr, doc.Database, model.Layer);
            EraseFaces(tr, doc, model.Id);
            var space = (BlockTableRecord)tr.GetObject(doc.Database.CurrentSpaceId, OpenMode.ForWrite);
            foreach (var tri in triangles)
            {
                var face = new Face(
                    new Point3d(tri.A.X, tri.A.Y, tri.A.Z),
                    new Point3d(tri.B.X, tri.B.Y, tri.B.Z),
                    new Point3d(tri.C.X, tri.C.Y, tri.C.Z),
                    new Point3d(tri.C.X, tri.C.Y, tri.C.Z),
                    true, true, true, true);
                face.SetDatabaseDefaults();
                face.Layer = model.Layer;
                face.Color = AcadColor.FromColorIndex(ColorMethod.ByLayer, 256);
                face.Visible = model.MeshVisible;
                space.AppendEntity(face);
                tr.AddNewlyCreatedDBObject(face, true);
                face.XData = new ResultBuffer(
                    new TypedValue((int)DxfCode.ExtendedDataRegAppName, AppName),
                    new TypedValue((int)DxfCode.ExtendedDataAsciiString, Clip(model.Id)));
            }

            tr.Commit();
        }

        TerrainStore.Save(doc.Database, model);
        doc.Editor.Regen();
        return triangles.Count;
    }

    public static bool Toggle(Autodesk.AutoCAD.ApplicationServices.Document doc, TerrainRecord model)
    {
        var show = !model.MeshVisible;
        var changed = 0;
        using (doc.LockDocument())
        using (var tr = doc.Database.TransactionManager.StartTransaction())
        {
            var space = (BlockTableRecord)tr.GetObject(doc.Database.CurrentSpaceId, OpenMode.ForRead);
            foreach (ObjectId id in space)
            {
                if (tr.GetObject(id, OpenMode.ForRead) is not Face face || face.IsErased)
                    continue;
                if (!Owned(face, model.Id))
                    continue;
                face.UpgradeOpen();
                face.Visible = show;
                changed++;
            }

            tr.Commit();
        }

        if (changed == 0)
            return false;
        model.MeshVisible = show;
        TerrainStore.Save(doc.Database, model);
        doc.Editor.Regen();
        return true;
    }

    public static bool TryQuery(TerrainRecord model, double x, double y, out double z)
    {
        var triangles = model.Triangles();
        if (triangles.Count == 0)
        {
            z = 0;
            return false;
        }

        return TinBuilder.TryElevation(triangles, x, y, out z);
    }

    private static int Collect(
        Transaction tr,
        Database db,
        string handle,
        List<TinVertex> target,
        List<TinSegment>? links)
    {
        Entity? entity;
        try
        {
            entity = OpenHandle(tr, db, handle);
        }
        catch (Autodesk.AutoCAD.Runtime.Exception)
        {
            return 1;
        }

        if (entity is null)
            return 1;
        if (entity is DBPoint point)
        {
            target.Add(new TinVertex(point.Position.X, point.Position.Y, point.Position.Z));
            return 0;
        }

        var vertices = Vertices(tr, entity);
        if (vertices.Count == 0)
            return 1;
        foreach (var vertex in vertices)
            target.Add(vertex);
        if (links is null || vertices.Count < 2)
            return 0;
        for (var i = 0; i < vertices.Count - 1; i++)
        {
            var a = vertices[i];
            var b = vertices[i + 1];
            var dx = a.X - b.X;
            var dy = a.Y - b.Y;
            if (dx * dx + dy * dy <= 1e-8)
                continue;
            links.Add(new TinSegment(a, b));
        }

        return 0;
    }

    private static List<TinVertex> Vertices(Transaction tr, Entity entity)
    {
        var list = new List<TinVertex>();
        if (entity is Polyline polyline)
        {
            for (var i = 0; i < polyline.NumberOfVertices; i++)
            {
                var point = polyline.GetPoint3dAt(i);
                var z = Math.Abs(point.Z) < 1e-9 ? polyline.Elevation : point.Z;
                list.Add(new TinVertex(point.X, point.Y, z));
            }

            if (polyline.Closed && list.Count > 1)
                list.Add(list[0]);
            return list;
        }

        if (entity is Polyline3d poly3)
        {
            foreach (ObjectId id in poly3)
            {
                if (tr.GetObject(id, OpenMode.ForRead) is PolylineVertex3d vertex)
                    list.Add(new TinVertex(vertex.Position.X, vertex.Position.Y, vertex.Position.Z));
            }

            if (poly3.Closed && list.Count > 1)
                list.Add(list[0]);
            return list;
        }

        if (entity is Polyline2d poly2)
        {
            foreach (ObjectId id in poly2)
            {
                if (tr.GetObject(id, OpenMode.ForRead) is not Vertex2d vertex)
                    continue;
                var z = Math.Abs(vertex.Position.Z) < 1e-9 ? poly2.Elevation : vertex.Position.Z;
                list.Add(new TinVertex(vertex.Position.X, vertex.Position.Y, z));
            }

            if (poly2.Closed && list.Count > 1)
                list.Add(list[0]);
        }

        return list;
    }

    private static bool IsClosed(Transaction tr, Entity entity)
    {
        if (entity is Polyline polyline)
        {
            if (polyline.NumberOfVertices < 2)
                return false;
            return polyline.Closed || Near(polyline.GetPoint3dAt(0), polyline.GetPoint3dAt(polyline.NumberOfVertices - 1));
        }
        if (entity is Polyline3d or Polyline2d)
        {
            var vertices = Vertices(tr, entity);
            return vertices.Count >= 4 && NearVertex(vertices[0], vertices[vertices.Count - 1]);
        }

        return false;
    }

    private static bool Near(Point3d a, Point3d b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return dx * dx + dy * dy < 1e-6;
    }

    private static bool NearVertex(TinVertex a, TinVertex b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return dx * dx + dy * dy < 1e-6;
    }

    private static Entity? OpenHandle(Transaction tr, Database db, string handle)
    {
        if (!long.TryParse(handle, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value))
            return null;
        var id = db.GetObjectId(false, new Handle(value), 0);
        if (!id.IsValid || id.IsErased)
            return null;
        return tr.GetObject(id, OpenMode.ForRead) as Entity;
    }

    private static void EraseFaces(Transaction tr, Autodesk.AutoCAD.ApplicationServices.Document doc, string modelId)
    {
        var space = (BlockTableRecord)tr.GetObject(doc.Database.CurrentSpaceId, OpenMode.ForRead);
        var doomed = new List<ObjectId>();
        foreach (ObjectId id in space)
        {
            if (tr.GetObject(id, OpenMode.ForRead) is Face face && Owned(face, modelId))
                doomed.Add(id);
        }

        foreach (var id in doomed)
        {
            var face = (Face)tr.GetObject(id, OpenMode.ForWrite);
            face.Erase();
        }
    }

    private static bool Owned(Face face, string modelId)
    {
        var data = face.GetXDataForApplication(AppName);
        if (data is null)
            return false;
        using (data)
        {
            foreach (TypedValue value in data)
            {
                if (value.TypeCode == (int)DxfCode.ExtendedDataAsciiString
                    && value.Value is string text
                    && string.Equals(text, modelId, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }

        return false;
    }

    private static void EnsureLayer(Transaction tr, Database db, string name)
    {
        var layers = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
        if (layers.Has(name))
            return;
        layers.UpgradeOpen();
        var record = new LayerTableRecord
        {
            Name = name,
            Color = AcadColor.FromColorIndex(ColorMethod.ByAci, 8)
        };
        layers.Add(record);
        tr.AddNewlyCreatedDBObject(record, true);
    }

    private static void EnsureApp(Transaction tr, Database db)
    {
        var apps = (RegAppTable)tr.GetObject(db.RegAppTableId, OpenMode.ForRead);
        if (apps.Has(AppName))
            return;
        apps.UpgradeOpen();
        var record = new RegAppTableRecord { Name = AppName };
        apps.Add(record);
        tr.AddNewlyCreatedDBObject(record, true);
    }

    private static string Clip(string text) => text.Length <= 255 ? text : text.Substring(0, 255);
}
