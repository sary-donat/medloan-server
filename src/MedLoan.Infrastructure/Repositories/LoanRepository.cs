using MedLoan.Application.Common.Interfaces;
using MedLoan.Domain.Entities;
using MedLoan.Infrastructure.Persistence;

namespace MedLoan.Infrastructure.Repositories;

public class LoanRepository(AppDbContext db) : ILoanRepository
{
    private readonly AppDbContext _db = db;

    public void Add(Loan loan) => _db.Loans.Add(loan);
}
