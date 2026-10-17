using MedLoan.Application.Common.Exceptions;
using MedLoan.Application.Common.Interfaces;
using MedLoan.Application.Common.Models;
using MedLoan.Domain.Entities;
using MedLoan.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace MedLoan.Application.Auth;

public class AuthService(
    IMemberRepository members,
    IPasswordHasher hasher,
    ITokenService tokenService,
    IUnitOfWork unitOfWork,
    TimeProvider time,
    ILogger<AuthService> logger) : IAuthService
{
    private const string DuplicateEmailMessage = "כתובת האימייל כבר רשומה";
    private const string InvalidCredentialsMessage = "אימייל או סיסמה שגויים";

    // A real BCrypt hash (work factor 12) of a random value. Verified when the email is unknown,
    // so the response time does not reveal whether the member exists.
    private const string DummyHash = "$2a$12$rMYW45TDWo0t5BpYMCIj/elKVhC0c4Y570cPZJK4rrLa6TgOEHzf6";

    private readonly IMemberRepository _members = members;
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

        var member = new Member
        {
            FullName = request.FullName,
            Email = email,
            Phone = request.Phone,
            PasswordHash = _hasher.Hash(request.Password),
            Role = UserRole.Member,
            IsActive = true,
            CreatedAt = _time.GetUtcNow().UtcDateTime
        };
        _members.Add(member);

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

        return Result<AuthResponse>.Success(CreateResponse(member));
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

        var response = CreateResponse(member);
        _logger.LogInformation("Member {MemberId} logged in", member.Id);

        return Result<AuthResponse>.Success(response);
    }

    private AuthResponse CreateResponse(Member member)
    {
        var token = _tokenService.CreateToken(member, out var expiresAt);
        return new AuthResponse(token, expiresAt, member.Id, member.FullName, member.Role);
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
