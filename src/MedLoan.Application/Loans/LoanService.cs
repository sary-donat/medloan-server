using AutoMapper;
using MedLoan.Application.Common.Exceptions;
using MedLoan.Application.Common.Interfaces;
using MedLoan.Application.Common.Models;
using MedLoan.Domain.Entities;
using MedLoan.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace MedLoan.Application.Loans;

public class LoanService(
    ILoanRepository loans,
    IItemCopyRepository copies,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    IMapper mapper,
    TimeProvider time,
    ILogger<LoanService> logger) : ILoanService
{
    private const int MaxActiveLoansPerMember = 3;
    private const string NotFoundMessage = "ההשאלה לא נמצאה";
    private const string ForbiddenMessage = "אין הרשאה לפעולה זו";
    private const string UnauthorizedMessage = "נדרשת התחברות";

    private readonly ILoanRepository _loans = loans;
    private readonly IItemCopyRepository _copies = copies;
    private readonly ICurrentUser _currentUser = currentUser;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IMapper _mapper = mapper;
    private readonly TimeProvider _time = time;
    private readonly ILogger<LoanService> _logger = logger;

    public async Task<Result<LoanDto>> CreateAsync(CreateLoanRequest request, CancellationToken ct)
    {
        if (_currentUser.Id is not int memberId)
            return Result<LoanDto>.Unauthorized(UnauthorizedMessage);

        // Tracked, with Item: the copy is updated under its concurrency token, and MaxLoanDays sets DueAt.
        var copy = await _copies.GetByIdForUpdateAsync(request.ItemCopyId, ct);
        if (copy is null)
            return Result<LoanDto>.NotFound("העותק לא נמצא");

        if (!copy.IsAvailable)
        {
            // A different message from the Version conflict below, so the log shows which check rejected the request.
            _logger.LogWarning("Copy {CopyId} not available. Member {MemberId} was rejected", copy.Id, memberId);
            return Result<LoanDto>.Conflict("העותק אינו זמין");
        }

        if (await _loans.CountActiveByMemberAsync(memberId, ct) >= MaxActiveLoansPerMember)
            return Result<LoanDto>.Validation($"לא ניתן להחזיק יותר מ-{MaxActiveLoansPerMember} השאלות פעילות");

        var now = _time.GetUtcNow().UtcDateTime;
        copy.CheckOut();
        var loan = new Loan
        {
            ItemCopyId = copy.Id,
            MemberId = memberId,
            LoanedAt = now,
            DueAt = now.AddDays(copy.Item.MaxLoanDays),
            Status = LoanStatus.Active
        };
        _loans.Add(loan);

        try
        {
            // UPDATE of the copy (WHERE Version = old) and INSERT of the loan, in one transaction.
            await _unitOfWork.SaveChangesAsync(ct);
        }
        catch (ConcurrencyConflictException)
        {
            _logger.LogWarning(
                "Concurrency conflict: copy {CopyId} was taken by another user. Member {MemberId} was rejected",
                copy.Id, memberId);

            return Result<LoanDto>.Conflict("העותק נתפס על ידי משתמש אחר. נא לנסות שוב.");
        }

        _logger.LogInformation("Loan {LoanId} created for copy {CopyId}", loan.Id, copy.Id);

        // Reloaded so the response includes the member's name.
        return await GetByIdAsync(loan.Id, ct);
    }

    public async Task<Result> ReturnAsync(int id, UpdateLoanRequest request, CancellationToken ct)
    {
        if (request.Returned != true)
            return Result.Validation("לא ניתן לבטל החזרה");

        // Tracked, with ItemCopy: the return is protected by the same concurrency token.
        var loan = await _loans.GetByIdForUpdateAsync(id, ct);
        if (loan is null)
            return Result.NotFound(NotFoundMessage);

        // No ownership check: the endpoint is Admin only.
        if (loan.Status == LoanStatus.Returned)
            return Result.Conflict("ההשאלה כבר הוחזרה");

        loan.ItemCopy.CheckIn();
        loan.ReturnedAt = _time.GetUtcNow().UtcDateTime;
        loan.Status = LoanStatus.Returned;

        try
        {
            await _unitOfWork.SaveChangesAsync(ct);
        }
        catch (ConcurrencyConflictException)
        {
            _logger.LogWarning(
                "Concurrency conflict: loan {LoanId} on copy {CopyId} was changed by another request. Admin {AdminId} was rejected",
                loan.Id, loan.ItemCopyId, _currentUser.Id);

            return Result.Conflict("ההשאלה עודכנה על ידי משתמש אחר. נא לנסות שוב.");
        }

        _logger.LogInformation("Loan {LoanId} returned", loan.Id);

        return Result.Success();
    }

    public async Task<PagedResult<LoanDto>> GetPagedAsync(LoanQueryParameters query, CancellationToken ct)
    {
        var page = await _loans.GetPagedAsync(query, _time.GetUtcNow().UtcDateTime, ct);
        return new PagedResult<LoanDto>(
            _mapper.Map<IReadOnlyList<LoanDto>>(page.Items), page.Page, page.PageSize, page.TotalCount);
    }

    public async Task<Result<LoanDto>> GetByIdAsync(int id, CancellationToken ct)
    {
        var loan = await _loans.GetByIdAsync(id, ct);
        if (loan is null)
            return Result<LoanDto>.NotFound(NotFoundMessage);

        if (!CanAccess(loan))
            return Result<LoanDto>.Forbidden(ForbiddenMessage);

        return Result<LoanDto>.Success(_mapper.Map<LoanDto>(loan));
    }

    public async Task<Result<IReadOnlyList<LoanDto>>> GetMineAsync(CancellationToken ct)
    {
        if (_currentUser.Id is not int memberId)
            return Result<IReadOnlyList<LoanDto>>.Unauthorized(UnauthorizedMessage);

        var loans = await _loans.GetByMemberAsync(memberId, ct);
        return Result<IReadOnlyList<LoanDto>>.Success(_mapper.Map<IReadOnlyList<LoanDto>>(loans));
    }

    // Resource-based check: the role policy cannot know who owns the loan.
    private bool CanAccess(Loan loan)
    {
        if (loan.MemberId == _currentUser.Id || _currentUser.IsAdmin)
            return true;

        _logger.LogWarning("Member {MemberId} tried to access loan {LoanId} of another member", _currentUser.Id, loan.Id);
        return false;
    }
}
