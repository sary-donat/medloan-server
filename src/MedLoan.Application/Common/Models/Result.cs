namespace MedLoan.Application.Common.Models;

public sealed record Result(bool IsSuccess, ErrorType Error, string? Message)
{
    public static Result Success()                  => new(true, ErrorType.None, null);
    public static Result NotFound(string msg)       => new(false, ErrorType.NotFound, msg);
    public static Result Validation(string msg)     => new(false, ErrorType.Validation, msg);
    public static Result Conflict(string msg)       => new(false, ErrorType.Conflict, msg);
    public static Result Forbidden(string msg)      => new(false, ErrorType.Forbidden, msg);
    public static Result Unauthorized(string msg)   => new(false, ErrorType.Unauthorized, msg);
}
