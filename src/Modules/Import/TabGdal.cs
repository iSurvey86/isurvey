using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using OSGeo.GDAL;
using OSGeo.OGR;

namespace iSurvey.Modules.Import;

/// <summary>Nạp GDAL native một lần. Gọi trước mọi lần mở file TAB.</summary>
internal static class TabGdal
{
    private static readonly string[] NativeDlls =
    {
        "gdal.dll", "gdal_wrap.dll", "ogr_wrap.dll", "osr_wrap.dll", "gdalconst_wrap.dll"
    };

    private static int _state;

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool SetDllDirectory(string? lpPathName);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr AddDllDirectory(string newDirectory);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr LoadLibrary(string lpFileName);

    public static void Ensure()
    {
        if (Volatile.Read(ref _state) == 1)
            return;

        lock (typeof(TabGdal))
        {
            if (_state == 1)
                return;

            // Civil giới hạn đường tìm DLL và đã nạp GEOS.dll của Map.
            // GDAL 3.13 cần bản GEOS đi kèm, nên tách tên import trước khi nạp.
            LoadBundleNatives();
            MaxRev.Gdal.Core.GdalBase.ConfigureAll();
            Ogr.RegisterAll();
            Gdal.SetConfigOption("GDAL_FILENAME_IS_UTF8", "YES");
            _state = 1;
        }
    }

    private static void LoadBundleNatives()
    {
        var home = Path.GetDirectoryName(typeof(TabGdal).Assembly.Location)
            ?? throw new InvalidOperationException("Không xác định được thư mục iSurvey.dll.");
        var native = Path.Combine(home, "runtimes", "win-x64", "native");
        if (!File.Exists(Path.Combine(native, "gdal.dll")))
            throw new DirectoryNotFoundException("Không thấy GDAL native: " + native);

        var geosC = IsolateGeos(native);
        SetDllDirectory(native);
        AddDllDirectory(native);
        try
        {
            // Thư mục acad.exe được tìm trước bundle. Bản libxml2 của Autodesk
            // thiếu hàm GDAL 3.13 cần, nên nạp bản đi kèm trước.
            PreloadShadowed(native);
            LoadOrThrow(Path.Combine(Path.GetDirectoryName(geosC)!, "geo2.dll"));
            LoadOrThrow(geosC);
            foreach (var name in NativeDlls)
                LoadOrThrow(Path.Combine(native, name));
        }
        finally
        {
            SetDllDirectory(null);
        }
    }

    /// <summary>
    /// Civil đã nạp GEOS.dll của Autodesk. geos_c của GDAL mà import tên đó sẽ dính
    /// bản sai và gdal.dll lỗi 127. Đổi import sang geo2.dll (cùng độ dài) rồi nạp trước.
    /// </summary>
    private static void PreloadShadowed(string native)
    {
        var appDir = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule?.FileName);
        if (string.IsNullOrEmpty(appDir))
            return;

        foreach (var file in Directory.GetFiles(native, "*.dll"))
        {
            var name = Path.GetFileName(file);
            if (name.Equals("geos.dll", StringComparison.OrdinalIgnoreCase))
                continue;
            if (File.Exists(Path.Combine(appDir, name)))
                LoadOrThrow(file);
        }
    }

    private static string IsolateGeos(string native)
    {
        var scratch = Path.Combine(Path.GetTempPath(), "isurvey_gdal");
        Directory.CreateDirectory(scratch);
        File.Copy(Path.Combine(native, "geos.dll"), Path.Combine(scratch, "geo2.dll"), true);
        var geosC = Path.Combine(scratch, "geos_c.dll");
        File.Copy(Path.Combine(native, "geos_c.dll"), geosC, true);
        RedirectImport(geosC, "geos.dll", "geo2.dll");
        return geosC;
    }

    private static void LoadOrThrow(string path)
    {
        if (LoadLibrary(path) != IntPtr.Zero)
            return;
        throw new DllNotFoundException(
            "Không nạp được " + Path.GetFileName(path) + " (Win32 " + Marshal.GetLastWin32Error() + ").");
    }

    private static void RedirectImport(string dllPath, string from, string to)
    {
        if (from.Length != to.Length)
            throw new InvalidOperationException("Tên DLL import phải cùng độ dài.");

        var bytes = File.ReadAllBytes(dllPath);
        int pe = BitConverter.ToInt32(bytes, 0x3C);
        int optional = pe + 24;
        ushort magic = BitConverter.ToUInt16(bytes, optional);
        int dataDirectory = optional + (magic == 0x20b ? 112 : 96);
        uint importRva = BitConverter.ToUInt32(bytes, dataDirectory + 8);
        ushort sections = BitConverter.ToUInt16(bytes, pe + 6);
        ushort optionalSize = BitConverter.ToUInt16(bytes, pe + 20);
        int sectionTable = optional + optionalSize;

        int RvaToOffset(uint rva)
        {
            for (int i = 0; i < sections; i++)
            {
                int section = sectionTable + (i * 40);
                uint virtualAddress = BitConverter.ToUInt32(bytes, section + 12);
                uint size = Math.Max(BitConverter.ToUInt32(bytes, section + 16), BitConverter.ToUInt32(bytes, section + 8));
                uint raw = BitConverter.ToUInt32(bytes, section + 20);
                if (rva >= virtualAddress && rva < virtualAddress + size)
                    return (int)(raw + (rva - virtualAddress));
            }
            return -1;
        }

        int import = RvaToOffset(importRva);
        while (import >= 0)
        {
            uint nameRva = BitConverter.ToUInt32(bytes, import + 12);
            uint firstThunk = BitConverter.ToUInt32(bytes, import);
            if (firstThunk == 0 && nameRva == 0)
                break;

            int nameOffset = RvaToOffset(nameRva);
            if (nameOffset >= 0 && ReadAsciiZ(bytes, nameOffset) == from)
            {
                Array.Copy(Encoding.ASCII.GetBytes(to), 0, bytes, nameOffset, to.Length);
                File.WriteAllBytes(dllPath, bytes);
                return;
            }
            import += 20;
        }

        throw new InvalidOperationException("Không thấy import " + from + " trong " + Path.GetFileName(dllPath) + ".");
    }

    private static string ReadAsciiZ(byte[] bytes, int offset)
    {
        int end = offset;
        while (end < bytes.Length && bytes[end] != 0)
            end++;
        return Encoding.ASCII.GetString(bytes, offset, end - offset);
    }
}
