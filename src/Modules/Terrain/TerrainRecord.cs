namespace iSurvey.Modules.Terrain;

/// <summary>Một mô hình địa hình lưu trong bản vẽ.</summary>
public sealed class TerrainRecord
{
    public string Id { get; set; } = "";

    public string Name { get; set; } = "";

    public string Layer { get; set; } = "iSurvey_MoHinh";

    public List<string> Points { get; set; } = new();

    /// <summary>
    /// Khi dựng lưới: điểm/block lấy XY, Z từ chữ số gần nhất trong bản vẽ.
    /// Không sửa entity CAD.
    /// </summary>
    public bool BlockElevFromLabels { get; set; }

    public List<string> Breaklines { get; set; } = new();

    public List<string> Contours { get; set; } = new();

    /// <summary>Một đường bao cũ. Bản mới dùng <see cref="Boundaries"/>.</summary>
    public string? Boundary { get; set; }

    public List<string> Boundaries { get; set; } = new();

    public int BoundaryCount
    {
        get
        {
            var count = 0;
            if (Boundaries is not null)
            {
                foreach (var handle in Boundaries)
                {
                    if (!string.IsNullOrWhiteSpace(handle))
                        count++;
                }
            }

            if (!string.IsNullOrWhiteSpace(Boundary) && !HasBoundary(Boundary))
                count++;
            return count;
        }
    }

    public bool HasBoundary(string handle)
    {
        if (Boundaries is null)
            return false;
        foreach (var item in Boundaries)
        {
            if (string.Equals(item, handle, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    public void RememberBoundary(string handle)
    {
        Boundaries ??= new List<string>();
        if (!string.IsNullOrWhiteSpace(Boundary) && !HasBoundary(Boundary))
            Boundaries.Add(Boundary);
        Boundary = null;
        if (!HasBoundary(handle))
            Boundaries.Add(handle);
    }

    public List<string> BoundaryHandles()
    {
        var list = new List<string>();
        if (Boundaries is not null)
        {
            foreach (var handle in Boundaries)
            {
                if (!string.IsNullOrWhiteSpace(handle) && !list.Exists(item => string.Equals(item, handle, StringComparison.OrdinalIgnoreCase)))
                    list.Add(handle);
            }
        }

        if (!string.IsNullOrWhiteSpace(Boundary) && !list.Exists(item => string.Equals(item, Boundary, StringComparison.OrdinalIgnoreCase)))
            list.Add(Boundary);
        return list;
    }

    public bool MeshVisible { get; set; } = true;

    /// <summary>Mỗi tam giác chín số: x,y,z của ba đỉnh, theo trục CAD.</summary>
    public List<double> Mesh { get; set; } = new();

    public int TriangleCount => Mesh.Count / 9;

    public string Summary()
    {
        var luoi = MeshVisible ? "đang hiện" : "đang ẩn";
        var bao = BoundaryCount == 0 ? "0 (lưới là bao lồi)" : BoundaryCount.ToString();
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
