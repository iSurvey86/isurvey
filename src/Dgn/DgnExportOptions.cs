using Autodesk.AutoCAD.DatabaseServices;
using iSurvey.Models;

namespace iSurvey.Dgn;

public enum DgnExportScope
{
    /// <summary>Toàn bộ Model Space.</summary>
    All,

    /// <summary>Chỉ các đối tượng đang chọn (PICKFIRST) khi gõ lệnh.</summary>
    Selected
}

public enum DgnExportVersion
{
    V8,
    V7
}

/// <summary>Tùy chọn một lần xuất CAD ➔ DGN (ghép từ hộp thoại và <see cref="MapUserSettings"/>).</summary>
public sealed class DgnExportOptions
{
    public DgnExportScope Scope { get; set; } = DgnExportScope.All;

    public DgnExportVersion Version { get; set; } = DgnExportVersion.V8;

    public bool Purge { get; set; } = true;

    public bool Overwrite { get; set; } = true;

    /// <summary>Đường dẫn file .dgn đích.</summary>
    public string OutputPath { get; set; } = string.Empty;

    /// <summary>true = prompt "conversion units" chọn Sub; mặc định Master.</summary>
    public bool UseSubUnits { get; set; }

    /// <summary>Tên mapping setup; rỗng = Enter (&lt;Standard&gt;).</summary>
    public string MappingSetup { get; set; } = string.Empty;

    /// <summary>Seed người dùng tự chỉ định (null/rỗng = tự động theo phiên bản).</summary>
    public string? SeedOverride { get; set; }

    /// <summary>Các đối tượng đang chọn, chụp lúc gõ lệnh (dùng khi Scope = Selected).</summary>
    public IReadOnlyList<ObjectId> SelectedIds { get; set; } = [];

    public string VersionText => Version == DgnExportVersion.V7 ? "V7" : "V8";

    public static DgnExportScope ParseScope(string? text) =>
        string.Equals(text, "Selected", StringComparison.OrdinalIgnoreCase) ? DgnExportScope.Selected : DgnExportScope.All;

    public static DgnExportVersion ParseVersion(string? text) =>
        string.Equals(text, "V7", StringComparison.OrdinalIgnoreCase) ? DgnExportVersion.V7 : DgnExportVersion.V8;

    public static DgnExportOptions FromSettings(MapUserSettings s) => new()
    {
        Scope = ParseScope(s.DgnExportScope),
        Version = ParseVersion(s.DgnExportVersion),
        Purge = s.DgnExportPurge,
        Overwrite = s.DgnExportOverwrite,
        UseSubUnits = string.Equals(s.DgnExportUnits, "Sub", StringComparison.OrdinalIgnoreCase),
        MappingSetup = s.DgnExportMappingSetup ?? string.Empty,
        SeedOverride = ParseVersion(s.DgnExportVersion) == DgnExportVersion.V7 ? s.DgnExportSeedV7 : s.DgnExportSeedV8
    };
}

/// <summary>Kết quả một lần xuất.</summary>
public sealed class DgnExportReport
{
    public string? Error { get; set; }

    public string? OutputPath { get; set; }

    public long OutputBytes { get; set; }

    public int Entities { get; set; }

    public string? SeedPath { get; set; }

    public string? SeedSource { get; set; }

    public bool UsedScratchCopy { get; set; }

    public TimeSpan Elapsed { get; set; }

    public string? LogPath { get; set; }
}
