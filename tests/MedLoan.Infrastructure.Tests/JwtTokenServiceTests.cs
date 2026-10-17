using MedLoan.Domain.Entities;
using MedLoan.Domain.Enums;
using MedLoan.Infrastructure.Auth;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;

namespace MedLoan.Infrastructure.Tests;

public class JwtTokenServiceTests
{
    // A test-only key (32+ characters for HS256), not a real secret.
    private static readonly JwtOptions Options = new()
    {
        Issuer = "test-issuer",
        Audience = "test-audience",
        Key = "test-signing-key-that-is-at-least-32-characters",
        ExpiryMinutes = 60
    };

    [Fact]
    public void JwtTokenService_CreateToken_ContainsSubAndRole()
    {
        var service = new JwtTokenService(Microsoft.Extensions.Options.Options.Create(Options), TimeProvider.System);
        var member = new Member
        {
            Id = 42,
            FullName = "מנהל בדיקה",
            Email = "admin@test.local",
            PasswordHash = "must-not-appear",
            Phone = "050-0000000",
            Role = UserRole.Admin
        };

        var token = service.CreateToken(member, out var expiresAt);
        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(token);

        Assert.Equal("42", jwt.Subject);
        Assert.Equal("Admin", jwt.GetClaim("role").Value);
        Assert.Equal("test-issuer", jwt.Issuer);
        Assert.Contains("test-audience", jwt.Audiences);
        Assert.Equal(expiresAt, jwt.ValidTo, TimeSpan.FromSeconds(1));

        // The token is only base64: nothing sensitive may be in it.
        Assert.DoesNotContain(jwt.Claims, c => c.Value == member.PasswordHash || c.Value == member.Phone);
    }
}
