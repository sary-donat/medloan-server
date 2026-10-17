using MedLoan.Application.Common.Interfaces;
using MedLoan.Infrastructure.Auth;
using MedLoan.Infrastructure.Persistence;
using MedLoan.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MedLoan.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // An empty value (e.g. an env var set without a value) is treated as missing, so startup fails here
        // and not on the first DB request.
        var connectionString = configuration.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                "Connection string 'ConnectionStrings:Default' is missing. " +
                "Set it with dotnet user-secrets (development) or the ConnectionStrings__Default environment variable.");

        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IItemRepository, ItemRepository>();
        services.AddScoped<IItemCopyRepository, ItemCopyRepository>();
        services.AddScoped<ILoanRepository, LoanRepository>();
        services.AddScoped<IMemberRepository, MemberRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ITagRepository, TagRepository>();

        // A missing or short Jwt:Key fails at startup, with a clear message.
        services.AddOptions<JwtOptions>()
            .BindConfiguration(JwtOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();

        return services;
    }
}
