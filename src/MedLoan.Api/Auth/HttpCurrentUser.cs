using System.Security.Claims;
using MedLoan.Application.Common.Interfaces;
using MedLoan.Domain.Enums;
using Microsoft.IdentityModel.JsonWebTokens;

namespace MedLoan.Api.Auth;

public sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? User => accessor.HttpContext?.User;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;
    public int? Id => int.TryParse(User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var id) ? id : null;
    public bool IsAdmin => User?.IsInRole(nameof(UserRole.Admin)) == true;
}
