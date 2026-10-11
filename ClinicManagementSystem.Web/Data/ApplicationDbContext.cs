
using ClinicManagementSystem.Web.Entities;
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        modelBuilder.Entity<InventoryTransaction>()
       .HasIndex(x => x.TransactionCode)
       .IsUnique();

        modelBuilder.Entity<InventoryTransaction>()
            .Property(x => x.TransactionType)
            .HasMaxLength(20)
            .IsRequired();

        modelBuilder.Entity<InventoryTransactionDetail>()
            .Property(x => x.Quantity)
            .HasPrecision(18, 3);

        modelBuilder.Entity<InventoryTransactionDetail>()
            .Property(x => x.UnitPrice)
            .HasPrecision(18, 2);

        modelBuilder.Entity<InventoryTransactionDetail>()
            .HasOne(x => x.InventoryTransaction)
            .WithMany(x => x.Details)
            .HasForeignKey(x => x.InventoryTransactionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<InventoryTransactionDetail>()
            .HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Patient>()
    .HasIndex(p => p.PatientCode)
    .IsUnique();

        modelBuilder.Entity<Patient>()
            .Property(p => p.PatientCode)
            .HasMaxLength(30)
            .IsRequired();

        modelBuilder.Entity<Patient>()
            .Property(p => p.FullName)
            .HasMaxLength(200)
            .IsRequired();

        modelBuilder.Entity<Patient>()
            .Property(p => p.Status)
            .HasMaxLength(20)
            .IsRequired();
    }
    public DbSet<InventoryTransaction> InventoryTransactions
    => Set<InventoryTransaction>();

    public DbSet<InventoryTransactionDetail> InventoryTransactionDetails
        => Set<InventoryTransactionDetail>();
    public DbSet<Patient> Patients => Set<Patient>();
}