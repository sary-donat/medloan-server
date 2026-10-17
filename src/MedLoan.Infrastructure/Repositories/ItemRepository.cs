using MedLoan.Application.Common.Interfaces;
using MedLoan.Application.Common.Models;
using MedLoan.Application.Items;
using MedLoan.Domain.Entities;
using MedLoan.Domain.Enums;
using MedLoan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MedLoan.Infrastructure.Repositories;

public class ItemRepository(AppDbContext db) : IItemRepository
{
    private readonly AppDbContext _db = db;

    public async Task<PagedResult<ItemSummaryDto>> GetPagedAsync(ItemQueryParameters query, CancellationToken ct)
    {
        var items = _db.Items.AsNoTracking();

        if (query.CategoryId is int categoryId)
            items = items.Where(i => i.CategoryId == categoryId);

        if (query.TagId is int tagId)
            items = items.Where(i => i.Tags.Any(t => t.Id == tagId));

        if (query.AvailableOnly)
            items = items.Where(i => i.Copies.Any(c => c.Status == CopyStatus.Available));

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{EscapeLikePattern(query.Search.Trim())}%";
            items = items.Where(i => EF.Functions.ILike(i.Name, pattern, "\\"));
        }

        var totalCount = await items.CountAsync(ct);

        // Computed in long: (page - 1) * PageSize overflows int for very large pages. Clamping keeps the
        // offset past any real row, so the page is simply empty.
        var skip = (int)Math.Min((long)(query.Page - 1) * PagingDefaults.PageSize, int.MaxValue);

        var page = await ApplySort(items, query.SortBy, query.SortDir)
            .Skip(skip)
            .Take(PagingDefaults.PageSize)
            .Select(i => new ItemSummaryDto(
                i.Id,
                i.Name,
                i.Manufacturer,
                i.CategoryId,
                i.Category.Name,
                i.RequiresDeposit,
                i.Copies.Count(c => c.Status == CopyStatus.Available)))
            .ToListAsync(ct);

        return new PagedResult<ItemSummaryDto>(page, query.Page, PagingDefaults.PageSize, totalCount);
    }

    public Task<Item?> GetDetailsAsync(int id, CancellationToken ct) =>
        _db.Items
            .AsNoTracking()
            .Include(i => i.Category)
            .Include(i => i.Tags)
            .Include(i => i.Copies)
            .AsSplitQuery()
            .FirstOrDefaultAsync(i => i.Id == id, ct);

    // Includes Tags: PUT replaces the item's tag list.
    public Task<Item?> GetByIdForUpdateAsync(int id, CancellationToken ct) =>
        _db.Items.Include(i => i.Tags).FirstOrDefaultAsync(i => i.Id == id, ct);

    public Task<bool> ExistsAsync(int id, CancellationToken ct) =>
        _db.Items.AnyAsync(i => i.Id == id, ct);

    public Task<bool> ExistsInCategoryAsync(int categoryId, CancellationToken ct) =>
        _db.Items.AnyAsync(i => i.CategoryId == categoryId, ct);

    public void Add(Item item) => _db.Items.Add(item);

    public void Remove(Item item) => _db.Items.Remove(item);

    // ThenBy(Id) keeps the order stable between pages when the sort key has ties.
    private static IOrderedQueryable<Item> ApplySort(IQueryable<Item> items, ItemSortBy sortBy, SortDirection sortDir)
    {
        var desc = sortDir == SortDirection.Desc;

        var ordered = sortBy switch
        {
            ItemSortBy.Name => desc
                ? items.OrderByDescending(i => i.Name)
                : items.OrderBy(i => i.Name),
            ItemSortBy.CreatedAt => desc
                ? items.OrderByDescending(i => i.CreatedAt)
                : items.OrderBy(i => i.CreatedAt),
            ItemSortBy.AvailableCopies => desc
                ? items.OrderByDescending(i => i.Copies.Count(c => c.Status == CopyStatus.Available))
                : items.OrderBy(i => i.Copies.Count(c => c.Status == CopyStatus.Available)),
            _ => throw new ArgumentOutOfRangeException(nameof(sortBy), sortBy, null)
        };

        return ordered.ThenBy(i => i.Id);
    }

    // The backslash is the escape character passed to ILIKE, so it is escaped first.
    private static string EscapeLikePattern(string value) =>
        value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}
