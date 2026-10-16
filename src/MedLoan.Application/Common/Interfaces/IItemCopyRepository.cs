using MedLoan.Domain.Entities;

namespace MedLoan.Application.Common.Interfaces;

public interface IItemCopyRepository
{
    Task<ItemCopy?> GetByIdForUpdateAsync(int id, CancellationToken ct);
}
