namespace MedLoan.Application.Common.Interfaces;

public interface ICurrentUser
{
    int? Id { get; }
    bool IsAdmin { get; }
    bool IsAuthenticated { get; }
}
