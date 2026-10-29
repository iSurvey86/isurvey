using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace iSurvey.Modules.Frame;

/// <summary>
/// Đọc bảng khung tên từ Excel .xlsx, .xlsm hoặc .xls.
/// Lấy cột STT (hoặc mã) và cột Nội dung, bỏ cột Tiêu đề.
/// </summary>
public static class FrameSheetImport
{
    /// <summary>Nội dung gợi ý trong file mẫu. Ô Tờ vẫn bị bỏ qua khi nhập.</summary>
    private static readonly string[] SampleContent =
    [
        "TỔNG CÔNG TY ĐIỆN LỰC",
        "CÔNG TY ĐIỆN LỰC",
        "Đường dây và TBA 110KV KCN Minh Châu",
        "TỈNH TUYÊN QUANG",
        "CÔNG TY CỔ PHẦN TƯ VẤN THIẾT KẾ",
        "Số nhà, đường, phường, tỉnh",
        "TỔNG GIÁM ĐỐC",
        "Họ và tên",
        "C.N.D.A",
        "Họ và tên",
        "C.N.K.S",
        "Họ và tên",
        "Kiểm soát",
        "Họ và tên",
        "Khảo sát",
        "Họ và tên",
        "BÁO CÁO KINH TẾ KỸ THUẬT",
        "PHẦN ĐƯỜNG DÂY",
        "MẶT BẰNG HƯỚNG TUYẾN",
        "10/2026",
        "/",
        "/",
        "/",
        "i/N (tự động)",
        "<SỐ HIỆU>"
    ];

    public static IReadOnlyList<(string Key, string Value)> Read(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        if (ext is ".xlsx" or ".xlsm")
            return Pairs(ReadXlsx(path));
        if (ext == ".xls")
            return Pairs(ReadXls(path));
        throw new InvalidOperationException("Chỉ nhận file Excel (.xlsx, .xlsm, .xls).");
    }

    /// <summary>Ghi file .xlsx mẫu: hàng đầu STT, Tiêu đề, Nội dung, rồi đủ 25 ô khung tên.</summary>
    public static void WriteSample(string path)
    {
        var sheet = new StringBuilder();
        sheet.Append("""<?xml version="1.0" encoding="UTF-8" standalone="yes"?>""");
        sheet.Append("""<worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">""");
        sheet.Append("""<cols><col min="1" max="1" width="8" customWidth="1"/>""");
        sheet.Append("""<col min="2" max="2" width="22" customWidth="1"/>""");
        sheet.Append("""<col min="3" max="3" width="56" customWidth="1"/></cols><sheetData>""");
        sheet.Append(SampleRow(1, "STT", "Tiêu đề", "Nội dung"));
        for (var i = 0; i < FrameFields.All.Length; i++)
        {
            var field = FrameFields.All[i];
            var content = i < SampleContent.Length ? SampleContent[i] : "";
            sheet.Append(SampleRow(i + 2, field.Stt.ToString(CultureInfo.InvariantCulture), field.Label, content));
        }

        sheet.Append("</sheetData></worksheet>");

        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        if (File.Exists(path))
            File.Delete(path);

        using var zip = ZipFile.Open(path, ZipArchiveMode.Create);
        WriteEntry(zip, "[Content_Types].xml", """
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
              <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
              <Default Extension="xml" ContentType="application/xml"/>
              <Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>
              <Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
              <Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/>
            </Types>
            """);
        WriteEntry(zip, "_rels/.rels", """
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
              <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/>
            </Relationships>
            """);
        WriteEntry(zip, "xl/workbook.xml", """
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
              <sheets><sheet name="Khung tên" sheetId="1" r:id="rId1"/></sheets>
            </workbook>
            """);
        WriteEntry(zip, "xl/_rels/workbook.xml.rels", """
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
              <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/>
              <Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/>
            </Relationships>
            """);
        WriteEntry(zip, "xl/styles.xml", """
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
              <fonts count="1"><font><sz val="11"/><name val="Calibri"/></font></fonts>
              <fills count="2"><fill><patternFill patternType="none"/></fill><fill><patternFill patternType="gray125"/></fill></fills>
              <borders count="1"><border><left/><right/><top/><bottom/><diagonal/></border></borders>
              <cellStyleXfs count="1"><xf numFmtId="0" fontId="0" fillId="0" borderId="0"/></cellStyleXfs>
              <cellXfs count="1"><xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0"/></cellXfs>
            </styleSheet>
            """);
        WriteEntry(zip, "xl/worksheets/sheet1.xml", sheet.ToString());
    }

