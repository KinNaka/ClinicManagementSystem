namespace ClinicManagementSystem.Web.Models.Entities
{
    public class ProductCategory
    {
        public long Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string status { get; set; } = "Active";
        public DateTime CreateAt { get; set; }
        public DateTime UpdateAt { get; set; }

        // Navigation property for related products
        public ICollection<Product> Products { get; set; } = new List<Product>();

    }
}
