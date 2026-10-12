
namespace ClinicManagementSystem.Web.Models.Entities
{
    public class SaleDetail
    {
        public long Id { get; set; }

        public long SaleId { get; set; }

        public long ProductId { get; set; }

        public decimal Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal LineTotal { get; set; }

        public string? Notes { get; set; }
        public string? SourceProductCode { get; set; }
        public string? SourceProductName { get; set; }
        public string? SourceUnit { get; set; }
        public string? KiotVietInfo { get; set; }

        public Sale Sale { get; set; } = null!;

        public Product Product { get; set; } = null!;
    }
}
