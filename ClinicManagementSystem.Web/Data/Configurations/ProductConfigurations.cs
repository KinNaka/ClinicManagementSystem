using ClinicManagementSystem.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicManagementSystem.Web.Data.Configurations

{
    public class ProductConfigurations : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            builder.ToTable("Products");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.ProductCode)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(x => x.CategoryId).IsRequired();

            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(x => x.Strength).HasMaxLength(100);
            
            builder.Property(x => x.DosageForm).HasMaxLength(100);

            builder.Property(x => x.Unit).HasMaxLength(50);

            builder.Property(x => x.Manufacturer).HasMaxLength(200);

            builder.Property(x => x.CostPrice)
                .IsRequired()
                .HasPrecision(18, 2);
            
            builder.Property(x => x.SellingPrice)
                .IsRequired()
                .HasPrecision(18, 2);

            builder.Property(x => x.MinimumStock).HasPrecision(18, 2);

            builder.Property(x => x.CurrentStock).HasPrecision(19, 3);
            builder.Property(x => x.KiotVietInfo).HasMaxLength(4000);

            builder.Property(x => x.Status)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(x => x.CreatedAt).IsRequired();

            builder.Property(x => x.UpdatedAt).IsRequired();

            builder.HasOne(x => x.Category)
                .WithMany(x => x.Products)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

        }
    }
}
