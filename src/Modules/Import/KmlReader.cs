using System.Globalization;
using System.Xml;
using System.Xml.Linq;

namespace iSurvey.Modules.Import;

/// <summary>Một hình trong KML, tọa độ WGS84 (kinh, vĩ, cao).</summary>
internal sealed class KmlShape
{
    public string Name { get; init; } = "";
    public bool IsPoint { get; init; }
    public bool CloseRing { get; init; }
    public required List<KmlVertex> Vertices { get; init; }
}

internal readonly record struct KmlVertex(double Longitude, double Latitude, double Elevation, bool HasElevation);

internal sealed class KmlReadResult
{
    public List<KmlShape> Shapes { get; } = [];
    public int InnerRings { get; set; }
    public int TracksSkipped { get; set; }
}

/// <summary>Đọc Placemark (Point, LineString, Polygon, MultiGeometry). Bỏ qua gx:Track.</summary>
internal static class KmlReader
{
    public static KmlReadResult Read(string kmlPath)
    {
        XDocument doc;
        try
        {
            doc = XDocument.Load(kmlPath);
        }
        catch (XmlException ex)
        {
            throw new InvalidOperationException("File KML không đúng định dạng XML: " + ex.Message, ex);
        }

        var result = new KmlReadResult();
        foreach (var placemark in doc.Descendants().Where(e => e.Name.LocalName == "Placemark"))
        {
            result.TracksSkipped += placemark.Descendants().Count(e => e.Name.LocalName == "Track");
            var name = ChildText(placemark, "name");
            foreach (var geometry in Geometries(placemark))
                AddGeometry(result, name, geometry);
        }

        return result;
    }

    private static IEnumerable<XElement> Geometries(XElement placemark)
    {
        foreach (var child in placemark.Elements())
        {
            var kind = child.Name.LocalName;
            if (kind is "Point" or "LineString" or "Polygon")
                yield return child;
            else if (kind == "MultiGeometry")
            {
                foreach (var nested in child.Elements())
                {
                    if (nested.Name.LocalName is "Point" or "LineString" or "Polygon")
                        yield return nested;
                }
            }
        }
    }

    private static void AddGeometry(KmlReadResult result, string name, XElement geometry)
    {
        switch (geometry.Name.LocalName)
        {
            case "Point":
                var point = ParseCoordinates(ChildText(geometry, "coordinates"));
                if (point.Count >= 1)
                    result.Shapes.Add(new KmlShape { Name = name, IsPoint = true, Vertices = new List<KmlVertex> { point[0] } });
                break;

            case "LineString":
                var line = ParseCoordinates(ChildText(geometry, "coordinates"));
                var closedLine = DropRepeatedEnd(line);
                if (line.Count >= 2)
                    result.Shapes.Add(new KmlShape { Name = name, CloseRing = closedLine, Vertices = line });
                break;

            case "Polygon":
                result.InnerRings += geometry.Elements().Count(e => e.Name.LocalName == "innerBoundaryIs");
                var outer = geometry.Elements().FirstOrDefault(e => e.Name.LocalName == "outerBoundaryIs");
                var ringText = outer?.Descendants().FirstOrDefault(e => e.Name.LocalName == "coordinates")?.Value;
                var ring = ParseCoordinates(ringText);
                DropRepeatedEnd(ring);
                if (ring.Count >= 3)
                    result.Shapes.Add(new KmlShape { Name = name, CloseRing = true, Vertices = ring });
                break;
        }
    }

    /// <summary>KML khép vòng bằng cách lặp đỉnh đầu ở cuối. Bỏ đỉnh trùng để polyline Closed không bị đoạn dài 0.</summary>
    private static bool DropRepeatedEnd(List<KmlVertex> vertices)
    {
        if (vertices.Count < 2 || !Same(vertices[0], vertices[vertices.Count - 1]))
            return false;
        vertices.RemoveAt(vertices.Count - 1);
        return true;
    }

    private static bool Same(KmlVertex a, KmlVertex b) =>
        Math.Abs(a.Longitude - b.Longitude) < 1e-9 && Math.Abs(a.Latitude - b.Latitude) < 1e-9;

    private static string ChildText(XElement parent, string localName) =>
        parent.Elements().FirstOrDefault(e => e.Name.LocalName == localName)?.Value?.Trim() ?? "";

    private static List<KmlVertex> ParseCoordinates(string? text)
    {
        var list = new List<KmlVertex>();
        if (string.IsNullOrWhiteSpace(text))
            return list;

        var body = text!;
        foreach (var token in body.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = token.Split(',');
            if (parts.Length < 2)
                continue;
            if (!double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var lon))
                continue;
            if (!double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var lat))
                continue;

            var alt = 0.0;
            var hasZ = parts.Length >= 3
                       && double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out alt);
            list.Add(new KmlVertex(lon, lat, hasZ ? alt : 0, hasZ));
        }

        return list;
    }
}
