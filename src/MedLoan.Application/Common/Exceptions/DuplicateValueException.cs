namespace MedLoan.Application.Common.Exceptions;

public class DuplicateValueException : Exception
{
    public DuplicateValueException()
    {

    }

    public DuplicateValueException(string message, Exception inner) : base(message, inner)
    {

    }
}
