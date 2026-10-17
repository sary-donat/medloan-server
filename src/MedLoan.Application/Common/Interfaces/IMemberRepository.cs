using MedLoan.Application.Common.Models;
using MedLoan.Application.Members;
using MedLoan.Domain.Entities;

namespace MedLoan.Application.Common.Interfaces;

public interface IMemberRepository
{
    Task<Member?> GetByEmailAsync(string email, CancellationToken ct);
    Task<Member?> GetByIdAsync(int id, CancellationToken ct);
    Task<Member?> GetByIdForUpdateAsync(int id, CancellationToken ct);
    Task<PagedResult<MemberDto>> GetPagedAsync(int page, CancellationToken ct);
    void Add(Member member);
}
