using MedLoan.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace MedLoan.Infrastructure.Tests;

// One in-memory database per test class instance. The connection stays open: an in-memory SQLite database
// lives only as long as its connection, and every context created here shares it.
public sealed class SqliteTestDatabase : IAsyncLifetime
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private DbContextOptions<AppDbContext> _options = null!;

    public async ValueTask InitializeAsync()
    {
        await _connection.OpenAsync(TestContext.Current.CancellationToken);
        _options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;

        // Also inserts the HasData seed, so tests must not create entities with fixed ids.
        await using var db = CreateContext();
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
    }

    public AppDbContext CreateContext() => new(_options);

    public async ValueTask DisposeAsync() => await _connection.DisposeAsync();
}
