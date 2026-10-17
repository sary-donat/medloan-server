using MedLoan.Application.Common.Exceptions;
using MedLoan.Domain.Entities;
using MedLoan.Domain.Enums;
using MedLoan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MedLoan.Infrastructure.Tests;

// Two separate DbContexts read the same copy before either saves: the race that a manual test from one
// machine almost never produces. Only the Version token can stop the second save.
public class ItemCopyConcurrencyTests : IAsyncLifetime
{
    private readonly SqliteTestDatabase _database = new();
    private int _copyId;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        await _database.InitializeAsync();

        // A new copy without a fixed Id, on a seeded item, so it cannot collide with the seed.
        await using var setup = _database.CreateContext();
        var copy = new ItemCopy
        {
            ItemId = 1,
            SerialNumber = $"TEST-{Guid.NewGuid():N}",
            Status = CopyStatus.Available,
            Condition = CopyCondition.Good
        };
        setup.ItemCopies.Add(copy);
        await setup.SaveChangesAsync(Ct);
        _copyId = copy.Id;
    }

    public ValueTask DisposeAsync() => _database.DisposeAsync();

    [Fact]
    public async Task SecondSave_WithStaleVersion_Throws()
    {
        await using var ctx1 = _database.CreateContext();
        await using var ctx2 = _database.CreateContext();

        var copy1 = await ctx1.ItemCopies.FindAsync([_copyId], Ct);
        var copy2 = await ctx2.ItemCopies.FindAsync([_copyId], Ct);
        Assert.NotNull(copy1);
        Assert.NotNull(copy2);
        Assert.Equal(copy1.Version, copy2.Version);
        var originalVersion = copy1.Version;

        copy1.CheckOut();
        await ctx1.SaveChangesAsync(Ct);
        Assert.NotEqual(originalVersion, copy1.Version);

        copy2.CheckOut();
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => ctx2.SaveChangesAsync(Ct));
    }

    [Fact]
    public async Task UnitOfWork_WhenConcurrencyConflict_ThrowsConcurrencyConflictException()
    {
        await using var ctx1 = _database.CreateContext();
        await using var ctx2 = _database.CreateContext();
        var unitOfWork1 = new UnitOfWork(ctx1);
        var unitOfWork2 = new UnitOfWork(ctx2);

        var copy1 = await ctx1.ItemCopies.FindAsync([_copyId], Ct);
        var copy2 = await ctx2.ItemCopies.FindAsync([_copyId], Ct);
        Assert.NotNull(copy1);
        Assert.NotNull(copy2);

        copy1.CheckOut();
        await unitOfWork1.SaveChangesAsync(Ct);

        copy2.CheckOut();
        var ex = await Assert.ThrowsAsync<ConcurrencyConflictException>(() => unitOfWork2.SaveChangesAsync(Ct));
        Assert.IsType<DbUpdateConcurrencyException>(ex.InnerException);
    }
}
