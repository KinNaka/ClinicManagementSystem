using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using ClinicManagementSystem.Web.Models.Identity;
using ClinicManagementSystem.Web.Entities;
using ClinicManagementSystem.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Web.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
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
        // ==============================
        // Sales Management Configuration
        // ==============================

        // Sale Code must be unique
        modelBuilder.Entity<Sale>()
            .HasIndex(s => s.SaleCode)
            .IsUnique();

        // Sale -> Patient
        modelBuilder.Entity<Sale>()
            .HasOne(s => s.Patient)
            .WithMany()
            .HasForeignKey(s => s.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        // Sale -> SaleDetails
        modelBuilder.Entity<SaleDetail>()
            .HasOne(d => d.Sale)
            .WithMany(s => s.Details)
            .HasForeignKey(d => d.SaleId)
            .OnDelete(DeleteBehavior.Restrict);

        // SaleDetail -> Product
        modelBuilder.Entity<SaleDetail>()
            .HasOne(d => d.Product)
            .WithMany()
            .HasForeignKey(d => d.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // Decimal precision
        modelBuilder.Entity<SaleDetail>()
            .Property(d => d.Notes)
            .HasMaxLength(500);

        modelBuilder.Entity<Sale>()
            .Property(s => s.TotalAmount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<SaleDetail>()
            .Property(d => d.Quantity)
            .HasPrecision(18, 3);

        modelBuilder.Entity<SaleDetail>()
            .Property(d => d.UnitPrice)
            .HasPrecision(18, 2);

        modelBuilder.Entity<SaleDetail>()
            .Property(d => d.LineTotal)
            .HasPrecision(18, 2);
    }
    public DbSet<InventoryTransaction> InventoryTransactions
    => Set<InventoryTransaction>();

    public DbSet<InventoryTransactionDetail> InventoryTransactionDetails
        => Set<InventoryTransactionDetail>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Sale> Sales => Set<Sale>();

    public DbSet<SaleDetail> SaleDetails => Set<SaleDetail>();
}
