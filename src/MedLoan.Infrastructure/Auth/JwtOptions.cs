using System.ComponentModel.DataAnnotations;

namespace MedLoan.Infrastructure.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required] public string Issuer { get; init; } = default!;
    [Required] public string Audience { get; init; } = default!;
    [Required, MinLength(32)] public string Key { get; init; } = default!;   // HS256 requires at least 256 bits
    [Range(5, 1440)] public int ExpiryMinutes { get; init; } = 60;
}
