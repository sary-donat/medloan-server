using MedLoan.Application.Common.Models;
using MedLoan.Application.Items;
using MedLoan.Domain.Entities;

namespace MedLoan.Application.Common.Interfaces;

public interface IItemRepository
{
    Task<PagedResult<ItemSummaryDto>> GetPagedAsync(ItemQueryParameters query, CancellationToken ct);
    Task<Item?> GetDetailsAsync(int id, CancellationToken ct);
    Task<Item?> GetByIdForUpdateAsync(int id, CancellationToken ct);
    Task<bool> ExistsAsync(int id, CancellationToken ct);
    Task<bool> ExistsInCategoryAsync(int categoryId, CancellationToken ct);
    void Add(Item item);
    void Remove(Item item);
}
