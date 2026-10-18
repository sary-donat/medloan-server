using MedLoan.Application.Common.Interfaces;
using MedLoan.Domain.Entities;
using MedLoan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MedLoan.Infrastructure.Repositories;

public class RefreshTokenRepository(AppDbContext db) : IRefreshTokenRepository
{
    private readonly AppDbContext _db = db;

    // Tracked: refresh and logout revoke the token they load.
    public Task<RefreshToken?> GetByHashAsync(string hash, CancellationToken ct) =>
        _db.RefreshTokens.Include(r => r.Member).FirstOrDefaultAsync(r => r.TokenHash == hash, ct);

    // Tracked: on reuse, every token returned here is revoked.
    public async Task<IReadOnlyList<RefreshToken>> GetActiveByMemberAsync(int memberId, DateTime now, CancellationToken ct) =>
        await _db.RefreshTokens
            .Where(r => r.MemberId == memberId && r.RevokedAt == null && r.ExpiresAt > now)
            .ToListAsync(ct);

    public void Add(RefreshToken token) => _db.RefreshTokens.Add(token);
}
