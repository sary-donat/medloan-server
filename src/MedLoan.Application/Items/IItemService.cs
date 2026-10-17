using MedLoan.Application.Common.Models;

namespace MedLoan.Application.Items;

public interface IItemService
{
    Task<PagedResult<ItemSummaryDto>> GetPagedAsync(ItemQueryParameters query, CancellationToken ct);
    Task<Result<ItemDetailsDto>> GetByIdAsync(int id, CancellationToken ct);
    Task<Result<ItemDetailsDto>> CreateAsync(CreateItemRequest request, CancellationToken ct);
    Task<Result> UpdateAsync(int id, UpdateItemRequest request, CancellationToken ct);
    Task<Result> DeleteAsync(int id, CancellationToken ct);
}
