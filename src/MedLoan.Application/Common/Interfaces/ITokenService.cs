using MedLoan.Domain.Entities;

namespace MedLoan.Application.Common.Interfaces;

public interface ITokenService
{
    string CreateToken(Member member, out DateTime expiresAt);
    string CreateRefreshToken(out DateTime expiresAt);
    string HashRefreshToken(string token);
}
