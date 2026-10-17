using MedLoan.Domain.Entities;
using MedLoan.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;

namespace MedLoan.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<ItemCopy> ItemCopies => Set<ItemCopy>();
    public DbSet<Member> Members => Set<Member>();
    public DbSet<Loan> Loans => Set<Loan>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        SeedData.Apply(modelBuilder);
    }

    // SaveChangesAsync(CancellationToken) delegates to this overload, so both paths renew the token.
    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<ItemCopy>())
        {
            if (entry.State == EntityState.Modified)
                entry.Entity.Version = Guid.NewGuid();
        }

        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }
}
