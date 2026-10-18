using MedLoan.Application.Common.Exceptions;
using MedLoan.Application.Common.Interfaces;
using MedLoan.Application.Common.Models;
using MedLoan.Domain.Entities;
using MedLoan.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace MedLoan.Application.Auth;

public class AuthService(
    IMemberRepository members,
    IRefreshTokenRepository refreshTokens,
    IPasswordHasher hasher,
    ITokenService tokenService,
    IUnitOfWork unitOfWork,
    TimeProvider time,
    ILogger<AuthService> logger) : IAuthService
{
    private const string DuplicateEmailMessage = "כתובת האימייל כבר רשומה";
    private const string InvalidCredentialsMessage = "אימייל או סיסמה שגויים";
    private const string SessionExpiredMessage = "ההתחברות פגה. נא להתחבר מחדש.";

    // A real BCrypt hash (work factor 12) of a random value. Verified when the email is unknown,
    // so the response time does not reveal whether the member exists.
    private const string DummyHash = "$2a$12$rMYW45TDWo0t5BpYMCIj/elKVhC0c4Y570cPZJK4rrLa6TgOEHzf6";

    private readonly IMemberRepository _members = members;
    private readonly IRefreshTokenRepository _refreshTokens = refreshTokens;
    private readonly IPasswordHasher _hasher = hasher;
    private readonly ITokenService _tokenService = tokenService;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly TimeProvider _time = time;
    private readonly ILogger<AuthService> _logger = logger;

    public async Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        var email = NormalizeEmail(request.Email);

        if (await _members.GetByEmailAsync(email, ct) is not null)
            return Result<AuthResponse>.Conflict(DuplicateEmailMessage);

        var now = _time.GetUtcNow().UtcDateTime;
        var member = new Member
        {
            FullName = request.FullName,
            Email = email,
            Phone = request.Phone,
            PasswordHash = _hasher.Hash(request.Password),
            Role = UserRole.Member,
            IsActive = true,
            CreatedAt = now
        };
        _members.Add(member);
        // Saved together with the member, in the same SaveChangesAsync.
        var refresh = IssueRefreshToken(member, now);

        try
        {
            await _unitOfWork.SaveChangesAsync(ct);
        }
        catch (DuplicateValueException)
        {
            // Another registration with the same email was saved between the check and the save.
            return Result<AuthResponse>.Conflict(DuplicateEmailMessage);
        }

        _logger.LogInformation("Member {MemberId} registered", member.Id);

        return Result<AuthResponse>.Success(CreateResponse(member, refresh));
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var member = await _members.GetByEmailAsync(NormalizeEmail(request.Email), ct);

        // Verify always runs, so an unknown email takes as long as a wrong password.
        var passwordValid = _hasher.Verify(request.Password, member?.PasswordHash ?? DummyHash);

        if (member is null || !passwordValid || !member.IsActive)
        {
            _logger.LogWarning("Failed login attempt for {Email}", request.Email);
            return Result<AuthResponse>.Unauthorized(InvalidCredentialsMessage);
        }

        var refresh = IssueRefreshToken(member, _time.GetUtcNow().UtcDateTime);
        await _unitOfWork.SaveChangesAsync(ct);

        var response = CreateResponse(member, refresh);
        _logger.LogInformation("Member {MemberId} logged in", member.Id);

        return Result<AuthResponse>.Success(response);
    }

    public async Task<Result<AuthResponse>> RefreshAsync(RefreshTokenRequest request, CancellationToken ct)
    {
        var now = _time.GetUtcNow().UtcDateTime;
        var stored = await _refreshTokens.GetByHashAsync(_tokenService.HashRefreshToken(request.RefreshToken), ct);

        if (stored is null || stored.ExpiresAt <= now)
            return Result<AuthResponse>.Unauthorized(SessionExpiredMessage);

        // A revoked token that comes back was copied: whoever holds the newer token may be an attacker,
        // so every active token of the member is revoked and everyone logs in again.
        if (stored.RevokedAt is not null)
        {
            var active = await _refreshTokens.GetActiveByMemberAsync(stored.MemberId, now, ct);
            foreach (var token in active)
                token.RevokedAt = now;
            await _unitOfWork.SaveChangesAsync(ct);

            _logger.LogWarning("Refresh token reuse for member {MemberId}. {RevokedCount} active tokens revoked",
                stored.MemberId, active.Count);
            return Result<AuthResponse>.Unauthorized(SessionExpiredMessage);
        }

        if (!stored.Member.IsActive)
            return Result<AuthResponse>.Unauthorized(SessionExpiredMessage);

        // Rotation: the old token is used up, and a new pair is issued.
        stored.RevokedAt = now;
        var refresh = IssueRefreshToken(stored.Member, now);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Member {MemberId} refreshed the session", stored.MemberId);

        return Result<AuthResponse>.Success(CreateResponse(stored.Member, refresh));
    }

    // Always succeeds (204): the response does not reveal whether the token existed.
    public async Task<Result> LogoutAsync(RefreshTokenRequest request, CancellationToken ct)
    {
        var now = _time.GetUtcNow().UtcDateTime;
        var stored = await _refreshTokens.GetByHashAsync(_tokenService.HashRefreshToken(request.RefreshToken), ct);

        if (stored is not null && stored.IsActive(now))
        {
            stored.RevokedAt = now;
            await _unitOfWork.SaveChangesAsync(ct);
        }

        return Result.Success();
    }

    // Only the hash goes to the DB. A new member (Id 0, before save) is linked through the navigation,
    // an existing one by MemberId: login reads the member untracked, and the navigation would insert it again.
    private (string Token, DateTime ExpiresAt) IssueRefreshToken(Member member, DateTime now)
    {
        var token = _tokenService.CreateRefreshToken(out var expiresAt);
        var entity = new RefreshToken
        {
            TokenHash = _tokenService.HashRefreshToken(token),
            CreatedAt = now,
            ExpiresAt = expiresAt
        };
        if (member.Id == 0)
            entity.Member = member;
        else
            entity.MemberId = member.Id;

        _refreshTokens.Add(entity);
        return (token, expiresAt);
    }

    private AuthResponse CreateResponse(Member member, (string Token, DateTime ExpiresAt) refresh)
    {
        var token = _tokenService.CreateToken(member, out var expiresAt);
        return new AuthResponse(token, expiresAt, refresh.Token, refresh.ExpiresAt, member.Id, member.FullName, member.Role);
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
