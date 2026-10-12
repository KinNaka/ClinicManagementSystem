using System.Globalization;
using System.ComponentModel.DataAnnotations;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using ClinicManagementSystem.Web.Models.Entities;
using ClinicManagementSystem.Web.Models.ViewModels.Patients;

namespace ClinicManagementSystem.Web.Services;

// Reads values from KiotViet .xlsx exports; never evaluates spreadsheet formulas.
public sealed class KiotVietCustomerReader(VietnamAddressService addresses)
{
    private static readonly XNamespace Ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    public const int MaximumRows = 20000;

    public List<PatientImportRow> Read(Stream stream)
    {
        using var zip = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
        if (zip.Entries.Count > 1000 || zip.Entries.Sum(e => e.Length) > 100 * 1024 * 1024)
            throw new InvalidDataException("File Excel quá lớn sau giải nén.");
        XDocument Load(string path)
        {
            var entry = zip.GetEntry(path) ?? throw new InvalidDataException("File không đúng định dạng Excel .xlsx.");
            using var content = entry.Open();
            using var xml = XmlReader.Create(content, new XmlReaderSettings {
                DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 100 * 1024 * 1024
            });
            return XDocument.Load(xml);
        }
        var workbook = Load("xl/workbook.xml");
        var date1904 = (string?)workbook.Root?.Element(Ns + "workbookPr")?.Attribute("date1904") is "1" or "true";
        var sheet = workbook.Descendants(Ns + "sheet").FirstOrDefault()
            ?? throw new InvalidDataException("File không có trang tính.");
        var relationshipId = (string?)sheet.Attribute(XName.Get("id", "http://schemas.openxmlformats.org/officeDocument/2006/relationships"));
        var relationship = Load("xl/_rels/workbook.xml.rels").Root?.Elements()
            .FirstOrDefault(e => (string?)e.Attribute("Id") == relationshipId && (string?)e.Attribute("TargetMode") != "External");
        var target = (string?)relationship?.Attribute("Target") ?? throw new InvalidDataException("Không đọc được trang tính.");
        var sheetPath = new Uri(new Uri("https://xlsx.local/xl/workbook.xml"), target).AbsolutePath.TrimStart('/');
        var shared = zip.GetEntry("xl/sharedStrings.xml") == null ? Array.Empty<string>() :
            Load("xl/sharedStrings.xml").Descendants(Ns + "si")
                .Select(si => string.Concat(si.Descendants(Ns + "t").Select(t => t.Value))).ToArray();
        string Value(XElement cell)
        {
            var raw = cell.Element(Ns + "v")?.Value ?? "";
            return (string?)cell.Attribute("t") switch {
                "s" => int.TryParse(raw, out var index) && index >= 0 && index < shared.Length ? shared[index] : throw new InvalidDataException("Chuỗi Excel không hợp lệ."),
                "inlineStr" => string.Concat(cell.Descendants(Ns + "t").Select(t => t.Value)),
                "e" => throw new InvalidDataException("File có ô lỗi Excel. Vui lòng sửa lỗi trước khi nhập."),
                _ => raw
            };
        }
        static string Column(XElement cell) => Regex.Match((string?)cell.Attribute("r") ?? "", "^[A-Z]+").Value;
        var sheetRows = Load(sheetPath).Descendants(Ns + "sheetData").Elements(Ns + "row").ToList();
        if (sheetRows.Count > MaximumRows + 1) throw new InvalidDataException($"Chỉ hỗ trợ tối đa {MaximumRows:N0} khách hàng mỗi file.");
        var header = sheetRows.FirstOrDefault() ?? throw new InvalidDataException("File Excel trống.");
        var headers = header.Elements(Ns + "c").ToDictionary(Column, c => Value(c).Trim());
        foreach (var name in new[] { "Mã khách hàng", "Tên khách hàng", "Tổng bán" })
            if (headers.Values.Count(h => h == name) != 1)
                throw new InvalidDataException($"File phải có đúng một cột '{name}'.");
        var result = new List<PatientImportRow>();
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "SE Asia Standard Time"));
        foreach (var row in sheetRows.Skip(1))
        {
            var values = row.Elements(Ns + "c").Where(c => headers.ContainsKey(Column(c)))
                .ToDictionary(c => headers[Column(c)], Value);
            string Get(string name) => values.GetValueOrDefault(name, "").Trim();
            if (values.Values.All(string.IsNullOrWhiteSpace)) continue;
            var item = new PatientImportRow { RowNumber = (int?)row.Attribute("r") ?? result.Count + 2 };
            var patient = item.Patient;
            patient.PatientCode = Get("Mã khách hàng"); // Keep the original identifier, including case and zeroes.
            patient.FullName = Get("Tên khách hàng");
            patient.PhoneNumber = Optional(Get("Điện thoại"));
            patient.Notes = Optional(Get("Ghi chú"));
            void PreserveNote(string label, string value)
            {
                patient.Notes = string.Join("\n", new[] { patient.Notes, label + ": " + value }.Where(s => !string.IsNullOrEmpty(s)));
            }
            if (patient.PhoneNumber != null && (patient.PhoneNumber.Length > 20 || !new PhoneAttribute().IsValid(patient.PhoneNumber)))
            {
                var original = patient.PhoneNumber;
                var match = Regex.Match(original, @"(?<!\d)(?:\+?84|0)(?:[ .-]*\d){8,10}(?!\d)");
                patient.PhoneNumber = match.Success ? Regex.Replace(match.Value, @"[ .-]", "") : null;
                PreserveNote("Điện thoại KiotViet gốc", original);
                item.Warnings.Add("Nội dung điện thoại gốc được giữ trong ghi chú; chỉ lấy số chính nếu nhận diện được.");
            }
            patient.Gender = Get("Giới tính") switch { "Nam" => "Male", "Nữ" => "Female", "Khác" => "Other", "" => null, _ => null };
            if (!string.IsNullOrEmpty(Get("Giới tính")) && patient.Gender == null)
                item.Warnings.Add("Giới tính không nhận diện được; để trống.");
            patient.Status = Get("Trạng thái") switch {
                "" or "1" or "True" or "true" or "Đang hoạt động" => "Active",
                "0" or "False" or "false" or "Ngừng hoạt động" => "Inactive",
                _ => "Active"
            };
            if (Get("Trạng thái") is not ("" or "1" or "True" or "true" or "Đang hoạt động" or "0" or "False" or "false" or "Ngừng hoạt động"))
                item.Errors.Add("Trạng thái không hợp lệ.");
            var birthday = Get("Ngày sinh");
            if (birthday.Length > 0)
            {
                try
                {
                    patient.DateOfBirth = double.TryParse(birthday, NumberStyles.Float, CultureInfo.InvariantCulture, out var serial)
                        ? DateOnly.FromDateTime(DateTime.FromOADate(serial + (date1904 ? 1462 : 0)))
                        : DateOnly.ParseExact(birthday, new[] { "dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd", "yyyy-MM-ddTHH:mm:ss", "dd/MM/yyyy HH:mm:ss" }, CultureInfo.InvariantCulture);
                    if (patient.DateOfBirth > today)
                    {
                        PreserveNote("Ngày sinh KiotViet cần kiểm tra", patient.DateOfBirth.Value.ToString("dd/MM/yyyy"));
                        patient.DateOfBirth = null;
                        item.Warnings.Add("Ngày sinh ở tương lai; để trống và giữ giá trị gốc trong ghi chú.");
                    }
                }
                catch (Exception ex) when (ex is FormatException or ArgumentException) { item.Errors.Add("Ngày sinh không hợp lệ."); }
            }
            var amount = Get("Tổng bán");
            if (amount.Length == 0) patient.LegacyTotalSales = 0;
            else if (decimal.TryParse(amount, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent,
                CultureInfo.InvariantCulture, out var money) && money >= 0 && money <= 9999999999999999.99m && decimal.Round(money, 2) == money)
                patient.LegacyTotalSales = money;
            else item.Errors.Add("Tổng bán phải là số không âm, tối đa 2 chữ số thập phân.");

            var area = Get("Khu vực giao hàng");
            var wardName = Get("Phường/Xã");
            var province = addresses.Provinces.FirstOrDefault(p => SameName(p.Name, area));
            var ward = province?.Wards.SingleOrDefault(w => SameName(w.Name, wardName));
            if (province != null && (ward != null || wardName.Length == 0))
            {
                patient.Address = Optional(Get("Địa chỉ"));
                patient.ProvinceCode = province.Code;
                patient.ProvinceName = province.Name;
                patient.WardCode = ward?.Code;
                patient.WardName = ward?.Name;
            }
            else
            {
                patient.Address = Optional(string.Join(", ", new[] { Get("Địa chỉ"), wardName, area }.Where(s => s.Length > 0)));
                if (area.Length > 0 || wardName.Length > 0)
                    item.Warnings.Add("Địa chỉ cũ được giữ nguyên dạng văn bản; cần chọn lại tỉnh/xã nếu đã sáp nhập.");
            }
            if (!Regex.IsMatch(patient.PatientCode, @"^[A-Za-z0-9_-]{1,30}$")) item.Errors.Add("Mã KH trống, quá 30 ký tự hoặc có ký tự không hợp lệ.");
            if (patient.FullName.Length is 0 or > 200) item.Errors.Add("Tên khách hàng trống hoặc quá 200 ký tự.");
            if (patient.PhoneNumber?.Length > 20) item.Errors.Add("Điện thoại quá 20 ký tự.");
            if (patient.Address?.Length > 300) item.Errors.Add("Địa chỉ quá 300 ký tự.");
            if (patient.Notes?.Length > 500) item.Errors.Add("Ghi chú quá 500 ký tự.");
            result.Add(item);
        }
        foreach (var group in result.GroupBy(r => r.Patient.PatientCode, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1))
            foreach (var row in group) row.Errors.Add("Mã KH trùng trong file; vui lòng sửa trước khi nhập.");
        if (result.Count == 0) throw new InvalidDataException("File không có khách hàng để nhập.");
        return result;
    }

    private static string? Optional(string value) => value.Length == 0 ? null : value;
    private static bool SameName(string a, string b) => string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase);
}
