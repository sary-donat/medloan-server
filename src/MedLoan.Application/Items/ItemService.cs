using AutoMapper;
using MedLoan.Application.Common.Interfaces;
using MedLoan.Application.Common.Models;
using MedLoan.Domain.Entities;

namespace MedLoan.Application.Items;

public class ItemService(
    IItemRepository items,
    ICategoryRepository categories,
    ITagRepository tags,
    ILoanRepository loans,
    IUnitOfWork unitOfWork,
    IMapper mapper,
    TimeProvider time) : IItemService
{
    private const string NotFoundMessage = "הפריט לא נמצא";
    private const string CategoryNotFoundMessage = "הקטגוריה לא קיימת";
    private const string TagNotFoundMessage = "תגית לא קיימת";

    private readonly IItemRepository _items = items;
    private readonly ICategoryRepository _categories = categories;
    private readonly ITagRepository _tags = tags;
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

        var tags = await LoadTagsAsync(request.TagIds, ct);
        if (tags is null)
            return Result<ItemDetailsDto>.Validation(TagNotFoundMessage);

        var item = _mapper.Map<Item>(request);
        item.Tags = tags;
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

        var tags = await LoadTagsAsync(request.TagIds, ct);
        if (tags is null)
            return Result.Validation(TagNotFoundMessage);

        _mapper.Map(request, item);

        // PUT replaces the whole item, tags included (the item was loaded with its tags).
        item.Tags.Clear();
        foreach (var tag in tags)
            item.Tags.Add(tag);

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

    // Null when an id does not exist. Missing or empty TagIds means no tags.
    private async Task<List<Tag>?> LoadTagsAsync(int[]? tagIds, CancellationToken ct)
    {
        var ids = (tagIds ?? []).Distinct().ToArray();
        if (ids.Length == 0)
            return [];

        var tags = await _tags.GetByIdsAsync(ids, ct);
        return tags.Count == ids.Length ? tags.ToList() : null;
    }
}
