using MedLoan.Application.Common.Models;

namespace MedLoan.Application.Loans;

public interface ILoanService
{
    Task<Result<LoanDto>> CreateAsync(CreateLoanRequest request, CancellationToken ct);
    Task<Result> ReturnAsync(int id, UpdateLoanRequest request, CancellationToken ct);
    Task<PagedResult<LoanDto>> GetPagedAsync(LoanQueryParameters query, CancellationToken ct);
    Task<Result<LoanDto>> GetByIdAsync(int id, CancellationToken ct);
    Task<Result<IReadOnlyList<LoanDto>>> GetMineAsync(CancellationToken ct);
}
