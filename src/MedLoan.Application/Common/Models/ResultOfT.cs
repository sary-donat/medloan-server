namespace MedLoan.Application.Common.Models;

public sealed record Result<T>(bool IsSuccess, T? Value, ErrorType Error, string? Message)
{
    public static Result<T> Success(T value)        => new(true, value, ErrorType.None, null);
    public static Result<T> NotFound(string msg)    => new(false, default, ErrorType.NotFound, msg);
    public static Result<T> Validation(string msg)  => new(false, default, ErrorType.Validation, msg);
    public static Result<T> Conflict(string msg)    => new(false, default, ErrorType.Conflict, msg);
    public static Result<T> Forbidden(string msg)   => new(false, default, ErrorType.Forbidden, msg);
    public static Result<T> Unauthorized(string msg)=> new(false, default, ErrorType.Unauthorized, msg);
}
