using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using iSurvey.Models;

namespace iSurvey.Modules.Export;

/// <summary>Ghi KML (WGS84) kèm Style màu CAD; Polygon không tô đặc.</summary>
internal static class KmlWriter
{
    private static readonly XNamespace KmlNs = "http://www.opengis.net/kml/2.2";

    public static void Write(string outputPath, bool useKmz, bool groupByLayer, bool useElevationZ,
        IReadOnlyList<KmlFeature> features, string documentName)
    {
        var kml = BuildDocument(features, groupByLayer, useElevationZ, documentName);
        var xml = kml.Declaration + Environment.NewLine + kml.ToString(SaveOptions.DisableFormatting);

        if (!useKmz)
        {
            File.WriteAllText(outputPath, xml, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            return;
        }

        var dir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        if (File.Exists(outputPath))
            File.Delete(outputPath);

        using var fs = File.Create(outputPath);
        using var zip = new ZipArchive(fs, ZipArchiveMode.Create);
        var entry = zip.CreateEntry("doc.kml", CompressionLevel.Optimal);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        writer.Write(xml);
    }

    private static XDocument BuildDocument(
        IReadOnlyList<KmlFeature> features,
        bool groupByLayer,
        bool useElevationZ,
        string documentName)
    {
        var docEl = new XElement(KmlNs + "Document",
            new XElement(KmlNs + "name", documentName),
            new XElement(KmlNs + "description", "Xuất từ iSurvey (VN-2000 → WGS84)"));

        // Style shared theo (kind, color, hideIcon, width)
        var styleIds = new Dictionary<string, string>(StringComparer.Ordinal);
        var styleIndex = 0;

        string EnsureStyle(KmlFeature f)
        {
            var key = $"{f.Kind}|{f.ColorKml}|{f.HideIcon}|{f.LineWidth:0.#}";
            if (styleIds.TryGetValue(key, out var existing))
                return existing;

            var id = $"S{styleIndex++}";
            styleIds[key] = id;
            docEl.Add(BuildStyle(id, f));
            return id;
        }

        void AddFeature(KmlFeature f, XElement parent)
        {
            var styleUrl = "#" + EnsureStyle(f);
            parent.Add(ToPlacemark(f, useElevationZ, styleUrl));
        }

        if (groupByLayer)
        {
            foreach (var group in features.GroupBy(f => f.LayerName, StringComparer.OrdinalIgnoreCase)
                         .OrderBy(g => g.Key, StringComparer.CurrentCultureIgnoreCase))
            {
                var folder = new XElement(KmlNs + "Folder",
                    new XElement(KmlNs + "name", group.Key));
                foreach (var f in group)
                    AddFeature(f, folder);
                docEl.Add(folder);
            }
        }
        else
        {
            foreach (var f in features)
                AddFeature(f, docEl);
        }

        return new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement(KmlNs + "kml", docEl));
    }

    private static XElement BuildStyle(string id, KmlFeature f)
    {
        var style = new XElement(KmlNs + "Style", new XAttribute("id", id));

        // Line
        style.Add(new XElement(KmlNs + "LineStyle",
            new XElement(KmlNs + "color", f.ColorKml),
            new XElement(KmlNs + "width", f.LineWidth.ToString("0.##", CultureInfo.InvariantCulture))));

        // Polygon: không tô (tránh mảng trắng), chỉ viền
        style.Add(new XElement(KmlNs + "PolyStyle",
            new XElement(KmlNs + "color", "00ffffff"),
            new XElement(KmlNs + "fill", "0"),
            new XElement(KmlNs + "outline", "1")));

        // Label theo màu CAD
        style.Add(new XElement(KmlNs + "LabelStyle",
            new XElement(KmlNs + "color", f.ColorKml),
            new XElement(KmlNs + "scale", f.Kind == KmlGeometryKind.Point ? "0.9" : "0.8")));

        // Ẩn pin vàng mặc định
        if (f.HideIcon || f.Kind == KmlGeometryKind.Point)
        {
            style.Add(new XElement(KmlNs + "IconStyle",
                new XElement(KmlNs + "scale", "0"),
                new XElement(KmlNs + "Icon",
                    new XElement(KmlNs + "href", ""))));
        }

        return style;
    }

    private static XElement ToPlacemark(KmlFeature f, bool useElevationZ, string styleUrl)
    {
        var pm = new XElement(KmlNs + "Placemark",
            new XElement(KmlNs + "styleUrl", styleUrl));

        if (!string.IsNullOrWhiteSpace(f.Name))
            pm.Add(new XElement(KmlNs + "name", f.Name));
        if (!string.IsNullOrWhiteSpace(f.Description))
            pm.Add(new XElement(KmlNs + "description", f.Description));

        var altitudeMode = useElevationZ ? "absolute" : "clampToGround";

        switch (f.Kind)
        {
            case KmlGeometryKind.Point:
                {
                    var p = f.Points[0];
                    pm.Add(new XElement(KmlNs + "Point",
                        new XElement(KmlNs + "altitudeMode", altitudeMode),
                        new XElement(KmlNs + "coordinates", FormatCoord(p, useElevationZ))));
                    break;
                }
            case KmlGeometryKind.Polygon:
                {
                    pm.Add(new XElement(KmlNs + "Polygon",
                        new XElement(KmlNs + "altitudeMode", altitudeMode),
                        new XElement(KmlNs + "outerBoundaryIs",
                            new XElement(KmlNs + "LinearRing",
                                new XElement(KmlNs + "coordinates",
                                    FormatCoordList(f.Points, useElevationZ))))));
                    break;
                }
            default:
                {
                    var pts = f.Points.ToList();
                    if (f.Closed && pts.Count >= 2 && !NearEqual(pts[0], pts[^1]))
                        pts.Add(pts[0]);

                    pm.Add(new XElement(KmlNs + "LineString",
                        new XElement(KmlNs + "altitudeMode", altitudeMode),
                        new XElement(KmlNs + "tessellate", "1"),
                        new XElement(KmlNs + "coordinates",
                            FormatCoordList(pts, useElevationZ))));
                    break;
                }
        }

        return pm;
    }

    private static bool NearEqual(GeoPoint3 a, GeoPoint3 b) =>
        Math.Abs(a.Longitude - b.Longitude) < 1e-12
        && Math.Abs(a.Latitude - b.Latitude) < 1e-12;

    private static string FormatCoord(GeoPoint3 p, bool useZ) =>
        useZ
            ? string.Create(CultureInfo.InvariantCulture, $"{p.Longitude},{p.Latitude},{p.Elevation}")
            : string.Create(CultureInfo.InvariantCulture, $"{p.Longitude},{p.Latitude},0");

    private static string FormatCoordList(IReadOnlyList<GeoPoint3> pts, bool useZ)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < pts.Count; i++)
        {
            if (i > 0)
                sb.Append(' ');
            sb.Append(FormatCoord(pts[i], useZ));
        }

        return sb.ToString();
    }
}
