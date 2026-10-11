
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

        public Sale Sale { get; set; } = null!;

        public Product Product { get; set; } = null!;
    }
}
