using System.Text.Json;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace iSurvey.Modules.Ai;

public sealed record AiToolDef(
    string Name,
    string Description,
    string ParametersJsonSchema,
    bool RequiresConfirm,
    Func<AiToolContext, JsonElement?, string> Execute);

/// <summary>Đăng ký tool kiểu MCP — Gemini functionDeclarations.</summary>
public static class AiToolRegistry
{
    private static readonly HashSet<string> AllowedCommands = new(StringComparer.OrdinalIgnoreCase)
    {
        "GOC", "ISURVEY_NUMBER_VERTICES",
        "DUOITUYEN", "ISURVEY_STRIP_MAP",
        "KHUNG", "ISURVEY_KHUNG", "KHUNGBV", "ISURVEY_KHUNG_BV",
        "UAVCLIP", "ISURVEY_UAV_CLIP",
        "D2C", "ISURVEY_DGN_TO_CAD",
        "T2C", "ISURVEY_TAB_TO_CAD",
        "C2D", "ISURVEY_CAD_TO_DGN",
        "INK", "ISURVEY_IMPORT_KML",
        "EXK", "EG", "ISURVEY_EXPORT_KML",
        "IG", "ISURVEY_MAP",
        "SG", "ISURVEY_MAP_SAT",
        "XG", "ISURVEY_DELETE_GE",
        "CBD", "CATBD",
        "SCN", "AUTOSCAN",
        "UPD", "ISURVEY_UPDATE",
        "ABT", "ISURVEY_ABOUT",
    };

    public static IReadOnlyList<AiToolDef> All { get; } =
    [
        new(
            "get_drawing_info",
            "Thong tin ban ve dang mo: ten file, so doi tuong uoc luong, goc toa do.",
            """{"type":"object","properties":{}}""",
            false,
            GetDrawingInfo),
        new(
            "list_layers",
            "Liet ke ten layer trong ban ve (toi da 80).",
            """{"type":"object","properties":{"filter":{"type":"string","description":"Loc ten layer chua chuoi (tuy chon)"}}}""",
            false,
            ListLayers),
        new(
            "get_selection_summary",
            "Tom tat doi tuong dang chon (PICKFIRST): so luong theo loai.",
            """{"type":"object","properties":{}}""",
            false,
            GetSelectionSummary),
        new(
            "list_isurvey_commands",
            "Danh sach lenh iSurvey thuong dung va mo ta ngan.",
            """{"type":"object","properties":{}}""",
            false,
            (_, _) => ListCommands()),
        new(
            "run_isurvey_command",
            "Chay mot lenh iSurvey da whitelist (GOC, UAVCLIP, D2C...). Can xac nhan nguoi dung.",
            """{"type":"object","properties":{"command":{"type":"string","description":"Ten lenh, vd GOC hoac ISURVEY_UAV_CLIP"}},"required":["command"]}""",
            true,
            RunCommand),
        new(
            "execute_cad",
            "THAO TAC CAD TONG QUAT: gui chuoi lenh/dap an len Command Line nhu nguoi go (RECTANG, CIRCLE, PLINE, POLYGON, ELLIPSE, HATCH, MOVE, ZOOM...). " +
            "steps = mang string theo thu tu lenh + toa do + option. Vi du HCN: [\"_.RECTANG\",\"0,0\",\"20,30\"]. Vi du tron: [\"_.CIRCLE\",\"0,0\",\"10\"]. " +
            "Khong can tool rieng cho tung hinh — moi lenh AutoCAD/Civil hop le deu dung duoc (tru blocklist nguy hiem).",
            """{"type":"object","properties":{"steps":{"type":"array","items":{"type":"string"},"description":"Day lenh/input Command Line"},"confirm":{"type":"boolean","description":"true neu muon user xac nhan truoc (mac dinh: chi lenh pha huy)"}},"required":["steps"]}""",
            false,
            ExecuteCad),
        new(
            "draw_rectangle",
            "Shortcut ve HCN bang API (nhanh). Uu tien execute_cad neu user yeu cau hinh/lenh bat ky.",
            """{"type":"object","properties":{"x1":{"type":"number"},"y1":{"type":"number"},"x2":{"type":"number"},"y2":{"type":"number"},"layer":{"type":"string"},"color":{"type":"integer"}},"required":["x1","y1","x2","y2"]}""",
            false,
            DrawRectangle),
        new(
            "draw_line",
            "Shortcut ve line bang API. Uu tien execute_cad cho truong hop tong quat.",
            """{"type":"object","properties":{"x1":{"type":"number"},"y1":{"type":"number"},"x2":{"type":"number"},"y2":{"type":"number"},"layer":{"type":"string"},"color":{"type":"integer"}},"required":["x1","y1","x2","y2"]}""",
            false,
            DrawLine),
        new(
            "draw_circle",
            "Shortcut ve circle bang API. Uu tien execute_cad cho truong hop tong quat.",
            """{"type":"object","properties":{"cx":{"type":"number"},"cy":{"type":"number"},"radius":{"type":"number"},"layer":{"type":"string"},"color":{"type":"integer"}},"required":["cx","cy","radius"]}""",
            false,
            DrawCircle),
        new(
            "draw_polyline",
            "Shortcut ve polyline bang API. Uu tien execute_cad cho truong hop tong quat.",
            """{"type":"object","properties":{"points":{"type":"array","items":{"type":"object","properties":{"x":{"type":"number"},"y":{"type":"number"}},"required":["x","y"]}},"closed":{"type":"boolean"},"layer":{"type":"string"},"color":{"type":"integer"}},"required":["points"]}""",
            false,
            DrawPolyline),
        new(
            "list_directory",
            "Liet ke file/thu muc trong may (sandbox: %USERPROFILE% va thu muc con). Khong can Node/MCP.",
            """{"type":"object","properties":{"path":{"type":"string","description":"Duong dan thu muc (tuyet doi hoac ~\\...)"}},"required":["path"]}""",
            false,
            ListDirectory),
        new(
            "read_text_file",
            "Doc file van ban nho (toi da 80KB) trong sandbox %USERPROFILE%. Khong can Node/MCP.",
            """{"type":"object","properties":{"path":{"type":"string","description":"Duong dan file"}},"required":["path"]}""",
            false,
            ReadTextFile),
    ];

