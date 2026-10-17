using MedLoan.Application.Common.Models;

namespace MedLoan.Application.Categories;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryDto>> GetAllAsync(CancellationToken ct);
    Task<Result<CategoryDto>> GetByIdAsync(int id, CancellationToken ct);
    Task<Result<CategoryDto>> CreateAsync(CreateCategoryRequest request, CancellationToken ct);
    Task<Result> UpdateAsync(int id, UpdateCategoryRequest request, CancellationToken ct);
    Task<Result> DeleteAsync(int id, CancellationToken ct);
}
