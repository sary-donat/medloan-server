namespace MedLoan.Application.Tests;

public class MappingTests
{
    // Catches a DTO member that no map fills, before any endpoint is called.
    [Fact]
    public void AutoMapperConfiguration_IsValid()
    {
        TestMapper.Instance.ConfigurationProvider.AssertConfigurationIsValid();
    }
}