    private static string SampleRow(int row, string stt, string title, string content) =>
        $"""<row r="{row}"><c r="A{row}" t="inlineStr"><is><t>{Xml(stt)}</t></is></c><c r="B{row}" t="inlineStr"><is><t>{Xml(title)}</t></is></c><c r="C{row}" t="inlineStr"><is><t>{Xml(content)}</t></is></c></row>""";

    private static string Xml(string text)
    {
        var sb = new StringBuilder(text.Length);
        foreach (var c in text)
        {
            switch (c)
            {
                case '&': sb.Append("&amp;"); break;
                case '<': sb.Append("&lt;"); break;
                case '>': sb.Append("&gt;"); break;
                default: sb.Append(c); break;
            }
        }

        return sb.ToString();
    }

    private static void WriteEntry(ZipArchive zip, string name, string xml)
    {
        var entry = zip.CreateEntry(name, CompressionLevel.Optimal);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
        writer.Write(xml.Trim());
    }

    private static List<(string Key, string Value)> Pairs(List<string[]> rows)
    {
        var header = -1;
        var keyCol = 0;
        var valCol = 1;
        var scan = Math.Min(rows.Count, 8);
        for (var i = 0; i < scan; i++)
        {
            var stt = FindHeader(rows[i], "stt");
            var noi = FindHeader(rows[i], "noi dung");
            if (stt < 0 && noi < 0)
                continue;
            header = i;
            keyCol = stt >= 0 ? stt : 0;
            valCol = noi >= 0 ? noi : rows[i].Length > 2 ? 2 : 1;
            break;
        }

        if (header < 0)
        {
            var width = 0;
            foreach (var row in rows)
            {
                if (row.Length > width)
                    width = row.Length;
            }

            valCol = width >= 3 ? 2 : 1;
        }

        var list = new List<(string, string)>();
        for (var i = header >= 0 ? header + 1 : 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var key = Cell(row, keyCol);
            var value = Cell(row, valCol);
            if (key.Length == 0)
                continue;
            if (double.TryParse(key, NumberStyles.Float, CultureInfo.InvariantCulture, out var n)
                && Math.Abs(n - Math.Round(n)) < 1e-6
                && n >= 0 && n < 100000)
                key = ((int)Math.Round(n)).ToString(CultureInfo.InvariantCulture);
            list.Add((key, value));
        }

        return list;
    }

    private static string Cell(string[] row, int col) =>
        col >= 0 && col < row.Length ? row[col].Trim() : "";

    private static int FindHeader(string[] row, string folded)
    {
        for (var i = 0; i < row.Length; i++)
        {
            if (Fold(row[i]) == folded)
                return i;
        }

        return -1;
    }

    private static string Fold(string text)
    {
        var form = text.Trim().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(form.Length);
        foreach (var c in form)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        return sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
    }

    private static List<string[]> ReadXlsx(string path)
    {
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var zip = new ZipArchive(fs, ZipArchiveMode.Read);
        var strings = ReadSharedStrings(zip);
        var formats = ReadFormats(zip);
        var sheetName = FirstSheetPath(zip);
        var entry = zip.GetEntry(sheetName) ?? throw new InvalidOperationException("File Excel không có sheet.");
        using var sheet = entry.Open();
        return ReadSheet(sheet, strings, formats);
    }

