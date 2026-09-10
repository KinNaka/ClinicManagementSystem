using ClinicManagementSystem.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicManagementSystem.Web.Data.Configurations
{
    public class ProductCategoryConfiguration : IEntityTypeConfiguration<ProductCategory>
    {
        public void Configure(EntityTypeBuilder<ProductCategory> builder)
        {
            builder.ToTable("ProductCategories");

            builder.HasKey(x=> x.Id);

            builder.Property(x => x.Code)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(x => x.Description).HasMaxLength(500);

            builder.Property(x => x.status)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(x => x.CreateAt).IsRequired();

            builder.Property(x => x.UpdateAt).IsRequired();
        }
    }
}
