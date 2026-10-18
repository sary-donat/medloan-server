using MedLoan.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedLoan.Api.Controllers;

[Route("api/auth")]
[AllowAnonymous]
public class AuthController(IAuthService auth) : ApiControllerBase
{
    private readonly IAuthService _auth = auth;

    // The new member is logged in right away; Location points to their profile.
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct) =>
        ToCreatedResult(await _auth.RegisterAsync(request, ct), "/api/members/me");

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct) =>
        ToActionResult(await _auth.LoginAsync(request, ct));

    // Anonymous: the access token may already be expired; the refresh token itself is the credential.
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken ct) =>
        ToActionResult(await _auth.RefreshAsync(request, ct));

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request, CancellationToken ct) =>
        ToActionResult(await _auth.LogoutAsync(request, ct));
}
