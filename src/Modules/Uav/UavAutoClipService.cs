using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Autodesk.AutoCAD.Geometry;
using iSurvey.Core;
using DrawingImage = System.Drawing.Image;

namespace iSurvey.Modules.Uav;

/// <summary>
/// Auto-Clip nền trắng/đen: downsample → ngưỡng → blob lớn nhất → lấp lỗ → contour ngoài → Douglas-Peucker.
/// Không giữ bitmap full-res trong RAM.
/// </summary>
internal static class UavAutoClipService
{
    private const int MaxEdgePx = 2048;
    private const int WhiteMin = 245;
    private const int BlackMax = 12;
    /// <summary>Tolerance DP nhỏ — tránh cắt chéo chỗ lõm (tam giác trắng trong biên).</summary>
    private const double SimplifyTolPx = 1.0;

    /// <summary>
    /// Trả về vòng clip trong hệ pixel ảnh gốc (góc trên-trái = 0,0 theo System.Drawing / CAD).
    /// Null nếu thất bại.
    /// </summary>
    public static List<Point2d>? TryBuildClipRingInFullPixels(
        string imagePath,
        out string message,
        out int fullWidth,
        out int fullHeight)
    {
        message = "";
        fullWidth = 0;
        fullHeight = 0;
        if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
        {
            message = "Không tìm thấy file ảnh trên đĩa (Unload vẫn cần đường dẫn ActiveFileName hợp lệ).";
            return null;
        }

        int fullW, fullH;
        byte[] mask;
        int dw, dh;

        try
        {
            using var fs = new FileStream(imagePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var src = DrawingImage.FromStream(fs, useEmbeddedColorManagement: false, validateImageData: false);
            fullW = src.Width;
            fullH = src.Height;
            fullWidth = fullW;
            fullHeight = fullH;
            if (fullW < 8 || fullH < 8)
            {
                message = "Ảnh quá nhỏ để Auto-Clip.";
                return null;
            }

            var scale = Math.Min(1.0, MaxEdgePx / (double)Math.Max(fullW, fullH));
            dw = Math.Max(1, (int)Math.Round(fullW * scale));
            dh = Math.Max(1, (int)Math.Round(fullH * scale));

            using var small = new Bitmap(dw, dh, PixelFormat.Format24bppRgb);
            using (var g = Graphics.FromImage(small))
            {
                g.InterpolationMode = InterpolationMode.Low;
                g.PixelOffsetMode = PixelOffsetMode.HighSpeed;
                g.CompositingMode = CompositingMode.SourceCopy;
                g.DrawImage(src, 0, 0, dw, dh);
            }

            // Giải phóng decoder sớm — chỉ giữ mask.
            mask = BuildForegroundMask(small);
        }
        catch (OutOfMemoryException)
        {
            message = "Không đủ RAM để đọc ảnh — thử Unload khác hoặc dùng chế độ 1 (Polyline).";
            return null;
        }
        catch (System.Exception ex)
        {
            message = "Đọc ảnh thất bại: " + ex.Message;
            return null;
        }

        var largest = KeepLargestBlob(mask, dw, dh, out var blobArea);
        if (blobArea < 64)
        {
            message = "Không tìm thấy vùng màu đủ lớn (kiểm tra ngưỡng trắng/đen).";
            return null;
        }

        FillHoles(largest, dw, dh);

        var contour = TraceExternalContour(largest, dw, dh);
        if (contour.Count < 3)
        {
            message = "Không lần được đường bao ngoài.";
            return null;
        }

        // Douglas-Peucker trên mặt phẳng pixel downsample.
        var as3 = contour.Select(p => new Point3d(p.X, p.Y, 0)).ToList();
        // Đóng vòng cho DP (điểm cuối = đầu).
        if (as3[0].DistanceTo(as3[^1]) > 1e-6)
            as3.Add(as3[0]);
        var simplified = GeoMath.DouglasPeucker(as3, SimplifyTolPx);
        if (simplified.Count >= 2 && simplified[0].DistanceTo(simplified[^1]) < 1e-6)
            simplified.RemoveAt(simplified.Count - 1);

        if (simplified.Count < 3)
        {
            message = "Đường bao sau khi làm mượt còn dưới 3 đỉnh.";
            return null;
        }

        // Scale lên hệ pixel System.Drawing (gốc trên-trái, Y xuống).
        // Đảo Y sang CAD (gốc dưới-trái) do caller — cần PixelToModelTransform của RasterImage.
        var sx = fullW / (double)dw;
        var sy = fullH / (double)dh;
        var ring = new List<Point2d>(simplified.Count);
        foreach (var p in simplified)
            ring.Add(new Point2d(p.X * sx, p.Y * sy));

        message = $"blob={blobArea}px@{dw}x{dh}, đỉnh={ring.Count}, gốc={fullW}x{fullH}";
        return ring;
    }

    /// <summary>
    /// Drawing (top-left, Y↓) → pixel CAD dùng với PixelToModelTransform / SetClipBoundary.
    /// AutoCAD: gốc pixel thường góc dưới-trái, Y↑ theo Orientation.Yaxis.
    /// </summary>
    public static List<Point2d> DrawingPixelsToCadPixels(
        IReadOnlyList<Point2d> drawingPixels,
        int fullHeight,
        bool cadYIncreasesUp)
    {
        var list = new List<Point2d>(drawingPixels.Count);
        foreach (var p in drawingPixels)
        {
            var y = cadYIncreasesUp ? fullHeight - p.Y : p.Y;
            list.Add(new Point2d(p.X, y));
        }

        return list;
    }

    /// <summary>Đoán hướng trục Y pixel CAD từ ma trận ảnh.</summary>
    public static bool DetectCadPixelYIncreasesUp(Matrix3d pixelToModel, Vector3d imageYaxis, int fullHeight)
    {
        if (fullHeight < 2)
            return true;

        var a = new Point3d(0, 0, 0).TransformBy(pixelToModel);
        var b = new Point3d(0, fullHeight * 0.25, 0).TransformBy(pixelToModel);
        var delta = b - a;
        var axis = imageYaxis.GetNormal();
        // +pixelY cùng hướng Orientation.Yaxis → gốc dưới-trái (Y↑ trong ảnh).
        return delta.DotProduct(axis) > 0;
    }

    /// <summary>Pixel gần trắng hoặc gần đen = nền (0); còn lại = nội dung (1).</summary>
    private static byte[] BuildForegroundMask(Bitmap bmp)
    {
        var w = bmp.Width;
        var h = bmp.Height;
        var mask = new byte[w * h];
        var data = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
        try
        {
            var stride = data.Stride;
            var buf = new byte[Math.Abs(stride) * h];
            Marshal.Copy(data.Scan0, buf, 0, buf.Length);
            for (var y = 0; y < h; y++)
            {
                var row = y * stride;
                for (var x = 0; x < w; x++)
                {
                    var i = row + x * 3;
                    var b = buf[i];
                    var g = buf[i + 1];
                    var r = buf[i + 2];
                    var nearWhite = r >= WhiteMin && g >= WhiteMin && b >= WhiteMin;
                    var nearBlack = r <= BlackMax && g <= BlackMax && b <= BlackMax;
                    mask[y * w + x] = (byte)(nearWhite || nearBlack ? 0 : 1);
                }
            }
        }
        finally
        {
            bmp.UnlockBits(data);
        }

        return mask;
    }

    private static byte[] KeepLargestBlob(byte[] mask, int w, int h, out int largestArea)
    {
        var labels = new int[w * h];
        var next = 1;
        largestArea = 0;
        var bestLabel = 0;

        for (var y = 0; y < h; y++)
        for (var x = 0; x < w; x++)
        {
            var i = y * w + x;
            if (mask[i] == 0 || labels[i] != 0)
                continue;

            var area = FloodLabel(mask, labels, w, h, x, y, next);
            if (area > largestArea)
            {
                largestArea = area;
                bestLabel = next;
            }

            next++;
        }

        var result = new byte[w * h];
        if (bestLabel == 0)
            return result;

        for (var i = 0; i < result.Length; i++)
            result[i] = (byte)(labels[i] == bestLabel ? 1 : 0);

        return result;
    }

    private static int FloodLabel(byte[] mask, int[] labels, int w, int h, int sx, int sy, int label)
    {
        var stack = new Stack<(int X, int Y)>();
        stack.Push((sx, sy));
        labels[sy * w + sx] = label;
        var area = 0;

        while (stack.Count > 0)
        {
            var (x, y) = stack.Pop();
            area++;
            TryPush(x + 1, y);
            TryPush(x - 1, y);
            TryPush(x, y + 1);
            TryPush(x, y - 1);
        }

        return area;

        void TryPush(int nx, int ny)
        {
            if ((uint)nx >= (uint)w || (uint)ny >= (uint)h)
                return;
            var i = ny * w + nx;
            if (mask[i] == 0 || labels[i] != 0)
                return;
            labels[i] = label;
            stack.Push((nx, ny));
        }
    }

    /// <summary>Lấp lỗ trong blob: nền không chạm biên ảnh → coi là lỗ (mái trắng) → đổ thành foreground.</summary>
    private static void FillHoles(byte[] mask, int w, int h)
    {
        var outside = new bool[w * h];
        var q = new Queue<(int X, int Y)>();

        void TryEnqueue(int x, int y)
        {
            if ((uint)x >= (uint)w || (uint)y >= (uint)h)
                return;
            var i = y * w + x;
            if (mask[i] != 0 || outside[i])
                return;
            outside[i] = true;
            q.Enqueue((x, y));
        }

        for (var x = 0; x < w; x++)
        {
            TryEnqueue(x, 0);
            TryEnqueue(x, h - 1);
        }

        for (var y = 0; y < h; y++)
        {
            TryEnqueue(0, y);
            TryEnqueue(w - 1, y);
        }

        while (q.Count > 0)
        {
            var (x, y) = q.Dequeue();
            TryEnqueue(x + 1, y);
            TryEnqueue(x - 1, y);
            TryEnqueue(x, y + 1);
            TryEnqueue(x, y - 1);
        }

        for (var i = 0; i < mask.Length; i++)
        {
            if (mask[i] == 0 && !outside[i])
                mask[i] = 1;
        }
    }

    /// <summary>Moore-neighborhood: đường bao ngoài (blob đặc sau FillHoles).</summary>
    private static List<Point2d> TraceExternalContour(byte[] mask, int w, int h)
    {
        // Điểm bắt đầu: top-most, rồi left-most foreground.
        var startX = -1;
        var startY = -1;
        for (var sy = 0; sy < h && startX < 0; sy++)
        {
            for (var sx = 0; sx < w; sx++)
            {
                if (mask[sy * w + sx] != 0)
                {
                    startX = sx;
                    startY = sy;
                    break;
                }
            }
        }

        if (startX < 0)
            return [];

        // Hướng: 0=E,1=SE,2=S,3=SW,4=W,5=NW,6=N,7=NE
        int[] dx = [1, 1, 0, -1, -1, -1, 0, 1];
        int[] dy = [0, 1, 1, 1, 0, -1, -1, -1];

        var contour = new List<Point2d>(1024);
        var cx = startX;
        var cy = startY;
        var dir = 6; // đến từ phía Bắc khi bắt đầu từ top pixel
        var guard = w * h * 8;

        do
        {
            contour.Add(new Point2d(cx + 0.5, cy + 0.5));
            var startDir = (dir + 6) % 8; // quét từ hướng back+2 (chuẩn Jacob)
            var found = false;
            for (var k = 0; k < 8; k++)
            {
                var d = (startDir + k) % 8;
                var nx = cx + dx[d];
                var ny = cy + dy[d];
                if ((uint)nx < (uint)w && (uint)ny < (uint)h && mask[ny * w + nx] != 0)
                {
                    cx = nx;
                    cy = ny;
                    dir = d;
                    found = true;
                    break;
                }
            }

            if (!found)
                break;

            guard--;
        } while ((cx != startX || cy != startY) && guard > 0);

        return contour;
    }
}
