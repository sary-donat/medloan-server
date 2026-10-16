namespace MedLoan.Application.Common.Exceptions;

public class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException()
    {
    }

    public ConcurrencyConflictException(string message, Exception inner) : base(message, inner)
    {
    }
}
