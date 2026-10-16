using MedLoan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedLoan.Infrastructure.Persistence.Configurations;

public class ItemCopyConfiguration : IEntityTypeConfiguration<ItemCopy>
{
    public void Configure(EntityTypeBuilder<ItemCopy> builder)
    {
        builder.Property(c => c.SerialNumber).IsRequired().HasMaxLength(50);
        builder.HasIndex(c => c.SerialNumber).IsUnique();

        builder.Property(c => c.Status).HasConversion<string>();
        builder.Property(c => c.Condition).HasConversion<string>();

        builder.Property(c => c.Version).IsConcurrencyToken();

        builder.HasOne(c => c.Item)
            .WithMany(i => i.Copies)
            .HasForeignKey(c => c.ItemId);
    }
}