    private static List<int> ReadFormats(ZipArchive zip)
    {
        var custom = new Dictionary<int, string>();
        var xfs = new List<int>();
        var entry = zip.GetEntry("xl/styles.xml");
        if (entry is null)
            return xfs;
        using var stream = entry.Open();
        using var reader = XmlReader.Create(stream);
        var inXfs = false;
        while (reader.Read())
        {
            if (reader.NodeType != XmlNodeType.Element)
                continue;
            if (reader.LocalName == "numFmt")
            {
                var idText = reader.GetAttribute("numFmtId");
                var code = reader.GetAttribute("formatCode") ?? "";
                if (int.TryParse(idText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
                    custom[id] = code;
            }
            else if (reader.LocalName == "cellXfs")
            {
                inXfs = true;
            }
            else if (inXfs && reader.LocalName == "xf")
            {
                var idText = reader.GetAttribute("numFmtId");
                xfs.Add(int.TryParse(idText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) ? id : 0);
            }
            else if (reader.LocalName == "cellStyles")
            {
                inXfs = false;
            }
        }

        for (var i = 0; i < xfs.Count; i++)
        {
            if (custom.TryGetValue(xfs[i], out var code))
                xfs[i] = DateKind(code);
            else
                xfs[i] = BuiltinKind(xfs[i]);
        }

        return xfs;
    }

    private static List<string> ReadSharedStrings(ZipArchive zip)
    {
        var list = new List<string>();
        var entry = zip.GetEntry("xl/sharedStrings.xml");
        if (entry is null)
            return list;
        using var stream = entry.Open();
        using var reader = XmlReader.Create(stream);
        while (reader.Read())
        {
            if (reader.NodeType != XmlNodeType.Element || reader.LocalName != "si")
                continue;
            var sb = new StringBuilder();
            using (var sub = reader.ReadSubtree())
            {
                while (sub.Read())
                {
                    if (sub.NodeType == XmlNodeType.Element && sub.LocalName == "t")
                        sb.Append(sub.ReadElementContentAsString());
                }
            }

            list.Add(sb.ToString());
        }

        return list;
    }

    private static string FirstSheetPath(ZipArchive zip)
    {
        var book = zip.GetEntry("xl/workbook.xml") ?? throw new InvalidOperationException("File Excel không đúng định dạng.");
        string? rid = null;
        using (var stream = book.Open())
        using (var reader = XmlReader.Create(stream))
        {
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "sheet")
                {
                    rid = reader.GetAttribute("id", "http://schemas.openxmlformats.org/officeDocument/2006/relationships");
                    break;
                }
            }
        }

        if (string.IsNullOrEmpty(rid))
            throw new InvalidOperationException("File Excel không có sheet.");

        var rels = zip.GetEntry("xl/_rels/workbook.xml.rels") ?? throw new InvalidOperationException("File Excel không đúng định dạng.");
        using var relStream = rels.Open();
        using var relReader = XmlReader.Create(relStream);
        while (relReader.Read())
        {
            if (relReader.NodeType != XmlNodeType.Element || relReader.LocalName != "Relationship")
                continue;
            if (relReader.GetAttribute("Id") != rid)
                continue;
            var target = relReader.GetAttribute("Target") ?? "";
            target = target.Replace('\\', '/').TrimStart('/');
            if (!target.StartsWith("xl/", StringComparison.OrdinalIgnoreCase))
                target = "xl/" + target;
            return target;
        }

        throw new InvalidOperationException("File Excel không có sheet.");
    }

    private static List<string[]> ReadSheet(Stream stream, List<string> shared, List<int> formats)
    {
        var cells = new Dictionary<long, string>();
        var maxRow = -1;
        var maxCol = -1;
        using var reader = XmlReader.Create(stream);
        while (reader.Read())
        {
            if (reader.NodeType != XmlNodeType.Element || reader.LocalName != "c")
                continue;
            var r = reader.GetAttribute("r") ?? "";
            var t = reader.GetAttribute("t") ?? "";
            var style = reader.GetAttribute("s");
            var col = ColumnIndex(r, out var row);
            string? value = null;
            if (!reader.IsEmptyElement)
            {
                using var sub = reader.ReadSubtree();
                sub.Read();
                while (sub.Read())
                {
                    if (sub.NodeType != XmlNodeType.Element)
                        continue;
                    if (sub.LocalName == "v")
                        value = sub.ReadElementContentAsString();
                    else if (sub.LocalName == "t" && t == "inlineStr")
                        value = sub.ReadElementContentAsString();
                }
            }

            var fmt = 0;
            if (int.TryParse(style, NumberStyles.Integer, CultureInfo.InvariantCulture, out var si) && si >= 0 && si < formats.Count)
                fmt = formats[si];
            var text = CellText(t, value, shared, fmt);
            if (text.Length == 0)
                continue;
            cells[((long)row << 32) | (uint)col] = text;
            if (row > maxRow)
                maxRow = row;
            if (col > maxCol)
                maxCol = col;
        }

        return Grid(cells, maxRow, maxCol);
    }

