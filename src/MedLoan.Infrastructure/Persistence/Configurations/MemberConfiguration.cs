using MedLoan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedLoan.Infrastructure.Persistence.Configurations;

public class MemberConfiguration : IEntityTypeConfiguration<Member>
{
    public void Configure(EntityTypeBuilder<Member> builder)
    {
        builder.Property(m => m.FullName).IsRequired().HasMaxLength(100);

        builder.Property(m => m.Email).IsRequired().HasMaxLength(200);
        builder.HasIndex(m => m.Email).IsUnique();

        builder.Property(m => m.Phone).IsRequired().HasMaxLength(20);

        builder.Property(m => m.Role).HasConversion<string>();
    }
}
