using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ClinicManagementSystem.Web.ViewModels.Inventory
{
    public class StockOutViewModel
    {
        public long? PatientId { get; set; }
        public List<SelectListItem> Patients { get; set; } = new();
        [Required]
        [MaxLength(500)]
        public string Reason { get; set; } = "Xuất sử dụng";

        public List<StockInDetailViewModel> Details { get; set; }
            = new();

        public List<SelectListItem> Products { get; set; }
            = new();
    }
}
