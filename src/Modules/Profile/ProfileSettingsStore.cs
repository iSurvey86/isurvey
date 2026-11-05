using System.Text.Json;

namespace iSurvey.Modules.Profile;

/// <summary>Lưu thiết lập form VEMCD tại %LocalAppData%\iSurvey\profile-draw-settings.json.</summary>
public static class ProfileSettingsStore
{
    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = null
    };

    public static string FilePath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "iSurvey",
            "profile-draw-settings.json");

    public static ProfileDrawOptions Load()
    {
        try
        {
            var path = FilePath;
            if (!File.Exists(path))
                return new ProfileDrawOptions();
            var text = File.ReadAllText(path);
            var opts = JsonSerializer.Deserialize<ProfileDrawOptions>(text, Json);
            if (opts is null)
                return new ProfileDrawOptions();
            if (opts.Rows is null || opts.Rows.Count == 0)
                opts.Rows = ProfileBandRow.CreateDefaultList();
            if (opts.VerticalScale <= 0)
                opts.VerticalScale = 100;
            if (opts.HorizontalScale <= 0)
                opts.HorizontalScale = 1000;
            if (opts.LabelWidth <= 0)
                opts.LabelWidth = 250;
            if (opts.LabelInset <= 0)
                opts.LabelInset = 40;
            if (opts.HeaderClearance < 0)
                opts.HeaderClearance = 10;
            if (opts.ProfileTextHeight <= 0)
                opts.ProfileTextHeight = 12;
            // Mẫu cũ lưu TextHeight 7.5 — nâng lên 12 nếu còn mặc định cũ.
            foreach (var row in opts.Rows)
            {
                if (Math.Abs(row.TextHeight - 7.5) < 1e-6)
                    row.TextHeight = 12;
            }

            return opts;
        }
        catch
        {
            return new ProfileDrawOptions();
        }
    }

    public static void Save(ProfileDrawOptions options)
    {
        var dir = Path.GetDirectoryName(FilePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(options, Json));
    }
}
