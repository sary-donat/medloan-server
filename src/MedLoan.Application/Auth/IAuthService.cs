using MedLoan.Application.Common.Models;

namespace MedLoan.Application.Auth;

public interface IAuthService
{
    Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct);
    Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct);
    Task<Result<AuthResponse>> RefreshAsync(RefreshTokenRequest request, CancellationToken ct);
    Task<Result> LogoutAsync(RefreshTokenRequest request, CancellationToken ct);
}
