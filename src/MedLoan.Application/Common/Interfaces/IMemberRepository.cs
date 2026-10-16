using MedLoan.Domain.Entities;

namespace MedLoan.Application.Common.Interfaces;

public interface IMemberRepository
{
    Task<Member?> GetByEmailAsync(string email, CancellationToken ct);
}
