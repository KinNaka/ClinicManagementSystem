namespace ClinicManagementSystem.Web.Models.Entities;

public class ComboComponent
{
    public long ComboProductId { get; set; }
    public long ComponentProductId { get; set; }
    public decimal Quantity { get; set; }
    public Product ComboProduct { get; set; } = null!;
    public Product ComponentProduct { get; set; } = null!;
}
