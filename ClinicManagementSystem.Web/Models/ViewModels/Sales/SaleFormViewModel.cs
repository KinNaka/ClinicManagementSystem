
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ClinicManagementSystem.Web.Models.ViewModels.Sales
{
    public class SaleFormViewModel
    {
        public long Id { get; set; }
        public long? CopyFromId { get; set; }
        public string? CopyFromCode { get; set; }
        public decimal Discount { get; set; }
        public decimal Surcharge { get; set; }

        public string SaleCode { get; set; } = string.Empty;

        [Range(1, long.MaxValue,
            ErrorMessage = "Vui lòng chọn bệnh nhân.")]
        public long PatientId { get; set; }

        [StringLength(500,
            ErrorMessage = "Ghi chú tối đa 500 ký tự.")]
        public string? Notes { get; set; }

        public string Status { get; set; } = "Draft";

        public decimal TotalAmount { get; set; }

        public List<SaleDetailFormViewModel> Details
        { get; set; } = new();

        // Dữ liệu phục vụ dropdown
        public List<SelectListItem> Patients
        { get; set; } = new();

        public List<SelectListItem> Products
        { get; set; } = new();
    }
}
