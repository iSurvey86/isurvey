using System.IO;
using Autodesk.AutoCAD.DatabaseServices;
using System.Windows.Media.Imaging;

namespace iSurvey.Modules.Frame;

/// <summary>Ảnh xem trước từ thumbnail có sẵn trong file DWG. Không mở bản vẽ lên màn hình.</summary>
internal static class DwgThumbnail
{
    public static BitmapSource? TryLoad(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return null;
        try
        {
            using var db = new Database(false, true);
            db.ReadDwgFile(path, FileOpenMode.OpenForReadAndAllShare, true, "");
            System.Drawing.Bitmap? bmp;
            try
            {
                bmp = db.ThumbnailBitmap;
            }
            catch
            {
                bmp = null;
            }

            try
            {
                db.CloseInput(true);
            }
            catch
            {
                // file đã đóng
            }

            if (bmp is null)
                return null;
            using (bmp)
            {
                using var ms = new MemoryStream();
                bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                ms.Position = 0;
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = ms;
                image.EndInit();
                image.Freeze();
                return image;
            }
        }
        catch
        {
            return null;
        }
    }
}
