using System.Text;
using MedLoan.Application.Common.Interfaces;
using MedLoan.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace MedLoan.Infrastructure.Auth;

public class JwtTokenService(IOptions<JwtOptions> options, TimeProvider time) : ITokenService
{
    private readonly JwtOptions _options = options.Value;
    private readonly TimeProvider _time = time;

    // The token holds only what identification and authorization need: it is base64, anyone can read it.
    public string CreateToken(Member member, out DateTime expiresAt)
    {
        var now = _time.GetUtcNow().UtcDateTime;
        expiresAt = now.AddMinutes(_options.ExpiryMinutes);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            IssuedAt = now,                 // iat and nbf from the same TimeProvider: otherwise they come from the real clock,
            NotBefore = now,                // and with FakeTimeProvider Expires could end up before NotBefore
            Expires = expiresAt,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key)), SecurityAlgorithms.HmacSha256),
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub]   = member.Id.ToString(),
                [JwtRegisteredClaimNames.Email] = member.Email,
                [JwtRegisteredClaimNames.Name]  = member.FullName,
                ["role"]                        = member.Role.ToString(),
                [JwtRegisteredClaimNames.Jti]   = Guid.NewGuid().ToString()
            }
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}
