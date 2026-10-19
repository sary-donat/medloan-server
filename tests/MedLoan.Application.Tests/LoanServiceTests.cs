using MedLoan.Application.Common.Exceptions;
using MedLoan.Application.Common.Interfaces;
using MedLoan.Application.Common.Models;
using MedLoan.Application.Loans;
using MedLoan.Domain.Entities;
using MedLoan.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace MedLoan.Application.Tests;

public class LoanServiceTests
{
    private const int MemberId = 7;
    private const int OtherMemberId = 8;

    private readonly Mock<ILoanRepository> _loans = new();
    private readonly Mock<IItemCopyRepository> _copies = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 3, 1, 10, 0, 0, TimeSpan.Zero));
    private readonly LoanService _service;

    public LoanServiceTests()
    {
        LoggedInAs(MemberId, isAdmin: false);
        _service = new LoanService(
            _loans.Object, _copies.Object, _currentUser.Object, _unitOfWork.Object,
            TestMapper.Instance, _time, NullLogger<LoanService>.Instance);
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task CreateLoan_WhenCopyAvailable_ReturnsSuccessAndMarksLoaned()
    {
        var copy = NewCopy(CopyStatus.Available);
        _copies.Setup(r => r.GetByIdForUpdateAsync(copy.Id, It.IsAny<CancellationToken>())).ReturnsAsync(copy);
        _loans.Setup(r => r.CountActiveByMemberAsync(MemberId, It.IsAny<CancellationToken>())).ReturnsAsync(0);

        // The service reloads the saved loan for the response; return the one it added, with its navigations.
        Loan? added = null;
        _loans.Setup(r => r.Add(It.IsAny<Loan>())).Callback<Loan>(l =>
        {
            l.Id = 100;
            l.ItemCopy = copy;
            l.Member = new Member { Id = MemberId, FullName = "חבר בדיקה" };
            added = l;
        });
        _loans.Setup(r => r.GetByIdAsync(100, It.IsAny<CancellationToken>())).ReturnsAsync(() => added);

        var result = await _service.CreateAsync(new CreateLoanRequest { ItemCopyId = copy.Id }, Ct);

        Assert.True(result.IsSuccess);
        Assert.Equal(CopyStatus.Loaned, copy.Status);
        Assert.Equal(MemberId, result.Value!.MemberId);
        Assert.Equal(LoanStatus.Active, result.Value.Status);
        Assert.Equal(_time.GetUtcNow().UtcDateTime.AddDays(copy.Item.MaxLoanDays), result.Value.DueAt);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateLoan_WhenCopyAlreadyLoaned_ReturnsConflict()
    {
        var copy = NewCopy(CopyStatus.Loaned);
        _copies.Setup(r => r.GetByIdForUpdateAsync(copy.Id, It.IsAny<CancellationToken>())).ReturnsAsync(copy);

        var result = await _service.CreateAsync(new CreateLoanRequest { ItemCopyId = copy.Id }, Ct);

        Assert.Equal(ErrorType.Conflict, result.Error);
        _loans.Verify(r => r.Add(It.IsAny<Loan>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateLoan_WhenSaveThrowsConcurrencyConflict_ReturnsConflict()
    {
        var copy = NewCopy(CopyStatus.Available);
        _copies.Setup(r => r.GetByIdForUpdateAsync(copy.Id, It.IsAny<CancellationToken>())).ReturnsAsync(copy);
        _loans.Setup(r => r.CountActiveByMemberAsync(MemberId, It.IsAny<CancellationToken>())).ReturnsAsync(0);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new ConcurrencyConflictException());

        var result = await _service.CreateAsync(new CreateLoanRequest { ItemCopyId = copy.Id }, Ct);

        Assert.Equal(ErrorType.Conflict, result.Error);
        Assert.Equal("העותק נתפס על ידי משתמש אחר. נא לנסות שוב.", result.Message);
    }

    [Fact]
    public async Task CreateLoan_WhenCopyNotFound_ReturnsNotFound()
    {
        _copies.Setup(r => r.GetByIdForUpdateAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((ItemCopy?)null);

        var result = await _service.CreateAsync(new CreateLoanRequest { ItemCopyId = 999 }, Ct);

        Assert.Equal(ErrorType.NotFound, result.Error);
    }

    [Fact]
    public async Task CreateLoan_WhenMemberReachedLimit_ReturnsValidation()
    {
        var copy = NewCopy(CopyStatus.Available);
        _copies.Setup(r => r.GetByIdForUpdateAsync(copy.Id, It.IsAny<CancellationToken>())).ReturnsAsync(copy);
        _loans.Setup(r => r.CountActiveByMemberAsync(MemberId, It.IsAny<CancellationToken>())).ReturnsAsync(3);

        var result = await _service.CreateAsync(new CreateLoanRequest { ItemCopyId = copy.Id }, Ct);

        Assert.Equal(ErrorType.Validation, result.Error);
        Assert.Equal(CopyStatus.Available, copy.Status);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetLoan_ByOwner_Succeeds()
    {
        var loan = ActiveLoanOf(MemberId);

        var result = await _service.GetByIdAsync(loan.Id, Ct);

        Assert.True(result.IsSuccess);
        Assert.Equal(loan.Id, result.Value!.Id);
        Assert.Equal(MemberId, result.Value.MemberId);
    }

    [Fact]
    public async Task GetLoan_ByOtherMember_ReturnsForbidden()
    {
        var loan = ActiveLoanOf(OtherMemberId);

        var result = await _service.GetByIdAsync(loan.Id, Ct);

        Assert.Equal(ErrorType.Forbidden, result.Error);
        Assert.Null(result.Value);
    }

    // Ownership is not checked on return: the endpoint is Admin only, so the service is called by an admin.
    [Fact]
    public async Task ReturnLoan_ByAdmin_Succeeds()
    {
        LoggedInAs(MemberId, isAdmin: true);
        var loan = ActiveLoanOf(OtherMemberId);

        var result = await _service.ReturnAsync(loan.Id, Returned(), Ct);

        Assert.True(result.IsSuccess);
        AssertReturned(loan);
    }

    [Fact]
    public async Task ReturnLoan_WhenSaveThrowsConcurrencyConflict_ReturnsConflict()
    {
        var loan = ActiveLoanOf(MemberId);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new ConcurrencyConflictException());

        var result = await _service.ReturnAsync(loan.Id, Returned(), Ct);

        Assert.Equal(ErrorType.Conflict, result.Error);
    }

    [Fact]
    public async Task ReturnLoan_WhenAlreadyReturned_ReturnsConflict()
    {
        var loan = ActiveLoanOf(MemberId);
        loan.Status = LoanStatus.Returned;
        loan.ItemCopy.Status = CopyStatus.Available;

        var result = await _service.ReturnAsync(loan.Id, Returned(), Ct);

        Assert.Equal(ErrorType.Conflict, result.Error);
        Assert.Equal("ההשאלה כבר הוחזרה", result.Message);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private void LoggedInAs(int id, bool isAdmin)
    {
        _currentUser.Setup(u => u.Id).Returns(id);
        _currentUser.Setup(u => u.IsAdmin).Returns(isAdmin);
        _currentUser.Setup(u => u.IsAuthenticated).Returns(true);
    }

    private static ItemCopy NewCopy(CopyStatus status) => new()
    {
        Id = 1,
        ItemId = 10,
        Item = new Item { Id = 10, Name = "כיסא גלגלים", MaxLoanDays = 30 },
        SerialNumber = "TEST-001",
        Status = status
    };

    private Loan ActiveLoanOf(int memberId)
    {
        var loan = new Loan
        {
            Id = 50,
            ItemCopyId = 1,
            ItemCopy = NewCopy(CopyStatus.Loaned),
            MemberId = memberId,
            Member = new Member { Id = memberId, FullName = "חבר בדיקה" },
            LoanedAt = _time.GetUtcNow().UtcDateTime.AddDays(-5),
            DueAt = _time.GetUtcNow().UtcDateTime.AddDays(25),
            Status = LoanStatus.Active
        };
        _loans.Setup(r => r.GetByIdForUpdateAsync(loan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(loan);
        _loans.Setup(r => r.GetByIdAsync(loan.Id, It.IsAny<CancellationToken>())).ReturnsAsync(loan);
        return loan;
    }

    private static UpdateLoanRequest Returned() => new() { Returned = true };

    private void AssertReturned(Loan loan)
    {
        Assert.Equal(LoanStatus.Returned, loan.Status);
        Assert.Equal(_time.GetUtcNow().UtcDateTime, loan.ReturnedAt);
        Assert.Equal(CopyStatus.Available, loan.ItemCopy.Status);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
