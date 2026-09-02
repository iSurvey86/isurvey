namespace iSurvey.Modules.Map;

/// <summary>Cache tile trên disk: tiles/{source}/{z}/{x}/{y}.jpg</summary>
public static class TileCacheService
{
    public const string DefaultSourceId = "google-satellite-hybrid";

    private static string RootDir => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "iSurvey",
        "tiles");

    public static string GetTilePath(string sourceId, int x, int y, int zoom)
    {
        return Path.Combine(RootDir, sourceId, zoom.ToString(), x.ToString(), $"{y}.jpg");
    }

    public static bool TryGetCached(string sourceId, int x, int y, int zoom, out string path)
    {
        path = GetTilePath(sourceId, x, y, zoom);
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

    public static async Task<string> SaveAsync(string sourceId, int x, int y, int zoom, byte[] bytes, CancellationToken ct = default)
    {
        var path = GetTilePath(sourceId, x, y, zoom);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        var temp = path + ".tmp";
        await File.WriteAllBytesAsync(temp, bytes, ct).ConfigureAwait(false);

        if (File.Exists(path))
            File.Delete(path);

        File.Move(temp, path);
        return path;
    }
}
