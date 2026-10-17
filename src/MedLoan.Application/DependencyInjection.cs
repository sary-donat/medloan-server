using AutoMapper;
using AutoMapper.Internal;
using MedLoan.Application.Categories;
using MedLoan.Application.Copies;
using MedLoan.Application.Items;
using Microsoft.Extensions.DependencyInjection;

namespace MedLoan.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // MaxDepth on every map: mitigation for CVE-2026-32933 while staying on AutoMapper 14.0.0.
        services.AddAutoMapper(
            cfg => cfg.Internal().ForAllMaps((_, map) => map.MaxDepth(5)),
            typeof(DependencyInjection).Assembly);

        services.AddSingleton(TimeProvider.System);

        services.AddScoped<IItemService, ItemService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<ICopyService, CopyService>();

        return services;
    }
}
