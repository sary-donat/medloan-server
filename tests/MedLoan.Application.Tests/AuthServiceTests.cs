using MedLoan.Application.Auth;
using MedLoan.Application.Common.Interfaces;
using MedLoan.Application.Common.Models;
using MedLoan.Domain.Entities;
using MedLoan.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace MedLoan.Application.Tests;

public class AuthServiceTests
{
    private const string Password = "Password123!";
    private const string StoredHash = "stored-hash";

    private const string NewRefreshToken = "new-refresh-token";
    private const string OldRefreshToken = "old-refresh-token";

    private static readonly DateTime RefreshExpiresAt = new(2026, 3, 8, 10, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IMemberRepository> _members = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokens = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<ITokenService> _tokenService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 3, 1, 10, 0, 0, TimeSpan.Zero));
    private readonly AuthService _service;

    public AuthServiceTests()
    {
        var expiresAt = new DateTime(2026, 3, 1, 11, 0, 0, DateTimeKind.Utc);
        _tokenService.Setup(t => t.CreateToken(It.IsAny<Member>(), out expiresAt)).Returns("test-token");
        var refreshExpiresAt = RefreshExpiresAt;
        _tokenService.Setup(t => t.CreateRefreshToken(out refreshExpiresAt)).Returns(NewRefreshToken);
        _tokenService.Setup(t => t.HashRefreshToken(It.IsAny<string>())).Returns<string>(t => "hash:" + t);

        _service = new AuthService(
            _members.Object, _refreshTokens.Object, _hasher.Object, _tokenService.Object, _unitOfWork.Object,
            _time, NullLogger<AuthService>.Instance);
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Register_WithNewEmail_CreatesMemberWithMemberRole()
    {
        _members.Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((Member?)null);
        _hasher.Setup(h => h.Hash(Password)).Returns(StoredHash);
        Member? added = null;
        _members.Setup(r => r.Add(It.IsAny<Member>())).Callback<Member>(m => added = m);

        var result = await _service.RegisterAsync(NewRegisterRequest("  New.Member@Example.com "), Ct);

        Assert.True(result.IsSuccess);
        Assert.NotNull(added);
        Assert.Equal(UserRole.Member, added.Role);
        Assert.True(added.IsActive);
        Assert.Equal("new.member@example.com", added.Email);
        Assert.Equal(StoredHash, added.PasswordHash);
        Assert.Equal(_time.GetUtcNow().UtcDateTime, added.CreatedAt);
        Assert.Equal("test-token", result.Value!.Token);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Register_WhenEmailExists_ReturnsConflict()
    {
        _members.Setup(r => r.GetByEmailAsync("member1@medloan.local", It.IsAny<CancellationToken>()))
            .ReturnsAsync(NewMember(isActive: true));

        var result = await _service.RegisterAsync(NewRegisterRequest("Member1@MedLoan.local"), Ct);

        Assert.Equal(ErrorType.Conflict, result.Error);
        _members.Verify(r => r.Add(It.IsAny<Member>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        _members.Setup(r => r.GetByEmailAsync("member1@medloan.local", It.IsAny<CancellationToken>()))
            .ReturnsAsync(NewMember(isActive: true));
        _hasher.Setup(h => h.Verify("WrongPassword!", StoredHash)).Returns(false);

        var result = await _service.LoginAsync(new LoginRequest { Email = "member1@medloan.local", Password = "WrongPassword!" }, Ct);

        Assert.Equal(ErrorType.Unauthorized, result.Error);
        Assert.Equal("אימייל או סיסמה שגויים", result.Message);
        _tokenService.Verify(t => t.CreateToken(It.IsAny<Member>(), out It.Ref<DateTime>.IsAny), Times.Never);
    }

    [Fact]
    public async Task Login_WhenMemberInactive_ReturnsUnauthorized()
    {
        _members.Setup(r => r.GetByEmailAsync("member1@medloan.local", It.IsAny<CancellationToken>()))
            .ReturnsAsync(NewMember(isActive: false));
        _hasher.Setup(h => h.Verify(Password, StoredHash)).Returns(true);

        var result = await _service.LoginAsync(new LoginRequest { Email = "member1@medloan.local", Password = Password }, Ct);

        // The same message as a wrong password: the response does not reveal that the account exists.
        Assert.Equal(ErrorType.Unauthorized, result.Error);
        Assert.Equal("אימייל או סיסמה שגויים", result.Message);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        var member = NewMember(isActive: true);
        _members.Setup(r => r.GetByEmailAsync("member1@medloan.local", It.IsAny<CancellationToken>())).ReturnsAsync(member);
        _hasher.Setup(h => h.Verify(Password, StoredHash)).Returns(true);

        var result = await _service.LoginAsync(new LoginRequest { Email = "member1@medloan.local", Password = Password }, Ct);

        Assert.True(result.IsSuccess);
        Assert.Equal("test-token", result.Value!.Token);
        Assert.Equal(new DateTime(2026, 3, 1, 11, 0, 0, DateTimeKind.Utc), result.Value.ExpiresAt);
        Assert.Equal(member.Id, result.Value.MemberId);
        Assert.Equal(UserRole.Member, result.Value.Role);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsRefreshToken()
    {
        var member = NewMember(isActive: true);
        _members.Setup(r => r.GetByEmailAsync("member1@medloan.local", It.IsAny<CancellationToken>())).ReturnsAsync(member);
        _hasher.Setup(h => h.Verify(Password, StoredHash)).Returns(true);
        RefreshToken? added = null;
        _refreshTokens.Setup(r => r.Add(It.IsAny<RefreshToken>())).Callback<RefreshToken>(t => added = t);

        var result = await _service.LoginAsync(new LoginRequest { Email = "member1@medloan.local", Password = Password }, Ct);

        Assert.True(result.IsSuccess);
        Assert.Equal(NewRefreshToken, result.Value!.RefreshToken);
        Assert.Equal(RefreshExpiresAt, result.Value.RefreshTokenExpiresAt);
        // Only the hash is stored, linked by MemberId (the member was read untracked).
        Assert.NotNull(added);
        Assert.Equal("hash:" + NewRefreshToken, added.TokenHash);
        Assert.Equal(member.Id, added.MemberId);
        Assert.Null(added.Member);
        Assert.Equal(RefreshExpiresAt, added.ExpiresAt);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Refresh_WithValidToken_ReturnsNewTokensAndRevokesOld()
    {
        var old = NewStoredToken(NewMember(isActive: true));
        SetupStored(old);
        RefreshToken? added = null;
        _refreshTokens.Setup(r => r.Add(It.IsAny<RefreshToken>())).Callback<RefreshToken>(t => added = t);

        var result = await _service.RefreshAsync(new RefreshTokenRequest { RefreshToken = OldRefreshToken }, Ct);

        Assert.True(result.IsSuccess);
        Assert.Equal("test-token", result.Value!.Token);
        Assert.Equal(NewRefreshToken, result.Value.RefreshToken);
        Assert.Equal(_time.GetUtcNow().UtcDateTime, old.RevokedAt);
        Assert.NotNull(added);
        Assert.Equal("hash:" + NewRefreshToken, added.TokenHash);
        Assert.Equal(old.MemberId, added.MemberId);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Refresh_WithRevokedToken_RevokesAllAndReturnsUnauthorized()
    {
        var member = NewMember(isActive: true);
        var old = NewStoredToken(member);
        old.RevokedAt = _time.GetUtcNow().UtcDateTime.AddMinutes(-5);
        SetupStored(old);
        var otherActive = new List<RefreshToken> { NewStoredToken(member), NewStoredToken(member) };
        _refreshTokens.Setup(r => r.GetActiveByMemberAsync(member.Id, _time.GetUtcNow().UtcDateTime, It.IsAny<CancellationToken>()))
            .ReturnsAsync(otherActive);

        var result = await _service.RefreshAsync(new RefreshTokenRequest { RefreshToken = OldRefreshToken }, Ct);

        Assert.Equal(ErrorType.Unauthorized, result.Error);
        Assert.All(otherActive, t => Assert.Equal(_time.GetUtcNow().UtcDateTime, t.RevokedAt));
        _refreshTokens.Verify(r => r.Add(It.IsAny<RefreshToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Refresh_WithExpiredToken_ReturnsUnauthorized()
    {
        var old = NewStoredToken(NewMember(isActive: true));
        old.ExpiresAt = _time.GetUtcNow().UtcDateTime.AddSeconds(-1);
        SetupStored(old);

        var result = await _service.RefreshAsync(new RefreshTokenRequest { RefreshToken = OldRefreshToken }, Ct);

        Assert.Equal(ErrorType.Unauthorized, result.Error);
        Assert.Null(old.RevokedAt);
        _refreshTokens.Verify(r => r.Add(It.IsAny<RefreshToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Refresh_WhenMemberInactive_ReturnsUnauthorized()
    {
        var old = NewStoredToken(NewMember(isActive: false));
        SetupStored(old);

        var result = await _service.RefreshAsync(new RefreshTokenRequest { RefreshToken = OldRefreshToken }, Ct);

        Assert.Equal(ErrorType.Unauthorized, result.Error);
        _tokenService.Verify(t => t.CreateToken(It.IsAny<Member>(), out It.Ref<DateTime>.IsAny), Times.Never);
        _refreshTokens.Verify(r => r.Add(It.IsAny<RefreshToken>()), Times.Never);
    }

    [Fact]
    public async Task Logout_RevokesRefreshToken()
    {
        var old = NewStoredToken(NewMember(isActive: true));
        SetupStored(old);

        var result = await _service.LogoutAsync(new RefreshTokenRequest { RefreshToken = OldRefreshToken }, Ct);

        Assert.True(result.IsSuccess);
        Assert.Equal(_time.GetUtcNow().UtcDateTime, old.RevokedAt);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private void SetupStored(RefreshToken token) =>
        _refreshTokens.Setup(r => r.GetByHashAsync("hash:" + OldRefreshToken, It.IsAny<CancellationToken>())).ReturnsAsync(token);

    private RefreshToken NewStoredToken(Member member) => new()
    {
        MemberId = member.Id,
        Member = member,
        TokenHash = "hash:" + OldRefreshToken,
        CreatedAt = _time.GetUtcNow().UtcDateTime.AddDays(-1),
        ExpiresAt = _time.GetUtcNow().UtcDateTime.AddDays(6)
    };

    private static RegisterRequest NewRegisterRequest(string email) => new()
    {
        FullName = "חבר חדש",
        Email = email,
        Phone = "050-1234567",
        Password = Password
    };

    private static Member NewMember(bool isActive) => new()
    {
        Id = 4,
        FullName = "חבר ראשון",
        Email = "member1@medloan.local",
        PasswordHash = StoredHash,
        Role = UserRole.Member,
        IsActive = isActive
    };
}
