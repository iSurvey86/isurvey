namespace iSurvey.Modules.Terrain;

/// <summary>Một mô hình địa hình lưu trong bản vẽ.</summary>
public sealed class TerrainRecord
{
    public string Id { get; set; } = "";

    public string Name { get; set; } = "";

    public string Layer { get; set; } = "iSurvey_MoHinh";

    public List<string> Points { get; set; } = new();

    public List<string> Breaklines { get; set; } = new();

    public List<string> Contours { get; set; } = new();

    public string? Boundary { get; set; }

    public bool MeshVisible { get; set; } = true;

    /// <summary>Mỗi tam giác chín số: x,y,z của ba đỉnh, theo trục CAD.</summary>
    public List<double> Mesh { get; set; } = new();

    public int TriangleCount => Mesh.Count / 9;

    public string Summary()
    {
        var bao = string.IsNullOrEmpty(Boundary) ? "chưa có (lưới là bao lồi)" : "có, dùng để cắt lưới";
        var luoi = MeshVisible ? "đang hiện" : "đang ẩn";
        return "Lớp " + Layer
            + "\nĐiểm " + Points.Count
            + ", đứt gãy " + Breaklines.Count
            + ", đồng mức " + Contours.Count
            + "\nĐường bao " + bao
            + "\nTam giác " + TriangleCount
            + ", lưới " + luoi;
    }

    public List<TinTriangle> Triangles()
    {
        var list = new List<TinTriangle>();
        for (var i = 0; i + 8 < Mesh.Count; i += 9)
        {
            list.Add(new TinTriangle(
                new TinVertex(Mesh[i], Mesh[i + 1], Mesh[i + 2]),
                new TinVertex(Mesh[i + 3], Mesh[i + 4], Mesh[i + 5]),
                new TinVertex(Mesh[i + 6], Mesh[i + 7], Mesh[i + 8])));
        }

        return list;
    }

    public void SetTriangles(IReadOnlyList<TinTriangle> triangles)
    {
        Mesh = new List<double>(triangles.Count * 9);
        foreach (var tri in triangles)
        {
            Mesh.Add(tri.A.X);
            Mesh.Add(tri.A.Y);
            Mesh.Add(tri.A.Z);
            Mesh.Add(tri.B.X);
            Mesh.Add(tri.B.Y);
            Mesh.Add(tri.B.Z);
            Mesh.Add(tri.C.X);
            Mesh.Add(tri.C.Y);
            Mesh.Add(tri.C.Z);
        }
    }
}