    public static AiToolDef? Find(string name) =>
        All.FirstOrDefault(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    public static string Execute(string name, AiToolContext ctx, JsonElement? args)
    {
        var tool = Find(name);
        if (tool is null)
            return JsonSerializer.Serialize(new { error = "unknown_tool", name });
        try
        {
            return tool.Execute(ctx, args);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    private static string GetDrawingInfo(AiToolContext ctx, JsonElement? _)
    {
        var db = ctx.Doc.Database;
        var name = ctx.Doc.Name;
        long count = 0;
        using (ctx.Doc.LockDocument())
        using (var tr = db.TransactionManager.StartTransaction())
        {
            var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
            var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);
            foreach (ObjectId id in ms)
            {
                count++;
                if (count > 50000) break;
            }
            tr.Commit();
        }

        return JsonSerializer.Serialize(new
        {
            file = name,
            model_space_entities = count,
            insunits = db.Insunits.ToString(),
            app_version = Auth.AuthManager.AppVersion,
            user = Auth.AuthManager.CurrentUserEmail
        });
    }

    private static string ListLayers(AiToolContext ctx, JsonElement? args)
    {
        string? filter = null;
        if (args is { ValueKind: JsonValueKind.Object } a && a.TryGetProperty("filter", out var f))
            filter = f.GetString();

        var names = new List<string>();
        using (ctx.Doc.LockDocument())
        using (var tr = ctx.Doc.Database.TransactionManager.StartTransaction())
        {
            var lt = (LayerTable)tr.GetObject(ctx.Doc.Database.LayerTableId, OpenMode.ForRead);
            foreach (ObjectId id in lt)
            {
                var layer = (LayerTableRecord)tr.GetObject(id, OpenMode.ForRead);
                if (!string.IsNullOrEmpty(filter) &&
                    layer.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                names.Add(layer.Name);
                if (names.Count >= 80) break;
            }
            tr.Commit();
        }

        return JsonSerializer.Serialize(new { count = names.Count, layers = names });
    }

    private static string GetSelectionSummary(AiToolContext ctx, JsonElement? _)
    {
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var sel = ctx.Ed.SelectImplied();
        if (sel.Status != Autodesk.AutoCAD.EditorInput.PromptStatus.OK || sel.Value is null)
            return JsonSerializer.Serialize(new { selected = 0, by_type = counts });

        using (ctx.Doc.LockDocument())
        using (var tr = ctx.Doc.Database.TransactionManager.StartTransaction())
        {
            foreach (SelectedObject so in sel.Value)
            {
                if (so == null) continue;
                var ent = tr.GetObject(so.ObjectId, OpenMode.ForRead) as Entity;
                var key = ent?.GetType().Name ?? "Unknown";
                counts[key] = counts.TryGetValue(key, out var n) ? n + 1 : 1;
            }
            tr.Commit();
        }

        return JsonSerializer.Serialize(new { selected = counts.Values.Sum(), by_type = counts });
    }

    private static string ListCommands()
    {
        var items = new[]
        {
            new { cmd = "GOC", desc = "Danh so goc tuyen" },
            new { cmd = "DUOITUYEN", desc = "Duoi thang tuyen + bang goc txt" },
            new { cmd = "KHUNG", desc = "Chen khung ban do chuan A0-A4, ty le 1/200..1/5000 + luoi toa do" },
            new { cmd = "KHUNGBV", desc = "Chen khung ban ve ky thuat A0-A4, ty le 1/200..1/5000 (khong luoi toa do)" },
            new { cmd = "UAVCLIP", desc = "Cat anh UAV theo bien / Auto-Clip" },
            new { cmd = "D2C", desc = "Nhap DGN -> CAD" },
            new { cmd = "T2C", desc = "Nhap MapInfo TAB -> CAD (mau MapInfo, layer theo MDSD)" },
            new { cmd = "C2D", desc = "Xuat CAD -> DGN" },
            new { cmd = "INK / EXK", desc = "Nhap / xuat KML" },
            new { cmd = "IG / SG", desc = "Chen Google Earth / Satellite" },
            new { cmd = "CBD", desc = "Cat binh do theo bien" },
            new { cmd = "SCN", desc = "Quet cau truc ban ve" },
            new { cmd = "UPD", desc = "Kiem tra cap nhat" },
        };
        return JsonSerializer.Serialize(items);
    }

    private static string RunCommand(AiToolContext ctx, JsonElement? args)
    {
        var cmd = args is { ValueKind: JsonValueKind.Object } a && a.TryGetProperty("command", out var c)
            ? (c.GetString() ?? "").Trim()
            : "";
        if (string.IsNullOrWhiteSpace(cmd))
            return JsonSerializer.Serialize(new { error = "missing_command" });

        var token = cmd.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)[0];
        if (!AllowedCommands.Contains(token))
            return JsonSerializer.Serialize(new { error = "command_not_allowed", command = token });

        if (ctx.ConfirmDestructive is not null &&
            !ctx.ConfirmDestructive($"Chạy lệnh iSurvey: {token}?\n(Cửa sổ chat sẽ gửi lệnh vào Command Line.)"))
            return JsonSerializer.Serialize(new { status = "cancelled_by_user", command = token });

        // Gửi lệnh sau khi đóng confirm — dùng idle để tránh xung đột modal.
        var send = token + "\n";
        AcadApp.DocumentManager.MdiActiveDocument?.SendStringToExecute(send, true, false, false);
        Auth.ActivityLogger.Enqueue("ai_tool", "run_isurvey_command", new { command = token });
        return JsonSerializer.Serialize(new { status = "queued", command = token });
    }

    /// <summary>Chặn lệnh nguy hiểm hệ thống — còn lại cho phép (mọi hình/lệnh CAD).</summary>
    private static readonly HashSet<string> BlockedCadCommands = new(StringComparer.OrdinalIgnoreCase)
    {
        "SHELL", "START", "SECURITY", "OPTIONS",
        "NETLOAD", "NETUNLOAD", "APPLOAD", "ARX", "LSP", "CUILOAD", "MENULOAD",
        "VBA", "VBALOAD", "VBAMAN", "SAFETYOPTIONS",
        "QUIT", "EXIT", "CLOSEALL", "SAVEAS",
    };

    private static readonly HashSet<string> DestructiveCadCommands = new(StringComparer.OrdinalIgnoreCase)
    {
        "ERASE", "DELETE", "PURGE", "OVERKILL", "LAYDEL", "LAYMRG",
    };

    private static string DrawRectangle(AiToolContext ctx, JsonElement? args)
    {
        if (!TryGetXY(args, "x1", "y1", out var x1, out var y1, out var err))
            return JsonSerializer.Serialize(new { error = err });
        if (!TryGetXY(args, "x2", "y2", out var x2, out var y2, out err))
            return JsonSerializer.Serialize(new { error = err });

        var minX = Math.Min(x1, x2);
        var maxX = Math.Max(x1, x2);
        var minY = Math.Min(y1, y2);
        var maxY = Math.Max(y1, y2);
        if (Math.Abs(maxX - minX) < 1e-9 || Math.Abs(maxY - minY) < 1e-9)
            return JsonSerializer.Serialize(new { error = "degenerate_rectangle" });

        return MutateModelSpace(ctx, (tr, ms) =>
        {
            var pl = new Polyline();
            pl.AddVertexAt(0, new Point2d(minX, minY), 0, 0, 0);
            pl.AddVertexAt(1, new Point2d(maxX, minY), 0, 0, 0);
            pl.AddVertexAt(2, new Point2d(maxX, maxY), 0, 0, 0);
            pl.AddVertexAt(3, new Point2d(minX, maxY), 0, 0, 0);
            pl.Closed = true;
            ApplyStyle(pl, args);
            ms.AppendEntity(pl);
            tr.AddNewlyCreatedDBObject(pl, true);
            return OkDrawn("rectangle", pl, new { x1 = minX, y1 = minY, x2 = maxX, y2 = maxY });
        });
    }

    private static string DrawLine(AiToolContext ctx, JsonElement? args)
    {
        if (!TryGetXY(args, "x1", "y1", out var x1, out var y1, out var err))
            return JsonSerializer.Serialize(new { error = err });
        if (!TryGetXY(args, "x2", "y2", out var x2, out var y2, out err))
            return JsonSerializer.Serialize(new { error = err });

        return MutateModelSpace(ctx, (tr, ms) =>
        {
            var line = new Line(new Point3d(x1, y1, 0), new Point3d(x2, y2, 0));
            ApplyStyle(line, args);
            ms.AppendEntity(line);
            tr.AddNewlyCreatedDBObject(line, true);
            return OkDrawn("line", line, new { x1, y1, x2, y2 });
        });
    }

    private static string DrawCircle(AiToolContext ctx, JsonElement? args)
    {
        if (!TryGetXY(args, "cx", "cy", out var cx, out var cy, out var err))
            return JsonSerializer.Serialize(new { error = err });
        if (!TryGetNumber(args, "radius", out var radius, out err) || radius <= 0)
            return JsonSerializer.Serialize(new { error = err.Length > 0 ? err : "invalid_radius" });

        return MutateModelSpace(ctx, (tr, ms) =>
        {
            var circle = new Circle(new Point3d(cx, cy, 0), Vector3d.ZAxis, radius);
            ApplyStyle(circle, args);
            ms.AppendEntity(circle);
            tr.AddNewlyCreatedDBObject(circle, true);
            return OkDrawn("circle", circle, new { cx, cy, radius });
        });
    }

    private static string DrawPolyline(AiToolContext ctx, JsonElement? args)
    {
        if (args is not { ValueKind: JsonValueKind.Object } a ||
            !a.TryGetProperty("points", out var pts) || pts.ValueKind != JsonValueKind.Array)
            return JsonSerializer.Serialize(new { error = "missing_points" });

        var points = new List<Point2d>();
        foreach (var p in pts.EnumerateArray())
        {
            if (p.ValueKind != JsonValueKind.Object) continue;
            if (!p.TryGetProperty("x", out var xe) || !p.TryGetProperty("y", out var ye)) continue;
            points.Add(new Point2d(xe.GetDouble(), ye.GetDouble()));
        }
        if (points.Count < 2)
            return JsonSerializer.Serialize(new { error = "need_at_least_2_points" });

        var closed = a.TryGetProperty("closed", out var c) && c.ValueKind is JsonValueKind.True;

        return MutateModelSpace(ctx, (tr, ms) =>
        {
            var pl = new Polyline();
            for (var i = 0; i < points.Count; i++)
                pl.AddVertexAt(i, points[i], 0, 0, 0);
            pl.Closed = closed;
            ApplyStyle(pl, args);
            ms.AppendEntity(pl);
            tr.AddNewlyCreatedDBObject(pl, true);
            return OkDrawn("polyline", pl, new { vertices = points.Count, closed });
        });
    }

    private static string ExecuteCad(AiToolContext ctx, JsonElement? args)
    {
        if (args is not { ValueKind: JsonValueKind.Object } a ||
            !a.TryGetProperty("steps", out var stepsEl) || stepsEl.ValueKind != JsonValueKind.Array)
            return JsonSerializer.Serialize(new { error = "missing_steps" });

        var steps = new List<string>();
        foreach (var s in stepsEl.EnumerateArray())
        {
            if (s.ValueKind != JsonValueKind.String) continue;
            var t = (s.GetString() ?? "").Trim();
            if (t.Length > 0) steps.Add(t);
        }
        if (steps.Count == 0)
            return JsonSerializer.Serialize(new { error = "empty_steps" });

        var firstToken = steps[0].Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)[0]
            .TrimStart('_', '.');
        if (BlockedCadCommands.Contains(firstToken))
            return JsonSerializer.Serialize(new { error = "command_blocked", command = firstToken });

        var forceConfirm = a.TryGetProperty("confirm", out var conf) && conf.ValueKind == JsonValueKind.True;
        var needsConfirm = forceConfirm || DestructiveCadCommands.Contains(firstToken);
        if (needsConfirm && ctx.ConfirmDestructive is not null)
        {
            var preview = string.Join(" → ", steps.Take(8));
            if (!ctx.ConfirmDestructive($"Thao tác CAD:\n{preview}"))
                return JsonSerializer.Serialize(new { status = "cancelled_by_user", steps });
        }

        // Gửi như người gõ Command Line (modeless-safe).
        var script = string.Join("\n", steps) + "\n";
        ctx.Doc.SendStringToExecute(script, true, false, false);
        Auth.ActivityLogger.Enqueue("ai_tool", "execute_cad", new { steps, first = firstToken });
        return JsonSerializer.Serialize(new { status = "queued", steps, command = firstToken });
    }

    private static string MutateModelSpace(AiToolContext ctx, Func<Transaction, BlockTableRecord, string> work)
    {
        using (ctx.Doc.LockDocument())
        using (var tr = ctx.Doc.Database.TransactionManager.StartTransaction())
        {
            var bt = (BlockTable)tr.GetObject(ctx.Doc.Database.BlockTableId, OpenMode.ForRead);
            var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
            var result = work(tr, ms);
            tr.Commit();
            try { ctx.Ed.Regen(); } catch { /* ignore */ }
            return result;
        }
    }

    private static string OkDrawn(string kind, Entity ent, object geometry) =>
        JsonSerializer.Serialize(new
        {
            status = "ok",
            drawn = kind,
            handle = ent.Handle.ToString(),
            layer = ent.Layer,
            geometry
        });

    private static void ApplyStyle(Entity ent, JsonElement? args)
    {
        if (args is not { ValueKind: JsonValueKind.Object } a)
            return;
        if (a.TryGetProperty("layer", out var layerEl))
        {
            var layer = layerEl.GetString()?.Trim();
            if (!string.IsNullOrEmpty(layer))
            {
                EnsureLayer(ent.Database, layer);
                ent.Layer = layer;
            }
        }
        if (a.TryGetProperty("color", out var colorEl) && colorEl.TryGetInt32(out var aci) && aci is >= 1 and <= 255)
            ent.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(ColorMethod.ByAci, (short)aci);
    }

    private static void EnsureLayer(Database db, string layerName)
    {
        // Dùng transaction đang mở (MutateModelSpace) — tránh nested transaction.
        var tr = db.TransactionManager.TopTransaction
                 ?? throw new InvalidOperationException("No active transaction for EnsureLayer.");
        var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
        if (lt.Has(layerName))
            return;
        lt.UpgradeOpen();
        var ltr = new LayerTableRecord { Name = layerName };
        lt.Add(ltr);
        tr.AddNewlyCreatedDBObject(ltr, true);
    }

    private static bool TryGetXY(JsonElement? args, string xn, string yn, out double x, out double y, out string error)
    {
        x = y = 0;
        error = "";
        if (!TryGetNumber(args, xn, out x, out error)) return false;
        return TryGetNumber(args, yn, out y, out error);
    }

    private static bool TryGetNumber(JsonElement? args, string name, out double value, out string error)
    {
        value = 0;
        error = "";
        if (args is not { ValueKind: JsonValueKind.Object } a || !a.TryGetProperty(name, out var el))
        {
            error = "missing_" + name;
            return false;
        }
        if (el.ValueKind == JsonValueKind.Number)
        {
            value = el.GetDouble();
            return true;
        }
        if (el.ValueKind == JsonValueKind.String && double.TryParse(el.GetString(), out value))
            return true;
        error = "invalid_" + name;
        return false;
    }

    private static string ListDirectory(AiToolContext _, JsonElement? args)
    {
        if (!TryResolveSandboxPath(args, out var path, out var err))
            return JsonSerializer.Serialize(new { error = err });
        if (!Directory.Exists(path))
            return JsonSerializer.Serialize(new { error = "not_a_directory", path });

        try
        {
            var dirs = Directory.GetDirectories(path).Select(Path.GetFileName).Where(n => n is not null).Take(80).ToList();
            var files = Directory.GetFiles(path).Select(Path.GetFileName).Where(n => n is not null).Take(120).ToList();
            return JsonSerializer.Serialize(new { path, directories = dirs, files, truncated = dirs.Count >= 80 || files.Count >= 120 });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message, path });
        }
    }

    private static string ReadTextFile(AiToolContext _, JsonElement? args)
    {
        if (!TryResolveSandboxPath(args, out var path, out var err))
            return JsonSerializer.Serialize(new { error = err });
        if (!File.Exists(path))
            return JsonSerializer.Serialize(new { error = "file_not_found", path });

        try
        {
            var info = new FileInfo(path);
            if (info.Length > 80 * 1024)
                return JsonSerializer.Serialize(new { error = "file_too_large", path, bytes = info.Length, max = 80 * 1024 });

            var text = File.ReadAllText(path);
            return JsonSerializer.Serialize(new { path, bytes = info.Length, content = text });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message, path });
        }
    }

    private static bool TryResolveSandboxPath(JsonElement? args, out string fullPath, out string error)
    {
        fullPath = "";
        error = "";
        var raw = args is { ValueKind: JsonValueKind.Object } a && a.TryGetProperty("path", out var p)
            ? (p.GetString() ?? "").Trim()
            : "";
        if (raw.Length == 0)
        {
            error = "missing_path";
            return false;
        }

        if (raw.StartsWith("~") || raw.StartsWith("%USERPROFILE%", StringComparison.OrdinalIgnoreCase))
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            raw = raw.StartsWith("~")
                ? home + raw[1..].TrimStart('\\', '/')
                : Environment.ExpandEnvironmentVariables(raw);
        }

        try { fullPath = Path.GetFullPath(raw); }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }

        var root = Path.GetFullPath(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile))
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var check = fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (!check.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            && !check.Equals(root, StringComparison.OrdinalIgnoreCase))
        {
            error = "path_outside_userprofile_sandbox";
            fullPath = "";
            return false;
        }
        return true;
    }

    /// <summary>Schema Gemini functionDeclarations.</summary>
    public static object[] ToGeminiDeclarations()
    {
        var list = new List<object>();
        foreach (var t in All)
        {
            using var doc = JsonDocument.Parse(t.ParametersJsonSchema);
            list.Add(new
            {
                name = t.Name,
                description = t.Description,
                parameters = doc.RootElement.Clone()
            });
        }
        return list.ToArray();
    }
}
