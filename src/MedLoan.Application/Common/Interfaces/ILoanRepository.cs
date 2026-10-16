using MedLoan.Domain.Entities;

namespace MedLoan.Application.Common.Interfaces;

public interface ILoanRepository
{
    void Add(Loan loan);
}
