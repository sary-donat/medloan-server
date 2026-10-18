using MedLoan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedLoan.Infrastructure.Persistence.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        // SHA-256 as lowercase hex: always 64 characters.
        builder.Property(r => r.TokenHash).IsRequired().HasMaxLength(64);
        builder.HasIndex(r => r.TokenHash).IsUnique();

        builder.HasIndex(r => r.MemberId);

        // No collection on Member, so the Member model does not change.
        builder.HasOne(r => r.Member)
            .WithMany()
            .HasForeignKey(r => r.MemberId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
