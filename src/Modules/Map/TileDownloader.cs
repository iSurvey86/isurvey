using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using iSurvey.Models;

namespace iSurvey.Modules.Map;

/// <summary>Tải tile Google — ưu tiên cache disk.</summary>
public sealed class TileDownloader : IDisposable
{
    private const int RetryAttempts = 3;
    private const int MaxConcurrent = 8;

    private static readonly SemaphoreSlim Gate = new(MaxConcurrent, MaxConcurrent);
    private readonly HttpClient _http;
    private readonly string _sourceId;
    private readonly string _urlTemplate;

    public TileDownloader(string sourceId)
    {
        _sourceId = sourceId;
        _urlTemplate = LoadUrlTemplate(sourceId);
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) iSurvey/1.0");
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("image/jpeg"));
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("image/png"));
    }

    public async Task<(string? Path, bool FromCache)> EnsureTileAsync(int x, int y, int zoom, CancellationToken ct = default)
    {
        if (TileCacheService.TryGetCached(_sourceId, x, y, zoom, out var cached))
            return (cached, true);

        for (var attempt = 1; attempt <= RetryAttempts; attempt++)
        {
            var bytes = await TryDownloadBytesAsync(x, y, zoom, ct).ConfigureAwait(false);
            if (bytes is not null)
            {
                var path = await TileCacheService.SaveAsync(_sourceId, x, y, zoom, bytes, ct).ConfigureAwait(false);
                return (path, false);
            }

            if (attempt < RetryAttempts)
                await Task.Delay(250 * attempt, ct).ConfigureAwait(false);
        }

        return (null, false);
    }

    public async Task<IReadOnlyList<(int X, int Y, int Z, string Path)>> EnsureTilesAsync(
        IReadOnlyList<(int X, int Y)> tiles,
        int zoom,
        IProgress<(int Done, int Total)>? progress = null,
        CancellationToken ct = default)
    {
        var results = new (int X, int Y, int Z, string Path)[tiles.Count];
        var done = 0;

        await Parallel.ForEachAsync(
            Enumerable.Range(0, tiles.Count),
            new ParallelOptions { MaxDegreeOfParallelism = MaxConcurrent, CancellationToken = ct },
            async (i, token) =>
            {
                var (x, y) = tiles[i];
                var (path, _) = await EnsureTileAsync(x, y, zoom, token).ConfigureAwait(false);
                if (path is not null)
                    results[i] = (x, y, zoom, path);

                var n = Interlocked.Increment(ref done);
                progress?.Report((n, tiles.Count));
            }).ConfigureAwait(false);

        return results.Where(r => r.Path is not null).ToList();
    }

    private async Task<byte[]?> TryDownloadBytesAsync(int x, int y, int z, CancellationToken ct)
    {
        var url = _urlTemplate
            .Replace("{x}", x.ToString(), StringComparison.OrdinalIgnoreCase)
            .Replace("{y}", y.ToString(), StringComparison.OrdinalIgnoreCase)
            .Replace("{z}", z.ToString(), StringComparison.OrdinalIgnoreCase);

        await Gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            using var response = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct)
                .ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);
        }
        catch
        {
            return null;
        }
        finally
        {
            Gate.Release();
        }
    }

    private static string LoadUrlTemplate(string sourceId)
    {
        var asmDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                     ?? AppContext.BaseDirectory;
        var path = Path.Combine(asmDir, "Data", "isurvey_map_sources.json");
        if (!File.Exists(path))
            path = Path.Combine(AppContext.BaseDirectory, "Data", "isurvey_map_sources.json");

        var json = File.ReadAllText(path);
        var sources = JsonSerializer.Deserialize<List<MapSourceEntry>>(json)
                      ?? throw new InvalidOperationException("Không parse được isurvey_map_sources.json");

        var source = sources.FirstOrDefault(s =>
                         s.Id.Equals(sourceId, StringComparison.OrdinalIgnoreCase))
                     ?? throw new InvalidOperationException($"Thiếu nguồn {sourceId}.");

        if (string.IsNullOrWhiteSpace(source.UrlTemplate))
            throw new InvalidOperationException($"URL nguồn {sourceId} rỗng.");

        return source.UrlTemplate;
    }

    public void Dispose() => _http.Dispose();
}
