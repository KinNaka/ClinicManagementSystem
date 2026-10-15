using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using ClinicManagementSystem.Web.Models.Entities;
using ClinicManagementSystem.Web.Models.ViewModels.Sales;

namespace ClinicManagementSystem.Web.Services;

public sealed class KiotVietSaleReader
{
    public List<SaleImportInvoice> Read(Stream stream, string fileName)
    {
        var sheet = KiotVietSheetReader.Read(stream, new[] { "Mã hóa đơn", "Thời gian", "Mã khách hàng", "Tên khách hàng", "Tổng tiền hàng", "Giảm giá hóa đơn", "Thu khác", "Mã hàng", "Tên hàng", "Số lượng", "Giá bán", "Thành tiền" });
        var invoices = new Dictionary<string, SaleImportInvoice>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in sheet.Rows)
        {
            string Get(string name) => row.Values.GetValueOrDefault(name, "").Trim();
            var code = Get("Mã hóa đơn");
            if (!invoices.TryGetValue(code, out var invoice))
            {
                invoice = new SaleImportInvoice { FileName = Path.GetFileName(fileName), RowNumber = row.Number, PatientCode = Get("Mã khách hàng"), PatientName = Get("Tên khách hàng"), Phone = Get("Điện thoại"),
                    Address = string.Join(", ", new[] { Get("Địa chỉ (Khách hàng)"), Get("Phường/Xã (Khách hàng)"), Get("Khu vực (Khách hàng)") }.Where(s => s.Length > 0)) };
                invoice.Sale.SaleCode = code;
                invoice.Sale.Status = "Imported";
                invoice.Sale.SourcePatientCode = invoice.PatientCode;
                invoice.Sale.SourcePatientName = invoice.PatientName;
                invoice.Sale.Notes = Get("Ghi chú");
                invoice.Sale.ImportedFileName = invoice.FileName;
                invoices.Add(code, invoice);
            }
            var sale = invoice.Sale;
            decimal Number(string name, int scale, bool required = true)
            {
                var text = Get(name);
                if (text.Length == 0 && !required) return 0;
                if (!decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || value < 0 || value > 999999999999999m)
                { invoice.Errors.Add($"Dòng {row.Number}: {name} không phải số không âm hợp lệ."); return 0; }
                if (name == "Số lượng" && value != decimal.Truncate(value))
                { invoice.Errors.Add($"Dòng {row.Number}: số lượng phải là số nguyên."); return value; }
                var rounded = decimal.Round(value, scale, MidpointRounding.AwayFromZero);
                if (Math.Abs(rounded - value) > 0.000001m) invoice.Warnings.Add($"Dòng {row.Number}: {name} được làm tròn đến {scale} chữ số thập phân.");
                return rounded;
            }
            DateTime Date(string name, bool required)
            {
                var text = Get(name);
                if (text.Length == 0 && !required) return sale.SaleDate;
                try
                {
                    var date = double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var serial)
                        ? DateTime.FromOADate(serial + (sheet.Date1904 ? 1462 : 0))
                        : DateTime.ParseExact(text, new[] { "dd/MM/yyyy HH:mm:ss", "dd/MM/yyyy HH:mm", "d/M/yyyy H:mm", "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-ddTHH:mm:ss.fff", "dd/MM/yyyy", "yyyy-MM-dd" }, CultureInfo.InvariantCulture);
                    if (date < new DateTime(1753, 1, 1)) throw new FormatException();
                    return DateTime.SpecifyKind(date, DateTimeKind.Unspecified);
                }
                catch (Exception ex) when (ex is ArgumentException or FormatException)
                { invoice.Errors.Add($"Dòng {row.Number}: {name} không hợp lệ."); return new DateTime(1753, 1, 1); }
            }
            var date = Date("Thời gian", true);
            var goods = Number("Tổng tiền hàng", 2);
            var discount = Number("Giảm giá hóa đơn", 2, false);
            var surcharge = Number("Thu khác", 2, false);
            if (sale.Details.Count == 0)
            {
                sale.SaleDate = date; sale.CreatedAt = Date("Thời gian tạo", false); sale.UpdatedAt = sale.CreatedAt;
                sale.ImportedGoodsTotal = goods; sale.ImportedDiscount = discount; sale.ImportedSurcharge = surcharge;
                sale.TotalAmount = goods - discount + surcharge;
                if (sale.TotalAmount < 0) invoice.Errors.Add("Tổng tiền sau giảm giá/thu khác bị âm.");
            }
            else if (invoice.PatientCode != Get("Mã khách hàng") || sale.SaleDate != date || goods != sale.ImportedGoodsTotal || discount != sale.ImportedDiscount || surcharge != sale.ImportedSurcharge)
                invoice.Errors.Add($"Dòng {row.Number}: thông tin chung không khớp với các dòng cùng mã hóa đơn.");
            var detail = new SaleDetail { SourceProductCode = Get("Mã hàng"), SourceProductName = Get("Tên hàng"), SourceUnit = Get("ĐVT"),
                Quantity = Number("Số lượng", 0), UnitPrice = Number("Giá bán", 2), LineTotal = Number("Thành tiền", 2), Notes = Get("Ghi chú hàng hóa") };
            detail.KiotVietInfo = JsonSerializer.Serialize(row.Values.Where(v => new[] { "Đơn giá", "Giảm giá %", "Giảm giá", "Nhóm hàng", "Hạn sử dụng", "Bảng giá" }.Contains(v.Key)).ToDictionary(v => v.Key, v => v.Value));
            if (detail.Quantity <= 0) invoice.Errors.Add($"Dòng {row.Number}: số lượng phải lớn hơn 0.");
            if (detail.SourceProductCode.Length is 0 or > 50 || detail.SourceProductCode.Any(char.IsControl)) invoice.Errors.Add($"Dòng {row.Number}: mã hàng trống hoặc không hợp lệ.");
            if (detail.SourceProductName.Length is 0 or > 200 || detail.SourceUnit.Length > 50 || detail.Notes.Length > 500 || detail.KiotVietInfo.Length > 4000) invoice.Errors.Add($"Dòng {row.Number}: tên/đơn vị/ghi chú hàng vượt giới hạn.");
            sale.Details.Add(detail); // Repeated product codes can be separate lots or differently priced lines.
        }
        foreach (var invoice in invoices.Values)
        {
            if (invoice.Sale.SaleCode.Length is 0 or > 50 || invoice.Sale.SaleCode.Any(char.IsControl)) invoice.Errors.Add("Mã hóa đơn trống hoặc quá 50 ký tự.");
            if (invoice.PatientCode.Length > 30 || (invoice.PatientCode.Length > 0 && !Regex.IsMatch(invoice.PatientCode, @"^[A-Za-z0-9_-]+(?:\{DEL\})?$"))) invoice.Errors.Add("Mã KH không hợp lệ hoặc quá 30 ký tự.");
            if (invoice.PatientCode.EndsWith("{DEL}", StringComparison.Ordinal))
                invoice.Warnings.Add("Giữ nguyên mã KH có hậu tố {DEL} trong file; tạo hồ sơ lịch sử riêng nếu chưa có, không tự gộp với mã bỏ hậu tố.");
            if (invoice.PatientName.Length > 200 || invoice.Sale.Notes?.Length > 500 || invoice.FileName.Length > 255) invoice.Errors.Add("Tên KH/ghi chú/tên file vượt giới hạn.");
            var linesTotal = invoice.Sale.Details.Sum(d => d.LineTotal);
            if (Math.Abs(linesTotal - invoice.Sale.ImportedGoodsTotal) > 0.01m)
                invoice.Errors.Add($"Tổng thành tiền chi tiết ({linesTotal:N2}) không khớp tổng tiền hàng ({invoice.Sale.ImportedGoodsTotal:N2}).");
            invoice.Errors = invoice.Errors.Distinct().ToList();
            invoice.Warnings = invoice.Warnings.Distinct().ToList();
        }
        if (invoices.Count == 0) throw new InvalidDataException("File không có hóa đơn.");
        if (invoices.Count > 20000) throw new InvalidDataException("Tối đa 20.000 hóa đơn mỗi file.");
        return invoices.Values.ToList();
    }
}
