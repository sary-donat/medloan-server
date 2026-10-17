using AutoMapper;
using MedLoan.Application.Common.Exceptions;
using MedLoan.Application.Common.Interfaces;
using MedLoan.Application.Common.Models;
using MedLoan.Domain.Entities;

namespace MedLoan.Application.Categories;

public class CategoryService(
    ICategoryRepository categories,
    IItemRepository items,
    IUnitOfWork unitOfWork,
    IMapper mapper) : ICategoryService
{
    private const string NotFoundMessage = "הקטגוריה לא נמצאה";
    private const string DuplicateNameMessage = "קיימת כבר קטגוריה בשם זה";

    private readonly ICategoryRepository _categories = categories;
    private readonly IItemRepository _items = items;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IMapper _mapper = mapper;

    public async Task<IReadOnlyList<CategoryDto>> GetAllAsync(CancellationToken ct)
    {
        var categories = await _categories.GetAllAsync(ct);
        return _mapper.Map<IReadOnlyList<CategoryDto>>(categories);
    }

    public async Task<Result<CategoryDto>> GetByIdAsync(int id, CancellationToken ct)
    {
        var category = await _categories.GetByIdAsync(id, ct);
        return category is null
            ? Result<CategoryDto>.NotFound(NotFoundMessage)
            : Result<CategoryDto>.Success(_mapper.Map<CategoryDto>(category));
    }

    public async Task<Result<CategoryDto>> CreateAsync(CreateCategoryRequest request, CancellationToken ct)
    {
        if (await _categories.NameExistsAsync(request.Name, null, ct))
            return Result<CategoryDto>.Conflict(DuplicateNameMessage);

        var category = _mapper.Map<Category>(request);
        _categories.Add(category);

        try
        {
            await _unitOfWork.SaveChangesAsync(ct);
        }
        catch (DuplicateValueException)
        {
            // Another request created the same name between the check and the save.
            return Result<CategoryDto>.Conflict(DuplicateNameMessage);
        }

        return Result<CategoryDto>.Success(_mapper.Map<CategoryDto>(category));
    }

    public async Task<Result> UpdateAsync(int id, UpdateCategoryRequest request, CancellationToken ct)
    {
        var category = await _categories.GetByIdForUpdateAsync(id, ct);
        if (category is null)
            return Result.NotFound(NotFoundMessage);

        // excludeId: keeping the category's own name is not a duplicate.
        if (await _categories.NameExistsAsync(request.Name, id, ct))
            return Result.Conflict(DuplicateNameMessage);

        _mapper.Map(request, category);

        try
        {
            await _unitOfWork.SaveChangesAsync(ct);
        }
        catch (DuplicateValueException)
        {
            return Result.Conflict(DuplicateNameMessage);
        }

        return Result.Success();
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct)
    {
        var category = await _categories.GetByIdForUpdateAsync(id, ct);
        if (category is null)
            return Result.NotFound(NotFoundMessage);

        if (await _items.ExistsInCategoryAsync(id, ct))
            return Result.Conflict("לא ניתן למחוק קטגוריה שיש בה פריטים");

        _categories.Remove(category);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
