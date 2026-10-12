
namespace ClinicManagementSystem.Web.Models.ViewModels.Sales
{
    public class SaleIndexViewModel
    {
        public string? SearchTerm { get; set; }

        public string? StatusFilter { get; set; }
        public const int PageSize = 50;
        public int Page { get; set; } = 1;
        public int TotalCount { get; set; }
        public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));

        public List<SaleListViewModel> Sales
        { get; set; } = new();
    }
}
