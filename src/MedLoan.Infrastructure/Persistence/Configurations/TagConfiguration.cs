using MedLoan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedLoan.Infrastructure.Persistence.Configurations;

public class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.Property(t => t.Name).IsRequired().HasMaxLength(50);
        builder.HasIndex(t => t.Name).IsUnique();
    }
}