    private static string CellText(string kind, string? value, List<string> shared, int fmt)
    {
        if (string.IsNullOrEmpty(value))
            return "";
        if (kind == "s" && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i) && i >= 0 && i < shared.Count)
            return shared[i];
        if (kind == "inlineStr")
            return value ?? "";
        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var n))
            return FormatNumber(n, fmt);
        return value ?? "";
    }

    private static int ColumnIndex(string reference, out int row)
    {
        var i = 0;
        var col = 0;
        while (i < reference.Length && char.IsLetter(reference[i]))
        {
            col = col * 26 + (char.ToUpperInvariant(reference[i]) - 'A' + 1);
            i++;
        }

        row = 0;
        if (i < reference.Length)
            int.TryParse(reference.Substring(i), NumberStyles.Integer, CultureInfo.InvariantCulture, out row);
        return col - 1;
    }

    private static List<string[]> ReadXls(string path)
    {
        var data = OleStorage.ReadStream(path, "Workbook");
        if (data is null)
            data = OleStorage.ReadStream(path, "Book");
        if (data is null)
            throw new InvalidOperationException("Không đọc được file .xls. Hãy lưu lại dạng .xlsx.");

        var cells = new Dictionary<long, string>();
        var strings = new List<string>();
        var formats = new List<int>();
        var maxRow = -1;
        var maxCol = -1;
        var i = 0;
        var sheet = false;
        var pastGlobals = false;
        int pendingRow = -1, pendingCol = -1;
        while (i + 4 <= data.Length)
        {
            var id = BitConverter.ToUInt16(data, i);
            var len = BitConverter.ToUInt16(data, i + 2);
            i += 4;
            if (i + len > data.Length)
                break;
            if (id == 0x0809)
            {
                sheet = pastGlobals;
                if (sheet)
                {
                    var dt = len >= 4 ? BitConverter.ToUInt16(data, i + 2) : (ushort)0x0010;
                    if (dt != 0x0010 && dt != 0x0020)
                        sheet = false;
                }
            }
            else if (id == 0x000A)
            {
                if (!pastGlobals)
                    pastGlobals = true;
                else if (sheet)
                    break;
                sheet = false;
            }
            else if (!pastGlobals && id == 0x00E0 && len >= 4)
            {
                formats.Add(BuiltinKind(U16(data, i + 2)));
            }
            else if (!pastGlobals && id == 0x00FC)
            {
                var chunks = new List<byte[]> { Slice(data, i, len) };
                var j = i + len;
                while (j + 4 <= data.Length && BitConverter.ToUInt16(data, j) == 0x003C)
                {
                    var clen = BitConverter.ToUInt16(data, j + 2);
                    j += 4;
                    chunks.Add(Slice(data, j, clen));
                    j += clen;
                }

                strings = Sst(chunks);
                i = j;
                continue;
            }
            else if (sheet)
            {
                if (id == 0x00FD && len >= 10)
                    Put(cells, ref maxRow, ref maxCol, U16(data, i), U16(data, i + 2), At(strings, (int)U32(data, i + 6)));
                else if (id == 0x0203 && len >= 14)
                    Put(cells, ref maxRow, ref maxCol, U16(data, i), U16(data, i + 2), FormatNumber(BitConverter.ToDouble(data, i + 6), XfKind(formats, U16(data, i + 4))));
                else if (id == 0x027E && len >= 10)
                    Put(cells, ref maxRow, ref maxCol, U16(data, i), U16(data, i + 2), FormatNumber(DecodeRk(U32(data, i + 6)), XfKind(formats, U16(data, i + 4))));
                else if (id == 0x00BD && len >= 6)
                {
                    var row = U16(data, i);
                    var col = U16(data, i + 2);
                    var last = U16(data, i + len - 2);
                    var p = 4;
                    while (col <= last && p + 6 <= len - 2)
                    {
                        Put(cells, ref maxRow, ref maxCol, row, col, FormatNumber(DecodeRk(U32(data, i + p + 2)), XfKind(formats, U16(data, i + p))));
                        col++;
                        p += 6;
                    }
                }
                else if (id == 0x0204 && len >= 8)
                    Put(cells, ref maxRow, ref maxCol, U16(data, i), U16(data, i + 2), UnicodeString(data, i + 6, len - 6));
                else if (id == 0x0006 && len >= 14)
                {
                    var row = U16(data, i);
                    var col = U16(data, i + 2);
                    if (data[i + 12] == 0xFF && data[i + 13] == 0xFF && data[i + 6] == 0)
                    {
                        pendingRow = row;
                        pendingCol = col;
                    }
                    else
                    {
                        Put(cells, ref maxRow, ref maxCol, row, col, FormatNumber(BitConverter.ToDouble(data, i + 6), XfKind(formats, U16(data, i + 4))));
                    }
                }
                else if (id == 0x0207 && pendingRow >= 0)
                {
                    Put(cells, ref maxRow, ref maxCol, pendingRow, pendingCol, UnicodeString(data, i, len));
                    pendingRow = -1;
                }
            }

            i += len;
        }

        return Grid(cells, maxRow, maxCol);
    }

    private static void Put(Dictionary<long, string> cells, ref int maxRow, ref int maxCol, int row, int col, string text)
    {
        if (text.Length == 0)
            return;
        cells[((long)row << 32) | (uint)col] = text;
        if (row > maxRow)
            maxRow = row;
        if (col > maxCol)
            maxCol = col;
    }

    private static List<string[]> Grid(Dictionary<long, string> cells, int maxRow, int maxCol)
    {
        var rows = new List<string[]>();
        if (maxRow < 0)
            return rows;
        for (var r = 0; r <= maxRow; r++)
        {
            var line = new string[maxCol + 1];
            for (var c = 0; c <= maxCol; c++)
                line[c] = cells.TryGetValue(((long)r << 32) | (uint)c, out var v) ? v : "";
            rows.Add(line);
        }

        return rows;
    }

    private static int XfKind(List<int> formats, int xf) =>
        xf >= 0 && xf < formats.Count ? formats[xf] : 0;

    private static string At(List<string> strings, int index) =>
        index >= 0 && index < strings.Count ? strings[index] : "";

    private static string FormatNumber(double n, int fmt = 0)
    {
        if (double.IsNaN(n) || double.IsInfinity(n))
            return "";
        if (fmt != 0)
        {
            try
            {
                var dt = DateTime.FromOADate(n);
                if (fmt == 2)
                    return dt.ToString("M/yyyy", CultureInfo.InvariantCulture);
                if (fmt == 3)
                    return dt.ToString("H:mm", CultureInfo.InvariantCulture);
                return dt.ToString("d/M/yyyy", CultureInfo.InvariantCulture);
            }
            catch (System.Exception)
            {
                // không phải số ngày: ghi số thường
            }
        }

        if (Math.Abs(n - Math.Round(n)) < 1e-6 && Math.Abs(n) < 1e12)
            return ((long)Math.Round(n)).ToString(CultureInfo.InvariantCulture);
        return n.ToString("0.########", CultureInfo.InvariantCulture);
    }

    /// <summary>0 = số, 1 = ngày đủ, 2 = tháng/năm, 3 = giờ.</summary>
    private static int DateKind(string code)
    {
        var bare = new StringBuilder();
        var quote = false;
        foreach (var c in code)
        {
            if (c == '"')
            {
                quote = !quote;
                continue;
            }

            if (!quote)
                bare.Append(char.ToLowerInvariant(c));
        }

        var s = bare.ToString();
        if (s.Contains('y') && s.Contains('m') && !s.Contains('d'))
            return 2;
        if (s.Contains('d') || s.Contains('y'))
            return 1;
        if (s.Contains('h') || s.Contains('s'))
            return 3;
        return 0;
    }

    private static bool IsBuiltinDate(int id) => id is >= 14 and <= 22 or >= 45 and <= 47;

    private static int BuiltinKind(int id)
    {
        if (id is 17 or 55 or 56)
            return 2;
        if (id is 18 or 19 or 20 or 21 or 45 or 46 or 47)
            return 3;
        if (IsBuiltinDate(id))
            return 1;
        return 0;
    }

    private static double DecodeRk(uint rk)
    {
        var div = (rk & 1) != 0;
        double v;
        if ((rk & 2) != 0)
            v = ((int)rk) >> 2;
        else
            v = BitConverter.Int64BitsToDouble((long)((ulong)(rk & 0xFFFFFFFC) << 32));
        return div ? v / 100.0 : v;
    }

    private static List<string> Sst(List<byte[]> chunks)
    {
        var reader = new ChunkReader(chunks);
        var total = reader.ReadU32();
        var unique = reader.ReadU32();
        _ = total;
        var list = new List<string>((int)unique);
        for (var n = 0; n < unique; n++)
            list.Add(reader.ReadSstString());
        return list;
    }

    private static string UnicodeString(byte[] data, int offset, int length)
    {
        if (length < 3 || offset + 3 > data.Length)
            return "";
        var cch = BitConverter.ToUInt16(data, offset);
        var flags = data[offset + 2];
        var wide = (flags & 1) != 0;
        var bytes = cch * (wide ? 2 : 1);
        if (offset + 3 + bytes > data.Length)
            bytes = Math.Max(0, data.Length - (offset + 3));
        if (wide)
            return Encoding.Unicode.GetString(data, offset + 3, bytes - (bytes % 2));
        var sb = new StringBuilder(cch);
        for (var k = 0; k < bytes && k < cch; k++)
            sb.Append((char)data[offset + 3 + k]);
        return sb.ToString();
    }

    private static byte[] Slice(byte[] data, int offset, int length)
    {
        var buf = new byte[length];
        Buffer.BlockCopy(data, offset, buf, 0, length);
        return buf;
    }

    private static int U16(byte[] data, int offset) => BitConverter.ToUInt16(data, offset);

    private static uint U32(byte[] data, int offset) => BitConverter.ToUInt32(data, offset);

    private sealed class ChunkReader
    {
        private readonly List<byte[]> _chunks;
        private int _index;
        private int _offset;
        private bool _inChars;
        private bool _wide;

        public ChunkReader(List<byte[]> chunks) => _chunks = chunks;

        public string ReadSstString()
        {
            var cch = ReadU16();
            var flags = ReadByte();
            _wide = (flags & 1) != 0;
            var rich = (flags & 8) != 0;
            var ext = (flags & 4) != 0;
            var runs = rich ? ReadU16() : 0;
            var extBytes = ext ? (int)ReadU32() : 0;
            _inChars = true;
            var sb = new StringBuilder(cch);
            for (var n = 0; n < cch; n++)
            {
                if (_wide)
                    sb.Append((char)ReadU16());
                else
                    sb.Append((char)ReadByte());
            }

            _inChars = false;
            for (var n = 0; n < runs * 4; n++)
                ReadByte();
            for (var n = 0; n < extBytes; n++)
                ReadByte();
            return sb.ToString();
        }

        public ushort ReadU16() => (ushort)(ReadByte() | (ReadByte() << 8));

        public uint ReadU32()
        {
            uint v = ReadByte();
            v |= (uint)ReadByte() << 8;
            v |= (uint)ReadByte() << 16;
            v |= (uint)ReadByte() << 24;
            return v;
        }

        private byte ReadByte()
        {
            while (_index < _chunks.Count && _offset >= _chunks[_index].Length)
            {
                _index++;
                _offset = 0;
                if (_index < _chunks.Count && _inChars)
                {
                    _wide = (_chunks[_index][0] & 1) != 0;
                    _offset = 1;
                }
            }

            if (_index >= _chunks.Count)
                throw new InvalidOperationException("File .xls bị cắt giữa bảng chữ.");
            return _chunks[_index][_offset++];
        }
    }

    private static class OleStorage
    {
        public static byte[]? ReadStream(string path, string name)
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var hdr = new byte[512];
            if (fs.Read(hdr, 0, 512) != 512 || hdr[0] != 0xD0 || hdr[1] != 0xCF)
                return null;
            var sectorSize = 1 << BitConverter.ToUInt16(hdr, 0x1E);
            var miniSize = 1 << BitConverter.ToUInt16(hdr, 0x20);
            var dirStart = BitConverter.ToInt32(hdr, 0x30);
            var miniCutoff = BitConverter.ToInt32(hdr, 0x38);
            var miniFatStart = BitConverter.ToInt32(hdr, 0x3C);
            var miniFatCount = BitConverter.ToInt32(hdr, 0x40);
            var difStart = BitConverter.ToInt32(hdr, 0x44);
            var difCount = BitConverter.ToInt32(hdr, 0x48);
            var fatSectors = new List<int>();
            for (var i = 0; i < 109; i++)
            {
                var s = BitConverter.ToInt32(hdr, 0x4C + i * 4);
                if (s >= 0)
                    fatSectors.Add(s);
            }

            var dif = difStart;
            for (var n = 0; n < difCount && dif >= 0; n++)
            {
                var buf = Sector(fs, dif, sectorSize);
                var per = sectorSize / 4 - 1;
                for (var i = 0; i < per; i++)
                {
                    var s = BitConverter.ToInt32(buf, i * 4);
                    if (s >= 0)
                        fatSectors.Add(s);
                }

                dif = BitConverter.ToInt32(buf, per * 4);
            }

            var fat = new List<int>();
            foreach (var s in fatSectors)
            {
                var buf = Sector(fs, s, sectorSize);
                for (var i = 0; i < sectorSize / 4; i++)
                    fat.Add(BitConverter.ToInt32(buf, i * 4));
            }

            byte[] Chain(int start, int size)
            {
                var ms = new MemoryStream();
                var s = start;
                var guard = 0;
                while (s >= 0 && guard++ < 1000000)
                {
                    var buf = Sector(fs, s, sectorSize);
                    ms.Write(buf, 0, buf.Length);
                    s = s < fat.Count ? fat[s] : -2;
                }

                var all = ms.ToArray();
                if (size >= 0 && size < all.Length)
                    Array.Resize(ref all, size);
                return all;
            }

            var dir = Chain(dirStart, -1);
            var miniStreamStart = BitConverter.ToInt32(dir, 0x74);
            int streamStart = -1, streamSize = 0;
            for (var i = 0; i + 128 <= dir.Length; i += 128)
            {
                var nameLen = BitConverter.ToUInt16(dir, i + 0x40);
                if (nameLen < 4 || i + nameLen > dir.Length)
                    continue;
                var entry = Encoding.Unicode.GetString(dir, i, nameLen - 2);
                if (!entry.Equals(name, StringComparison.OrdinalIgnoreCase) || dir[i + 0x42] != 2)
                    continue;
                streamStart = BitConverter.ToInt32(dir, i + 0x74);
                streamSize = BitConverter.ToInt32(dir, i + 0x78);
                break;
            }

            if (streamStart < 0)
                return null;
            if (streamSize < miniCutoff && miniFatStart >= 0)
            {
                var miniFatBytes = Chain(miniFatStart, miniFatCount * sectorSize);
                var miniFat = new List<int>();
                for (var i = 0; i + 4 <= miniFatBytes.Length; i += 4)
                    miniFat.Add(BitConverter.ToInt32(miniFatBytes, i));
                var miniStream = Chain(miniStreamStart, -1);
                var ms = new MemoryStream();
                var s = streamStart;
                var guard = 0;
                while (s >= 0 && guard++ < 1000000)
                {
                    var at = s * miniSize;
                    var take = Math.Min(miniSize, miniStream.Length - at);
                    if (take <= 0)
                        break;
                    ms.Write(miniStream, at, take);
                    s = s < miniFat.Count ? miniFat[s] : -2;
                }

                var all = ms.ToArray();
                if (streamSize < all.Length)
                    Array.Resize(ref all, streamSize);
                return all;
            }

            return Chain(streamStart, streamSize);
        }

        private static byte[] Sector(FileStream fs, int sector, int sectorSize)
        {
            var buf = new byte[sectorSize];
            fs.Position = (sector + 1L) * sectorSize;
            var read = 0;
            while (read < sectorSize)
            {
                var n = fs.Read(buf, read, sectorSize - read);
                if (n == 0)
                    break;
                read += n;
            }

            return buf;
        }
    }
}
