
using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.Web.Models.ViewModels.Sales
{
    public class SaleDetailFormViewModel
    {
        [Range(1, long.MaxValue,
            ErrorMessage = "Vui lòng chọn sản phẩm.")]
        public long ProductId { get; set; }
        public long SourceDetailId { get; set; }
        public decimal PreservedQuantity { get; set; }
        public decimal PreservedLineTotal { get; set; }

        [Range(typeof(decimal), "0.001", "999999999",
            ErrorMessage = "Số lượng phải từ 0,001 đến 999999999.")]
        public decimal Quantity { get; set; } = 1;

        [StringLength(500, ErrorMessage = "Ghi chú sản phẩm tối đa 500 ký tự.")]
        public string? Notes { get; set; }

        // Chỉ dùng để hiển thị trên giao diện.
        // Service sẽ lấy giá bán từ database.
        public string ProductName { get; set; } = string.Empty;

        public string? Unit { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal LineTotal { get; set; }
    }
}
