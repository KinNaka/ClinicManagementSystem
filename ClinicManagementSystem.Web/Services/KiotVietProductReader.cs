using System.Globalization;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using ClinicManagementSystem.Web.Models.Entities;
using ClinicManagementSystem.Web.Models.ViewModels.Products;

namespace ClinicManagementSystem.Web.Services;

public sealed class KiotVietProductReader
{
    public List<ProductImportRow> Read(Stream stream, bool importStock, bool resetNegativeStock)
    {
        using var zip = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
        if (zip.Entries.Count > 1000 || zip.Entries.Sum(e => e.Length) > 100 * 1024 * 1024)
            throw new InvalidDataException("File Excel quá lớn sau giải nén.");
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        XDocument Load(string path)
        {
            using var content = (zip.GetEntry(path) ?? throw new InvalidDataException("File không đúng định dạng .xlsx.")).Open();
            using var xml = XmlReader.Create(content, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 100 * 1024 * 1024 });
            return XDocument.Load(xml);
        }
        var sheet = Load("xl/workbook.xml").Descendants(ns + "sheet").FirstOrDefault() ?? throw new InvalidDataException("File không có trang tính.");
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
        if (rows.Count > 20001) throw new InvalidDataException("Tối đa 20.000 hàng hóa mỗi file.");
        var header = rows.FirstOrDefault() ?? throw new InvalidDataException("File trống.");
        var headers = header.Elements(ns + "c").ToDictionary(Column, c => Value(c).Trim());
        foreach (var name in new[] { "Loại hàng", "Nhóm hàng(3 Cấp)", "Mã hàng", "Tên hàng", "Giá bán", "Giá vốn", "Tồn kho" })
            if (headers.Values.Count(h => h == name) != 1) throw new InvalidDataException($"File phải có đúng một cột '{name}'.");
        var result = new List<ProductImportRow>();
        foreach (var source in rows.Skip(1))
        {
            var values = source.Elements(ns + "c").Where(c => headers.ContainsKey(Column(c))).ToDictionary(c => headers[Column(c)], Value);
            string Get(string name) => values.GetValueOrDefault(name, "").Trim();
            if (values.Values.All(string.IsNullOrWhiteSpace)) continue;
            var row = new ProductImportRow { RowNumber = (int?)source.Attribute("r") ?? result.Count + 2, CategoryName = Get("Nhóm hàng(3 Cấp)"), TypeName = Get("Loại hàng") };
            var p = row.Product;
            p.ProductCode = Get("Mã hàng"); p.Name = Get("Tên hàng");
            p.Unit = Null(Get("ĐVT")); p.Strength = Null(Get("Hàm lượng"));
            p.IsCombo = row.TypeName == "Combo - đóng gói"; p.IsService = row.TypeName == "Dịch vụ";
            p.Status = p.IsCombo ? "INACTIVE" : "ACTIVE";
            if (row.TypeName is not ("Hàng hóa" or "Thuốc" or "Dịch vụ" or "Combo - đóng gói")) row.Errors.Add("Loại hàng không được hỗ trợ.");
            decimal Number(string name, int scale, bool allowNegative = false)
            {
                var value = Get(name);
                if (value.Length == 0) { row.Warnings.Add($"'{name}' trống, dùng 0."); return 0; }
                if (!decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) || (!allowNegative && number < 0) || Math.Abs(number) > 999999999999999m)
                { row.Errors.Add($"'{name}' không phải số hợp lệ."); return 0; }
                if (name == "Tồn kho" && number != decimal.Truncate(number))
                { row.Errors.Add("Tồn kho phải là số nguyên; hãy kiểm tra lại dữ liệu nguồn."); return number; }
                var rounded = decimal.Round(number, scale, MidpointRounding.AwayFromZero);
                if (rounded != number) row.Warnings.Add($"'{name}' làm tròn đến {scale} chữ số thập phân.");
                return rounded;
            }
            p.SellingPrice = Number("Giá bán", 2); p.CostPrice = Number("Giá vốn", 2);
            row.SourceStock = Number("Tồn kho", 0, true);
            p.CurrentStock = importStock && !p.IsCombo && !p.IsService ? row.SourceStock : 0;
            if (p.CurrentStock < 0 && resetNegativeStock) { p.CurrentStock = 0; row.Warnings.Add("Tồn kho âm đưa về 0 theo lựa chọn nhập; giá trị gốc được giữ trong thông tin KiotViet."); }
            else if (p.CurrentStock < 0) row.Warnings.Add("Giữ tồn kho âm từ KiotViet; cần đối chiếu và điều chỉnh kho sau nhập.");
            if (p.IsCombo) row.Warnings.Add("Combo chưa có thành phần trong file: nhập trạng thái ngừng kinh doanh, cấu hình thành phần trước khi kích hoạt.");
            if (p.IsService) row.Warnings.Add("Dịch vụ: không quản lý hoặc trừ tồn kho.");
            if (p.Unit == null) row.Warnings.Add("Thiếu đơn vị tính, để trống để bổ sung sau.");
            if (row.CategoryName.Length == 0) { row.CategoryName = "Chưa phân nhóm (KiotViet)"; row.Warnings.Add("Chưa có nhóm hàng, đưa vào nhóm chưa phân nhóm."); }
            p.KiotVietInfo = string.Join("\n", new[] { "Loại hàng", "Mã vạch", "Mã thuốc CSDL về dược", "Hoạt chất", "Quy cách đóng gói", "Vị trí", "Tồn kho" }.Where(name => Get(name).Length > 0).Select(name => name + ": " + Get(name)));
            if (p.ProductCode.Length is 0 or > 50 || p.ProductCode.Any(char.IsControl)) row.Errors.Add("Mã hàng trống, quá 50 ký tự hoặc có ký tự điều khiển.");
            if (p.Name.Length is 0 or > 200) row.Errors.Add("Tên hàng trống hoặc quá 200 ký tự.");
            if (row.CategoryName.Length > 200) row.Errors.Add("Nhóm hàng quá 200 ký tự.");
            if (p.Unit?.Length > 50 || p.Strength?.Length > 100 || p.KiotVietInfo.Length > 4000) row.Errors.Add("Thông tin hàng vượt giới hạn độ dài.");
            result.Add(row);
        }
        foreach (var group in result.GroupBy(r => r.Product.ProductCode, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1))
            foreach (var row in group) row.Errors.Add("Mã hàng trùng trong file. Vui lòng sửa trước khi nhập.");
        if (result.Count == 0) throw new InvalidDataException("File không có hàng hóa.");
        return result;
    }
    private static string? Null(string s) => s.Length == 0 ? null : s;
}
