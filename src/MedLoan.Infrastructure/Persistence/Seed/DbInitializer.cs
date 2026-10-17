using MedLoan.Application.Common.Interfaces;
using MedLoan.Domain.Entities;
using MedLoan.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace MedLoan.Infrastructure.Persistence.Seed;

// Demo members are seeded here and not in HasData: each one needs a real password hash.
public static class DbInitializer
{
    public static async Task<int> SeedAsync(AppDbContext db, IPasswordHasher hasher, TimeProvider time, CancellationToken ct)
    {
        if (await db.Members.AnyAsync(ct))
            return 0;

        var now = time.GetUtcNow().UtcDateTime;
        var members = new[]
        {
            NewMember("מנהל הגמ\"ח", "admin@medloan.local", "050-0000000", "Admin123!", UserRole.Admin),
            NewMember("חבר ראשון", "member1@medloan.local", "050-0000001", "Member123!", UserRole.Member),
            NewMember("חבר שני", "member2@medloan.local", "050-0000002", "Member123!", UserRole.Member)
        };

        db.Members.AddRange(members);
        await db.SaveChangesAsync(ct);

        return members.Length;

        Member NewMember(string fullName, string email, string phone, string password, UserRole role) => new()
        {
            FullName = fullName,
            Email = email,
            Phone = phone,
            PasswordHash = hasher.Hash(password),
            Role = role,
            IsActive = true,
            CreatedAt = now
        };
    }
}
