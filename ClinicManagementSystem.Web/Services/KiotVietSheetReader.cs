using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace ClinicManagementSystem.Web.Services;

public sealed record KiotVietSheet(bool Date1904, List<KiotVietSheetRow> Rows);
public sealed record KiotVietSheetRow(int Number, Dictionary<string, string> Values);

public static class KiotVietSheetReader
{
    public static KiotVietSheet Read(Stream stream, string[] requiredHeaders)
    {
        using var zip = new ZipArchive(stream, ZipArchiveMode.Read, true);
        if (zip.Entries.Count > 1000 || zip.Entries.Sum(e => e.Length) > 150 * 1024 * 1024)
            throw new InvalidDataException("File Excel quá lớn sau giải nén.");
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        XDocument Load(string path)
        {
            using var content = (zip.GetEntry(path) ?? throw new InvalidDataException("File không đúng định dạng .xlsx.")).Open();
            using var xml = XmlReader.Create(content, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 150 * 1024 * 1024 });
            return XDocument.Load(xml);
        }
        var workbook = Load("xl/workbook.xml");
        var date1904 = (string?)workbook.Root?.Element(ns + "workbookPr")?.Attribute("date1904") is "1" or "true";
        var sheet = workbook.Descendants(ns + "sheet").FirstOrDefault() ?? throw new InvalidDataException("File không có trang tính.");
        var id = (string?)sheet.Attribute(XName.Get("id", "http://schemas.openxmlformats.org/officeDocument/2006/relationships"));
        var target = (string?)Load("xl/_rels/workbook.xml.rels").Root?.Elements().FirstOrDefault(e => (string?)e.Attribute("Id") == id && (string?)e.Attribute("TargetMode") != "External")?.Attribute("Target")
            ?? throw new InvalidDataException("Không đọc được trang tính.");
        var path = new Uri(new Uri("https://xlsx.local/xl/workbook.xml"), target).AbsolutePath.TrimStart('/');
        var strings = zip.GetEntry("xl/sharedStrings.xml") == null ? Array.Empty<string>() : Load("xl/sharedStrings.xml").Descendants(ns + "si").Select(s => string.Concat(s.Descendants(ns + "t").Select(t => t.Value))).ToArray();
        string Value(XElement c)
        {
            var raw = c.Element(ns + "v")?.Value ?? "";
            return (string?)c.Attribute("t") switch {
                "s" => int.TryParse(raw, out var index) && index >= 0 && index < strings.Length ? strings[index] : throw new InvalidDataException("Chuỗi Excel không hợp lệ."),
                "inlineStr" => string.Concat(c.Descendants(ns + "t").Select(t => t.Value)),
                "e" => throw new InvalidDataException("File có ô lỗi Excel. Vui lòng sửa trước khi nhập."), _ => raw
            };
        }
        string Column(XElement c) => Regex.Match((string?)c.Attribute("r") ?? "", "^[A-Z]+").Value;
        var rows = Load(path).Descendants(ns + "sheetData").Elements(ns + "row").ToList();
        if (rows.Count > 100001) throw new InvalidDataException("Tối đa 100.000 dòng chi tiết mỗi file.");
        var header = rows.FirstOrDefault() ?? throw new InvalidDataException("File trống.");
        var headers = header.Elements(ns + "c").ToDictionary(Column, c => Value(c).Trim());
        if (headers.Values.Where(s => s.Length > 0).Distinct().Count() != headers.Values.Count(s => s.Length > 0))
            throw new InvalidDataException("Tên cột bị trùng trong file.");
        foreach (var name in requiredHeaders)
            if (headers.Values.Count(h => h == name) != 1) throw new InvalidDataException($"File phải có đúng một cột '{name}'.");
        var result = rows.Skip(1).Select(row => new KiotVietSheetRow((int?)row.Attribute("r") ?? 0,
            row.Elements(ns + "c").Where(c => headers.TryGetValue(Column(c), out var name) && name.Length > 0).ToDictionary(c => headers[Column(c)], Value)))
            .Where(row => row.Values.Values.Any(s => !string.IsNullOrWhiteSpace(s))).ToList();
        return new KiotVietSheet(date1904, result);
    }
}
