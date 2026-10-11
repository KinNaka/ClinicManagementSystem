
namespace ClinicManagementSystem.Web.Models.ViewModels.Sales
{
    public class SaleIndexViewModel
    {
        public string? SearchTerm { get; set; }

        public string? StatusFilter { get; set; }

        public List<SaleListViewModel> Sales
        { get; set; } = new();
    }
}
