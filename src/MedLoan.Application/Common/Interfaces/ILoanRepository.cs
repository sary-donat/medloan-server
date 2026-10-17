using MedLoan.Application.Common.Models;
using MedLoan.Application.Loans;
using MedLoan.Domain.Entities;

namespace MedLoan.Application.Common.Interfaces;

public interface ILoanRepository
{
    void Add(Loan loan);
    Task<PagedResult<Loan>> GetPagedAsync(LoanQueryParameters query, DateTime now, CancellationToken ct);
    Task<int> CountActiveByMemberAsync(int memberId, CancellationToken ct);
    Task<bool> AnyLoansForItemAsync(int itemId, CancellationToken ct);
}
