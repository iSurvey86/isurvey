namespace iSurvey.Modules.Map;

/// <summary>Cache tile trên disk: tiles/{source}/{z}/{x}/{y}.jpg</summary>
public static class TileCacheService
{
    public const string DefaultSourceId = "google-satellite-hybrid";

    private static string RootDir => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "iSurvey",
        "tiles");

    public static string GetTilePath(string sourceId, int x, int y, int zoom, int scale = 1)
    {
        var folder = scale <= 1
            ? sourceId
            : sourceId + "-x" + scale.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return Path.Combine(RootDir, folder, zoom.ToString(), x.ToString(), $"{y}.jpg");
    }

    public static bool TryGetCached(string sourceId, int x, int y, int zoom, out string path)
        => TryGetCached(sourceId, x, y, zoom, 1, out path);

    public static bool TryGetCached(string sourceId, int x, int y, int zoom, int scale, out string path)
    {
        path = GetTilePath(sourceId, x, y, zoom, scale);
        if (!File.Exists(path))
            return false;

        try
        {
            return new FileInfo(path).Length > 512;
        }
        catch
        {
            return false;
        }
    }

    public static Task<string> SaveAsync(string sourceId, int x, int y, int zoom, byte[] bytes, CancellationToken ct = default)
        => SaveAsync(sourceId, x, y, zoom, bytes, 1, ct);

    public static async Task<string> SaveAsync(string sourceId, int x, int y, int zoom, byte[] bytes, int scale, CancellationToken ct = default)
    {
        var path = GetTilePath(sourceId, x, y, zoom, scale);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        ct.ThrowIfCancellationRequested();
        var temp = path + ".tmp";
        File.WriteAllBytes(temp, bytes);

        if (File.Exists(path))
            File.Delete(path);

        File.Move(temp, path);
        return path;
    }
}
