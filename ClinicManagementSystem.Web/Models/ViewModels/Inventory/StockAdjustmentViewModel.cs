using System.ComponentModel.DataAnnotations;
namespace ClinicManagementSystem.Web.Models.ViewModels.Inventory;
public class StockAdjustmentViewModel
{
    [Required(ErrorMessage = "Nhập lý do kiểm kho."), StringLength(500)]
    public string Reason { get; set; } = "Kiểm kho và cân tồn thực tế";
    public List<StockCountRow> Details { get; set; } = new();
}
public class StockCountRow
{
    public long ProductId { get; set; }
    public string ProductCode { get; set; } = "";
    public string ProductName { get; set; } = "";
    public string? Unit { get; set; }
    public decimal ExpectedStock { get; set; }
    [Range(typeof(decimal), "0", "999999999", ErrorMessage = "Tồn thực tế phải từ 0 đến 999999999.")]
    public decimal? CountedStock { get; set; }
    [StringLength(180)]
    public string? Note { get; set; }
}
