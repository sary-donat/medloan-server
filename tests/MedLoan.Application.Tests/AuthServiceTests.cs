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

    private readonly Mock<IMemberRepository> _members = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<ITokenService> _tokenService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 3, 1, 10, 0, 0, TimeSpan.Zero));
    private readonly AuthService _service;

    public AuthServiceTests()
    {
        var expiresAt = new DateTime(2026, 3, 1, 11, 0, 0, DateTimeKind.Utc);
        _tokenService.Setup(t => t.CreateToken(It.IsAny<Member>(), out expiresAt)).Returns("test-token");

        _service = new AuthService(
            _members.Object, _hasher.Object, _tokenService.Object, _unitOfWork.Object,
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
