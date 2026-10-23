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
    public DbSet<ProductEditHistory> ProductEditHistories => Set<ProductEditHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<ProductEditHistory>().HasIndex(h => new { h.ProductId, h.ChangedAt, h.Id });
        modelBuilder.Entity<FollowUpAppointment>().HasIndex(a => a.SaleId).IsUnique().HasFilter("[SaleId] IS NOT NULL");
        modelBuilder.Entity<FollowUpAppointment>().HasIndex(a => new { a.AppointmentDate, a.Status, a.Id });
        modelBuilder.Entity<FollowUpAppointment>().HasOne(a => a.Patient).WithMany().HasForeignKey(a => a.PatientId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<FollowUpAppointment>().HasOne(a => a.Sale).WithMany().HasForeignKey(a => a.SaleId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<FollowUpAppointment>().ToTable(t =>
        {
            t.HasCheckConstraint("CK_FollowUpAppointment_Status", "[Status] IN ('Pending', 'Attended', 'Cancelled')");
            t.HasCheckConstraint("CK_FollowUpAppointment_Days", "[MedicationDays] BETWEEN 1 AND 3650");
            t.HasCheckConstraint("CK_FollowUpAppointment_Date", "[AppointmentDate] >= [MedicationStartDate]");
        });
        modelBuilder.Entity<Patient>().HasOne(p => p.SharedPhoneWithPatient).WithMany()
            .HasForeignKey(p => p.SharedPhoneWithPatientId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ComboComponent>().HasKey(c => new { c.ComboProductId, c.ComponentProductId });
        modelBuilder.Entity<ComboComponent>().Property(c => c.Quantity).HasPrecision(18, 2);
        modelBuilder.Entity<ComboComponent>().ToTable(t => t.HasCheckConstraint("CK_ComboComponent_Quantity", "[Quantity] > 0"));
        modelBuilder.Entity<ComboComponent>().HasOne(c => c.ComboProduct).WithMany(p => p.Components)
            .HasForeignKey(c => c.ComboProductId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ComboComponent>().HasOne(c => c.ComponentProduct).WithMany()
            .HasForeignKey(c => c.ComponentProductId).OnDelete(DeleteBehavior.Restrict);
        
        modelBuilder.Entity<InventoryTransaction>().HasOne(t => t.Patient).WithMany().HasForeignKey(t => t.PatientId).OnDelete(DeleteBehavior.Restrict);
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
            .Property(p => p.LegacyTotalSales)
            .HasPrecision(18, 2);

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
        modelBuilder.Entity<Sale>().Property(s => s.ImportedGoodsTotal).HasPrecision(18, 2);
        modelBuilder.Entity<Sale>().Property(s => s.ImportedDiscount).HasPrecision(18, 2);
        modelBuilder.Entity<Sale>().Property(s => s.ImportedSurcharge).HasPrecision(18, 2);
        modelBuilder.Entity<SaleDetail>().Property(d => d.SourceProductCode).HasMaxLength(50);
        modelBuilder.Entity<SaleDetail>().Property(d => d.SourceProductName).HasMaxLength(200);
        modelBuilder.Entity<SaleDetail>().Property(d => d.SourceUnit).HasMaxLength(50);
        modelBuilder.Entity<SaleDetail>().Property(d => d.KiotVietInfo).HasMaxLength(4000);

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
    public DbSet<FollowUpAppointment> FollowUpAppointments => Set<FollowUpAppointment>();
    public DbSet<Sale> Sales => Set<Sale>();

    public DbSet<SaleDetail> SaleDetails => Set<SaleDetail>();
}
