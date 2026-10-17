using AutoMapper;
using MedLoan.Application.Common.Interfaces;
using MedLoan.Application.Common.Models;
using MedLoan.Domain.Entities;

namespace MedLoan.Application.Items;

public class ItemService(
    IItemRepository items,
    ICategoryRepository categories,
    ILoanRepository loans,
    IUnitOfWork unitOfWork,
    IMapper mapper,
    TimeProvider time) : IItemService
{
    private const string NotFoundMessage = "הפריט לא נמצא";
    private const string CategoryNotFoundMessage = "הקטגוריה לא קיימת";

    private readonly IItemRepository _items = items;
    private readonly ICategoryRepository _categories = categories;
    private readonly ILoanRepository _loans = loans;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IMapper _mapper = mapper;
    private readonly TimeProvider _time = time;

    public Task<PagedResult<ItemSummaryDto>> GetPagedAsync(ItemQueryParameters query, CancellationToken ct) =>
        _items.GetPagedAsync(query, ct);

    public async Task<Result<ItemDetailsDto>> GetByIdAsync(int id, CancellationToken ct)
    {
        var item = await _items.GetDetailsAsync(id, ct);
        return item is null
            ? Result<ItemDetailsDto>.NotFound(NotFoundMessage)
            : Result<ItemDetailsDto>.Success(_mapper.Map<ItemDetailsDto>(item));
    }

    public async Task<Result<ItemDetailsDto>> CreateAsync(CreateItemRequest request, CancellationToken ct)
    {
        if (!await _categories.ExistsAsync(request.CategoryId, ct))
            return Result<ItemDetailsDto>.Validation(CategoryNotFoundMessage);

        var item = _mapper.Map<Item>(request);
        item.CreatedAt = _time.GetUtcNow().UtcDateTime;
        _items.Add(item);

        await _unitOfWork.SaveChangesAsync(ct);

        // Reloaded so the response includes CategoryName.
        return await GetByIdAsync(item.Id, ct);
    }

    public async Task<Result> UpdateAsync(int id, UpdateItemRequest request, CancellationToken ct)
    {
        var item = await _items.GetByIdForUpdateAsync(id, ct);
        if (item is null)
            return Result.NotFound(NotFoundMessage);

        if (!await _categories.ExistsAsync(request.CategoryId, ct))
            return Result.Validation(CategoryNotFoundMessage);

        _mapper.Map(request, item);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct)
    {
        var item = await _items.GetByIdForUpdateAsync(id, ct);
        if (item is null)
            return Result.NotFound(NotFoundMessage);

        // Loan history is kept, so an item that was ever loaned is not deleted.
        if (await _loans.AnyLoansForItemAsync(id, ct))
            return Result.Conflict("לא ניתן למחוק פריט שיש לו היסטוריית השאלות. ניתן להוציא את העותקים משימוש.");

        // The item's copies are deleted by the database (cascade).
        _items.Remove(item);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
