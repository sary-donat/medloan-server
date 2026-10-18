using MedLoan.Domain.Common;

namespace MedLoan.Domain.Entities;

// Only the hash is stored; the token itself goes to the client and nowhere else (11.13).
public class RefreshToken : BaseEntity
{
    public int MemberId { get; set; }
    public Member Member { get; set; } = null!;
    public string TokenHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }

    public bool IsActive(DateTime now) => RevokedAt == null && ExpiresAt > now;
}
