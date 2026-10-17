namespace MedLoan.Application.Common.Interfaces;

public interface ICategoryRepository
{
    Task<bool> NameExistsAsync(string name, CancellationToken ct);
}
