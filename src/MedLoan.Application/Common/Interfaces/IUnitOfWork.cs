namespace MedLoan.Application.Common.Interfaces;

public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken ct);
}
