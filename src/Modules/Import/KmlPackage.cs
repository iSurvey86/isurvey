using System.IO.Compression;

namespace iSurvey.Modules.Import;

/// <summary>Mở .kml trực tiếp, hoặc lấy file .kml đầu tiên trong .kmz.</summary>
internal static class KmlPackage
{
    public static string Materialize(string path, out string? tempDirectory)
    {
        tempDirectory = null;
        if (!path.EndsWith(".kmz", StringComparison.OrdinalIgnoreCase))
            return path;

        tempDirectory = Path.Combine(Path.GetTempPath(), "isurvey-kml-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);

        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var zip = new ZipArchive(stream, ZipArchiveMode.Read);
        var entry = zip.Entries.FirstOrDefault(e =>
            e.Name.Length > 0 && e.Name.EndsWith(".kml", StringComparison.OrdinalIgnoreCase));
        if (entry is null)
            throw new InvalidOperationException("File KMZ không chứa file .kml.");

        var dest = Path.Combine(tempDirectory, "doc.kml");
        using (var input = entry.Open())
        using (var output = File.Create(dest))
            input.CopyTo(output);

        return dest;
    }

    public static void Cleanup(string? tempDirectory)
    {
        if (string.IsNullOrEmpty(tempDirectory))
            return;
        try
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
        catch
        {
            // Thư mục tạm không chặn lệnh.
        }
    }
}
