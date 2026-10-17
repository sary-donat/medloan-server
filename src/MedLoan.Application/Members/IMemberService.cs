using MedLoan.Application.Common.Models;

namespace MedLoan.Application.Members;

public interface IMemberService
{
    Task<PagedResult<MemberDto>> GetPagedAsync(int page, CancellationToken ct);
    Task<Result<MemberDto>> GetCurrentAsync(CancellationToken ct);
    Task<Result<MemberDto>> GetByIdAsync(int id, CancellationToken ct);
    Task<Result> UpdateAsync(int id, UpdateMemberRequest request, CancellationToken ct);
}
