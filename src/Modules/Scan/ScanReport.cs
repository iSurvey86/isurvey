using System.Globalization;
using System.Text;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace iSurvey.Modules.Scan;

/// <summary>Dựng báo cáo AUTOSCAN (3 phần). Nội dung file là UTF-8 có BOM; thông báo Command Line là ASCII.</summary>
internal sealed class ScanReport
{
    private const int MaxTextSamples = 100;
    private const int SamplesPerGroup = 2;
    private const int MaxSubEntities = 10;
    private const int MaxDxfVertexLines = 25;
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    private sealed class Group
    {
        public required string Key { get; init; }
        public int Count;
        public List<ObjectId> Samples { get; } = [];
    }

    private sealed record TextSample(string Layer, double Height, Point3d Pos, string Content);

    public string Build(Database db, Transaction tr, IEnumerable<ObjectId> ids, string drawingName)
    {
        var groups = new Dictionary<string, Group>(StringComparer.Ordinal);
        var textsByLayer = new SortedDictionary<string, List<TextSample>>(StringComparer.OrdinalIgnoreCase);
        var total = 0;

        foreach (var id in ids)
        {
            if (tr.GetObject(id, OpenMode.ForRead) is not Entity ent)
                continue;

            total++;
            var key = GroupKey(ent, tr);
            if (!groups.TryGetValue(key, out var g))
            {
                g = new Group { Key = key };
                groups[key] = g;
            }

            g.Count++;
            if (g.Samples.Count < SamplesPerGroup)
                g.Samples.Add(id);

            var sample = TryTextSample(ent);
            if (sample is not null)
            {
                if (!textsByLayer.TryGetValue(sample.Layer, out var list))
                {
                    list = [];
                    textsByLayer[sample.Layer] = list;
                }

                list.Add(sample);
            }
        }

        var sb = new StringBuilder();
        sb.AppendLine("=== iSurvey AUTOSCAN - Bao cao cau truc ban ve ===");
        sb.AppendLine($"Ban ve : {drawingName}");
        sb.AppendLine($"Ngay   : {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"Tong so doi tuong quet: {total}");
        sb.AppendLine($"So nhom [EntityType | Layer | BlockName]: {groups.Count}");
        sb.AppendLine();

        var ordered = groups.Values.OrderByDescending(g => g.Count).ThenBy(g => g.Key, StringComparer.Ordinal).ToList();

        // Phan I
        sb.AppendLine("--- PHAN I: THONG KE THEO NHOM [EntityType | Layer | BlockName] ---");
        foreach (var g in ordered)
            sb.AppendLine($"{g.Count,8}  {g.Key}");
        sb.AppendLine();

        // Phan II
        AppendTextSection(sb, textsByLayer);

        // Phan III
        sb.AppendLine("--- PHAN III: MAU DAI DIEN (toi da 2 doi tuong / nhom) ---");
        sb.AppendLine("(Ma DXF duoc mo phong tu thuoc tinh doi tuong, dang (ma . gia tri))");
        sb.AppendLine();
        foreach (var g in ordered)
        {
            sb.AppendLine($"### NHOM {g.Key}  (so luong: {g.Count})");
            var n = 0;
            foreach (var id in g.Samples)
            {
                n++;
                if (tr.GetObject(id, OpenMode.ForRead) is Entity ent)
                    AppendSample(sb, ent, tr, n);
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static string GroupKey(Entity ent, Transaction tr)
    {
        var type = ent.GetRXClass().DxfName ?? ent.GetType().Name.ToUpperInvariant();
        var block = ent is BlockReference br ? BlockNameOf(br, tr) : "-";
        return $"{type} | {ent.Layer} | {block}";
    }

    private static string BlockNameOf(BlockReference br, Transaction tr)
    {
        try
        {
            var id = br.IsDynamicBlock ? br.DynamicBlockTableRecord : br.BlockTableRecord;
            return ((BlockTableRecord)tr.GetObject(id, OpenMode.ForRead)).Name;
        }
        catch
        {
            return br.Name;
        }
    }

    private static TextSample? TryTextSample(Entity ent)
    {
        switch (ent)
        {
            case DBText t:
                return new TextSample(t.Layer, t.Height, t.Position, OneLine(t.TextString));
            case MText m:
            {
                string content;
                try { content = m.Text; }
                catch { content = m.Contents; }
                return new TextSample(m.Layer, m.TextHeight, m.Location, OneLine(content));
            }
            default:
                return null;
        }
    }

    private static string OneLine(string s) =>
        s.Replace("\r\n", " / ").Replace("\n", " / ").Replace("\r", " / ");

    /// <summary>Lấy tối đa 100 mẫu chữ, luân phiên theo layer để layer hiếm vẫn có mặt.</summary>
    private static void AppendTextSection(StringBuilder sb, SortedDictionary<string, List<TextSample>> byLayer)
    {
        var totalTexts = byLayer.Values.Sum(l => l.Count);
        sb.AppendLine($"--- PHAN II: NOI DUNG CHU (TEXT/MTEXT, toi da {MaxTextSamples} mau; tong {totalTexts}) ---");

        var picked = new List<TextSample>();
        var round = 0;
        while (picked.Count < MaxTextSamples)
        {
            var any = false;
            foreach (var kv in byLayer)
            {
                if (round < kv.Value.Count)
                {
                    picked.Add(kv.Value[round]);
                    any = true;
                    if (picked.Count >= MaxTextSamples)
                        break;
                }
            }

            if (!any)
                break;
            round++;
        }

        var i = 0;
        foreach (var t in picked.OrderBy(p => p.Layer, StringComparer.OrdinalIgnoreCase))
        {
            i++;
            sb.AppendLine(
                $"{i,4}. Layer={t.Layer} | Cao={F(t.Height)} | X={F(t.Pos.X)} Y={F(t.Pos.Y)} Z={F(t.Pos.Z)} | \"{t.Content}\"");
        }

        sb.AppendLine();
    }

    private void AppendSample(StringBuilder sb, Entity ent, Transaction tr, int index)
    {
        var dxf = ent.GetRXClass().DxfName ?? ent.GetType().Name;
        sb.AppendLine($"  [Mau {index}] {dxf}  Handle={ent.Handle}  Layer={ent.Layer}");

        try
        {
            var e = ent.GeometricExtents;
            sb.AppendLine($"    BoundingBox: ({F(e.MinPoint.X)}, {F(e.MinPoint.Y)}, {F(e.MinPoint.Z)}) -> " +
                          $"({F(e.MaxPoint.X)}, {F(e.MaxPoint.Y)}, {F(e.MaxPoint.Z)})");
        }
        catch
        {
            sb.AppendLine("    BoundingBox: (khong co)");
        }

        var (lines, vertexLines) = DescribeDxf(ent, tr);
        sb.AppendLine("    Ma DXF (mo phong):");
        foreach (var l in lines)
            sb.AppendLine($"      {l}");

        if (vertexLines.Count > MaxDxfVertexLines)
        {
            var head = MaxDxfVertexLines / 2 + 1;
            var tail = MaxDxfVertexLines - head;
            foreach (var l in vertexLines.Take(head))
                sb.AppendLine($"      {l}");
            sb.AppendLine($"      ... (rut gon {vertexLines.Count - head - tail} dong dinh) ...");
            foreach (var l in vertexLines.Skip(vertexLines.Count - tail))
                sb.AppendLine($"      {l}");
        }
        else
        {
            foreach (var l in vertexLines)
                sb.AppendLine($"      {l}");
        }

        AppendSubEntities(sb, ent, tr);
    }

    private static (List<string> Lines, List<string> VertexLines) DescribeDxf(Entity ent, Transaction tr)
    {
        var lines = new List<string>
        {
            $"(0 . \"{ent.GetRXClass().DxfName}\")",
            $"(5 . \"{ent.Handle}\")",
            $"(8 . \"{ent.Layer}\")",
            $"(6 . \"{ent.Linetype}\")",
            $"(62 . {ent.ColorIndex})",
            $"(370 . {(int)ent.LineWeight})"
        };
        if (Math.Abs(ent.LinetypeScale - 1.0) > 1e-9)
            lines.Add($"(48 . {F(ent.LinetypeScale)})");

        var v = new List<string>();

        switch (ent)
        {
            case Line l:
                lines.Add($"(10 . {P(l.StartPoint)})");
                lines.Add($"(11 . {P(l.EndPoint)})");
                break;
            case Circle c:
                lines.Add($"(10 . {P(c.Center)})");
                lines.Add($"(40 . {F(c.Radius)})");
                break;
            case Arc a:
                lines.Add($"(10 . {P(a.Center)})");
                lines.Add($"(40 . {F(a.Radius)})");
                lines.Add($"(50 . {F(a.StartAngle * 180.0 / Math.PI)})");
                lines.Add($"(51 . {F(a.EndAngle * 180.0 / Math.PI)})");
                break;
            case DBPoint pt:
                lines.Add($"(10 . {P(pt.Position)})");
                break;
            case DBText t:
                lines.Add($"(1 . \"{OneLine(t.TextString)}\")");
                lines.Add($"(7 . \"{t.TextStyleName}\")");
                lines.Add($"(10 . {P(t.Position)})");
                if (Math.Abs(t.AlignmentPoint.X) > 0 || Math.Abs(t.AlignmentPoint.Y) > 0)
                    lines.Add($"(11 . {P(t.AlignmentPoint)})");
                lines.Add($"(40 . {F(t.Height)})");
                lines.Add($"(41 . {F(t.WidthFactor)})");
                lines.Add($"(50 . {F(t.Rotation * 180.0 / Math.PI)})");
                lines.Add($"(72 . {(int)t.HorizontalMode})");
                lines.Add($"(73 . {(int)t.VerticalMode})");
                break;
            case MText m:
            {
                var contents = OneLine(m.Contents);
                if (contents.Length > 80)
                    contents = contents[..80] + "...";
                lines.Add($"(1 . \"{contents}\")");
                lines.Add($"(10 . {P(m.Location)})");
                lines.Add($"(40 . {F(m.TextHeight)})");
                lines.Add($"(41 . {F(m.Width)})");
                lines.Add($"(71 . {(int)m.Attachment})");
                lines.Add($"(50 . {F(m.Rotation * 180.0 / Math.PI)})");
                break;
            }
            case BlockReference br:
                lines.Add($"(2 . \"{BlockNameOf(br, tr)}\")");
                lines.Add($"(10 . {P(br.Position)})");
                lines.Add($"(41 . {F(br.ScaleFactors.X)})");
                lines.Add($"(42 . {F(br.ScaleFactors.Y)})");
                lines.Add($"(43 . {F(br.ScaleFactors.Z)})");
                lines.Add($"(50 . {F(br.Rotation * 180.0 / Math.PI)})");
                break;
            case Polyline pl:
            {
                lines.Add($"(90 . {pl.NumberOfVertices})");
                lines.Add($"(70 . {(pl.Closed ? 1 : 0) + (pl.Plinegen ? 128 : 0)})");
                lines.Add($"(38 . {F(pl.Elevation)})");
                for (var i = 0; i < pl.NumberOfVertices; i++)
                {
                    var p = pl.GetPoint2dAt(i);
                    var sb = new StringBuilder($"(10 . ({F(p.X)} {F(p.Y)}))");
                    var sw = pl.GetStartWidthAt(i);
                    var ew = pl.GetEndWidthAt(i);
                    var bg = pl.GetBulgeAt(i);
                    if (Math.Abs(sw) > 0) sb.Append($" (40 . {F(sw)})");
                    if (Math.Abs(ew) > 0) sb.Append($" (41 . {F(ew)})");
                    if (Math.Abs(bg) > 0) sb.Append($" (42 . {F(bg)})");
                    v.Add(sb.ToString());
                }

                break;
            }
            case Polyline2d p2:
            {
                var flag = (p2.Closed ? 1 : 0)
                           + (p2.PolyType == Poly2dType.FitCurvePoly ? 2 : 0)
                           + (p2.PolyType is Poly2dType.QuadSplinePoly or Poly2dType.CubicSplinePoly ? 4 : 0);
                lines.Add($"(70 . {flag})");
                lines.Add($"(38 . {F(p2.Elevation)})");
                foreach (ObjectId id in p2)
                {
                    if (tr.GetObject(id, OpenMode.ForRead) is Vertex2d vx)
                        v.Add($"VERTEX (70 . {(int)vx.VertexType}) (10 . {P(vx.Position)})" +
                              (Math.Abs(vx.Bulge) > 0 ? $" (42 . {F(vx.Bulge)})" : ""));
                }

                break;
            }
            case Polyline3d p3:
            {
                var spline = p3.PolyType is Poly3dType.QuadSplinePoly or Poly3dType.CubicSplinePoly;
                var flag = 8 + (p3.Closed ? 1 : 0) + (spline ? 4 : 0);
                lines.Add($"(70 . {flag})");
                foreach (ObjectId id in p3)
                {
                    if (tr.GetObject(id, OpenMode.ForRead) is PolylineVertex3d vx)
                        v.Add($"VERTEX (70 . {Vertex3dFlag(vx.VertexType)}) (10 . {P(vx.Position)})");
                }

                break;
            }
            case Hatch h:
                lines.Add($"(2 . \"{h.PatternName}\")");
                lines.Add($"(70 . {(h.PatternType == HatchPatternType.PreDefined && h.PatternName == "SOLID" ? 1 : 0)})");
                lines.Add($"(91 . {h.NumberOfLoops})");
                break;
        }

        return (lines, v);
    }

    private static int Vertex3dFlag(Vertex3dType t) => t switch
    {
        Vertex3dType.ControlVertex => 48, // 32 (3D vertex) + 16 (spline frame control)
        Vertex3dType.FitVertex => 40,     // 32 + 8 (spline vertex created by fitting)
        _ => 32
    };

    private static void AppendSubEntities(StringBuilder sb, Entity ent, Transaction tr)
    {
        var rows = new List<string>();
        var total = 0;

        switch (ent)
        {
            case BlockReference br:
                foreach (ObjectId id in br.AttributeCollection)
                {
                    total++;
                    if (rows.Count < MaxSubEntities && tr.GetObject(id, OpenMode.ForRead) is AttributeReference at)
                        rows.Add($"ATTRIB Tag={at.Tag} Text=\"{OneLine(at.TextString)}\" Layer={at.Layer} Pos={P(at.Position)}");
                }

                break;
            case Polyline pl:
                total = pl.NumberOfVertices;
                for (var i = 0; i < Math.Min(total, MaxSubEntities); i++)
                {
                    var p = pl.GetPoint2dAt(i);
                    rows.Add($"VERTEX[{i}] ({F(p.X)} {F(p.Y)}) bulge={F(pl.GetBulgeAt(i))}");
                }

                break;
            case Polyline2d p2:
                foreach (ObjectId id in p2)
                {
                    total++;
                    if (rows.Count < MaxSubEntities && tr.GetObject(id, OpenMode.ForRead) is Vertex2d vx)
                        rows.Add($"VERTEX[{total - 1}] type={vx.VertexType} Pos={P(vx.Position)}");
                }

                break;
            case Polyline3d p3:
                foreach (ObjectId id in p3)
                {
                    total++;
                    if (rows.Count < MaxSubEntities && tr.GetObject(id, OpenMode.ForRead) is PolylineVertex3d vx)
                        rows.Add($"VERTEX[{total - 1}] type={vx.VertexType} Pos={P(vx.Position)}");
                }

                break;
            default:
                return;
        }

        if (total == 0)
            return;

        sb.AppendLine($"    Sub-entities (hien {rows.Count}/{total}):");
        foreach (var r in rows)
            sb.AppendLine($"      {r}");
    }

    private static string F(double d) => d.ToString("0.####", Inv);

    private static string P(Point3d p) => $"({F(p.X)} {F(p.Y)} {F(p.Z)})";
}
