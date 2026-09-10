using ClinicManagementSystem.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Web.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
    public DbSet<ProductCategory> ProductCategories { get; set; }
    public DbSet<Product> Products { get; set; }


}