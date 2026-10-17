using AutoMapper;
using Microsoft.Extensions.DependencyInjection;

namespace MedLoan.Application.Tests;

// The real mapper, registered exactly as on the server (profiles and MaxDepth), not a hand-built MapperConfiguration.
public static class TestMapper
{
    private static readonly Lazy<IMapper> Mapper = new(() =>
        new ServiceCollection()
            .AddApplication()
            .BuildServiceProvider()
            .GetRequiredService<IMapper>());

    public static IMapper Instance => Mapper.Value;
}
