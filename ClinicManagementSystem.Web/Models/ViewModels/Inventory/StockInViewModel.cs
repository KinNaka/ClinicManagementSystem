using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ClinicManagementSystem.Web.ViewModels.Inventory
{
    public class StockInViewModel
    {
        [MaxLength(100)]
        public string? ReferenceNumber { get; set; }

        [Required]
        [MaxLength(500)]
        public string Reason { get; set; } = "Nhập hàng";

        public List<StockInDetailViewModel> Details { get; set; }
            = new();

        public List<SelectListItem> Products { get; set; }
            = new();
    }
}