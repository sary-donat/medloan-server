using MedLoan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedLoan.Infrastructure.Persistence.Configurations;

public class ItemConfiguration : IEntityTypeConfiguration<Item>
{
    public void Configure(EntityTypeBuilder<Item> builder)
    {
        builder.Property(i => i.Name).IsRequired().HasMaxLength(150);
        builder.Property(i => i.Description).HasMaxLength(1000);
        builder.Property(i => i.Manufacturer).HasMaxLength(100);

        builder.HasOne(i => i.Category)
            .WithMany(c => c.Items)
            .HasForeignKey(i => i.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Many-to-many. Seed: 1 wheelchair (foldable, portable), 3 walker (foldable), 5 crutches (portable, child size),
        // 7 oxygen concentrator (needs power), 8 nebulizer (needs power, portable), 11 blood pressure monitor (portable).
        builder.HasMany(i => i.Tags)
            .WithMany(t => t.Items)
            .UsingEntity(j => j.ToTable("ItemTags").HasData(
                new { ItemsId = 1, TagsId = 1 },
                new { ItemsId = 1, TagsId = 4 },
                new { ItemsId = 3, TagsId = 1 },
                new { ItemsId = 5, TagsId = 4 },
                new { ItemsId = 5, TagsId = 3 },
                new { ItemsId = 7, TagsId = 2 },
                new { ItemsId = 8, TagsId = 2 },
                new { ItemsId = 8, TagsId = 4 },
                new { ItemsId = 11, TagsId = 4 }));
    }
}
