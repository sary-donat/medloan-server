using System.ComponentModel.DataAnnotations;

namespace MedLoan.Application.Auth;

// The body of both POST /api/auth/refresh and POST /api/auth/logout.
public sealed record RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; init; } = string.Empty;
}
