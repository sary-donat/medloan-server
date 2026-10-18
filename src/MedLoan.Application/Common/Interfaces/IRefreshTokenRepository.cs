using MedLoan.Domain.Entities;

namespace MedLoan.Application.Common.Interfaces;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByHashAsync(string hash, CancellationToken ct);
    Task<IReadOnlyList<RefreshToken>> GetActiveByMemberAsync(int memberId, DateTime now, CancellationToken ct);
    void Add(RefreshToken token);
}
