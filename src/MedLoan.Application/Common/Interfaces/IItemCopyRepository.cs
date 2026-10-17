using MedLoan.Domain.Entities;

namespace MedLoan.Application.Common.Interfaces;

public interface IItemCopyRepository
{
    Task<ItemCopy?> GetByIdForUpdateAsync(int id, CancellationToken ct);
    Task<IReadOnlyList<ItemCopy>> GetByItemIdAsync(int itemId, CancellationToken ct);
    Task<bool> SerialNumberExistsAsync(string serialNumber, CancellationToken ct);
    void Add(ItemCopy copy);
    void Remove(ItemCopy copy);
}
