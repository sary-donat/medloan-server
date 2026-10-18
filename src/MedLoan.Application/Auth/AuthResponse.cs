using MedLoan.Domain.Enums;

namespace MedLoan.Application.Auth;

public sealed record AuthResponse(
    string Token,
    DateTime ExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt,
    int MemberId,
    string FullName,
    UserRole Role);
